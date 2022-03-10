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

    internal sealed class DynamoTableTransactGetItems<TRecord> : IDynamoTableTransactGetItems<TRecord>
        where TRecord : class

    {

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly TransactGetItemsRequest _request;
        private readonly List<DynamoRequestConverter> _converters;

        //--- Constructors ---
        public DynamoTableTransactGetItems(DynamoTable table, TransactGetItemsRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converters = _request.TransactItems
                .Select(transactItem => new DynamoRequestConverter(transactItem.Get.ExpressionAttributeNames, _table.SerializerOptions))
                .ToList();
        }

        //--- Methods ---
        public IDynamoTableTransactGetItems<TRecord> Get<T>(System.Linq.Expressions.Expression<Func<TRecord, T>> attribute) {
            foreach(var converter in _converters) {
                converter.AddProjection(attribute.Body);
            }
            return this;
        }

        public async Task<(bool Success, IEnumerable<TRecord> Items)> TryExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default) {
             if(!_request.TransactItems.Any()) {
                throw new ArgumentException("primary keys cannot be empty");
            }
            if(_request.TransactItems.Count > 25) {
                throw new ArgumentException("too many primary keys");
            }
            for(var i = 0; i < _request.TransactItems.Count; ++i) {
                _request.TransactItems[i].Get.ProjectionExpression = _converters[i].ConvertProjections();
            }

            // perform transaction
            try {
                var result = new List<TRecord>();
                var response = await _table.DynamoClient.TransactGetItemsAsync(_request, cancellationToken);
                foreach(var itemResponse in response.Responses) {
                    var record = _table.DeserializeItem<TRecord>(itemResponse.Item);
                    if(!(record is null)) {
                        result.Add(record);
                    }
                }
                return (Success: true, Items: result);
            } catch(TransactionCanceledException) {

                // transaction failed
                return (Success: false, Items: Enumerable.Empty<TRecord>());
            }
       }
    }
}