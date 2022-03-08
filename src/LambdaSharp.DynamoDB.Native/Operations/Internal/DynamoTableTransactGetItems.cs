/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Internal;

namespace LambdaSharp.DynamoDB.Native.Operations.Internal {

    internal sealed class DynamoTableTransactGetItems : IDynamoTableTransactGetItems {

        //--- Constants ---
        private const int MILLISECOND_BACKOFF = 100;

        //--- Types ---
        internal sealed class DynamoTableTransactGetItemsEntry<TRecord> : IDynamoTableTransactGetItemsBegin<TRecord>
            where TRecord : class
        {

            //--- Fields ---
            private readonly DynamoTableTransactGetItems _parent;
            private readonly DynamoRequestConverter _converter;

            //--- Constructors ---
            public DynamoTableTransactGetItemsEntry(DynamoTableTransactGetItems parent, DynamoRequestConverter converter) {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
                _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            }

            //--- Methods ---
            public IDynamoTableTransactGetItemsBegin<TRecord> Get<T>(System.Linq.Expressions.Expression<Func<TRecord, T>> attribute) {
                _converter.AddProjection(attribute.Body);

                // NOTE (2021-07-02, bjorg): we always fetch `_t` to allow polymorphic deserialization
                _converter.AddProjection("_t");
                return this;
            }

            public IDynamoTableTransactGetItems End() => _parent;
        }

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly TransactGetItemsRequest _request;
        private readonly List<DynamoRequestConverter> _converters;

        //--- Constructors ---
        public DynamoTableTransactGetItems(DynamoTable table, TransactGetItemsRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converters = new List<DynamoRequestConverter>();
        }

        //--- Methods ---
        public IDynamoTableTransactGetItemsBegin<TRecord> BeginGetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false) where TRecord : class {

            // transaction GetItem
            var transactGetItem = new TransactGetItem {
                Get = new Get {
                    TableName = _table.TableName,
                    Key = new Dictionary<string, AttributeValue> {
                        [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                        [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                    }
                }
            };
            _request.TransactItems.Add(transactGetItem);

            // add expressions converter
            var converter = new DynamoRequestConverter(transactGetItem.Get.ExpressionAttributeNames, _table.SerializerOptions);
            _converters.Add(converter);

            // register expected type
            converter.AddExpectedType(typeof(TRecord));
            return new DynamoTableTransactGetItemsEntry<TRecord>(this, converter);
        }

        public async Task<(bool Success, IEnumerable<object> Items)> TryExecuteAsync(int maxAttempts, CancellationToken cancellationToken = default) {
            if(!_request.TransactItems.Any()) {
                throw new InvalidOperationException("BatchGetItems cannot be empty");
            }
            if(_request.TransactItems.Count > 25) {
                throw new InvalidOperationException("BatchGetItems too many operations");
            }
            for(var i = 0; i < _request.TransactItems.Count; ++i) {
                _request.TransactItems[i].Get.ProjectionExpression = _converters[i].ConvertProjections();
            }

            // perform transaction
            try {
                var result = new List<object>();
                var response = await _table.DynamoClient.TransactGetItemsAsync(_request, cancellationToken);

                // merge expected types from all converters
                var mergedExpectedTypes = new Dictionary<string, Type>();
                foreach(var (expectedTypeName, expectedType) in _converters.SelectMany(converter => converter.ExpectedTypes)) {

                    // ignore collisions as they are more likely than conflicts
                    mergedExpectedTypes[expectedTypeName] = expectedType;
                }
                foreach(var itemResponse in response.Responses) {
                    var record = _table.DeserializeItemUsingRecordType(itemResponse.Item, typeof(object), mergedExpectedTypes);
                    if(!(record is null)) {
                        result.Add(record);
                    }
                }
                return (Success: true, Items: result);
            } catch(TransactionCanceledException) {

                // transaction failed
                return (Success: false, Items: Enumerable.Empty<object>());
            }
        }
    }
}