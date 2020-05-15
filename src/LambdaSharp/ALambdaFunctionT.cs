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

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="ALambdaFunction{TRequest, TResponse}"/> builds on the <see cref="ALambdaFunction"/>
    /// by adding request and response types which are automatically deserialized and serialized, respectively.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The response payload type.</typeparam>
    public abstract class ALambdaFunction<TRequest, TResponse> : ALambdaFunction {

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaFunction{TRequest, TResponse}"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        protected ALambdaFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaFunction{TRequest, TResponse}"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaFunction(ILambdaFunctionDependencyProvider provider) : base(provider) {

            // read environment variable to determine if request/response messages should be serialized to the log for debugging purposes
            bool.TryParse(System.Environment.GetEnvironmentVariable("DEBUG_REQUEST_RESPONSE"), out var debugLogMessage);
            DebugRequestResponse = debugLogMessage;
            if(DebugRequestResponse) {
                LogInfo($"typeof(LambdaSerializer): {LambdaSerializer.GetType().FullName}");
            }
        }

        //--- Properties ---

        /// <summary>
        /// The <see cref="DebugRequestResponse"/> property indicates if the the requests received and responses emitted
        /// by this Lambda function should be shown in the CloudWatch logs. This can be useful to determine check for
        /// issues caused by inconsistencies in serialization or deserialization.
        /// </summary>
        /// <value>Boolean indicating if request/response are logged</value>
        protected bool DebugRequestResponse { get; set; }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ProcessMessageAsync(TRequest)"/> method is invoked for every deserialized request. It is
        /// responsible for processing the request and returning a response.
        /// </summary>
        /// <param name="message">The deserialized request.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<TResponse> ProcessMessageAsync(TRequest message);

        //--- Methods ---

        /// <summary>
        /// The <see cref="ProcessMessageStreamAsync(Stream)"/> deserializes the request stream into
        /// a <typeparamref name="TRequest"/> instance and invokes the <see cref="ProcessMessageAsync(TRequest)"/> method.
        /// </summary>
        /// <remarks>
        /// This method is <c>sealed</c> and cannot be overridden.
        /// </remarks>
        /// <param name="stream">The stream with the request payload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public override sealed async Task<Stream> ProcessMessageStreamAsync(Stream stream) {
            if(DebugRequestResponse) {
                using(var memoryStream = new MemoryStream()) {

                    // copy request stream to an in-memory stream
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    // log the received data verbatim
                    LogInfo($"received data: {Encoding.UTF8.GetString(memoryStream.ToArray())}");

                    // deserialize request
                    var request = LambdaSerializer.Deserialize<TRequest>(memoryStream);

                    // log how the request was deserialized
                    LogInfo($"deserialized request: {LambdaSerializer.Serialize(request)}");

                    // process request
                    var response = await ProcessMessageAsync(request);

                    // serialize response
                    var responseStream = new MemoryStream();
                    LambdaSerializer.Serialize(response, responseStream);
                    responseStream.Position = 0;

                    // log the serialize response
                    LogInfo($"serializer response: {Encoding.UTF8.GetString(responseStream.ToArray())}");
                    return responseStream;
                }
            } else {
                var request = LambdaSerializer.Deserialize<TRequest>(stream);
                var response = await ProcessMessageAsync(request);
                var responseStream = new MemoryStream();
                LambdaSerializer.Serialize(response, responseStream);
                responseStream.Position = 0;
                return responseStream;
            }
        }
    }
}