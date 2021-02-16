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

using Amazon.DynamoDBv2.DocumentModel;

namespace LambdaSharp.App.EventBus {

    internal static class TableEx {

        //--- Extension Methods ---
        public static Search QueryBeginsWith(this Table table, Primitive hashKey, Primitive rangeKeyPrefix) {
            var filter = new QueryFilter();
            filter.AddCondition("PK", QueryOperator.Equal, new DynamoDBEntry[] { hashKey });
            filter.AddCondition("SK", QueryOperator.BeginsWith, new DynamoDBEntry[] { rangeKeyPrefix });
            return table.Query(new QueryOperationConfig {
                Filter = filter
            });
        }

        public static Search QueryGS1BeginsWith(this Table table, Primitive hashKey, Primitive rangeKeyPrefix) {
            var filter = new QueryFilter();
            filter.AddCondition("GS1PK", QueryOperator.Equal, new DynamoDBEntry[] { hashKey });
            filter.AddCondition("GS1SK", QueryOperator.BeginsWith, new DynamoDBEntry[] { rangeKeyPrefix });
            return table.Query(new QueryOperationConfig {
                IndexName = "GS1",
                Filter = filter
            });
        }
    }
}
