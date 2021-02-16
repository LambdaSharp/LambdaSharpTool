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
using System.Threading.Tasks;
using LambdaSharp;

namespace MacroSample.MyFunction {

    public class MacroRequest {

        //--- Properties ---
        public string Region { get; set; }
        public string AccountId { get; set; }
        public IDictionary<string, object> Fragment { get; set; }
        public string TransformId { get; set; }
        public IDictionary<string, object> Params { get; set; }
        public string RequestId { get; set; }
        public IDictionary<string, object> TemplateParameterValues { get; set; }
    }

    public class MacroResponse {

        //--- Properties ---
        public string RequestId { get; set; }
        public string Status { get; set; }
        public object Fragment { get; set; }
    }

    public sealed class Function : ALambdaFunction<MacroRequest, MacroResponse> {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<MacroResponse> ProcessMessageAsync(MacroRequest request) {
            LogInfo($"AwsRegion = {request.Region}");
            LogInfo($"AccountID = {request.AccountId}");
            LogInfo($"Fragment = {LambdaSerializer.Serialize(request.Fragment)}");
            LogInfo($"TransformID = {request.TransformId}");
            LogInfo($"Params = {LambdaSerializer.Serialize(request.Params)}");
            LogInfo($"RequestID = {request.RequestId}");
            LogInfo($"TemplateParameterValues = {LambdaSerializer.Serialize(request.TemplateParameterValues)}");

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