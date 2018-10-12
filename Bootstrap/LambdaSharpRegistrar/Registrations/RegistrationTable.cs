﻿/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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

namespace MindTouch.LambdaSharpRegistrar {

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
                Tier = TryGetAsString("Tier"),
                ModuleId = TryGetAsString("ModuleId"),
                ModuleName = TryGetAsString("ModuleName"),
                ModuleVersion = TryGetAsString("ModuleVersion"),
                FunctionId = TryGetAsString("FunctionId"),
                FunctionName = TryGetAsString("FunctionName"),
                FunctionLogGroupName = TryGetAsString("FunctionLogGroupName"),
                FunctionPlatform = TryGetAsString("FunctionPlatform"),
                FunctionFramework = TryGetAsString("FunctionFramework"),
                FunctionLanguage = TryGetAsString("FunctionLanguage"),
                FunctionGitSha = TryGetAsString("FunctionGitSha"),
                FunctionGitBranch = TryGetAsString("FunctionGitBranch"),
                FunctionMaxMemory = TryGetAsInt("FunctionMaxMemory"),
                FunctionMaxDuration = TryGetAsTimeSpan("FunctionMaxDuration")
            };

            // local functions
            string TryGetAsString(string key)
                => document.TryGetValue(key, out DynamoDBEntry entry) ? entry.AsString() : null;

            int TryGetAsInt(string key)
                => document.TryGetValue(key, out DynamoDBEntry entry) ? entry.AsInt() : 0;

            TimeSpan TryGetAsTimeSpan(string key)
                => document.TryGetValue(key, out DynamoDBEntry entry) ? TimeSpan.Parse(entry.AsString()) : TimeSpan.Zero;
        }

        public async Task PutOwnerMetaDataAsync(string id, OwnerMetaData owner) {
            var document = new Document {
                ["Id"] = id,
                ["Tier"] = owner.Tier,
                ["ModuleId"] = owner.ModuleId,
                ["ModuleName"] = owner.ModuleName,
                ["ModuleVersion"] = owner.ModuleVersion,
            };
            if(owner.FunctionId != null) {
                document["FunctionId"] = owner.FunctionId;
                document["FunctionName"] = owner.FunctionName;
                document["FunctionLogGroupName"] = owner.FunctionLogGroupName;
                document["FunctionPlatform"] = owner.FunctionPlatform;
                document["FunctionFramework"] = owner.FunctionFramework;
                document["FunctionLanguage"] = owner.FunctionLanguage;
                document["FunctionGitSha"] = owner.FunctionGitSha;
                document["FunctionGitBranch"] = owner.FunctionGitBranch;
                document["FunctionMaxMemory"] = owner.FunctionMaxMemory;
                document["FunctionMaxDuration"] = owner.FunctionMaxDuration.ToString();
            }
            await _table.PutItemAsync(document);
        }

        public async Task DeleteOwnerMetaDataAsync(string id) {
            await _table.DeleteItemAsync(id);
        }
    }
}