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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LambdaSharp.DynamoDB.Native.Operations {

    /// <summary>
    /// Interface to specify the GetItem operation.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public interface IDynamoTableGetItem<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Selects a record property to fetch.
        /// </summary>
        /// <param name="attribute">A lambda expression that returns the record property.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableGetItem<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);

        /// <summary>
        /// Execute the GetItem operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The record when found and condition is met. <c>null</c>, otherwise.</returns>
        Task<TRecord?> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
