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
using LambdaSharp.DynamoDB.Native.Query;
using LambdaSharp.DynamoDB.Native.Query.Internal;

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal class DynamoQueryPartitionKeyConstraint : IDynamoQueryPartitionKeyConstraint {

        //--- Constructors ---
        public DynamoQueryPartitionKeyConstraint(string? indexName, string pkName, string skName) {
            IndexName = indexName;
            PKName = pkName ?? throw new ArgumentNullException(nameof(pkName));
            SKName = skName ?? throw new ArgumentNullException(nameof(skName));
        }

        //--- Properties ---
        public string? IndexName { get; }
        public string PKName { get; }
        public string SKName { get; }

        //--- Methods ---
        public IDynamoQuerySortKeyConstraint SelectPK(string pkValue)
            => new DynamoQuerySelectAny<object>(IndexName, PKName, SKName, pkValue, Enumerable.Empty<Type>());

        public IDynamoQuerySortKeyConstraint<TRecord> SelectPK<TRecord>(string pkValue)
            where TRecord : class
            => new DynamoQuerySelectAny<TRecord>(IndexName, PKName, SKName, pkValue, Enumerable.Empty<Type>());
    }
}

