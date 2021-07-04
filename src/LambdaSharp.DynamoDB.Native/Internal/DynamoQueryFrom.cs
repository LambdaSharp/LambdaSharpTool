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

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal class DynamoQueryFrom : IDynamoQueryFrom {

        //--- Constructors ---
        public DynamoQueryFrom(string? indexName, string partitionKeyName, string sortKeyName) {
            IndexName = indexName;
            PartitionKeyName = partitionKeyName ?? throw new ArgumentNullException(nameof(partitionKeyName));
            SortKeyName = sortKeyName ?? throw new ArgumentNullException(nameof(sortKeyName));
        }

        //--- Properties ---
        public string? IndexName { get; }
        public string PartitionKeyName { get; }
        public string SortKeyName { get; }

        //--- Methods ---
        public IDynamoQuerySelect<TRecord> Select<TRecord>(string pkValue)
            where TRecord : class
            => new DynamoQuerySelectAny<TRecord>(IndexName, PartitionKeyName, SortKeyName, pkValue);
    }
}

