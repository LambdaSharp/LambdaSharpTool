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
using System.Threading.Tasks;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="ALambdaFunction{TRequest, TResponse}"/> builds on the <see cref="ALambdaFunction"/>
    /// by adding request and response types which are automatically deserialized and serialized, respectively.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The response payload type.</typeparam>
    public abstract class ALambdaFunction<TRequest, TResponse> : ALambdaFunction
        where TRequest : notnull
        where TResponse : notnull
    {

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
        protected ALambdaFunction(ILambdaFunctionDependencyProvider? provider) : base(provider) { }

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
#if DEBUG_LAMBDASHARP

            // copy request stream to an in-memory stream
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            // log the received data verbatim
            LogInfo($"received data {LambdaSerializer.GetType().FullName}: {System.Text.Encoding.UTF8.GetString(memoryStream.ToArray())}");
            stream = memoryStream;

            // deserialize request
            var request = LambdaSerializer.Deserialize<TRequest>(stream);

            // log how the request was deserialized
            LogInfo($"deserialized to {System.Text.Json.JsonSerializer.Serialize(request)}");

            // process request
            var response = await ProcessMessageAsync(request);

            //
            var responseStream = new MemoryStream();
            LambdaSerializer.Serialize(response, responseStream);
            responseStream.Position = 0;
            LogInfo($"responded data: {System.Text.Encoding.UTF8.GetString(responseStream.ToArray())}");
#else
            var request = LambdaSerializer.Deserialize<TRequest>(stream);
            var response = await ProcessMessageAsync(request);
            var responseStream = new MemoryStream();
            LambdaSerializer.Serialize(response, responseStream);
            responseStream.Position = 0;
#endif
            return responseStream;
        }
    }
}