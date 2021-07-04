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
using LambdaSharp.DynamoDB.Native.Internal;

namespace LambdaSharp.DynamoDB.Native {

    public static class DynamoQuery {

        //--- Class Methods ---
        public static IDynamoQueryFrom FromMainIndex() => new DynamoQueryFrom(indexName: null, "PK", "SK");

        public static IDynamoQueryFrom FromIndex(string indexName, string partitionKeyName, string sortKeyName)
            => new DynamoQueryFrom(indexName ?? throw new ArgumentNullException(nameof(indexName)), partitionKeyName, sortKeyName);

        public static IDynamoQuerySelect<TRecord> Select<TRecord>(string pkValue)
            where TRecord : class
            => FromMainIndex().Select<TRecord>(pkValue);

        public static IDynamoQuerySelect<TRecord> SelectFormat<TRecord>(string partitionKeyValuePattern, params string[] values) where TRecord : class {
            for(var i = 0; i < values.Length; ++i) {
                if(values[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(values));
                }
            }
            var pkValue = string.Format(partitionKeyValuePattern ?? throw new ArgumentNullException(nameof(partitionKeyValuePattern)), values);
            return Select<TRecord>(pkValue);
        }
    }
}

