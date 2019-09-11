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
using System.Threading.Tasks;
using Amazon.Lambda.Core;
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
        /// Retrieves the <see cref="ILambdaSerializer"/> instance used for serializing/deserializing JSON data.
        /// </summary>
        /// <value>The <see cref="ILambdaSerializer"/> instance.</value>
        ILambdaSerializer JsonSerializer { get; }

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
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<byte[]> DecryptSecretAsync(byte[] secretBytes, Dictionary<string, string> encryptionContext = null);

        /// <summary>
        /// Encrypt a sequence of bytes using the specified KMS key. The Lambda function requires
        /// permission to use the <c>kms:Encrypt</c> opeartion on the specified KMS key.
        /// </summary>
        /// <param name="plaintextBytes">Array containing plaintext byte to encrypt.</param>
        /// <param name="encryptionKeyId">The KMS key ID used encrypt the plaintext bytes.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<byte[]> EncryptSecretAsync(byte[] plaintextBytes, string encryptionKeyId = null, Dictionary<string, string> encryptionContext = null);

        /// <summary>
        /// Send a message to the specified SQS queue. The Lambda function requires <c>sqs:SendMessage</c> permission
        /// on the specified SQS queue.
        /// </summary>
        /// <param name="queueUrl">The SQS queue URL.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messageAttributes">Optional attributes for the message.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task SendMessageToQueueAsync(string queueUrl, string message, IEnumerable<KeyValuePair<string, string>> messageAttributes = null);
    }
}
