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
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LambdaSharp.App.EventBus.Records;

namespace LambdaSharp.App.EventBus {

    public sealed class DataTable : ADataTable {

        //--- Constants ---
        private const string VALID_SYMBOLS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string CONNECTION_PREFIX = "WS#";
        private const string RULE_PREFIX = "RULE#";
        private const string INFO = "INFO";

        //--- Class Fields ---
        private readonly static Random _random = new Random();

        //--- Class Methods ---
        public static string GetRandomString(int length)
            => new string(Enumerable.Repeat(VALID_SYMBOLS, length).Select(chars => chars[_random.Next(chars.Length)]).ToArray());

        //--- Constructors ---
        public DataTable(string tableName, IAmazonDynamoDB dynamoDbClient = null) : base(tableName, dynamoDbClient) { }

        //--- Methods ---

        #region Connection Record
        public async Task<ConnectionRecord> GetConnectionRecordAsync(string connectionId, CancellationToken cancellationToken = default)
            => Deserialize<ConnectionRecord>(await GetItemAsync(CONNECTION_PREFIX + connectionId, INFO, cancellationToken));

        public Task CreateConnectionRecordAsync(ConnectionRecord record, CancellationToken cancellationToken = default)
            => CreateItemsAsync(
                record,
                pk: CONNECTION_PREFIX + record.ConnectionId,
                sk: INFO,
                cancellationToken
            );

        public Task UpdateConnectionRecordAsync(ConnectionRecord record, CancellationToken cancellationToken = default)
            => UpdateItemsAsync(
                record,
                pk: CONNECTION_PREFIX + record.ConnectionId,
                sk: INFO,
                cancellationToken
            );

        public Task DeleteConnectionRecordAsync(string connectionId, CancellationToken cancellationToken = default)
            => DeleteItemAsync(
                pk: CONNECTION_PREFIX + connectionId,
                sk: INFO,
                cancellationToken
            );
        #endregion

        #region Rule Record
        public Task CreateOrUpdateRuleRecordAsync(RuleRecord record, CancellationToken cancellationToken = default)
            => CreateOrUpdateItemsAsync(
                record,
                pk: CONNECTION_PREFIX + record.ConnectionId,
                sk: RULE_PREFIX + record.Rule,
                cancellationToken
            );
        public Task DeleteRuleRecordAsync(string connectionId, string ruleId, CancellationToken cancellationToken = default)
            => DeleteItemAsync(
                pk: CONNECTION_PREFIX + connectionId,
                sk: RULE_PREFIX + ruleId,
                cancellationToken
            );

        public async Task<IEnumerable<RuleRecord>> GetAllRuleRecordAsync(string connectionId, CancellationToken cancellationToken = default)
            => (await SearchBeginsWith(
                pk: CONNECTION_PREFIX + connectionId,
                skPrefix: RULE_PREFIX,
                cancellationToken
            )).Select(document => Deserialize<RuleRecord>(document)).ToList();

        public Task DeleteAllRuleRecordAsync(IEnumerable<RuleRecord> records, CancellationToken cancellationToken = default)
            => DeleteItemsAsync(records.Select(record => (
                PK: CONNECTION_PREFIX + record.ConnectionId,
                SK: RULE_PREFIX + record.Rule
            )), cancellationToken);
        #endregion

    }
}
