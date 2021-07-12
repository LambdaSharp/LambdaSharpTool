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

namespace LambdaSharp.DynamoDB.Native.Query {

    public interface IDynamoQuerySortKeyConstraint {

        //--- Methods ---
        IDynamoQueryClause WhereSKMatchesAny();
        IDynamoQueryClause WhereSKEquals(string skValue);
        IDynamoQueryClause WhereSKIsGreaterThan(string skValue);
        IDynamoQueryClause WhereSKIsGreaterThanOrEquals(string skValue);
        IDynamoQueryClause WhereSKIsLessThan(string skValue);
        IDynamoQueryClause WhereSKIsLessThanOrEquals(string skValue);
        IDynamoQueryClause WhereSKIsBetween(string skLowerBound, string skUpperBound);
        IDynamoQueryClause WhereSKBeginsWith(string skValuePrefix);
    }

    public interface IDynamoQuerySortKeyConstraint<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoQueryClause<TRecord> WhereSKMatchesAny();
        IDynamoQueryClause<TRecord> WhereSKEquals(string skValue);
        IDynamoQueryClause<TRecord> WhereSKIsGreaterThan(string skValue);
        IDynamoQueryClause<TRecord> WhereSKIsGreaterThanOrEquals(string skValue);
        IDynamoQueryClause<TRecord> WhereSKIsLessThan(string skValue);
        IDynamoQueryClause<TRecord> WhereSKIsLessThanOrEquals(string skValue);
        IDynamoQueryClause<TRecord> WhereSKIsBetween(string skLowerBound, string skUpperBound);
        IDynamoQueryClause<TRecord> WhereSKBeginsWith(string skValuePrefix);
    }
}
