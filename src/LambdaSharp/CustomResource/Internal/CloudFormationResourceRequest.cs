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

namespace LambdaSharp.CustomResource.Internal {

    internal class CloudFormationResourceRequest<TProperties> {

        //--- Properties ---

        /// The request type is set by the AWS CloudFormation stack operation
        /// (create-stack, update-stack, or delete-stack) that was initiated
        /// by the template developer for the stack that contains the custom
        /// resource.
        public RequestType RequestType { get; set; }

        /// The response URL identifies a presigned S3 bucket that receives
        /// responses from the custom resource provider to AWS CloudFormation.
        public string ResponseURL { get; set; }

        /// The Amazon Resource Name (ARN) that identifies the stack that
        /// contains the custom resource.
        ///
        /// Combining the StackId with the RequestId forms a value that you
        /// can use to uniquely identify a request on a particular custom
        /// resource.
        public string StackId { get; set; }

        /// A unique ID for the request.
        ///
        /// Combining the StackId with the RequestId forms a value that you
        /// can use to uniquely identify a request on a particular custom
        /// resource.
        public string RequestId { get; set; }

        /// The template developer-chosen resource type of the custom resource
        /// in the AWS CloudFormation template. Custom resource type names can
        /// be up to 60 characters long and can include alphanumeric and the
        /// following characters: _@-.
        public string ResourceType { get; set; }

        /// The template developer-chosen name (logical ID) of the custom
        /// resource in the AWS CloudFormation template. This is provided to
        /// facilitate communication between the custom resource provider and
        /// the template developer.
        public string LogicalResourceId { get; set; }

        /// A required custom resource provider-defined physical ID that is
        /// unique for that provider.
        public string PhysicalResourceId { get; set; }

        /// This field contains the contents of the Properties object sent by
        /// the template developer. Its contents are defined by the custom
        /// resource provider.
        public TProperties ResourceProperties { get; set; }

        /// Used only for Update requests. Contains the resource properties
        /// that were declared previous to the update request.
        public TProperties OldResourceProperties { get; set; }
    }
}
