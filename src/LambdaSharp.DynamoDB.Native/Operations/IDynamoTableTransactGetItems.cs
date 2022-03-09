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
    /// Interface to specify the TransactGetItems operation with mixed record types.
    /// </summary>
    public interface IDynamoTableTransactGetItems {

        //--- Methods ---

        /// <summary>
        /// Begin specification of a GetItem operation for TransactGetItems.
        /// </summary>
        /// <param name="primaryKey">Primary key of the item to retrieve.</param>
        /// <param name="consistentRead">Boolean indicating if the read operation should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        IDynamoTableTransactGetItemsBegin<TRecord> BeginGetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class;

        /// <summary>
        /// Attempts the TransactGetItems operation.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of attempts with exponential back when encountering provisioned throughput is exceeded.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A tuple indicating the success of the transatcion and the list of found items when successful.</returns>
        Task<(bool Success, IEnumerable<object> Items)> TryExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);

        //--- Default Methods ---

        /// <summary>
        /// Add a GetItem operation to TransactGetItems that retrieves all attributes for the given primary key.
        ///
        /// This method is the same: <c>BeginGetItem(primaryKey, consistentRead).End()</c>.
        /// </summary>
        /// <param name="primaryKey">Primary key of the item to retrieve.</param>
        /// <param name="consistentRead">Boolean indicating if the read operation should be performed against the main partition (2x cost compared to eventual consistent read).</param>
        /// <typeparam name="TRecord">The record type.</typeparam>
        IDynamoTableTransactGetItems GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class
            => BeginGetItem(primaryKey, consistentRead).End();
    }

    /// <summary>
    /// Interface to specify a typed GetItem operation for TranactGetItems with mixed record types.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public interface IDynamoTableTransactGetItemsBegin<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Selects a record property to fetch.
        /// </summary>
        /// <param name="attribute">A lambda expression that returns the record property.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableTransactGetItemsBegin<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);

        /// <summary>
        /// End specification of the GetItem operation for TransactGetItems.
        /// </summary>
        IDynamoTableTransactGetItems End();
    }

    /// <summary>
    /// Interface to specify the TransactGetItems operation for a specific record type.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public interface IDynamoTableTransactGetItems<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Selects a record property to fetch.
        /// </summary>
        /// <param name="attribute">A lambda expression that returns the record property.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableTransactGetItems<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);

        /// <summary>
        /// Attempts the TransactGetItems operation.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of attempts with exponential back when encountering provisioned throughput is exceeded.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A tuple indicating the success of the transatcion and the list of found items when successful.</returns>
        Task<(bool Success, IEnumerable<TRecord> Items)> TryExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);
    }
}
