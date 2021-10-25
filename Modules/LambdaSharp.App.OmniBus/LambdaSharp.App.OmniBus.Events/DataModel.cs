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

using LambdaSharp.DynamoDB.Native;
using LambdaSharp.App.OmniBus.Events.Records;

namespace LambdaSharp.App.OmniBus.Events {

    internal static class DataModel {

        //--- Constants ---
        public const string CONNECTION_PK_PATTERN = "WS={0}";
        public const string CONNECTION_SK_PATTERN = "INFO";
        public const string RULE_PK_PATTERN = "WS={0}";
        public const string RULE_SK_PATTERN = "RULE={1}";

        //--- Extension Methods ---
        public static DynamoPrimaryKey<ConnectionRecord> GetPrimaryKey(this ConnectionRecord record)
            =>  MakeConnectionRecordPrimaryKey(record.ConnectionId);
        public static DynamoPrimaryKey<RuleRecord> GetPrimaryKey(this RuleRecord record)
            =>  MakeRuleRecordPrimaryKey(record.ConnectionId, record.Name);

        //--- Class Methods ---
        public static DynamoPrimaryKey<ConnectionRecord> MakeConnectionRecordPrimaryKey(string connectionId)
            => new DynamoPrimaryKey<ConnectionRecord>(CONNECTION_PK_PATTERN, CONNECTION_SK_PATTERN, connectionId);

        public static DynamoPrimaryKey<RuleRecord> MakeRuleRecordPrimaryKey(string connectionId, string ruleId)
            => new DynamoPrimaryKey<RuleRecord>(RULE_PK_PATTERN, RULE_SK_PATTERN, connectionId, ruleId);

        public static IDynamoQueryClause<RuleRecord> SelectRuleRecords(string connectionId)
            => DynamoQuery.SelectPKFormat<RuleRecord>(RULE_PK_PATTERN, connectionId)
                .WhereSKBeginsWith(string.Format(RULE_SK_PATTERN, "", ""));
    }
}
