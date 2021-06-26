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

// TODO: split and rename file

namespace LambdaSharp.DynamoDB.Native {

    public class DynamoKey {

        //--- Constructors ---
        public DynamoKey(string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyValue) {
            PartitionKeyName = partitionKeyName ?? throw new ArgumentNullException(nameof(partitionKeyName));
            SortKeyName = sortKeyName ?? throw new ArgumentNullException(nameof(sortKeyName));
            PartitionKeyValue = partitionKeyValue ?? throw new ArgumentNullException(nameof(partitionKeyValue));
            SortKeyValue = sortKeyValue ?? throw new ArgumentNullException(nameof(sortKeyValue));
        }

        public DynamoKey(string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, string sortKeyValuePattern, params string[] keys) {
            for(var i = 0; i < keys.Length; ++i) {
                if(keys[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(keys));
                }
            }
            PartitionKeyName = partitionKeyName ?? throw new ArgumentNullException(nameof(partitionKeyName));
            SortKeyName = sortKeyName ?? throw new ArgumentNullException(nameof(sortKeyName));
            PartitionKeyValue = string.Format(partitionKeyValuePattern ?? throw new ArgumentNullException(nameof(partitionKeyValuePattern)), keys);
            SortKeyValue = string.Format(sortKeyValuePattern ?? throw new ArgumentNullException(nameof(sortKeyValuePattern)), keys);
        }

        //--- Properties ---
        public string PartitionKeyName { get; }
        public string SortKeyName { get; }
        public string PartitionKeyValue { get; }
        public string SortKeyValue { get; }
    }

    public class DynamoPrimaryKey : DynamoKey {

        //--- Constructors ---
        public DynamoPrimaryKey(string pk, string sk) : base("PK", "SK", pk, sk) { }

        public DynamoPrimaryKey(string pkPattern, string skPattern, params string[] keys) : base("PK", "SK", pkPattern, skPattern, keys) { }

        //--- Properties ---
        public string PK => PartitionKeyValue;
        public string SK => SortKeyValue;
    }

    public class DynamoPrimaryKey<TRecord> : DynamoPrimaryKey where TRecord : class {

        //--- Constructors ---
        public DynamoPrimaryKey(string pk, string sk) : base(pk, sk) { }
        public DynamoPrimaryKey(string pkPattern, string skPattern, params string[] keys) : base(pkPattern, skPattern, keys) { }
    }

    public abstract class ADynamoSecondaryKey : DynamoKey {

        //--- Constructors ---
        public ADynamoSecondaryKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyValue)
            : base(partitionKeyName, partitionKeyValue, sortKeyName, sortKeyValue)
        {
            IndexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        }

        public ADynamoSecondaryKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, string sortKeyValuePattern, params string[] keys)
            : base(partitionKeyName, partitionKeyValuePattern, sortKeyName, sortKeyValuePattern, keys)
        {
            IndexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        }

        //--- Properties ---
        public string IndexName { get; }
    }

    public class DynamoLocalIndexKey : ADynamoSecondaryKey {

        //--- Constructors ---
        public DynamoLocalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyValue)
            : base(indexName, partitionKeyName, partitionKeyValue, sortKeyName, sortKeyValue) { }

        public DynamoLocalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, string sortKeyValuePattern, params string[] keys)
            : base(indexName, partitionKeyName, partitionKeyValuePattern, sortKeyName, sortKeyValuePattern, keys) { }
    }

    public class DynamoLocalIndexKey<TRecord> : DynamoLocalIndexKey where TRecord : class {

        //--- Constructors ---
        public DynamoLocalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyValue)
            : base(indexName, partitionKeyName, partitionKeyValue, sortKeyName, sortKeyValue) { }

        public DynamoLocalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, string sortKeyValuePattern, params string[] keys)
            : base(indexName, partitionKeyName, partitionKeyValuePattern, sortKeyName, sortKeyValuePattern, keys) { }
    }

    public class DynamoGlobalIndexKey : ADynamoSecondaryKey {

        //--- Constructors ---
        public DynamoGlobalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyValue)
            : base(indexName, partitionKeyName, partitionKeyValue, sortKeyName, sortKeyValue) { }

        public DynamoGlobalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, string sortKeyValuePattern, params string[] keys)
            : base(indexName, partitionKeyName, partitionKeyValuePattern, sortKeyName, sortKeyValuePattern, keys) { }
    }

    public class DynamoGlobalIndexKey<TRecord> : DynamoGlobalIndexKey where TRecord : class {

        //--- Constructors ---
        public DynamoGlobalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValue, string sortKeyValue)
            : base(indexName, partitionKeyName, partitionKeyValue, sortKeyName, sortKeyValue) { }

        public DynamoGlobalIndexKey(string indexName, string partitionKeyName, string sortKeyName, string partitionKeyValuePattern, string sortKeyValuePattern, params string[] keys)
            : base(indexName, partitionKeyName, partitionKeyValuePattern, sortKeyName, sortKeyValuePattern, keys) { }
    }
}
