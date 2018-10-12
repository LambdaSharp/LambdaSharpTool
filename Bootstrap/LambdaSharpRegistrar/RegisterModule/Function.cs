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
        public string StackName;
        public string StackId;
        public string ModuleId;
        public string ModuleName;
        public string ModuleVersion;
        public string FunctionId;
        public string FunctionName;
        public string FunctionLogGroupName;
        public int FunctionMaxMemory;
        public int FunctionMaxDuration;
        public string FunctionPlatform { get; set; }
        public string FunctionFramework { get; set; }
        public string FunctionLanguage { get; set; }
        public string FunctionGitSha { get; set; }
        public string FunctionGitBranch { get; set; }
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
            var properties = request.ResourceProperties;

            // validate request
            if(properties.Tier != DeploymentTier) {

                // TODO (2018-10-11, bjorg): better exception
                throw new Exception("tier mismatch");
            }

            // determine the kind of registration that is requested
            switch(request.ResourceType) {
            case "Custom::LambdaSharpModuleRegistration":
                LogInfo($"Registering Module: Id={properties.ModuleId}, Name={properties.ModuleName}, Version={properties.ModuleVersion}");
                return Respond($"registration:module:{properties.ModuleId}");
            case "Custom::LambdaSharpFunctionRegistration":
                LogInfo($"Registering Function: Id={properties.FunctionId}, Name={properties.FunctionName}, LogGroup={properties.FunctionLogGroupName}");
                return Respond($"registration:function:{properties.FunctionId}");
            default:

                // TODO (2018-10-11, bjorg): better exception
                throw new Exception($"bad resource type: {request.ResourceType}");
            }
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

        private Response<ResponseProperties> Respond(string registration) {
            return new Response<ResponseProperties> {
                PhysicalResourceId = registration,
                Properties = new ResponseProperties {
                    Registration = registration
                }
            };
        }
    }
}
