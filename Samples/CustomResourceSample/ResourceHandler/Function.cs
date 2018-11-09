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
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;
using MindTouch.LambdaSharp.CustomResource;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CustomResourceSample.ResourceHandler {

    public class RequestProperties {

        //--- Properties ---

        // TODO: add request resource properties
    }

    public class ResponseProperties {

        //--- Properties ---

        // TODO: add response resource properties
    }

    public class Function : ALambdaCustomResourceFunction<RequestProperties, ResponseProperties> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        protected override async Task<Response<ResponseProperties>> HandleCreateResourceAsync(Request<RequestProperties> request) {

            // TODO: create resource using configuration settings from request properties

            return new Response<ResponseProperties> {

                // assign a physical resource ID to custom resource
                PhysicalResourceId = "MyResource:123",

                // set response properties
                Properties = new ResponseProperties {

                    // TODO
                }
            };
        }

        protected override async Task<Response<ResponseProperties>> HandleDeleteResourceAsync(Request<RequestProperties> request) {

            // TODO: delete resource using information from request properties

            return new Response<ResponseProperties>();
        }

        protected override async Task<Response<ResponseProperties>> HandleUpdateResourceAsync(Request<RequestProperties> request) {

            // TODO: update resource using configuration settings from request properties

            return new Response<ResponseProperties> {

                // optionally assign a new physical resource ID to custom resource
                PhysicalResourceId = "MyResource:123",

                // set updated response properties
                Properties = new ResponseProperties {

                    // TODO
               }
            };
        }
    }
}