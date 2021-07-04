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

namespace LambdaSharp.DynamoDB.Native {

    public interface IDynamoQuerySelect<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoQuerySelect<TRecord> WhereSKMatchesAny();
        IDynamoQuerySelect<TRecord> WhereSKEquals(string skValue);
        IDynamoQuerySelect<TRecord> WhereSKIsGreaterThan(string skValue);
        IDynamoQuerySelect<TRecord> WhereSKIsGreaterThanOrEquals(string skValue);
        IDynamoQuerySelect<TRecord> WhereSKIsLessThan(string skValue);
        IDynamoQuerySelect<TRecord> WhereSKIsLessThanOrEquals(string skValue);
        IDynamoQuerySelect<TRecord> WhereSKIsBetween(string skLowerBound, string skUpperBound);
        IDynamoQuerySelect<TRecord> WhereSKBeginsWith(string skValuePrefix);
    }

    public interface IDynamoQuerySelect {

        //--- Methods ---
        IDynamoQuerySelect WhereSKMatchesAny();
        IDynamoQuerySelect WhereSKEquals(string skValue);
        IDynamoQuerySelect WhereSKIsGreaterThan(string skValue);
        IDynamoQuerySelect WhereSKIsGreaterThanOrEquals(string skValue);
        IDynamoQuerySelect WhereSKIsLessThan(string skValue);
        IDynamoQuerySelect WhereSKIsLessThanOrEquals(string skValue);
        IDynamoQuerySelect WhereSKIsBetween(string skLowerBound, string skUpperBound);
        IDynamoQuerySelect WhereSKBeginsWith(string skValuePrefix);
    }
}
