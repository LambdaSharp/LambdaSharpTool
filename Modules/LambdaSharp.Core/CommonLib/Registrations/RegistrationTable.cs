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
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace LambdaSharp.Core.Registrations {

    public class RegistrationTable {

        //--- Fields ---
        private readonly Table _table;

        //--- Constructors ---
        public RegistrationTable(IAmazonDynamoDB dynamoClient, string tableName) {
            _table = Table.LoadTable(dynamoClient, tableName);
        }

        public async Task<OwnerMetaData> GetOwnerMetaDataAsync(string id) {
            var document = await _table.GetItemAsync(id);
            if(document == null) {
                return null;
            }
            return new OwnerMetaData {
                Module = TryGetAsString("Module"),
                ModuleId = TryGetAsString("ModuleId"),
                FunctionId = TryGetAsString("FunctionId"),
                FunctionName = TryGetAsString("FunctionName"),
                FunctionLogGroupName = TryGetAsString("FunctionLogGroupName"),
                FunctionPlatform = TryGetAsString("FunctionPlatform"),
                FunctionFramework = TryGetAsString("FunctionFramework"),
                FunctionLanguage = TryGetAsString("FunctionLanguage"),
                FunctionMaxMemory = TryGetAsInt("FunctionMaxMemory"),
                FunctionMaxDuration = TryGetAsTimeSpan("FunctionMaxDuration"),
                RollbarProjectId = TryGetAsInt("RollbarProjectId"),
                RollbarAccessToken = TryGetAsString("RollbarAccessToken")
            };

            // local functions
            string TryGetAsString(string key)
                => document.TryGetValue(key, out var entry) ? entry.AsString() : null;

            int TryGetAsInt(string key)
                => document.TryGetValue(key, out var entry) ? entry.AsInt() : 0;

            TimeSpan TryGetAsTimeSpan(string key)
                => document.TryGetValue(key, out var entry) ? TimeSpan.Parse(entry.AsString()) : TimeSpan.Zero;
        }

        public async Task PutOwnerMetaDataAsync(string id, OwnerMetaData owner) {
            var document = new Document {
                ["Id"] = id,
                ["Module"] = owner.Module,
                ["ModuleId"] = owner.ModuleId
            };
            if(owner.FunctionId != null) {
                document["FunctionId"] = owner.FunctionId;
                document["FunctionName"] = owner.FunctionName;
                document["FunctionLogGroupName"] = owner.FunctionLogGroupName;
                document["FunctionPlatform"] = owner.FunctionPlatform;
                document["FunctionFramework"] = owner.FunctionFramework;
                document["FunctionLanguage"] = owner.FunctionLanguage;
                document["FunctionMaxMemory"] = owner.FunctionMaxMemory;
                document["FunctionMaxDuration"] = owner.FunctionMaxDuration.ToString();
            }
            if(owner.RollbarAccessToken != null) {
                document["RollbarProjectId"] = owner.RollbarProjectId;
                document["RollbarAccessToken"] = owner.RollbarAccessToken;

            }
            await _table.PutItemAsync(document);
        }

        public async Task DeleteOwnerMetaDataAsync(string id) {
            await _table.DeleteItemAsync(id);
        }
    }
}
