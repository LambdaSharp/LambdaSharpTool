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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using MindTouch.LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MacroSample.MyFunction {

    public class MacroRequest {

        //--- Properties ---

        // TODO (2018-09-06, bjorg): use official AWS events definition once available
        public string region;
        public string accountId;
        public IDictionary<string, object> fragment;
        public string transformId;
        public IDictionary<string, object> @params;
        public string requestId;
        public IDictionary<string, object> templateParameterValues;
    }

    public class MacroResponse {

        //--- Properties ---

        // TODO (2018-09-06, bjorg): use official AWS events definition once available
        public string requestId;
        public string status;
        public object fragment;
    }

    public class Function : ALambdaFunction<MacroRequest, MacroResponse> {

        //--- Class Methods ---
        private static string SerializeToJson(object value) {
            using(var stream = new MemoryStream()) {
                new JsonSerializer().Serialize(value, stream);
                stream.Position = 0;
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<MacroResponse> ProcessMessageAsync(MacroRequest request, ILambdaContext context) {
            LogInfo($"AwsRegion = {request.region}");
            LogInfo($"AccountID = {request.accountId}");
            LogInfo($"Fragment = {SerializeToJson(request.fragment)}");
            LogInfo($"TransformID = {request.transformId}");
            LogInfo($"Params = {SerializeToJson(request.@params)}");
            LogInfo($"RequestID = {request.requestId}");
            LogInfo($"TemplateParameterValues = {SerializeToJson(request.templateParameterValues)}");

            // macro for string operations
            try {
                if(!request.@params.TryGetValue("Value", out object value)) {
                    throw new ArgumentException("missing parameter: 'Value");
                }
                if(!(value is string text)) {
                    throw new ArgumentException("parameter 'Value' must be a string");
                }
                string result;
                switch(request.transformId) {
                case "StringToUpper":
                    result = text.ToUpper();
                    break;
                case "StringToLower":
                    result = text.ToLower();
                    break;
                default:
                    throw new NotSupportedException($"requested operation is not supported: '{request.transformId}'");
                }

                // return successful response
                return new MacroResponse {
                    requestId = request.requestId,
                    status = "SUCCESS",
                    fragment = result
                };
            } catch(Exception e) {

                // an error occurred
                return new MacroResponse {
                    requestId = request.requestId,
                    status = $"ERROR: {e.Message}"
                };
            }
        }
    }
}