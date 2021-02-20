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
using Amazon.Lambda.SNSEvents;
using LambdaSharp.Logging;
using LambdaSharp.Logging.Metrics;
using LambdaSharp.Serialization;
using LambdaSharp.SimpleNotificationService.Extensions;

namespace LambdaSharp.SimpleNotificationService {

    /// <summary>
    /// The <see cref="ALambdaTopicFunction{TMessage}"/> is the abstract base class for handling
    /// <a href="https://aws.amazon.com/sns/">Amazon Simple Notification Service (SNS)</a> events.
    /// </summary>
    /// <typeparam name="TMessage">The SNS topic message type.</typeparam>
    public abstract class ALambdaTopicFunction<TMessage> : ALambdaFunction {

        //--- Fields ---
        private SNSEvent.SNSMessage? _currentRecord;

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaTopicFunction{TMessage}"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        protected ALambdaTopicFunction(ILambdaJsonSerializer serializer) : this(serializer, provider: null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaTopicFunction{TMessage}"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaTopicFunction(ILambdaJsonSerializer serializer, ILambdaFunctionDependencyProvider? provider) : base(provider) {
            LambdaSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        //--- Properties ---

        /// <summary>
        /// The <see cref="CurrentRecord"/> property holds the SNS message record that is currently being processed.
        /// </summary>
        /// <remarks>
        /// This property is only set during the invocation of <see cref="ProcessMessageAsync(TMessage)"/>. Otherwise, it returns <c>null</c>.
        /// </remarks>
        /// <value>The <see cref="SNSEvent.SNSMessage"/> instance.</value>
        protected SNSEvent.SNSMessage CurrentRecord => _currentRecord ?? throw new InvalidOperationException();

        /// <summary>
        /// An instance of <see cref="ILambdaJsonSerializer"/> used for serializing/deserializing JSON data.
        /// </summary>
        /// <value>The <see cref="ILambdaJsonSerializer"/> instance.</value>
        protected ILambdaJsonSerializer LambdaSerializer { get; }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ProcessMessageAsync(TMessage)"/> method is invoked for every received SNS topic message.
        /// </summary>
        /// <param name="message">The deserialized SNS topic message.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task ProcessMessageAsync(TMessage message);

        //--- Methods ---

        /// <summary>
        /// The <see cref="Deserialize(string)"/> method converts the SNS topic message from string to a typed instance.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="Amazon.Lambda.Core.ILambdaSerializer.Deserialize{TMessage}(Stream)"/> to convert the SNS topic message string
        /// into a <paramtyperef name="TMessage"/> instance. Override this method to provide a custom message deserialization implementation.
        /// </remarks>
        /// <param name="body">The SNS topic message.</param>
        /// <returns>The deserialized SNS topic message.</returns>
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

            // read stream into memory
            LogInfo("reading stream body");
            string snsEventBody;
            using(var reader = new StreamReader(stream)) {
                snsEventBody = reader.ReadToEnd();
            }
            var stopwatch = Stopwatch.StartNew();
            var metrics = new List<LambdaMetric>();

            // process received sns record (there is only ever one)
            try {

                // sns event deserialization
                LogInfo("deserializing SNS event");
                try {
                    var snsEvent = LambdaSerializerSettings.LambdaSharpSerializer.Deserialize<SNSEvent>(snsEventBody);
                    _currentRecord = snsEvent.Records.First().Sns;

                    // message deserialization
                    LogInfo("deserializing message");
                    var message = Deserialize(CurrentRecord.Message);

                    // process message
                    LogInfo("processing message");
                    await ProcessMessageAsync(message);

                    // record successful processing metrics
                    stopwatch.Stop();
                    var now = DateTimeOffset.UtcNow;
                    metrics.Add(("MessageSuccess.Count", 1, LambdaMetricUnit.Count));
                    metrics.Add(("MessageSuccess.Latency", stopwatch.Elapsed.TotalMilliseconds, LambdaMetricUnit.Milliseconds));
                    metrics.Add(("MessageSuccess.Lifespan", (now - CurrentRecord.GetLifespanTimestamp()).TotalSeconds, LambdaMetricUnit.Seconds));
                    return "Ok".ToStream();
                } catch(Exception e) {
                    LogError(e);
                    try {

                        // attempt to send failed message to the dead-letter queue
                        await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.SNS, LambdaSerializer.Serialize(snsEventBody), e);

                        // record failed processing metrics
                        metrics.Add(("MessageDead.Count", 1, LambdaMetricUnit.Count));
                    } catch {

                        // NOTE (2020-04-22, bjorg): since the message could not be sent to the dead-letter queue,
                        //  the next best action is to let Lambda retry it; unfortunately, there is no way
                        //  of knowing how many attempts have occurred already.

                        // unable to forward message to dead-letter queue; report failure to lambda so it can retry
                        metrics.Add(("MessageFailed.Count", 1, LambdaMetricUnit.Count));
                        throw;
                    }
                    return $"ERROR: {e.Message}".ToStream();
                }
            } finally {
                _currentRecord = null;
                LogMetric(metrics);
            }
        }
    }
}
