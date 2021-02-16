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

using System.Threading.Tasks;
using Amazon.Lambda.RuntimeSupport;
using LambdaSharp;

namespace Sample.LambdaSelfContained.MySelfContainedFunction {

    public class FunctionRequest { }

    public class FunctionResponse {

        //--- Properties ---
        public string Message { get; set; }
    }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Class Methods ---
        public static async Task Main(string[] args) {

            // NOTE: this method is the entry point for Lambda functions with a custom runtime
            using var handlerWrapper = HandlerWrapper.GetHandlerWrapper(new Function().FunctionHandlerAsync);
            using var bootstrap = new LambdaBootstrap(handlerWrapper);
            await bootstrap.RunAsync();
       }

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            LogInfo("Custom runtime function initialization");
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            LogInfo("Custom runtime function invocation");
            return new FunctionResponse {
                Message = "Success!!!"
            };
        }
    }
}
