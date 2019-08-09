/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.Demo.WebSocketsChat.Common {

    public class ConnectionsTable {

        //--- Fields ---
        private readonly string _tableName;
        private readonly IAmazonDynamoDB _dynamoDbClient;

        //--- Constructors ---
        public ConnectionsTable(string tableName, IAmazonDynamoDB dynamoDBClient) {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _dynamoDbClient = dynamoDBClient ?? throw new ArgumentNullException(nameof(dynamoDBClient));
        }

        //--- Methods ---
        public async Task InsertRowAsync(string connectionId)
            => await _dynamoDbClient.PutItemAsync(new PutItemRequest {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue> {
                    ["ConnectionId"] = new AttributeValue {
                        S = connectionId
                    }
                }
            });

        public async Task<IEnumerable<string>> GetAllRowsAsync()
            => (await _dynamoDbClient.ScanAsync(new ScanRequest {
                    TableName = _tableName,
                    ProjectionExpression = "ConnectionId"
            }))
                .Items
                .Select(item => item["ConnectionId"].S)
                .ToList();

        public Task DeleteRowAsync(string connectionId)
            => _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue> {
                    ["ConnectionId"] = new AttributeValue {
                        S = connectionId
                    }
                }
            });
    }
}
