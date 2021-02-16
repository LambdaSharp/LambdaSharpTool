/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using LambdaSharp.Logging;
using LambdaSharp.Logging.Metrics;
using LambdaSharp.Serialization;

namespace LambdaSharp.CloudWatch {

    /// <summary>
    /// The <see cref="ALambdaEventFunction{TEvent}"/> is the abstract base class for handling
    /// <a href="https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/WhatIsCloudWatchEvents.html">Amazon CloudWatch Events</a> events.
    /// </summary>
    /// <typeparam name="TMessage">The CloudWatch event message type.</typeparam>
    public abstract class ALambdaEventFunction<TMessage> : ALambdaFunction {

        //--- Fields ---
        private CloudWatchEvent<TMessage>?  _currentEvent;

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaEventFunction{TEvent}"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        protected ALambdaEventFunction(ILambdaJsonSerializer serializer) : this(serializer, provider: null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaEventFunction{TEvent}"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaEventFunction(ILambdaJsonSerializer serializer, ILambdaFunctionDependencyProvider? provider) : base(provider) {
            LambdaSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        //--- Properties ---

        /// <summary>
        /// The <see cref="CurrentEvent"/> property holds the CloudWatch event that is currently being processed.
        /// </summary>
        /// <remarks>
        /// This property is only set during the invocation of <see cref="ProcessEventAsync(TMessage)"/>. Otherwise, it returns <c>null</c>.
        /// </remarks>
        /// <value>The <see cref="CloudWatchEvent{TEvent}"/> instance.</value>
        protected CloudWatchEvent<TMessage> CurrentEvent => _currentEvent ?? throw new InvalidOperationException();

        /// <summary>
        /// An instance of <see cref="ILambdaJsonSerializer"/> used for serializing/deserializing JSON data.
        /// </summary>
        /// <value>The <see cref="ILambdaJsonSerializer"/> instance.</value>
        protected ILambdaJsonSerializer LambdaSerializer { get; }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ProcessEventAsync(TMessage)"/> method is invoked for every received CloudWatch event.
        /// </summary>
        /// <param name="message">The deserialized CloudWatch event message.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task ProcessEventAsync(TMessage message);

        //--- Methods ---

        /// <summary>
        /// The <see cref="Deserialize(string, string)"/> method converts the CloudWatch event detail from string to a typed instance.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="Amazon.Lambda.Core.ILambdaSerializer.Deserialize{TMessage}(Stream)"/> to convert the CloudWatch event detail string
        /// into a <paramtyperef name="TMessage"/> instance. Override this method to provide a custom message deserialization implementation.
        /// </remarks>
        /// <param name="detailType">The CloudWatch event detail type</param>
        /// <param name="detail">The CloudWatch event detail.</param>
        /// <returns>The deserialized The CloudWatch event message.</returns>
        public virtual TMessage Deserialize(string detailType, string detail) => LambdaSerializer.Deserialize<TMessage>(detail);

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
            string cloudWatchEventBody;
            using(var reader = new StreamReader(stream)) {
                cloudWatchEventBody = reader.ReadToEnd();
            }
            var stopwatch = Stopwatch.StartNew();
            var metrics = new List<LambdaMetric>();

            // process received event (there is only ever one)
            try {

                // cloudwatch event deserialization
                LogInfo("deserializing CloudWatch event");
                try {

                    // deserialize using JsonElement as type for 'Detail' property; this allows us to re-deserialize it with the custom lambda serializer
                    var cloudWatchEvent = LambdaSerializerSettings.LambdaSharpSerializer.Deserialize<CloudWatchEvent<JsonElement>>(cloudWatchEventBody);

                    // message deserialization
                    LogInfo("deserializing event detail");
                    var detail = Deserialize(cloudWatchEvent.DetailType, cloudWatchEvent.Detail.GetRawText());

                    // process event
                    LogInfo("processing event");
                    _currentEvent = new CloudWatchEvent<TMessage> {
                        Account = cloudWatchEvent.Account,
                        Detail = detail,
                        DetailType = cloudWatchEvent.DetailType,
                        Id = cloudWatchEvent.Id,
                        Region = cloudWatchEvent.Region,
                        Resources = cloudWatchEvent.Resources,
                        Source = cloudWatchEvent.Source,
                        Time = cloudWatchEvent.Time,
                        Version = cloudWatchEvent.Version
                    };
                    await ProcessEventAsync(_currentEvent.Detail);

                    // record successful processing metrics
                    stopwatch.Stop();
                    var now = DateTimeOffset.UtcNow;
                    metrics.Add(("MessageSuccess.Count", 1, LambdaMetricUnit.Count));
                    metrics.Add(("MessageSuccess.Latency", stopwatch.Elapsed.TotalMilliseconds, LambdaMetricUnit.Milliseconds));
                    metrics.Add(("MessageSuccess.Lifespan", (now - CurrentEvent.Time).TotalSeconds, LambdaMetricUnit.Seconds));
                    return "Ok".ToStream();
                } catch(Exception e) {
                    LogError(e);
                    try {

                        // attempt to send failed event to the dead-letter queue
                        await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.CloudWatch, LambdaSerializer.Serialize(cloudWatchEventBody), e);

                        // record failed processing metrics
                        metrics.Add(("MessageDead.Count", 1, LambdaMetricUnit.Count));
                    } catch {

                        // NOTE (2020-04-22, bjorg): since the event could not be sent to the dead-letter queue,
                        //  the next best action is to let Lambda retry it; unfortunately, there is no way
                        //  of knowing how many attempts have occurred already.

                        // unable to forward event to dead-letter queue; report failure to lambda so it can retry
                        metrics.Add(("MessageFailed.Count", 1, LambdaMetricUnit.Count));
                        throw;
                    }
                    return $"ERROR: {e.Message}".ToStream();
                }
            } finally {
                _currentEvent = null;
                LogMetric(metrics);
            }
        }
    }
}
