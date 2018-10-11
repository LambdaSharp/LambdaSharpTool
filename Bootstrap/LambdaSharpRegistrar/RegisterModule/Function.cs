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
using MindTouch.LambdaSharp;
using MindTouch.LambdaSharp.CustomResource;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MindTouch.LambdaSharpRegistrar.RegisterModule {

    public class RequestProperties {

        //--- Properties ---
        public string Tier;
        public string ModuleId;
        public string ModuleName;
        public string ModuleVersion;
        public string StackName;
        public string StackId;
    }

    public class ResponseProperties {

        //--- Properties ---
        public string Registration { get; set; }
    }

    public class Function : ALambdaCustomResourceFunction<RequestProperties, ResponseProperties> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        protected override async Task<Response<ResponseProperties>> HandleCreateResourceAsync(Request<RequestProperties> request) {
            var prop = request.ResourceProperties;
            if(prop.Tier != DeploymentTier) {

                // TODO
                throw new Exception("tier mismatch");
            }
            LogInfo($"Registering Module: Id={prop.ModuleId}, Name={prop.ModuleName}, Version={prop.ModuleVersion}");
            var registration = $"registration:{request.ResourceProperties.ModuleId}";
            return new Response<ResponseProperties> {
                PhysicalResourceId = registration,
                Properties = new ResponseProperties {
                    Registration = registration
                }
            };
        }

        protected override async Task<Response<ResponseProperties>> HandleDeleteResourceAsync(Request<RequestProperties> request) {
            return new Response<ResponseProperties>();
        }

        protected override async Task<Response<ResponseProperties>> HandleUpdateResourceAsync(Request<RequestProperties> request) {
            var registration = $"registration:{request.ResourceProperties.ModuleId}";
            return new Response<ResponseProperties> {
                PhysicalResourceId = registration,
                Properties = new ResponseProperties {
                    Registration = registration
                }
            };
        }
    }
}
