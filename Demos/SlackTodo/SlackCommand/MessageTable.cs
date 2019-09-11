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

namespace Demo.SlackTodo {

    public class TaskRecord {

        //--- Properties ---
        public string TaskId { get; set; }
        public string UserId { get; set; }
        public string Text { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    public class TaskTable {

        //--- Constants ---
        private const string TASK_ID = "TaskId";
        private const string TASK_USER_ID = "UserId";
        private const string TASK_TEXT = "Text";
        private const string TASK_TIMESTAMP = "Timestamp";

        //--- Fields ---
        private readonly string _tableName;
        private readonly IAmazonDynamoDB _dynamoClient;

        //--- Constructors ---
        public TaskTable(string tableName, IAmazonDynamoDB dynamoClient = null) {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _dynamoClient = dynamoClient ?? new AmazonDynamoDBClient();
        }

        //--- Methods ---
        public Task InsertTaskAsync(TaskRecord task) {
            var values = new Dictionary<string, AttributeValue> {
                [TASK_ID] = new AttributeValue { S = Guid.NewGuid().ToString() },
                [TASK_USER_ID] = new AttributeValue { S = task.UserId },
                [TASK_TEXT] = new AttributeValue { S = task.Text },
                [TASK_TIMESTAMP] = new AttributeValue { S = task.Timestamp.ToUnixTimeSeconds().ToString() },
            };
            return _dynamoClient.PutItemAsync(_tableName, values);
        }

        public async Task<IEnumerable<TaskRecord>> ListTasksAsync(string userId) {
            var request = new ScanRequest {
                TableName = _tableName
            };
            var tasks = new List<TaskRecord>();
            do {
                var response = await _dynamoClient.ScanAsync(request);
                tasks.AddRange(response.Items.Select(item => new TaskRecord {
                    TaskId = item[TASK_ID].S,
                    UserId = item[TASK_USER_ID].S,
                    Text = item[TASK_TEXT].S,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item[TASK_TIMESTAMP].S))
                }).Where(task => task.UserId == userId));
                request.ExclusiveStartKey = response.LastEvaluatedKey;
            } while(request.ExclusiveStartKey.Any());
            return tasks.OrderBy(task => task.Timestamp).ToList();
        }

        public Task DeleteTask(string messgageId) {
            return _dynamoClient.DeleteItemAsync(_tableName, new Dictionary<string, AttributeValue> {
                [TASK_ID] = new AttributeValue { S = messgageId }
            });
        }
    }
}
