/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace ApiSample.MyFunction {

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<APIGatewayProxyResponse> ProcessProxyRequestAsync(APIGatewayProxyRequest request) {
            LogInfo($"Body = {request.Body}");
            LogInfo($"HttpMethod = {request.HttpMethod}");
            LogInfo($"IsBase64Encoded = {request.IsBase64Encoded}");
            LogInfo($"Path = {request.Path}");
            LogInfo($"RequestContext.ResourcePath = {request.RequestContext.ResourcePath}");
            LogInfo($"RequestContext.Stage = {request.RequestContext.Stage}");
            LogInfo($"Resource = {request.Resource}");
            LogDictionary("Headers", request.Headers);
            LogDictionary("PathParameters", request.PathParameters);
            LogDictionary("QueryStringParameters", request.QueryStringParameters);
            LogDictionary("StageVariables", request.StageVariables);
            return new APIGatewayProxyResponse {
                Body = "Ok",
                Headers = new Dictionary<string, string> {
                    ["Content-Type"] = "text/plain"
                },
                StatusCode = 200
            };

            // local function
            void LogDictionary(string prefix, IDictionary<string, string> keyValues) {
                if(keyValues != null) {
                    foreach(var keyValue in keyValues) {
                        LogInfo($"{prefix}.{keyValue.Key} = {keyValue.Value}");
                    }
                }
            }
        }
    }
}