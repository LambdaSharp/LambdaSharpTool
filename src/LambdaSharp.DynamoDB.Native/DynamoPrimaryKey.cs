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

namespace LambdaSharp.DynamoDB.Native {

    /// <summary>
    /// Represents an untyped DynamoDB primary key.
    /// </summary>
    public class DynamoPrimaryKey {

        //--- Constructors ---

        /// <summary>
        /// Creates a new <see cref="DynamoPrimaryKey"/> instance.
        /// </summary>
        /// <param name="pkValue">The partition key (PK) value.</param>
        /// <param name="skValue">The sort key (SK) value.</param>
        public DynamoPrimaryKey(string pkValue, string skValue) {
            PKValue = pkValue ?? throw new ArgumentNullException(nameof(pkValue));
            SKValue = skValue ?? throw new ArgumentNullException(nameof(skValue));
        }

        /// <summary>
        /// Creates a new <see cref="DynamoPrimaryKey"/> instance.
        /// </summary>
        /// <param name="pkValueFormat">The format string for the partition key (PK) value.</param>
        /// <param name="skValueFormat">The format string for the sort key (PK) value.</param>
        /// <param name="values">A string array that contains zero or more strings for both format strings.</param>
        public DynamoPrimaryKey(string pkValueFormat, string skValueFormat, params string[] values) {
            for(var i = 0; i < values.Length; ++i) {
                if(values[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(values));
                }
            }
            PKValue = string.Format(pkValueFormat ?? throw new ArgumentNullException(nameof(pkValueFormat)), values);
            SKValue = string.Format(skValueFormat ?? throw new ArgumentNullException(nameof(skValueFormat)), values);
        }

        //--- Properties ---

        /// <summary>
        /// The partition key (PK) name.
        /// </summary>
        public string PKName => "PK";

        /// <summary>
        /// The sort key (SK) name.
        /// </summary>
        public string SKName => "SK";

        /// <summary>
        /// The partition key (PK) value.
        /// </summary>
        public string PKValue { get; }

        /// <summary>
        /// The sort key (SK) value.
        /// </summary>
        public string SKValue { get; }
    }

    /// <summary>
    /// Represents a typed DynamoDB primary key.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public class DynamoPrimaryKey<TRecord> : DynamoPrimaryKey where TRecord : class {

        //--- Constructors ---

        /// <summary>
        /// Creates a new <see cref="DynamoPrimaryKey{TRecord}"/> instance.
        /// </summary>
        /// <param name="pkValue">The partition key (PK) value.</param>
        /// <param name="skValue">The sort key (SK) value.</param>
        public DynamoPrimaryKey(string pkValue, string skValue) : base(pkValue, skValue) { }

        /// <summary>
        /// Creates a new <see cref="DynamoPrimaryKey{TRecord}"/> instance.
        /// </summary>
        /// <param name="pkValueFormat">The format string for the partition key (PK) value.</param>
        /// <param name="skValueFormat">The format string for the sort key (PK) value.</param>
        /// <param name="values">A string array that contains zero or more strings for both format strings.</param>
        public DynamoPrimaryKey(string pkValueFormat, string skValueFormat, params string[] values) : base(pkValueFormat, skValueFormat, values) { }
    }
}
