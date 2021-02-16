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

using System.Threading;
using System.Threading.Tasks;

namespace LambdaSharp.Finalizer {

    /// <summary>
    /// The <see cref="ILambdaFinalizerDependencyProvider"/> interface provides all the required dependencies
    /// for <see cref="ALambdaFinalizerFunction"/> instances. This interface follows the <i>Dependency Provider</i> pattern
    /// where all side-effecting methods and properties must be provided by an outside implementation.
    /// </summary>
    public interface ILambdaFinalizerDependencyProvider : ILambdaFunctionDependencyProvider {

        //--- Methods ---

        /// <summary>
        /// Checks if the specified stack is currently being deleted.
        /// </summary>
        /// <param name="stackId">CloudFormation stack ID</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Boolean indicating if the specified stack is being deleted.</returns>
        Task<bool> IsStackDeleteInProgressAsync(string stackId, CancellationToken cancellationToken = default);
    }
}