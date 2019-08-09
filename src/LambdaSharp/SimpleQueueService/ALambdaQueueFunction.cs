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
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using LambdaSharp.Exceptions;
using LambdaSharp.Logger;

namespace LambdaSharp.SimpleQueueService {

    /// <summary>
    /// The <see cref="ALambdaQueueFunction{TMessage}"/> is the abstract base class for handling
    /// <a href="https://aws.amazon.com/sqs/">Amazon Simple Queue Service (SQS)</a> events.
    /// </summary>
    /// <typeparam name="TMessage">The SQS queue message type.</typeparam>
    public abstract class ALambdaQueueFunction<TMessage> : ALambdaFunction {

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaQueueFunction{TMessage}"/> instance using the default
        /// implementation of <see cref="ILambdaQueueFunctionDependencyProvider"/>.
        /// </summary>
        protected ALambdaQueueFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaQueueFunction{TMessage}"/> instance using a
        /// custom implementation of <see cref="ILambdaQueueFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaQueueFunctionDependencyProvider"/>.</param>
        protected ALambdaQueueFunction(ILambdaQueueFunctionDependencyProvider provider) : base(provider ?? new LambdaQueueFunctionDependencyProvider()) { }

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
        protected SQSEvent.SQSMessage CurrentRecord { get; private set; }

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
        /// This method invokes <see cref="ALambdaFunction.DeserializeJson{TMessage}(string)"/> to convert the SQS queue message string
        /// into a <paramtyperef name="TMessage"/> instance. Override this method to provide a custom message deserialization implementation.
        /// </remarks>
        /// <param name="body">The SQS queue message.</param>
        /// <returns>The deserialized SQS queue message.</returns>
        public virtual TMessage Deserialize(string body) => DeserializeJson<TMessage>(body);

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
            var failureCounter = 0;

            // deserialize stream to sqs event
            LogInfo("deserializing stream to SQS event");
            var sqsEvent = DeserializeJson<SQSEvent>(stream);

            // process all received sqs records
            var successfulMessages = new List<SQSEvent.SQSMessage>();
            foreach(var sqsRecord in sqsEvent.Records) {
                CurrentRecord = sqsRecord;
                try {

                    // attempt to deserialize the sqs record
                    LogInfo("deserializing message");
                    var message = Deserialize(sqsRecord.Body);

                    // attempt to process the sqs message
                    LogInfo("processing message");
                    await ProcessMessageAsync(message);
                    successfulMessages.Add(sqsRecord);
                } catch(LambdaRetriableException e) {

                    // record error as warning; function will need to fail to prevent deletion
                    LogErrorAsWarning(e);
                    ++failureCounter;
                } catch(Exception e) {
                    LogError(e);

                    // send straight to the dead letter queue and prevent from re-trying
                    try {
                        await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.SQS, SerializeJson(sqsRecord), e);
                        successfulMessages.Add(sqsRecord);
                    } catch {

                        // no dead-letter queue configured; function will need to fail to prevent deletion
                        ++failureCounter;
                    }
                } finally {
                    CurrentRecord = null;
                }
            }

            // check if any failures occurred
            if(failureCounter > 0) {

                // delete all messages that were successfully processed to avoid them being tried again
                if(successfulMessages.Count > 0) {
                    await Provider.DeleteMessagesFromQueueAsync(
                        AwsConverters.ConvertQueueArnToUrl(successfulMessages.First().EventSourceArn),
                        successfulMessages.Select(message =>
                            (MessageId: message.MessageId, ReceiptHandle: message.ReceiptHandle)
                        )
                    );
                }

                // fail invocation to prevent messages from being deleted
                throw new LambdaAbortException($"processing failed: {failureCounter} errors ({successfulMessages.Count} messages succeeded)");
            }
            return $"processed {successfulMessages.Count} messages".ToStream();
        }
    }
}