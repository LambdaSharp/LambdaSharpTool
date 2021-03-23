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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace LambdaSharp.App.EventBus {

    public abstract class ADataTable {

        //--- Class Fields ---
        private readonly static PutItemOperationConfig CreateItemConfig = new PutItemOperationConfig {
            ConditionalExpression = new Expression {
                ExpressionStatement = "attribute_not_exists(#PK)",
                ExpressionAttributeNames = {
                    ["#PK"] = "PK"
                }
            }
        };

        private readonly static PutItemOperationConfig UpdateItemConfig = new PutItemOperationConfig {
            ConditionalExpression = new Expression {
                ExpressionStatement = "attribute_exists(#PK)",
                ExpressionAttributeNames = {
                    ["#PK"] = "PK"
                }
            }
        };

        //--- Class Methods ---
        protected async static Task<IEnumerable<Document>> DoSearchAsync(Search search, CancellationToken cancellationToken = default) {
            var results = new List<Document>();
            do {
                var documents = await search.GetNextSetAsync(cancellationToken);
                results.AddRange(documents);
            } while(!search.IsDone);
            return results;
        }

        protected static T Deserialize<T>(Document record)
            => (record != null)
                ? JsonSerializer.Deserialize<T>(record.ToJson())
                : default;


        //--- Fields ---
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly Table _table;

        //--- Constructors ---
        public ADataTable(string tableName, IAmazonDynamoDB dynamoDbClient) {
            TableName = tableName ?? throw new System.ArgumentNullException(nameof(tableName));
            _dynamoDbClient = dynamoDbClient ?? new AmazonDynamoDBClient();
            _table = Table.LoadTable(dynamoDbClient, tableName);
        }

        //--- Properties ---
        public string TableName { get; }

        //--- Methods ---
        protected Task<Document> GetItemAsync(string pk, string sk, CancellationToken cancellationToken)
            => _table.GetItemAsync(pk, sk, cancellationToken);

        protected Task<IEnumerable<Document>> SearchBeginsWith(string pk, string skPrefix, CancellationToken cancellationToken) {
            var filter = new QueryFilter();
            filter.AddCondition("PK", QueryOperator.Equal, new DynamoDBEntry[] { pk });
            filter.AddCondition("SK", QueryOperator.BeginsWith, new DynamoDBEntry[] { skPrefix });
            var search = _table.Query(new QueryOperationConfig {
                Filter = filter
            });
            return DoSearchAsync(search, cancellationToken);
        }

        protected Task CreateOrUpdateItemsAsync<T>(T item, string pk, string sk, CancellationToken cancellationToken) {
            var document = Document.FromJson(JsonSerializer.Serialize(item));
            document["_Type"] = item.GetType().Name;
            document["_Modified"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            document["PK"] = pk ?? throw new ArgumentNullException(nameof(pk));
            document["SK"] = sk ?? throw new ArgumentNullException(nameof(sk));
            return _table.PutItemAsync(document, cancellationToken);
        }

        protected Task CreateItemsAsync<T>(T item, string pk, string sk, CancellationToken cancellationToken) {
            var document = Document.FromJson(JsonSerializer.Serialize(item));
            document["_Type"] = item.GetType().Name;
            document["_Modified"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            document["PK"] = pk ?? throw new ArgumentNullException(nameof(pk));
            document["SK"] = sk ?? throw new ArgumentNullException(nameof(sk));
            return _table.PutItemAsync(document, CreateItemConfig, cancellationToken);
        }

        protected Task UpdateItemsAsync<T>(T item, string pk, string sk, CancellationToken cancellationToken) {
            var document = Document.FromJson(JsonSerializer.Serialize(item));
            document["_Type"] = item.GetType().Name;
            document["_Modified"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            document["PK"] = pk ?? throw new ArgumentNullException(nameof(pk));
            document["SK"] = sk ?? throw new ArgumentNullException(nameof(sk));
            return _table.PutItemAsync(document, UpdateItemConfig, cancellationToken);
        }

        protected Task DeleteItemAsync(string pk, string sk, CancellationToken cancellationToken)
            => _table.DeleteItemAsync(pk, sk, cancellationToken);

        protected Task DeleteItemsAsync(IEnumerable<(string PK, string SK)> keys, CancellationToken cancellationToken)
            => Task.WhenAll(keys.Select(key => _table.DeleteItemAsync(key.PK, key.SK, cancellationToken)));
    }
}
