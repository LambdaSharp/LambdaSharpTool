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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using LambdaSharp.ConfigSource;

namespace LambdaSharp.SimpleQueueService {

    /// <summary>
    /// The <see cref="LambdaQueueFunctionDependencyProvider"/> class provides all the default, runtime dependencies
    /// for <see cref="ALambdaQueueFunction{TMessage}"/> instances.
    /// </summary>
    public class LambdaQueueFunctionDependencyProvider : LambdaFunctionDependencyProvider, ILambdaQueueFunctionDependencyProvider {

        //--- Constructors ---

        /// <summary>
        /// Creates new instance of <see cref="LambdaQueueFunctionDependencyProvider"/>, which provides the implementation for the required dependencies for <see cref="ALambdaQueueFunction{TMessage}"/>.
        /// </summary>
        /// <param name="utcNowCallback">A function that return the current <c>DateTime</c> in UTC timezone. Defaults to <see cref="DateTime.UtcNow"/> when <c>null</c>.</param>
        /// <param name="logCallback">An action that logs a string message. Defaults to <see cref="LambdaLogger.Log"/> when <c>null</c>.</param>
        /// <param name="configSource">A <see cref="ILambdaConfigSource"/> instance from which the Lambda function configuration is read. Defaults to <see cref="LambdaSystemEnvironmentSource"/> instance when <c>null</c>.</param>
        /// <param name="jsonSerializer">A <see cref="ILambdaSerializer"/> instance for serializing and deserializing JSON data. Defaults to <see cref="LambdaLogger.Log"/> when <c>null</c>.</param>
        /// <param name="kmsClient">A <see cref="IAmazonKeyManagementService"/> client instance. Defaults to <see cref="AmazonKeyManagementServiceClient"/> when <c>null</c>.</param>
        /// <param name="sqsClient">A <see cref="IAmazonSQS"/> client instance. Defaults to <see cref="AmazonSQSClient"/> when <c>null</c>.</param>
        public LambdaQueueFunctionDependencyProvider(
            Func<DateTime> utcNowCallback = null,
            Action<string> logCallback = null,
            ILambdaConfigSource configSource = null,
            ILambdaSerializer jsonSerializer = null,
            IAmazonKeyManagementService kmsClient = null,
            IAmazonSQS sqsClient = null
        ) : base(utcNowCallback, logCallback, configSource, jsonSerializer, kmsClient, sqsClient) { }

        //--- Methods ---

        /// <inheritdoc/>
        public Task DeleteMessagesFromQueueAsync(string queueUrl, IEnumerable<(string MessageId, string ReceiptHandle)> messages) {
            return SqsClient.DeleteMessageBatchAsync(new DeleteMessageBatchRequest {
                QueueUrl = queueUrl,
                Entries = messages.Select(message =>
                    new DeleteMessageBatchRequestEntry(message.MessageId, message.ReceiptHandle)
                ).ToList()
            });
        }
    }
}