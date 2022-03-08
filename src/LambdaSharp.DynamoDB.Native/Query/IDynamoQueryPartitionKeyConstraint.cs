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

namespace LambdaSharp.DynamoDB.Native.Query {

    /// <summary>
    /// Interface for specifying the partition key (PK) constraint.
    /// </summary>
    public interface IDynamoQueryPartitionKeyConstraint {

        //--- Methods ---

        /// <summary>
        /// Add an untyped partition key (PK) constraint.
        /// </summary>
        /// <param name="pkValue">Partition key (PK) value.</param>
        IDynamoQuerySortKeyConstraint SelectPK(string pkValue);

        /// <summary>
        /// Add a partition key (PK) constraint.
        /// </summary>
        /// <param name="pkValue">The partition key (PK) value.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        IDynamoQuerySortKeyConstraint<TRecord> SelectPK<TRecord>(string pkValue) where TRecord : class;

        //--- Default Methods ---

        /// <summary>
        /// Add an untyped partition key (PK) constraint.
        /// </summary>
        /// <param name="pkValueFormat">Format string for the partition key (PK) value.</param>
        /// <param name="values">A string array that contains zero or more strings for the partition key format string.</param>
        IDynamoQuerySortKeyConstraint SelectPKFormat(string pkValueFormat, params string[] values) {
            for(var i = 0; i < values.Length; ++i) {
                if(values[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(values));
                }
            }
            var pkValue = string.Format(pkValueFormat ?? throw new ArgumentNullException(nameof(pkValueFormat)), values);
            return SelectPK(pkValue);
        }

        /// <summary>
        /// Add a partition key (PK) constraint.
        /// </summary>
        /// <param name="pkValueFormat">Format string for the partition key (PK) value.</param>
        /// <param name="values">A string array that contains zero or more strings for the partition key format string.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        IDynamoQuerySortKeyConstraint<TRecord> SelectPKFormat<TRecord>(string pkValueFormat, params string[] values)
            where TRecord : class
        {
            for(var i = 0; i < values.Length; ++i) {
                if(values[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(values));
                }
            }
            var pkValue = string.Format(pkValueFormat ?? throw new ArgumentNullException(nameof(pkValueFormat)), values);
            return SelectPK<TRecord>(pkValue);
        }
    }
}

