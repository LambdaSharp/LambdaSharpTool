/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ApiSample.MyFunction {

    public class Function : ALambdaApiGatewayFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context) {
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