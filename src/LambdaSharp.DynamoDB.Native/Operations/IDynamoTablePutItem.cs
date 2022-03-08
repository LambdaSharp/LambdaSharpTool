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
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Native.Operations {

    /// <summary>
    /// Interface to specify a PutItem operation.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public interface IDynamoTablePutItem<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Add condition for PutItem operation.
        /// </summary>
        /// <param name="condition">A lambda predicate representing the DynamoDB condition expression.</param>
        IDynamoTablePutItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);

        /// <summary>
        /// Set the value of a DynamoDB item attribute. Used for storing attributes used by local/global secondary indices.
        /// </summary>
        /// <param name="key">Name of attribute.</param>
        /// <param name="value">Value of attribute.</param>
        IDynamoTablePutItem<TRecord> Set(string key, AttributeValue value);

        /// <summary>
        /// Execute the PutItem operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>True, when successful. False, when condition is not met.</returns>
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the PutItem operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Old record when found. <c>null</c>, otherwise.</returns>
        Task<TRecord?> ExecuteReturnOldItemAsync(CancellationToken cancellationToken = default);

        //--- Default Methods ---

        /// <summary>
        /// Set the value of a DynamoDB item attribute. Used for storing attributes used by local/global secondary indices.
        /// </summary>
        /// <param name="key">Name of attribute.</param>
        /// <param name="value">Value of attribute.</param>
        IDynamoTablePutItem<TRecord> Set(string key, string value)
            => Set(key, new AttributeValue(value));
    }
}
