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
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.CustomResource;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CustomResourceSample.ResourceHandler {

    public class ResourceProperties {

        //--- Properties ---

        // TO-DO: add request resource properties
    }

    public class ResourceAttributes {

        //--- Properties ---

        // TO-DO: add response resource attributes
    }

    public class Function : ALambdaCustomResourceFunction<ResourceProperties, ResourceAttributes> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<Response<ResourceAttributes>> ProcessCreateResourceAsync(Request<ResourceProperties> request) {

            // TO-DO: create resource using configuration settings from request properties

            return new Response<ResourceAttributes> {

                // assign a physical resource ID to the custom resource
                PhysicalResourceId = "MyResource:123",

                // set response properties
                Attributes = new ResourceAttributes {

                    // TO-DO: set response attributes
                }
            };
        }

        public override async Task<Response<ResourceAttributes>> ProcessDeleteResourceAsync(Request<ResourceProperties> request) {

            // TO-DO: delete resource using information from request properties

            return new Response<ResourceAttributes>();
        }

        public override async Task<Response<ResourceAttributes>> ProcessUpdateResourceAsync(Request<ResourceProperties> request) {

            // TO-DO: update resource using configuration settings from request properties

            return new Response<ResourceAttributes> {

                // optionally assign a new physical resource ID to the custom resource
                PhysicalResourceId = "MyResource:123",

                // set updated response properties
                Attributes = new ResourceAttributes {

                    // TO-DO: set response attributes
                }
            };
        }
    }
}