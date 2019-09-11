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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using LambdaSharp.ConfigSource;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="LambdaFunctionDependencyProvider"/> class provides all the default, runtime dependencies
    /// for <see cref="ALambdaFunction"/> instances. Classes derived from <see cref="ALambdaFunction"/> that
    /// require additional dependencies should create their own <i>Dependency Provider</i> interface, derived from
    /// <see cref="ILambdaFunctionDependencyProvider"/> with a companion implementation class derived from
    /// <see cref="LambdaFunctionDependencyProvider"/>.
    ///
    /// This separation of concerns makes it simple to create isolated unit tests to validate the behavior of
    /// the Lambda function implementation. For convenience sake, classes derived from <see cref="ALambdaFunction"/>
    /// should provide both an empty constructor that instantiate the default implementation and a constructor with
    /// that takes an instance implementing the required <i>Dependency Provider</i> interface.
    /// </summary>
    public class LambdaFunctionDependencyProvider : ILambdaFunctionDependencyProvider {

        //--- Fields ---
        private readonly Func<DateTime> _nowCallback;
        private readonly Action<string> _logCallback;

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="LambdaFunctionDependencyProvider"/>, which provides the implementation for the required dependencies for <see cref="ALambdaFunction"/>.
        /// </summary>
        /// <param name="utcNowCallback">A function that return the current <c>DateTime</c> in UTC timezone. Defaults to <see cref="DateTime.UtcNow"/> when <c>null</c>.</param>
        /// <param name="logCallback">An action that logs a string message. Defaults to <see cref="LambdaLogger.Log"/> when <c>null</c>.</param>
        /// <param name="configSource">A <see cref="ILambdaConfigSource"/> instance from which the Lambda function configuration is read. Defaults to <see cref="LambdaSystemEnvironmentSource"/> instance when <c>null</c>.</param>
        /// <param name="jsonSerializer">A <see cref="ILambdaSerializer"/> instance for serializing and deserializing JSON data. Defaults to <see cref="LambdaLogger.Log"/> when <c>null</c>.</param>
        /// <param name="kmsClient">A <see cref="IAmazonKeyManagementService"/> client instance. Defaults to <see cref="AmazonKeyManagementServiceClient"/> when <c>null</c>.</param>
        /// <param name="sqsClient">A <see cref="IAmazonSQS"/> client instance. Defaults to <see cref="AmazonSQSClient"/> when <c>null</c>.</param>
        public LambdaFunctionDependencyProvider(
            Func<DateTime> utcNowCallback = null,
            Action<string> logCallback = null,
            ILambdaConfigSource configSource = null,
            ILambdaSerializer jsonSerializer = null,
            IAmazonKeyManagementService kmsClient = null,
            IAmazonSQS sqsClient = null
        ) {
            _nowCallback = utcNowCallback ?? (() => DateTime.UtcNow);
            _logCallback = logCallback ?? LambdaLogger.Log;
            ConfigSource = configSource ?? new LambdaSystemEnvironmentSource();
            JsonSerializer = jsonSerializer ?? new JsonSerializer();
            KmsClient = kmsClient ?? new AmazonKeyManagementServiceClient();
            SqsClient = sqsClient ?? new AmazonSQSClient();
        }

        //--- Properties ---

        /// <inheritdoc/>
        public DateTime UtcNow => _nowCallback();

        /// <inheritdoc/>
        public ILambdaConfigSource ConfigSource { get; private set; }

        /// <inheritdoc/>
        public ILambdaSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Retrieves the <see cref="IAmazonKeyManagementService"/> instance used for communicating with the
        /// <a href="https://aws.amazon.com/kms/">AWS Key Management Service (KMS)</a> service.
        /// </summary>
        public IAmazonKeyManagementService KmsClient { get; private set; }

        /// <summary>
        /// Retrieves the <see cref="IAmazonSQS"/> instance used for communicating with the
        /// <a href="https://aws.amazon.com/sqs/">Amazon Simple Queue Service (SQS)</a> service.
        /// </summary>
        public IAmazonSQS SqsClient { get; private set; }

        //--- Methods ---

        /// <inheritdoc/>
        public virtual void Log(string message) => _logCallback(message);

        /// <inheritdoc/>
        public virtual async Task<byte[]> DecryptSecretAsync(byte[] secretBytes, Dictionary<string, string> encryptionContext)
            => (await KmsClient.DecryptAsync(new DecryptRequest {
                    CiphertextBlob = new MemoryStream(secretBytes),
                    EncryptionContext = encryptionContext
                })).Plaintext.ToArray();

        /// <inheritdoc/>
        public virtual async Task<byte[]> EncryptSecretAsync(byte[] plaintextBytes, string encryptionKeyId, Dictionary<string, string> encryptionContext)
            => (await KmsClient.EncryptAsync(new EncryptRequest {
                    KeyId = encryptionKeyId,
                    Plaintext = new MemoryStream(plaintextBytes),
                    EncryptionContext = encryptionContext
                })).CiphertextBlob.ToArray();

        /// <inheritdoc/>
        public virtual Task SendMessageToQueueAsync(string deadLetterQueueUrl, string message, IEnumerable<KeyValuePair<string, string>> messageAttributes)
            => SqsClient.SendMessageAsync(new SendMessageRequest {
                QueueUrl = deadLetterQueueUrl,
                MessageBody = message,
                MessageAttributes = messageAttributes?.ToDictionary(kv => kv.Key, kv => new MessageAttributeValue {
                    DataType = "String",
                    StringValue = kv.Value
                }) ?? new Dictionary<string, MessageAttributeValue>()
            });
    }
}
