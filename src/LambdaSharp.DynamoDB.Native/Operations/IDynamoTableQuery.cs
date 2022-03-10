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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LambdaSharp.DynamoDB.Native.Operations {

    /// <summary>
    /// Interface to specify Query operation with mixed record types.
    /// </summary>
    public interface IDynamoTableQuery {

        //--- Methods ---

        /// <summary>
        /// Filter query results.
        /// </summary>
        /// <param name="filter">A lambda predicate representing the DynamoDB filter expression.</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        IDynamoTableQuery Where<TRecord>(Expression<Func<TRecord, bool>> filter) where TRecord : class;

        /// <summary>
        /// Selects a record property to fetch.
        /// </summary>
        /// <param name="attribute">A lambda expression that returns the record property.</param>
        /// <typeparam name="TRecord">The property type.</typeparam>
        /// <typeparam name="T">The record type.</typeparam>
        IDynamoTableQuery Get<TRecord, T>(Expression<Func<TRecord, T>> attribute) where TRecord : class;

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="fetchAllAttributes">Fetch all attributes from main index when querying a local/global secondary index.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Asynchronous enumerable which returns found records.</returns>
        IAsyncEnumerable<object> ExecuteAsyncEnumerable(bool fetchAllAttributes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="fetchAllAttributes">Fetch all attributes from main index when querying a local/global secondary index.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of all found records.</returns>
        Task<IEnumerable<object>> ExecuteAsync(bool fetchAllAttributes, CancellationToken cancellationToken = default);

        //--- Default Methods ---

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Asynchronous enumerable which returns found records.</returns>
        IAsyncEnumerable<object> ExecuteAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: false, cancellationToken);

        /// <summary>
        /// Execute Query operation and fetch all attributes from main index when querying a local/global secondary index.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Asynchronous enumerable which returns found records.</returns>
        IAsyncEnumerable<object> ExecuteFetchAllAttributesAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: true, cancellationToken);

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of all found records.</returns>
        Task<IEnumerable<object>> ExecuteAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: false, cancellationToken);

        /// <summary>
        /// Execute Query operation and fetch all attributes from main index when querying a local/global secondary index.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of all found records.</returns>
        Task<IEnumerable<object>> ExecuteFetchAllAttributesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: true, cancellationToken);
    }

    /// <summary>
    /// Interface to specify Query operation.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public interface IDynamoTableQuery<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Filter query results.
        /// </summary>
        /// <param name="filter">A lambda predicate representing the DynamoDB filter expression.</param>
        IDynamoTableQuery<TRecord> Where(Expression<Func<TRecord, bool>> filter);

        /// <summary>
        /// Selects a record property to fetch.
        /// </summary>
        /// <param name="attribute">A lambda expression that returns the record property.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableQuery<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="fetchAllAttributes">Fetch all attributes from main index when querying a local/global secondary index.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Asynchronous enumerable which returns found records.</returns>
        IAsyncEnumerable<TRecord> ExecuteAsyncEnumerable(bool fetchAllAttributes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="fetchAllAttributes">Fetch all attributes from main index when querying a local/global secondary index.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of all found records.</returns>
        Task<IEnumerable<TRecord>> ExecuteAsync(bool fetchAllAttributes, CancellationToken cancellationToken = default);

        //--- Default Methods ---

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Asynchronous enumerable which returns found records.</returns>
        IAsyncEnumerable<TRecord> ExecuteAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: false, cancellationToken);

        /// <summary>
        /// Execute Query operation and fetch all attributes from main index when querying a local/global secondary index.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Asynchronous enumerable which returns found records.</returns>
        IAsyncEnumerable<TRecord> ExecuteFetchAllAttributesAsyncEnumerable(CancellationToken cancellationToken = default) => ExecuteAsyncEnumerable(fetchAllAttributes: true, cancellationToken);

        /// <summary>
        /// Execute Query operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of all found records.</returns>
        Task<IEnumerable<TRecord>> ExecuteAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: false, cancellationToken);

        /// <summary>
        /// Execute Query operation and fetch all attributes from main index when querying a local/global secondary index.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of all found records.</returns>
        Task<IEnumerable<TRecord>> ExecuteFetchAllAttributesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(fetchAllAttributes: true, cancellationToken);
    }
}
