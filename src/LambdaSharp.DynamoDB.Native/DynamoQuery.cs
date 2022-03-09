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
using LambdaSharp.DynamoDB.Native.Internal;
using LambdaSharp.DynamoDB.Native.Query;

namespace LambdaSharp.DynamoDB.Native {

    /// <summary>
    /// Interface identifying a DynamoDB query clause for mixed records.
    /// </summary>
    public interface IDynamoQueryClause {

        //--- Methods ---

        /// <summary>
        /// Limit results to record with the specified type.
        /// </summary>
        /// <param name="type">Type filter to add.</param>
        IDynamoQueryClause WithTypeFilter(Type type);

        //--- Default Methods ---

        /// <summary>
        /// Limit results to record with the specified type.
        /// </summary>
        /// <typeparam name="T">Type to filter by.</typeparam>
        IDynamoQueryClause WithTypeFilter<T>( ) => WithTypeFilter(typeof(T));
    }

    /// <summary>
    /// Interface identifying a DynamoDB query clause.
    /// </summary>
    public interface IDynamoQueryClause<TRecord> where TRecord : class { }

    /// <summary>
    /// The <see cref="DynamoQuery"/> defines static methods for building DynamoDB
    /// query clauses.
    /// </summary>
    public static class DynamoQuery {

        //--- Class Methods ---

        /// <summary>
        /// Build a DynamoDB query clause using the main index.
        /// </summary>
        public static IDynamoQueryPartitionKeyConstraint FromMainIndex() => new DynamoQueryPartitionKeyConstraint(indexName: null, "PK", "SK");

        /// <summary>
        /// Build a DynamoDB query clause using the a local/global secondary index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="pkName">The partition key (PK) name.</param>
        /// <param name="skName">The sort key (PK) name.</param>
        /// <returns></returns>
        public static IDynamoQueryPartitionKeyConstraint FromIndex(string indexName, string pkName, string skName)
            => new DynamoQueryPartitionKeyConstraint(indexName ?? throw new ArgumentNullException(nameof(indexName)), pkName, skName);

        /// <summary>
        /// Build an untyped DynamoDB query clause by selecting a partition key (PK) on the main index.
        /// </summary>
        /// <param name="pkValue">Partition key (PK) value.</param>
        public static IDynamoQuerySortKeyConstraint SelectPK(string pkValue)
            => FromMainIndex().SelectPK(pkValue);

        /// <summary>
        /// Build a DynamoDB query clause by selecting a partition key (PK) on the main index.
        /// </summary>
        /// <param name="pkValue">The partition key (PK) value.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public static IDynamoQuerySortKeyConstraint<TRecord> SelectPK<TRecord>(string pkValue)
            where TRecord : class
            => FromMainIndex().SelectPK<TRecord>(pkValue);

        /// <summary>
        /// Build an untyped DynamoDB query clause by selecting a partition key (PK) on the main index.
        /// </summary>
        /// <param name="pkValueFormat">Format string for the partition key (PK) value.</param>
        /// <param name="values">A string array that contains zero or more strings for the partition key format string.</param>
        public static IDynamoQuerySortKeyConstraint SelectPKFormat(string pkValueFormat, params string[] values) {
            for(var i = 0; i < values.Length; ++i) {
                if(values[i] is null) {
                    throw new ArgumentException($"key[{i}] is null", nameof(values));
                }
            }
            var pkValue = string.Format(pkValueFormat ?? throw new ArgumentNullException(nameof(pkValueFormat)), values);
            return SelectPK(pkValue);
        }

        /// <summary>
        /// Build a DynamoDB query clause by selecting a partition key (PK) on the main index.
        /// </summary>
        /// <param name="pkValueFormat">Format string for the partition key (PK) value.</param>
        /// <param name="values">A string array that contains zero or more strings for the partition key format string.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        public static IDynamoQuerySortKeyConstraint<TRecord> SelectPKFormat<TRecord>(string pkValueFormat, params string[] values) where TRecord : class {
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

