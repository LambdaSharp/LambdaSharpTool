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

namespace LambdaSharp.DynamoDB.Native.Query {

    /// <summary>
    /// Interface for specifying the sort key (PK) constraint for an untyped DynamoDB query.
    /// </summary>
    public interface IDynamoQuerySortKeyConstraint {

        //--- Methods ---

        /// <summary>
        /// Skip adding sort key (SK) constraint.
        /// </summary>
        IDynamoQueryClause WhereSKMatchesAny();

        /// <summary>
        /// Add sort key (SK) 'equals' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause WhereSKEquals(string skValue);

        /// <summary>
        /// Add sort key (SK) 'greater than' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause WhereSKIsGreaterThan(string skValue);

        /// <summary>
        /// Add sort key (SK) 'greater than or equal' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause WhereSKIsGreaterThanOrEquals(string skValue);

        /// <summary>
        /// Add sort key (SK) 'less then' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause WhereSKIsLessThan(string skValue);

        /// <summary>
        /// Add sort key (SK) 'less than or equal' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause WhereSKIsLessThanOrEquals(string skValue);

        /// <summary>
        /// Add sort key (SK) must be 'between two values' constraint.
        /// </summary>
        /// <param name="skLowerBound">Lower bound value for sort key (SK).</param>
        /// <param name="skUpperBound">Upper bound value for sort key (SK).</param>
        IDynamoQueryClause WhereSKIsBetween(string skLowerBound, string skUpperBound);

        /// <summary>
        /// Add sort key (SK) must begins with constraint.
        /// </summary>
        /// <param name="skValuePrefix">Prefix for sort key (SK).</param>
        IDynamoQueryClause WhereSKBeginsWith(string skValuePrefix);
    }

    /// <summary>
    /// Interface for specifying the sort key (PK) constraint for a DynamoDB query.
    /// </summary>
    public interface IDynamoQuerySortKeyConstraint<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Skip adding sort key (SK) constraint.
        /// </summary>
        IDynamoQueryClause<TRecord> WhereSKMatchesAny();

        /// <summary>
        /// Add sort key (SK) 'equals' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause<TRecord> WhereSKEquals(string skValue);

        /// <summary>
        /// Add sort key (SK) 'greater than' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause<TRecord> WhereSKIsGreaterThan(string skValue);

        /// <summary>
        /// Add sort key (SK) 'greater than or equal' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause<TRecord> WhereSKIsGreaterThanOrEquals(string skValue);

        /// <summary>
        /// Add sort key (SK) 'less then' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause<TRecord> WhereSKIsLessThan(string skValue);

        /// <summary>
        /// Add sort key (SK) 'less than or equal' constraint.
        /// </summary>
        /// <param name="skValue">Value to compare sort key (SK) to.</param>
        IDynamoQueryClause<TRecord> WhereSKIsLessThanOrEquals(string skValue);

        /// <summary>
        /// Add sort key (SK) must be 'between two values' constraint.
        /// </summary>
        /// <param name="skLowerBound">Lower bound value for sort key (SK).</param>
        /// <param name="skUpperBound">Upper bound value for sort key (SK).</param>
        IDynamoQueryClause<TRecord> WhereSKIsBetween(string skLowerBound, string skUpperBound);

        /// <summary>
        /// Add sort key (SK) must begins with constraint.
        /// </summary>
        /// <param name="skValuePrefix">Prefix for sort key (SK).</param>
        IDynamoQueryClause<TRecord> WhereSKBeginsWith(string skValuePrefix);
    }
}
