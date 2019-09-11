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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LambdaSharp.CustomResource.Internal {

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum CloudFormationResourceResponseStatus {
        SUCCESS,
        FAILED
    }

    internal class CloudFormationResourceResponse<TAttributes> {

        //--- Properties ---

        /// The status value sent by the custom resource provider in response
        /// to an AWS CloudFormation-generated request.
        public CloudFormationResourceResponseStatus Status { get; set; }

        /// Describes the reason for a failure response.
        ///
        /// Required: Required if Status is FAILED. It's optional otherwise.
        public string Reason { get; set; }

        /// The Amazon Resource Name (ARN) that identifies the stack that
        /// contains the custom resource. This response value should be copied
        /// verbatim from the request.
        public string StackId { get; set; }

        /// This value should be an identifier unique to the custom resource
        /// vendor, and can be up to 1 Kb in size. The value must be a
        /// non-empty string and must be identical for all responses for the
        /// same resource.
        public string PhysicalResourceId { get; set; }

        /// A unique ID for the request. This response value should be copied
        /// verbatim from the request.
        public string RequestId { get; set; }

        /// The template developer-chosen name (logical ID) of the custom
        /// resource in the AWS CloudFormation template. This response
        /// value should be copied verbatim from the request.
        public string LogicalResourceId { get; set; }

        /// Optional. Indicates whether to mask the output of the custom
        /// resource when retrieved by using the Fn::GetAtt function. If set to
        /// true, all returned values are masked with asterisks (*****). The
        /// default value is false.
        public bool NoEcho;

        /// Optional. The custom resource provider-defined name-value pairs to
        /// send with the response. You can access the values provided here by
        /// name in the template with Fn::GetAtt.
        public TAttributes Data;
    }
}
