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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Native.Exceptions;
using LambdaSharp.DynamoDB.Native.Internal;

namespace LambdaSharp.DynamoDB.Native.Operations.Internal {

    internal sealed class DynamoTableBatchGetItems<TRecord> : IDynamoTableBatchGetItems<TRecord>
        where TRecord : class
    {

        //--- Constants ---
        private const int MILLISECOND_BACKOFF = 100;

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly BatchGetItemRequest _request;
        private readonly DynamoRequestConverter _converter;

        //--- Constructors ---
        public DynamoTableBatchGetItems(DynamoTable table, BatchGetItemRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _converter = new DynamoRequestConverter(request.RequestItems.Single().Value.ExpressionAttributeNames, _table.SerializerOptions);
        }

        //--- Methods ---
        public IDynamoTableBatchGetItems<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute) {
            _converter.AddProjection(attribute.Body);
            return this;
        }

        public async Task<IEnumerable<TRecord>> ExecuteAsync(int maxAttempts, CancellationToken cancellationToken = default) {
            var requestTableAndKeys = _request.RequestItems.First();
            requestTableAndKeys.Value.ProjectionExpression = _converter.ConvertProjections();

            // NOTE (2021-06-30, bjorg): batch operations may run out of capacity/bandwidth and may have to be completed in batches themselves
            var result = new List<TRecord>();
            var attempts = 1;
            do {
                try {
                    var response = await _table.DynamoClient.BatchGetItemAsync(_request, cancellationToken);
                    if(response.Responses.Any()) {
                        foreach(var item in response.Responses.Single().Value) {
                            var record = _table.DeserializeItem<TRecord>(item);
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
                    throw new DynamoTableBatchGetItemsMaxAttemptsExceededException(result);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(MILLISECOND_BACKOFF << (attempts++ - 1)));
            } while(true);
            return result;
        }
    }
}