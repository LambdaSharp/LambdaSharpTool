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
using System.Threading.Tasks;

namespace LambdaSharp.DynamoDB.Native.Operations {

    public interface IDynamoTableQuery {

        //--- Methods ---
        IDynamoTableQuery Where<TRecord>(Expression<Func<TRecord, bool>> filter) where TRecord : class;
        IDynamoTableQuery Get<TRecord, T>(Expression<Func<TRecord, T>> attribute) where TRecord : class;
        IAsyncEnumerable<object> ExecuteAsyncEnumerable(bool fetchAllAttributes, CancellationToken cancellationToken = default);
        Task<IEnumerable<object>> ExecuteAsync(bool fetchAllAttributes, CancellationToken cancellationToken = default);

        //--- Default Methods ---
        IAsyncEnumerable<object> ExecuteAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: false, cancellationToken);
        IAsyncEnumerable<object> ExecuteFetchAllAttributesAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: true, cancellationToken);
        Task<IEnumerable<object>> ExecuteAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: false, cancellationToken);
        Task<IEnumerable<object>> ExecuteFetchAllAttributesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: true, cancellationToken);
    }

    public interface IDynamoTableQuery<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableQuery<TRecord> Where(Expression<Func<TRecord, bool>> filter);
        IDynamoTableQuery<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);
        IAsyncEnumerable<TRecord> ExecuteAsyncEnumerable(bool fetchAllAttributes, CancellationToken cancellationToken = default);
        Task<IEnumerable<TRecord>> ExecuteAsync(bool fetchAllAttributes, CancellationToken cancellationToken = default);

        //--- Default Methods ---
        IAsyncEnumerable<TRecord> ExecuteAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: false, cancellationToken);
        IAsyncEnumerable<TRecord> ExecuteFetchAllAttributesAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: true, cancellationToken);
        Task<IEnumerable<TRecord>> ExecuteAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: false, cancellationToken);
        Task<IEnumerable<TRecord>> ExecuteFetchAllAttributesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: true, cancellationToken);
    }
}
