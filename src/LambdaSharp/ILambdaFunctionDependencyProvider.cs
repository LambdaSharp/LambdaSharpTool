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
using System.Threading;
using System.Threading.Tasks;
using LambdaSharp.ConfigSource;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="ILambdaFunctionDependencyProvider"/> interface provides all the required dependencies
    /// for <see cref="ALambdaFunction"/> instances. This interface follows the <i>Dependency Provider</i> pattern
    /// where all side-effecting methods and properties must be provided by an outside implementation.
    /// </summary>
    public interface ILambdaFunctionDependencyProvider {

        //--- Properties ---

        /// <summary>
        /// Retrieves the current date-time in UTC timezone.
        /// </summary>
        /// <returns>Current <see cref="DateTime"/> in UTC timezone</returns>
        DateTime UtcNow { get; }

        /// <summary>
        /// Retrieves the <see cref="ILambdaConfigSource"/> instance used for initializing the Lambda function.
        /// </summary>
        /// <value>The <see cref="ILambdaConfigSource"/> instance.</value>
        ILambdaConfigSource ConfigSource { get; }

        /// <summary>
        /// The <see cref="DebugLoggingEnabled"/> property indicates if the requests received and responses emitted
        /// by this Lambda function should be shown in the CloudWatch logs. This can be useful to determine check for
        /// issues caused by inconsistencies in serialization or deserialization.
        /// </summary>
        /// <value>Boolean indicating if requests and responses are logged</value>
        bool DebugLoggingEnabled { get; }

        //--- Methods --

        /// <summary>
        /// Write a message to the log stream. In production, this should be the CloudWatch log associated with
        /// the Lambda function.
        /// </summary>
        /// <param name="message">Message to write to the log stream.</param>
        void Log(string message);

        /// <summary>
        /// Decrypt a sequence of bytes with an optional encryption context. The Lambda function
        /// requires permission to use the <c>kms:Decrypt</c> operation on the KMS key used to
        /// encrypt the original message.
        /// </summary>
        /// <param name="secretBytes">Array containing the encrypted bytes.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<byte[]> DecryptSecretAsync(byte[] secretBytes, Dictionary<string, string>? encryptionContext = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Encrypt a sequence of bytes using the specified KMS key. The Lambda function requires
        /// permission to use the <c>kms:Encrypt</c> opeartion on the specified KMS key.
        /// </summary>
        /// <param name="plaintextBytes">Array containing plaintext byte to encrypt.</param>
        /// <param name="encryptionKeyId">The KMS key ID used encrypt the plaintext bytes.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<byte[]> EncryptSecretAsync(byte[] plaintextBytes, string encryptionKeyId, Dictionary<string, string>? encryptionContext = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to the specified SQS queue. The Lambda function requires <c>sqs:SendMessage</c> permission
        /// on the specified SQS queue.
        /// </summary>
        /// <param name="queueUrl">The SQS queue URL.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messageAttributes">Optional attributes for the message.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task SendMessageToQueueAsync(string queueUrl, string message, IEnumerable<KeyValuePair<string, string>>? messageAttributes = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event will be forwarded to the default EventBridge by LambdaSharp.Core (requires Core Services to be enabled).
        /// </summary>
        /// <param name="timestamp">The timestamp of the event.</param>
        /// <param name="eventbus">The event bus that will receive the event.</param>
        /// <param name="source">The source application of the event.</param>
        /// <param name="detailType">Free-form string used to decide what fields to expect in the event detail.</param>
        /// <param name="detail">Optional data-structure serialized as JSON string. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task SendEventAsync(DateTimeOffset timestamp, string eventbus, string source, string detailType, string detail, IEnumerable<string> resources, CancellationToken cancellationToken = default);
    }
}
