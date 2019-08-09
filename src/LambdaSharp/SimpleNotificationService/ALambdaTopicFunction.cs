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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using LambdaSharp.Exceptions;
using LambdaSharp.Logger;

namespace LambdaSharp.SimpleNotificationService {

    /// <summary>
    /// The <see cref="ALambdaTopicFunction{TMessage}"/> is the abstract base class for handling
    /// <a href="https://aws.amazon.com/sns/">Amazon Simple Notification Service (SNS)</a> events.
    /// </summary>
    /// <typeparam name="TMessage">The SNS topic message type.</typeparam>
    public abstract class ALambdaTopicFunction<TMessage> : ALambdaFunction {

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaTopicFunction{TMessage}"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        protected ALambdaTopicFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaTopicFunction{TMessage}"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaTopicFunction(ILambdaFunctionDependencyProvider provider) : base(provider) { }

        //--- Properties ---

        /// <summary>
        /// The <see cref="CurrentRecord"/> property holds the SNS message record that is currently being processed.
        /// </summary>
        /// <remarks>
        /// This property is only set during the invocation of <see cref="ProcessMessageStreamAsync(Stream)"/>. Otherwise, it returns <c>null</c>.
        /// </remarks>
        /// <value>The <see cref="SNSEvent.SNSMessage"/> instance.</value>
        protected SNSEvent.SNSMessage CurrentRecord { get; private set; }

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
        /// This method invokes <see cref="ALambdaFunction.DeserializeJson{TMessage}(string)"/> to convert the SNS topic message string
        /// into a <paramtyperef name="TMessage"/> instance. Override this method to provide a custom message deserialization implementation.
        /// </remarks>
        /// <param name="body">The SNS topic message.</param>
        /// <returns>The deserialized SNS topic message.</returns>
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

            // read stream into memory
            LogInfo("reading stream body");
            string snsEventBody;
            using(var reader = new StreamReader(stream)) {
                snsEventBody = reader.ReadToEnd();
            }

            // process received sns record (there is only ever one)
            try {

                // sns event deserialization
                LogInfo("deserializing SNS event");
                var snsEvent = DeserializeJson<SNSEvent>(snsEventBody);
                CurrentRecord = snsEvent.Records.First().Sns;

                // message deserialization
                LogInfo("deserializing message");
                var message = Deserialize(CurrentRecord.Message);

                // process message
                LogInfo("processing message");
                await ProcessMessageAsync(message);
                return "Ok".ToStream();
            } catch(Exception e) when(!(e is LambdaRetriableException)) {
                LogError(e);
                await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.SNS, snsEventBody, e);
                return $"ERROR: {e.Message}".ToStream();
            } finally {
                CurrentRecord = null;
            }
        }
    }
}
