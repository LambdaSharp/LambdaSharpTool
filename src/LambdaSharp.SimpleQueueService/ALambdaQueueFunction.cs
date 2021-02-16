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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using LambdaSharp.Exceptions;
using LambdaSharp.Logging;
using LambdaSharp.Logging.Metrics;
using LambdaSharp.Serialization;
using LambdaSharp.SimpleQueueService.Extensions;

namespace LambdaSharp.SimpleQueueService {

    /// <summary>
    /// The <see cref="ALambdaQueueFunction{TMessage}"/> is the abstract base class for handling
    /// <a href="https://aws.amazon.com/sqs/">Amazon Simple Queue Service (SQS)</a> events.
    /// </summary>
    /// <remarks>
    /// When the Lambda function is declared with a Dead-Letter Queue (DLQ), the function attempts a
    /// failed message up to 3 times, by default. The default can be overridden by setting a different
    /// value for the <c>MAX_QUEUE_RETRIES</c> environment variable. Without a DLQ, the function will
    /// attempt a message indefinitely.
    /// </remarks>
    /// <typeparam name="TMessage">The SQS queue message type.</typeparam>
    public abstract class ALambdaQueueFunction<TMessage> : ALambdaFunction {

        //--- Fields ---
        private SQSEvent.SQSMessage? _currentRecord;

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaQueueFunction{TMessage}"/> instance using the default
        /// implementation of <see cref="ILambdaQueueFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        protected ALambdaQueueFunction(ILambdaJsonSerializer serializer) : this(serializer, provider: null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaQueueFunction{TMessage}"/> instance using a
        /// custom implementation of <see cref="ILambdaQueueFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        /// <param name="provider">Custom implementation of <see cref="ILambdaQueueFunctionDependencyProvider"/>.</param>
        protected ALambdaQueueFunction(ILambdaJsonSerializer serializer, ILambdaQueueFunctionDependencyProvider? provider) : base(provider ?? new LambdaQueueFunctionDependencyProvider()) {
            LambdaSerializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
        }

        //--- Properties ---

        /// <summary>
        /// The <see cref="ILambdaQueueFunctionDependencyProvider"/> instance used by the Lambda function to
        /// satisfy its required dependencies.
        /// </summary>
        /// <value>The <see cref="ILambdaQueueFunctionDependencyProvider"/> instance.</value>
        protected new ILambdaQueueFunctionDependencyProvider Provider => (ILambdaQueueFunctionDependencyProvider)base.Provider;

        /// <summary>
        /// The <see cref="CurrentRecord"/> property holds the SQS queue message record that is currently being processed.
        /// </summary>
        /// <remarks>
        /// This property is only set during the invocation of <see cref="ProcessMessageStreamAsync(Stream)"/>. Otherwise, it returns <c>null</c>.
        /// </remarks>
        /// <value>The <see cref="SQSEvent.SQSMessage"/> instance.</value>
        protected SQSEvent.SQSMessage CurrentRecord => _currentRecord ?? throw new InvalidOperationException();

        /// <summary>
        /// An instance of <see cref="ILambdaJsonSerializer"/> used for serializing/deserializing JSON data.
        /// </summary>
        /// <value>The <see cref="ILambdaJsonSerializer"/> instance.</value>
        protected ILambdaJsonSerializer LambdaSerializer { get; }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ProcessMessageAsync(TMessage)"/> method is invoked for every received SQS queue message.
        /// </summary>
        /// <param name="message">The deserialized SQS queue message.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task ProcessMessageAsync(TMessage message);

        //--- Methods ---

        /// <summary>
        /// The <see cref="Deserialize(string)"/> method converts the SQS queue message from string to a typed instance.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="Amazon.Lambda.Core.ILambdaSerializer.Deserialize{TMessage}(Stream)"/> to convert the SQS queue message string
        /// into a <paramtyperef name="TMessage"/> instance. Override this method to provide a custom message deserialization implementation.
        /// </remarks>
        /// <param name="body">The SQS queue message.</param>
        /// <returns>The deserialized SQS queue message.</returns>
        public virtual TMessage Deserialize(string body) => LambdaSerializer.Deserialize<TMessage>(body);

        /// <summary>
        /// The <see cref="ProcessMessageStreamAsync(Stream)"/> method is overridden to
        /// provide specific behavior for this base class.
        /// </summary>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        /// <param name="stream">The stream with the request payload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public override sealed async Task<Stream> ProcessMessageStreamAsync(Stream stream) {

            // deserialize stream to sqs event
            LogInfo("deserializing stream to SQS event");
            var sqsEvent = LambdaSerializerSettings.LambdaSharpSerializer.Deserialize<SQSEvent>(stream);
            if(!sqsEvent.Records.Any()) {
                return $"empty batch".ToStream();
            }

            // process all received sqs records
            var eventSourceArn = sqsEvent.Records.First().EventSourceArn;
            var successfulMessages = new List<SQSEvent.SQSMessage>();
            foreach(var record in sqsEvent.Records) {
                _currentRecord = record;
                var metrics = new List<LambdaMetric>();
                try {
                    var stopwatch = Stopwatch.StartNew();

                    // attempt to deserialize the sqs record
                    LogInfo("deserializing message");
                    var message = Deserialize(record.Body);

                    // attempt to process the sqs message
                    LogInfo("processing message");
                    await ProcessMessageAsync(message);
                    successfulMessages.Add(record);

                    // record successful processing metrics
                    stopwatch.Stop();
                    var now = DateTimeOffset.UtcNow;
                    metrics.Add(("MessageSuccess.Count", 1, LambdaMetricUnit.Count));
                    metrics.Add(("MessageSuccess.Latency", stopwatch.Elapsed.TotalMilliseconds, LambdaMetricUnit.Milliseconds));
                    metrics.Add(("MessageSuccess.Lifespan", (now - record.GetLifespanTimestamp()).TotalSeconds, LambdaMetricUnit.Seconds));
                } catch(Exception e) {

                    // NOTE (2020-04-21, bjorg): delete message if error is not retriable (i.e. logic error) or
                    //  the message has reached it's maximum number of retries.
                    var deleteMessage = !(e is LambdaRetriableException)
                        || (record.GetApproximateReceiveCount() >= await Provider.GetMaxRetriesForQueueAsync(record.EventSourceArn));

                    // the intent is to delete the message
                    if(deleteMessage) {

                        // NOTE (2020-04-22, bjorg): always log an error since the intent is to send
                        //  this message to the dead-letter queue.
                        LogError(e);
                        try {

                            // attempt to send failed message to the dead-letter queue
                            await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.SQS, LambdaSerializer.Serialize(record), e);

                            // record forwarded message as successful so it gets deleted from the queue
                            successfulMessages.Add(record);

                            // record failed processing metrics
                            metrics.Add(("MessageDead.Count", 1, LambdaMetricUnit.Count));
                        } catch {

                            // record attempted processing metrics
                            metrics.Add(("MessageFailed.Count", 1, LambdaMetricUnit.Count));
                        }
                    } else {

                        // record attempted processing metrics
                        metrics.Add(("MessageFailed.Count", 1, LambdaMetricUnit.Count));

                        // log error as a warning as we expect to see this message again
                        LogErrorAsWarning(e);
                    }
                } finally {
                    _currentRecord = null;
                    LogMetric(metrics);
                }
            }

            // check if any failures occurred
            if((sqsEvent.Records.Count != successfulMessages.Count) && (successfulMessages.Count > 0)) {

                // delete all messages that were successfully processed to avoid them being tried again
                await Provider.DeleteMessagesFromQueueAsync(
                    eventSourceArn,
                    successfulMessages.Select(message =>
                        (MessageId: message.MessageId, ReceiptHandle: message.ReceiptHandle)
                    )
                );

                // fail invocation to prevent messages from being deleted
                throw new LambdaAbortException($"processing failed: {sqsEvent.Records.Count - successfulMessages.Count} errors ({successfulMessages.Count} messages succeeded)");
            }
            return $"processed {successfulMessages.Count} messages".ToStream();
        }
    }
}