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
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MacroSample.MyFunction {

    public class MacroRequest {

        //--- Properties ---
        public string Region;
        public string AccountId;
        public IDictionary<string, object> Fragment;
        public string TransformId;
        public IDictionary<string, object> Params;
        public string RequestId;
        public IDictionary<string, object> TemplateParameterValues;
    }

    public class MacroResponse {

        //--- Properties ---
        public string RequestId;
        public string Status;
        public object Fragment;
    }

    public class Function : ALambdaFunction<MacroRequest, MacroResponse> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<MacroResponse> ProcessMessageAsync(MacroRequest request) {
            LogInfo($"AwsRegion = {request.Region}");
            LogInfo($"AccountID = {request.AccountId}");
            LogInfo($"Fragment = {SerializeJson(request.Fragment)}");
            LogInfo($"TransformID = {request.TransformId}");
            LogInfo($"Params = {SerializeJson(request.Params)}");
            LogInfo($"RequestID = {request.RequestId}");
            LogInfo($"TemplateParameterValues = {SerializeJson(request.TemplateParameterValues)}");

            // macro for string operations
            try {
                if(!request.Params.TryGetValue("Value", out var value)) {
                    throw new ArgumentException("missing parameter: 'Value");
                }
                if(!(value is string text)) {
                    throw new ArgumentException("parameter 'Value' must be a string");
                }
                string result;
                switch(request.TransformId) {
                case "StringToUpper":
                    result = text.ToUpper();
                    break;
                case "StringToLower":
                    result = text.ToLower();
                    break;
                default:
                    throw new NotSupportedException($"requested operation is not supported: '{request.TransformId}'");
                }

                // return successful response
                return new MacroResponse {
                    RequestId = request.RequestId,
                    Status = "SUCCESS",
                    Fragment = result
                };
            } catch(Exception e) {

                // an error occurred
                return new MacroResponse {
                    RequestId = request.RequestId,
                    Status = $"ERROR: {e.Message}"
                };
            }
        }
    }
}