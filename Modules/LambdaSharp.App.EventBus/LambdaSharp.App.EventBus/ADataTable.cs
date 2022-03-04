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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace LambdaSharp.App.EventBus {

    public abstract class ADataTable {

        //--- Class Fields ---
        private readonly static Expression ItemDoesNotExistCondition = new Expression {
            ExpressionStatement = "attribute_not_exists(#PK)",
            ExpressionAttributeNames = {
                ["#PK"] = "PK"
            }
        };

        private readonly static Expression ItemExistsCondition = new Expression {
            ExpressionStatement = "attribute_exists(#PK)",
            ExpressionAttributeNames = {
                ["#PK"] = "PK"
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


        //--- Fields ---

        protected JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
            IgnoreNullValues = true,
            WriteIndented = false,
            Converters = {
                new JsonStringEnumConverter()
            }
        };

        //--- Constructors ---
        public ADataTable(string tableName, IAmazonDynamoDB? dynamoDbClient) {
            TableName = tableName ?? throw new System.ArgumentNullException(nameof(tableName));
            DynamoDbClient = dynamoDbClient ?? new AmazonDynamoDBClient();
            Table = Table.LoadTable(dynamoDbClient, tableName);
        }

        //--- Properties ---
        protected  IAmazonDynamoDB DynamoDbClient { get; }
        protected Table Table { get; }
        protected string TableName { get; }

        //--- Methods ---
        protected Task<Document> GetItemAsync(string pk, string sk, CancellationToken cancellationToken)
            => Table.GetItemAsync(pk, sk, cancellationToken);

        protected Task<IEnumerable<Document>> Search(string pk, QueryOperator skOperator, string skValue, CancellationToken cancellationToken) {
            var filter = new QueryFilter();
            filter.AddCondition("PK", QueryOperator.Equal, new DynamoDBEntry[] { pk });
            filter.AddCondition("SK", skOperator, new DynamoDBEntry[] { skValue });
            var search = Table.Query(new QueryOperationConfig {
                Filter = filter
            });
            return DoSearchAsync(search, cancellationToken);
        }

        protected Task CreateOrUpdateItemAsync<T>(T item, string pk, string sk, CancellationToken cancellationToken) where T : notnull
            => CreateOrUpdateItemAsync(item, pk, sk, condition: null, cancellationToken);

        protected Task CreateItemAsync<T>(T item, string pk, string sk, CancellationToken cancellationToken) where T : notnull
            => CreateOrUpdateItemAsync(item, pk, sk, ItemDoesNotExistCondition, cancellationToken);

        protected Task UpdateItemAsync<T>(T item, string pk, string sk, CancellationToken cancellationToken) where T : notnull
            => CreateOrUpdateItemAsync(item, pk, sk, ItemExistsCondition, cancellationToken);

        protected Task CreateOrUpdateItemAsync<T>(T item, string pk, string sk, Expression? condition, CancellationToken cancellationToken) where T : notnull {
            var document = Serialize(item);
            document["_Type"] = item.GetType().Name;
            document["_Modified"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            document["PK"] = pk ?? throw new ArgumentNullException(nameof(pk));
            document["SK"] = sk ?? throw new ArgumentNullException(nameof(sk));

            // add operation condition when provided
            PutItemOperationConfig? operationConfig = null;
            if(condition != null) {
                operationConfig = new PutItemOperationConfig {
                    ConditionalExpression = condition
                };
            }
            return Table.PutItemAsync(document, operationConfig, cancellationToken);
        }

        protected Task DeleteItemAsync(string pk, string sk, CancellationToken cancellationToken)
            => Table.DeleteItemAsync(pk, sk, cancellationToken);

        protected Task DeleteItemsAsync(IEnumerable<(string PK, string SK)> keys, CancellationToken cancellationToken)
            => Task.WhenAll(keys.Select(key => Table.DeleteItemAsync(key.PK, key.SK, cancellationToken)));

        protected Document Serialize<T>(T item)
            => Document.FromJson(JsonSerializer.Serialize(item, JsonSerializerOptions));

        [return: NotNullIfNotNull("record")]
        protected T? Deserialize<T>(Document? record) where T : class
            => (record != null)
                ? JsonSerializer.Deserialize<T>(record.ToJson(), JsonSerializerOptions)
                : default;
    }
}
