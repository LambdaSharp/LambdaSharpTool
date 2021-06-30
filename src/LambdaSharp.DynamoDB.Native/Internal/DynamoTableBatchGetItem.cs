/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using LambdaSharp.DynamoDB.Native.Operations;
using LambdaSharp.DynamoDB.Native.Exceptions;

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal sealed class DynamoTableBatchGetItem : IDynamoTableBatchGetItem {

        //--- Constants ---
        private const int MILLISECOND_BACKOFF = 100;

        //--- Types ---
        internal sealed class DynamoTableBatchGetItemEntry<TRecord> : IDynamoTableBatchGetItemEntry<TRecord>
            where TRecord : class
        {

            //--- Fields ---
            private readonly DynamoTableBatchGetItem _parent;

            //--- Constructors ---
            public DynamoTableBatchGetItemEntry(DynamoTableBatchGetItem parent)
                => _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            //--- Methods ---
            public IDynamoTableBatchGetItemEntry<TRecord> Get<T>(System.Linq.Expressions.Expression<Func<TRecord, T>> attribute) {
                _parent._converter.AddProjection(attribute.Body);
                return this;
            }

            public IDynamoTableBatchGetItem Execute(CancellationToken cancellationToken = default)
                => _parent;
        }

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly BatchGetItemRequest _request;
        private readonly DynamoRequestConverter _converter;
        private readonly Dictionary<string, Type> _expectedTypes = new Dictionary<string, Type>();

        //--- Constructors ---
        public DynamoTableBatchGetItem(DynamoTable table, BatchGetItemRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converter = new DynamoRequestConverter(_request.RequestItems.Single().Value.ExpressionAttributeNames, _table.SerializerOptions);
        }

        //--- Methods ---
        public IDynamoTableBatchGetItemEntry<TRecord> AddGetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false) where TRecord : class {
            _request.RequestItems.First().Value.Keys.Add(new Dictionary<string, AttributeValue> {
                [primaryKey.PartitionKeyName] = new AttributeValue(primaryKey.PartitionKeyValue),
                [primaryKey.SortKeyName] = new AttributeValue(primaryKey.SortKeyValue)
            });

            // NOTE (2021-06-24, bjorg): we always fetch `_t` to allow polymorphic deserialization
            _converter.AddProjection("_t");
            _expectedTypes[typeof(TRecord).FullName ?? throw new ArgumentException("type name is <null>")] = typeof(TRecord);
            return new DynamoTableBatchGetItemEntry<TRecord>(this);
        }

        public async Task<IEnumerable<object>> ExecuteAsync(int maxAttempts, CancellationToken cancellationToken = default) {
            var requestTableAndKeys = _request.RequestItems.First();
            if(!requestTableAndKeys.Value.Keys.Any()) {
                throw new ArgumentException("primary keys cannot be empty");
            }
            if(requestTableAndKeys.Value.Keys.Count() > 100) {
                throw new ArgumentException("too many primary keys");
            }
            requestTableAndKeys.Value.ProjectionExpression = _converter.ConvertProjections();

            // NOTE (2021-06-30, bjorg): batch operations may run out of capacity/bandwidth and may have to be completed in batches themselves
            var result = new List<object>();
            var attempts = 1;
            do {
                try {
                    var response = await _table.DynamoClient.BatchGetItemAsync(_request, cancellationToken);
                    if(response.Responses.Any()) {
                        foreach(var item in response.Responses.Single().Value) {
                            object? record;
                            if(
                                item.TryGetValue("_t", out var itemTypeName)
                                && !(itemTypeName.S is null)
                                && _expectedTypes.TryGetValue(itemTypeName.S, out var itemType)
                            ) {
                                record = _table.DeserializeItem(item, itemType ?? typeof(object));
                            } else {
                                record = _table.DeserializeItem(item, typeof(object));
                            }
                            if(!(record is null)) {
                                result.Add(record);
                            }
                        }
                    }

                    // check if all requested primary keys were processed
                    if(!response.UnprocessedKeys.Any()) {
                        break;
                    }

                    // repeat request with unprocessed primary keys
                    requestTableAndKeys.Value.Keys = response.UnprocessedKeys.First().Value.Keys;
                } catch(ProvisionedThroughputExceededException) {

                    // NOTE (2021-06-30, bjorg): not a single item could be returned due to insufficient read capacity
                }

                // use exponential backoff before attempting next operation
                if(attempts >= maxAttempts) {
                    throw new DynamoTableMaxAttemptsExceededException(result);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(MILLISECOND_BACKOFF << (attempts++ - 1)));
            } while(true);
            return result;
        }
    }
}