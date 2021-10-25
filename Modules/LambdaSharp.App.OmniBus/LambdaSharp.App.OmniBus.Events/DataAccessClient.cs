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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LambdaSharp.DynamoDB.Native;
using LambdaSharp.App.OmniBus.Events.Records;

namespace LambdaSharp.App.OmniBus.Events {

    public class DataAccessClient : IConnectionsDataAccess, IRulesDataAccess {

        //--- Constructors ---
        public DataAccessClient(string tableName, IAmazonDynamoDB dynamoClient = null)
            => Table = new DynamoTable(tableName, dynamoClient);

        //--- Properties ---
        protected IDynamoTable Table { get; }
        protected IRulesDataAccess RulesDataAccess => (IRulesDataAccess)this;

        //--- Methods ---
        #region Connection Record
        public Task<ConnectionRecord> GetConnectionRecordAsync(string connectionId, CancellationToken cancellationToken)
            => Table.GetItemAsync(DataModel.MakeConnectionRecordPrimaryKey(connectionId), cancellationToken: cancellationToken);

        public Task<ConnectionRecord> GetConnectionRecordAndIncreaseRulesCounterAsync(string connectionId, CancellationToken cancellationToken)
            => Table.UpdateItem(DataModel.MakeConnectionRecordPrimaryKey(connectionId))
                .WithCondition(record => record.RulesCounter < 100)
                .Set(record => record.RulesCounter, record => record.RulesCounter + 1)
                .ExecuteReturnNewItemAsync(cancellationToken);

        public Task<ConnectionRecord> GetConnectionRecordAndDecreaseRulesCounterAsync(string connectionId, CancellationToken cancellationToken)
            => Table.UpdateItem(DataModel.MakeConnectionRecordPrimaryKey(connectionId))
                .WithCondition(record => record.RulesCounter > 0)
                .Set(record => record.RulesCounter, record => record.RulesCounter - 1)
                .ExecuteReturnNewItemAsync(cancellationToken);

        public Task CreateConnectionRecordAsync(ConnectionRecord record, CancellationToken cancellationToken)
            => Table.PutItem(record.GetPrimaryKey(), record)
                .WithCondition(record => DynamoCondition.DoesNotExist(record))
                .ExecuteAsync(cancellationToken);

        public async Task<bool> UpdateConnectionRecordSubscriptionAsync(
            ConnectionRecord record,
            string subscriptionArn,
            CancellationToken cancellationToken
        ) => await Table.UpdateItem(record.GetPrimaryKey())
            .WithCondition(record => DynamoCondition.DoesNotExist(record.SubscriptionArn))
            .Set(record => record.SubscriptionArn, subscriptionArn)
            .ExecuteAsync(cancellationToken);

        public Task<ConnectionRecord> DeleteConnectionRecordAsync(string connectionId, CancellationToken cancellationToken)
            => Table.DeleteItem(DataModel.MakeConnectionRecordPrimaryKey(connectionId))
                .ExecuteReturnOldItemAsync(cancellationToken);

        public Task<bool> DeleteConnectionRecordSubscriptionAsync(string connectionId, CancellationToken cancellationToken)
            => Table.UpdateItem(DataModel.MakeConnectionRecordPrimaryKey(connectionId))
                .WithCondition(record => DynamoCondition.Exists(record.SubscriptionArn))
                .Remove(record => record.SubscriptionArn)
                .ExecuteAsync();
        #endregion

        #region Rule Record
        public Task<bool> CreateRuleRecordAsync(RuleRecord record, CancellationToken cancellationToken = default)
            => Table.PutItem(record.GetPrimaryKey(), record)
                .WithCondition(record => DynamoCondition.DoesNotExist(record))
                .ExecuteAsync();

        public Task<bool> DeleteRuleRecordAsync(string connectionId, string ruleId, CancellationToken cancellationToken)
            => Table.DeleteItem(DataModel.MakeRuleRecordPrimaryKey(connectionId, ruleId))
                .WithCondition(record => DynamoCondition.Exists(record))
                .ExecuteAsync(cancellationToken);

        public async Task<IEnumerable<RuleRecord>> GetAllRuleRecordAsync(string connectionId, CancellationToken cancellationToken)
            => await Table.Query(DataModel.SelectRuleRecords(connectionId))
                .ExecuteAsync(cancellationToken);

        public async Task DeleteAllRuleRecordAsync(string connectionId, CancellationToken cancellationToken = default) {
            var rules = await RulesDataAccess.GetAllRuleRecordAsync(connectionId, cancellationToken);
            while(rules.Any()) {
                var batch = Table.BatchWriteItems();
                foreach(var rule in rules.Take(25)) {
                    batch.DeleteItem(DataModel.MakeRuleRecordPrimaryKey(connectionId, rule.Name));
                }
                await batch.ExecuteAsync(cancellationToken: cancellationToken);
                rules = rules.Skip(25);
            }
        }
        #endregion
    }
}
