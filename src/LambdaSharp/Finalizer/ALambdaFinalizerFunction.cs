/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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

using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp.CustomResource;

namespace LambdaSharp.Finalizer {

    /// <summary>
    /// The <see cref="ALambdaFinalizerFunction"/> is the abstract base class for implementing a LambdaSharp module <i>Finalizer</i>.
    /// The <i>Finalizer</i> is a CloudFormation custom resource that is created after all other resources in the LambdaSharp module
    /// have been created. The <i>Finalizer</i> is used to perform custom logic when deploying, creating, or tearing down a
    /// LambdaSharp module.
    /// </summary>
    public abstract class ALambdaFinalizerFunction : ALambdaCustomResourceFunction<FinalizerProperties, FinalizerAttributes> {

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaFinalizerFunction"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        protected ALambdaFinalizerFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaFinalizerFunction"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaFinalizerFunction(ILambdaFunctionDependencyProvider provider) : base(provider) { }

        //--- Methods ---

        /// <summary>
        /// The <see cref="CreateDeployment(FinalizerProperties)"/> method is invoked when the LambdaSharp module is first created.
        /// </summary>
        /// <param name="request">The <see cref="FinalizerProperties"/> instance with the new deployment information.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task CreateDeployment(FinalizerProperties request) { }

        /// <summary>
        /// The <see cref="CreateDeployment(FinalizerProperties)"/> method is invoked when the LambdaSharp module is being updated.
        /// </summary>
        /// <param name="next">The <see cref="FinalizerProperties"/> instance with the next deployment information.</param>
        /// <param name="previous">The <see cref="FinalizerProperties"/> instance with the previous deployment information.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task UpdateDeployment(FinalizerProperties next, FinalizerProperties previous) { }

        /// <summary>
        /// The <see cref="CreateDeployment(FinalizerProperties)"/> method is invoked when the LambdaSharp module is being torn down.
        /// </summary>
        /// <param name="current">The <see cref="FinalizerProperties"/> instance with the current deployment information.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task DeleteDeployment(FinalizerProperties current) { }

        /// <inheritdoc/>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        public override sealed async Task<Response<FinalizerAttributes>> ProcessCreateResourceAsync(Request<FinalizerProperties> request) {
            await CreateDeployment(request.ResourceProperties);
            return new Response<FinalizerAttributes> {
                PhysicalResourceId = $"Finalizer:{request.ResourceProperties.DeploymentChecksum}",
                Attributes = new FinalizerAttributes()
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        public override sealed async Task<Response<FinalizerAttributes>> ProcessDeleteResourceAsync(Request<FinalizerProperties> request) {
            await DeleteDeployment(request.ResourceProperties);
            return new Response<FinalizerAttributes>();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        public override sealed async Task<Response<FinalizerAttributes>> ProcessUpdateResourceAsync(Request<FinalizerProperties> request) {
            await UpdateDeployment(request.ResourceProperties, request.OldResourceProperties);
            return new Response<FinalizerAttributes> {
                PhysicalResourceId = $"Finalizer:{request.OldResourceProperties.DeploymentChecksum}",
                Attributes = new FinalizerAttributes()
            };
        }
    }
}