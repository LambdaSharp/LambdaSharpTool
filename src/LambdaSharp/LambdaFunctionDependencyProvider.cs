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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Lambda.Core;
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
        private readonly bool _debugLoggingEnabled;

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="LambdaFunctionDependencyProvider"/>, which provides the implementation for the required dependencies for <see cref="ALambdaFunction"/>.
        /// </summary>
        /// <param name="utcNowCallback">A function that return the current <c>DateTime</c> in UTC timezone. Defaults to <see cref="DateTime.UtcNow"/> when <c>null</c>.</param>
        /// <param name="logCallback">An action that logs a string message. Defaults to <see cref="LambdaLogger.Log"/> when <c>null</c>.</param>
        /// <param name="configSource">A <see cref="ILambdaConfigSource"/> instance from which the Lambda function configuration is read. Defaults to <see cref="LambdaSystemEnvironmentSource"/> instance when <c>null</c>.</param>
        /// <param name="kmsClient">A <see cref="IAmazonKeyManagementService"/> client instance. Defaults to <see cref="AmazonKeyManagementServiceClient"/> when <c>null</c>.</param>
        /// <param name="sqsClient">A <see cref="IAmazonSQS"/> client instance. Defaults to <see cref="AmazonSQSClient"/> when <c>null</c>.</param>
        /// <param name="eventsClient">A <see cref="IAmazonCloudWatchEvents"/> client instance. Defaults to <see cref="AmazonCloudWatchEventsClient"/> when <c>null</c>.</param>
        /// <param name="debugLoggingEnabled">A boolean indicating if debug logging is enabled.</param>
        public LambdaFunctionDependencyProvider(
            Func<DateTime>? utcNowCallback = null,
            Action<string>? logCallback = null,
            ILambdaConfigSource? configSource = null,
            IAmazonKeyManagementService? kmsClient = null,
            IAmazonSQS? sqsClient = null,
            IAmazonCloudWatchEvents? eventsClient = null,
            bool? debugLoggingEnabled = null
        ) {
            _nowCallback = utcNowCallback ?? (() => DateTime.UtcNow);
            _logCallback = logCallback ?? LambdaLogger.Log;
            ConfigSource = configSource ?? new LambdaSystemEnvironmentSource();
            KmsClient = kmsClient ?? new AmazonKeyManagementServiceClient();
            SqsClient = sqsClient ?? new AmazonSQSClient();
            EventsClient = eventsClient ?? new AmazonCloudWatchEventsClient();

            // determine if debug logging is enabled
            if(debugLoggingEnabled.HasValue) {
                _debugLoggingEnabled = debugLoggingEnabled.Value;
            } else {

                // read environment variable to determine if request/response messages should be serialized to the log for debugging purposes
                var value = System.Environment.GetEnvironmentVariable("DEBUG_LOGGING_ENABLED") ?? "false";
                _debugLoggingEnabled = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        //--- Properties ---

        /// <summary>
        /// Retrieves the current date-time in UTC timezone.
        /// </summary>
        /// <returns>Current <see cref="DateTime"/> in UTC timezone</returns>
        public DateTime UtcNow => _nowCallback();

        /// <summary>
        /// Retrieves the <see cref="ILambdaConfigSource"/> instance used for initializing the Lambda function.
        /// </summary>
        /// <value>The <see cref="ILambdaConfigSource"/> instance.</value>
        public ILambdaConfigSource ConfigSource { get; }

        /// <summary>
        /// Retrieves the <see cref="IAmazonKeyManagementService"/> instance used for communicating with the
        /// <a href="https://aws.amazon.com/kms/">AWS Key Management Service (KMS)</a> service.
        /// </summary>
        public IAmazonKeyManagementService KmsClient { get; }

        /// <summary>
        /// Retrieves the <see cref="IAmazonSQS"/> instance used for communicating with the
        /// <a href="https://aws.amazon.com/sqs/">Amazon Simple Queue Service (SQS)</a> service.
        /// </summary>
        public IAmazonSQS SqsClient { get; }

        /// <summary>
        /// Retrieves the <see cref="IAmazonCloudWatchEvents"/> instance used for communicating with
        /// <a href="https://aws.amazon.com/eventbridge/">Amazon EventBridge</a> service.
        /// </summary>
        public IAmazonCloudWatchEvents EventsClient { get; }

        /// <summary>
        /// The <see cref="DebugLoggingEnabled"/> property indicates if debug log entries should be emitted
        /// to CloudWatch logs.
        /// </summary>
        public bool DebugLoggingEnabled => _debugLoggingEnabled;

        //--- Methods ---

        /// <summary>
        /// Write a message to the log stream. In production, this should be the CloudWatch log associated with
        /// the Lambda function.
        /// </summary>
        /// <param name="message">Message to write to the log stream.</param>
        public virtual void Log(string message) => _logCallback(message);

        /// <summary>
        /// Decrypt a sequence of bytes with an optional encryption context. The Lambda function
        /// requires permission to use the <c>kms:Decrypt</c> operation on the KMS key used to
        /// encrypt the original message.
        /// </summary>
        /// <param name="secretBytes">Array containing the encrypted bytes.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task<byte[]> DecryptSecretAsync(byte[] secretBytes, Dictionary<string, string>? encryptionContext, CancellationToken cancellationToken = default)
            => (await KmsClient.DecryptAsync(new DecryptRequest {
                    CiphertextBlob = new MemoryStream(secretBytes),
                    EncryptionContext = encryptionContext
                }, cancellationToken)).Plaintext.ToArray();

        /// <summary>
        /// Encrypt a sequence of bytes using the specified KMS key. The Lambda function requires
        /// permission to use the <c>kms:Encrypt</c> opeartion on the specified KMS key.
        /// </summary>
        /// <param name="plaintextBytes">Array containing plaintext byte to encrypt.</param>
        /// <param name="encryptionKeyId">The KMS key ID used encrypt the plaintext bytes.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task<byte[]> EncryptSecretAsync(byte[] plaintextBytes, string encryptionKeyId, Dictionary<string, string>? encryptionContext, CancellationToken cancellationToken)
            => (await KmsClient.EncryptAsync(new EncryptRequest {
                    KeyId = encryptionKeyId,
                    Plaintext = new MemoryStream(plaintextBytes),
                    EncryptionContext = encryptionContext
                }, cancellationToken)).CiphertextBlob.ToArray();

        /// <summary>
        /// Send a message to the specified SQS queue. The Lambda function requires <c>sqs:SendMessage</c> permission
        /// on the specified SQS queue.
        /// </summary>
        /// <param name="deadLetterQueueUrl">The SQS queue URL.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messageAttributes">Optional attributes for the message.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task SendMessageToQueueAsync(string deadLetterQueueUrl, string message, IEnumerable<KeyValuePair<string, string>>? messageAttributes, CancellationToken cancellationToken)
            => SqsClient.SendMessageAsync(new SendMessageRequest {
                QueueUrl = deadLetterQueueUrl,
                MessageBody = message,
                MessageAttributes = messageAttributes?.ToDictionary(kv => kv.Key, kv => new MessageAttributeValue {
                    DataType = "String",
                    StringValue = kv.Value
                }) ?? new Dictionary<string, MessageAttributeValue>()
            }, cancellationToken);

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
        public virtual Task SendEventAsync(DateTimeOffset timestamp, string eventbus, string source, string detailType, string detail, IEnumerable<string> resources, CancellationToken cancellationToken)
            => EventsClient.PutEventsAsync(new PutEventsRequest {
                Entries = {
                    new PutEventsRequestEntry {
                        Time = timestamp.UtcDateTime,
                        EventBusName =  eventbus,
                        Source = source,
                        DetailType = detailType,
                        Detail = detail,
                        Resources = resources?.ToList() ?? new List<string>()
                    }
                }
            }, cancellationToken);
    }
}
