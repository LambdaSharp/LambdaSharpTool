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
        public DynamoKey(string pkName, string skName, string pkValue, string skValue) {
            PKName = pkName ?? throw new ArgumentNullException(nameof(pkName));
            SKName = skName ?? throw new ArgumentNullException(nameof(skName));
            PKValue = pkValue ?? throw new ArgumentNullException(nameof(pkValue));
            SKValue = skValue ?? throw new ArgumentNullException(nameof(skValue));
        }

        public DynamoKey(string pkName, string skName, string pkValueFormat, string skValueFormat, params string[] values) {
            for(var i = 0; i < values.Length; ++i) {
                if(values[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(values));
                }
            }
            PKName = pkName ?? throw new ArgumentNullException(nameof(pkName));
            SKName = skName ?? throw new ArgumentNullException(nameof(skName));
            PKValue = string.Format(pkValueFormat ?? throw new ArgumentNullException(nameof(pkValueFormat)), values);
            SKValue = string.Format(skValueFormat ?? throw new ArgumentNullException(nameof(skValueFormat)), values);
        }

        //--- Properties ---
        public string PKName { get; }
        public string SKName { get; }
        public string PKValue { get; }
        public string SKValue { get; }
    }

    public class DynamoPrimaryKey : DynamoKey {

        //--- Constructors ---
        public DynamoPrimaryKey(string pk, string sk) : base("PK", "SK", pk, sk) { }

        public DynamoPrimaryKey(string pkName, string skName, string pkValue, string skValue)
            : base(pkName, skName, pkValue, skValue) { }

        public DynamoPrimaryKey(string pkValueFormat, string skValueFormat, params string[] values) : base("PK", "SK", pkValueFormat, skValueFormat, values) { }

        public DynamoPrimaryKey(string pkName, string skName, string pkValueFormat, string skValueFormat, params string[] values)
            : base(pkName, skName, pkValueFormat, skValueFormat, values) { }
    }

    public class DynamoPrimaryKey<TRecord> : DynamoPrimaryKey where TRecord : class {

        //--- Constructors ---
        public DynamoPrimaryKey(string pk, string sk) : base(pk, sk) { }
        public DynamoPrimaryKey(string pkValueFormat, string skValueFormat, params string[] values) : base(pkValueFormat, skValueFormat, values) { }
    }

    public abstract class ADynamoSecondaryKey : DynamoKey {

        //--- Constructors ---
        public ADynamoSecondaryKey(string indexName, string pkName, string skName, string pkValue, string skValue)
            : base(pkName, skName, pkValue, skValue)
        {
            IndexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        }

        public ADynamoSecondaryKey(string indexName, string pkName, string skName, string pkValueFormat, string skValueFormat, params string[] values)
            : base(pkName, skName, pkValueFormat, skValueFormat, values)
        {
            IndexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        }

        //--- Properties ---
        public string IndexName { get; }
    }

    public class DynamoLocalIndexKey : ADynamoSecondaryKey {

        //--- Constructors ---
        public DynamoLocalIndexKey(string indexName, string pkName, string skName, string pkValue, string skValue)
            : base(indexName, pkName, skName, pkValue, skValue) { }

        public DynamoLocalIndexKey(string indexName, string pkName, string skName, string pkValueFormat, string skValueFormat, params string[] values)
            : base(indexName, pkName, skName, pkValueFormat, skValueFormat, values) { }
    }

    public class DynamoGlobalIndexKey : ADynamoSecondaryKey {

        //--- Constructors ---
        public DynamoGlobalIndexKey(string indexName, string pkName, string skName, string pkValue, string skValue)
            : base(indexName, pkName, skName, pkValue, skValue) { }

        public DynamoGlobalIndexKey(string indexName, string pkName, string skName, string pkValueFormat, string skValueFormat, params string[] values)
            : base(indexName, pkName, skName, pkValueFormat, skValueFormat, values) { }
    }
}
