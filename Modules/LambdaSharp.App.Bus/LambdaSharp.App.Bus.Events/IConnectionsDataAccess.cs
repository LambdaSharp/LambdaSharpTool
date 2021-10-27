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
using System.Threading;
using System.Threading.Tasks;
using LambdaSharp.App.Bus.Events.Records;

namespace LambdaSharp.App.Bus.Events {

    public interface IConnectionsDataAccess {

        //--- Methods ---
        Task CreateConnectionRecordAsync(ConnectionRecord record, CancellationToken cancellationToken = default);
        Task<ConnectionRecord> GetConnectionRecordAsync(string connectionId, CancellationToken cancellationToken = default);
        Task<ConnectionRecord> GetConnectionRecordAndIncreaseRulesCounterAsync(string connectionId, CancellationToken cancellationToken = default);
        Task<ConnectionRecord> GetConnectionRecordAndDecreaseRulesCounterAsync(string connectionId, CancellationToken cancellationToken = default);
        Task<ConnectionRecord> DeleteConnectionRecordAsync(string connectionId, CancellationToken cancellationToken = default);
        Task<bool> DeleteConnectionRecordSubscriptionAsync(string connectionId, CancellationToken cancellationToken = default);

        Task<bool> UpdateConnectionRecordSubscriptionAsync(
            ConnectionRecord record,
            string subscriptionArn,
            CancellationToken cancellationToken = default
        );
    }

    public interface IRulesDataAccess {

        //--- Methods ---
        Task<bool> CreateRuleRecordAsync(RuleRecord record, CancellationToken cancellationToken = default);
        Task<bool> DeleteRuleRecordAsync(string connectionId, string ruleId, CancellationToken cancellationToken = default);
        Task<IEnumerable<RuleRecord>> GetAllRuleRecordAsync(string connectionId, CancellationToken cancellationToken = default);
        Task DeleteAllRuleRecordAsync(string connectionId, CancellationToken cancellationToken = default);
    }
}
