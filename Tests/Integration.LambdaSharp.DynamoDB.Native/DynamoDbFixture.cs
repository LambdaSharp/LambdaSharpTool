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
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Xunit;

namespace Integration.LambdaSharp.DynamoDB.Native {

    public class DynamoDbFixture : IDisposable {

        //--- Fields ---
        private bool _tableCreated;

        //--- Constructors ---
        public DynamoDbFixture() {
            DynamoClient = new AmazonDynamoDBClient();

            // check environment variable if a table is pre-defined
            TableName = Environment.GetEnvironmentVariable("LAMBDASHARP_DYNAMODB_NATIVE_TEST_TABLE");
            if(string.IsNullOrEmpty(TableName)) {

                // create table
                TableName = "LambdaSharpDynamoDBNative-Tests";
                CreateTableAsync().GetAwaiter().GetResult();
            } else {

                // clear out all records
                DeleteAllItemsAsync().GetAwaiter().GetResult();
            }
        }

        //--- Properties ---
        public IAmazonDynamoDB DynamoClient { get; }
        public string TableName { get; }

        //--- Methods ---
        public void Dispose() {
            if(_tableCreated) {
                DeleteTableAsync().GetAwaiter().GetResult();
            }
        }

        private async Task CreateTableAsync() {

            // TODO (2021-07-14, bjorg): support dynamic table create to run integration tests (is this really a good idea?)
            // * create table
            // * add global index GSI1: (GSI1PK, GSI1SK)
            // * only set bool when table was successfully created
            _tableCreated = true;
            throw new NotImplementedException();
        }

        private async Task DeleteTableAsync() {
            throw new NotImplementedException();
        }

        private async Task DeleteAllItemsAsync() {

            // scan for all records
            var scanRequest = new ScanRequest {
                TableName = TableName
            };
            do {
                var scanResponse = await DynamoClient.ScanAsync(scanRequest);

                // delete each returned item
                foreach(var item in scanResponse.Items) {
                    await DynamoClient.DeleteItemAsync(new DeleteItemRequest {
                        TableName = TableName,
                        Key = {
                            ["PK"] = item["PK"],
                            ["SK"] = item["SK"]
                        }
                    });
                }
                scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
            } while(scanRequest.ExclusiveStartKey.Any());
        }
    }

    [CollectionDefinition("DynamoDB")]
    public class DatabaseCollection : ICollectionFixture<DynamoDbFixture> { }
}
