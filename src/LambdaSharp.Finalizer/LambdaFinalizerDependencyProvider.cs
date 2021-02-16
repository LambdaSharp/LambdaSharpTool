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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.KeyManagementService;
using Amazon.Lambda.Core;
using Amazon.SQS;
using LambdaSharp.ConfigSource;

namespace LambdaSharp.Finalizer {

    /// <summary>
    /// The <see cref="LambdaFinalizerDependencyProvider"/> class provides all the default, runtime dependencies
    /// for <see cref="ALambdaFinalizerFunction"/> instances.
    /// </summary>
    public class LambdaFinalizerDependencyProvider : LambdaFunctionDependencyProvider, ILambdaFinalizerDependencyProvider {

        //--- Fields ---
        private readonly IAmazonCloudFormation _cloudFormationClient;

        //--- Constructors ---

        /// <summary>
        /// Creates new instance of <see cref="LambdaFinalizerDependencyProvider"/>, which provides the implementation for the required dependencies for <see cref="ALambdaFinalizerFunction"/>.
        /// </summary>
        /// <param name="utcNowCallback">A function that return the current <c>DateTime</c> in UTC timezone. Defaults to <see cref="DateTime.UtcNow"/> when <c>null</c>.</param>
        /// <param name="logCallback">An action that logs a string message. Defaults to <see cref="LambdaLogger.Log"/> when <c>null</c>.</param>
        /// <param name="configSource">A <see cref="ILambdaConfigSource"/> instance from which the Lambda function configuration is read. Defaults to <see cref="LambdaSystemEnvironmentSource"/> instance when <c>null</c>.</param>
        /// <param name="kmsClient">A <see cref="IAmazonKeyManagementService"/> client instance. Defaults to <see cref="AmazonKeyManagementServiceClient"/> when <c>null</c>.</param>
        /// <param name="sqsClient">A <see cref="IAmazonSQS"/> client instance. Defaults to <see cref="AmazonSQSClient"/> when <c>null</c>.</param>
        /// <param name="cloudFormationClient">A <see cref="IAmazonCloudFormation"/> client instance. Defaults to <see cref="AmazonCloudFormationClient"/> when <c>null</c>.</param>
        public LambdaFinalizerDependencyProvider(
            Func<DateTime>? utcNowCallback = null,
            Action<string>? logCallback = null,
            ILambdaConfigSource? configSource = null,
            IAmazonKeyManagementService? kmsClient = null,
            IAmazonSQS? sqsClient = null,
            IAmazonCloudFormation? cloudFormationClient = null
        ) : base(utcNowCallback, logCallback, configSource, kmsClient, sqsClient) {
            _cloudFormationClient = cloudFormationClient ?? new AmazonCloudFormationClient();
        }

        //--- Methods ---

        /// <summary>
        /// Checks if the specified stack is currently being deleted.
        /// </summary>
        /// <param name="stackId">CloudFormation stack ID</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Boolean indicating if the specified stack is being deleted.</returns>
        public async Task<bool> IsStackDeleteInProgressAsync(string stackId, CancellationToken cancellationToken) {
            var stack = (await _cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest {
                StackName = stackId
            }, cancellationToken)).Stacks.FirstOrDefault();
            return stack?.StackStatus == "DELETE_IN_PROGRESS";
        }
    }
}