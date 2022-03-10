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
using LambdaSharp.DynamoDB.Native.Exceptions;

namespace LambdaSharp.DynamoDB.Native.Operations.Internal {

    internal sealed class DynamoTableBatchWriteItems : IDynamoTableBatchWriteItems {

        //--- Constants ---
        private const int MILLISECOND_BACKOFF = 100;

        //--- Types ---
        private class DynamoTableBatchWriteItemsPutItem<TRecord> : IDynamoTableBatchWriteItemsPutItem<TRecord>
            where TRecord : class
        {

            //--- Fields ---
            private readonly DynamoTableBatchWriteItems _parent;
            private readonly PutRequest _putRequest;

            //--- Constructors ---
            public DynamoTableBatchWriteItemsPutItem(DynamoTableBatchWriteItems parent, PutRequest putRequest) {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
                _putRequest = putRequest ?? throw new ArgumentNullException(nameof(putRequest));
            }

            //--- Methods ---
            public IDynamoTableBatchWriteItemsPutItem<TRecord> Set(string key, AttributeValue value) {
                _putRequest.Item[key] = value;
                return this;
            }

            public IDynamoTableBatchWriteItems End( ) => _parent;
        }

        //--- Fields ---
        private readonly DynamoTable _table;
        private readonly BatchWriteItemRequest _request;

        //--- Constructors ---
        public DynamoTableBatchWriteItems(DynamoTable table, BatchWriteItemRequest request) {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        //--- Methods ---
        public IDynamoTableBatchWriteItems PutItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey) where TRecord : class {
            _request.RequestItems.First().Value.Add(new WriteRequest {
                PutRequest = new PutRequest {
                    Item = _table.SerializeItem(record, primaryKey)
                }
            });
            return this;
        }

        public IDynamoTableBatchWriteItemsPutItem<TRecord> BeginPutItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, TRecord record) where TRecord : class {
            var putRequest = new PutRequest {
                Item = _table.SerializeItem(record, primaryKey)
            };
            _request.RequestItems.First().Value.Add(new WriteRequest {
                PutRequest = putRequest
            });
            return new DynamoTableBatchWriteItemsPutItem<TRecord>(this, putRequest);
        }

        public IDynamoTableBatchWriteItems DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey) where TRecord : class {
            _request.RequestItems.First().Value.Add(new WriteRequest {
                DeleteRequest = new DeleteRequest {
                    Key = new Dictionary<string, AttributeValue> {
                        [primaryKey.PKName] = new AttributeValue(primaryKey.PKValue),
                        [primaryKey.SKName] = new AttributeValue(primaryKey.SKValue)
                    }
                }
            });
            return this;
        }

        public async Task ExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default) {
            var requestTableItems = _request.RequestItems.First();
            if(!requestTableItems.Value.Any()) {
                throw new ArgumentException("primary keys cannot be empty");
            }
            if(requestTableItems.Value.Count() > 25) {
                throw new ArgumentException("too many primary keys");
            }

            // NOTE (2021-06-30, bjorg): batch operations may run out of capacity/bandwidth and may have to be completed in batches themselves
            var attempts = 1;
            do {
                try {
                    var response = await _table.DynamoClient.BatchWriteItemAsync(_request, cancellationToken);

                    // check if all requested primary keys were processed
                    if(!response.UnprocessedItems.Any()) {
                        break;
                    }

                    // repeat request with unprocessed primary keys
                    requestTableItems.Value.Clear();
                    requestTableItems.Value.AddRange(response.UnprocessedItems.Single().Value);
                } catch(ProvisionedThroughputExceededException) {

                    // NOTE (2021-06-30, bjorg): not a single item could be returned due to insufficient read capacity
                }

                // use exponential backoff before attempting next operation
                if(attempts >= maxAttempts) {
                    throw new DynamoTableBatchWriteItemsMaxAttemptsExceededException(requestTableItems.Value);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(MILLISECOND_BACKOFF << (attempts++ - 1)));
            } while(true);
        }
    }
}