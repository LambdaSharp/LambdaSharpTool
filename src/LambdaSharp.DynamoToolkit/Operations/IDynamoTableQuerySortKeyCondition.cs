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

namespace LambdaSharp.DynamoToolkit.Operations {

    /*
     * KEY EXPRESSION OPERATORS AND FUNCTIONS
     * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.html#Query.KeyConditionExpressions
     *
     * sort-key-condition ::=
     *     operand comparator operand
     *     | operand BETWEEN operand AND operand
     *     | function
     *
     * comparator ::=
     *     =
     *     | <
     *     | <=
     *     | >
     *     | >=
     *
     * function ::=
     *     begins_with (path, substr)
     */


    public interface IDynamoTableQuerySortKeyCondition {

        //--- Methods ---
        IDynamoTableQuery WhereSKMatchesAny();
        IDynamoTableQuery WhereSKEquals(string skValue);
        IDynamoTableQuery WhereSKBeginsWith(string skValuePrefix);
        IDynamoTableQuery WhereSKIsGreaterThan(string skValue);
        IDynamoTableQuery WhereSKIsGreaterThanOrEquals(string skValue);
        IDynamoTableQuery WhereSKIsLessThan(string skValue);
        IDynamoTableQuery WhereSKIsLessThanOrEquals(string skValue);
        IDynamoTableQuery WhereSKIsBetween(string skLowValue, string skHighValue);
    }

    public interface IDynamoTableQuerySortKeyCondition<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableQuery<TRecord> WhereSKMatchesAny();
        IDynamoTableQuery<TRecord> WhereSKEquals(string skValue);
        IDynamoTableQuery<TRecord> WhereSKBeginsWith(string skValuePrefix);
        IDynamoTableQuery<TRecord> WhereSKIsGreaterThan(string skValue);
        IDynamoTableQuery<TRecord> WhereSKIsGreaterThanOrEquals(string skValue);
        IDynamoTableQuery<TRecord> WhereSKIsLessThan(string skValue);
        IDynamoTableQuery<TRecord> WhereSKIsLessThanOrEquals(string skValue);
        IDynamoTableQuery<TRecord> WhereSKIsBetween(string skLowValue, string skHighValue);
    }
}
