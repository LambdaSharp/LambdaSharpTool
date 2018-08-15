/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;

namespace MindTouch.LambdaSharp {

    [Obsolete("This class is obsolete. Use ALambdaFunction<TRequest, TResponse> instead.")]
    public abstract class ALambdaFunction<TRequest> : ALambdaFunction {

        //--- Fields ---
        protected readonly JsonSerializer JsonSerializer = new JsonSerializer();

        //--- Constructors ---
        protected ALambdaFunction() : this(LambdaFunctionConfiguration.Instance) { }

        protected ALambdaFunction(LambdaFunctionConfiguration configuration) : base(configuration) { }

        //--- Abstract Methods ---
        public abstract Task<object> ProcessMessageAsync(TRequest message, ILambdaContext context);

        //--- Methods ---
        public override async Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context) {
            var message = JsonSerializer.Deserialize<TRequest>(stream);
            return await ProcessMessageAsync(message, context);
        }
    }

    public abstract class ALambdaFunction<TRequest, TResponse> : ALambdaFunction {

        //--- Fields ---
        protected readonly JsonSerializer JsonSerializer = new JsonSerializer();

        //--- Constructors ---
        protected ALambdaFunction() : this(LambdaFunctionConfiguration.Instance) { }

        protected ALambdaFunction(LambdaFunctionConfiguration configuration) : base(configuration) { }

        //--- Abstract Methods ---
        public abstract Task<TResponse> ProcessMessageAsync(TRequest message, ILambdaContext context);

        //--- Methods ---
        public override async Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context) {
            var message = JsonSerializer.Deserialize<TRequest>(stream);
            return await ProcessMessageAsync(message, context);
        }
    }
}