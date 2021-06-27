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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace LambdaSharp.DynamoDB.Native.Operations {

    /*
     * FILTER EXPRESSION OPERATORS AND FUNCTIONS
     * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.html#Query.FilterExpression
     *
     * filter-expression ::=
     *     operand comparator operand
     *     | operand BETWEEN operand AND operand
     *     | operand IN ( operand (',' operand (, ...) ))
     *     | function
     *     | condition AND condition
     *     | condition OR condition
     *     | NOT condition
     *     | ( condition )
     *
     * comparator ::=
     *     =
     *     | <>
     *     | <
     *     | <=
     *     | >
     *     | >=
     *
     * function ::=
     *     attribute_exists (path)
     *     | attribute_not_exists (path)
     *     | attribute_type (path, type)
     *     | begins_with (path, substr)
     *     | contains (path, operand)
     *     | size (path)
     */

    public interface IDynamoTableQuery {

        //--- Methods ---
        IDynamoTableQuery WithFilter<TRecord>(Expression<Func<TRecord, bool>> filter) where TRecord : class;
        IDynamoTableQuery Get<TRecord, T>(Expression<Func<TRecord, T>> attribute) where TRecord : class;
        IDynamoTableQuery WithTypeFilter<T>();
        IDynamoTableQuery WithTypeFilter(Type type);

        // TODO: IAsyncEnumerable<TRecord?> is technically not an async method
        IAsyncEnumerable<object> ExecuteAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<object> ExecuteFetchAllAttributesAsync(CancellationToken cancellationToken = default);
    }

    public interface IDynamoTableQuery<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableQuery<TRecord> WithFilter(Expression<Func<TRecord, bool>> filter);
        IDynamoTableQuery<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);

        // TODO: IAsyncEnumerable<TRecord?> is technically not an async method
        IAsyncEnumerable<TRecord> ExecuteAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<TRecord> ExecuteFetchAllAttributesAsync(CancellationToken cancellationToken = default);
    }
}
