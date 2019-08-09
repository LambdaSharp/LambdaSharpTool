/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
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

        //--- Constants ---
        private const string FINALIZER_PHYSICAL_ID = "Finalizer:Module";

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
                PhysicalResourceId = FINALIZER_PHYSICAL_ID,
                Attributes = new FinalizerAttributes()
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        public override sealed async Task<Response<FinalizerAttributes>> ProcessDeleteResourceAsync(Request<FinalizerProperties> request) {

            // check if old naming scheme is used for the physical id of the finalizer
            if(request.PhysicalResourceId != FINALIZER_PHYSICAL_ID) {

                // NOTE (2019-07-11, bjorg): in 0.7, the physical id was changed from being based on the module hash (checksum), to
                //  a constant identifier. Changing the physical id of a custom resource causes CloudFormation to perform
                //  a replacement operation, which deletes the old custom resource. That would cause the finalizer to perform
                //  its clean-up operation when the stack is not being deleted. Instead, we need to check if the stack itself
                //  is being deleted to confirm the proper invocation of the finalizer.

                // fetch status of stack to confirm this is a delete operation
                try {
                    var stack = (await new AmazonCloudFormationClient().DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = request.StackId
                    })).Stacks.FirstOrDefault();
                    if((stack != null) && (stack.StackStatus != "DELETE_IN_PROGRESS")) {

                        // ignore finalizer delete if stack is not being deleted
                        LogInfo("skipping finalizer delete, because the stack is not being deleted");
                        return new Response<FinalizerAttributes>();
                    }
                } catch(Exception e) {
                    LogErrorAsInfo(e, "unable to describe stack {0} to determine if an update or delete operation is being performed", request.StackId);
                }
            }
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
                PhysicalResourceId = FINALIZER_PHYSICAL_ID,
                Attributes = new FinalizerAttributes()
            };
        }
    }
}