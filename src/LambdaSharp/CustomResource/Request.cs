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

namespace LambdaSharp.CustomResource {

    /// <summary>
    /// The <see cref="RequestType"/> enumeration describes the CloudFormation
    /// operation on the custom resource.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestType {

        /// <summary>
        /// AWS CloudFormation is attempting to create the resource.
        /// </summary>
        Create,

        /// <summary>
        /// AWS CloudFormation is attempting to update the resource.
        /// </summary>
        Update,

        /// <summary>
        /// AWS CloudFormation is attempting to delete the resource.
        /// </summary>
        Delete
    }

    /// <summary>
    /// The <see cref="Request{TProperties}"/> is a generic class that
    /// wraps the <typeparamref name="TProperties"/> request with additional
    /// properties sent by the AWS CloudFormation service.
    /// </summary>
    /// <typeparam name="TProperties">The request properties for the custom resource.</typeparam>
    public class Request<TProperties> {

        //--- Properties ---

        /// <summary>
        /// The request type is set by the AWS CloudFormation stack operation
        /// (create-stack, update-stack, or delete-stack) that was initiated
        /// by the template developer for the stack that contains the custom
        /// resource.
        /// </summary>
        public RequestType RequestType { get; set; }

        /// <summary>
        /// The template developer-chosen resource type of the custom resource
        /// in the AWS CloudFormation template. Custom resource type names can
        /// be up to 60 characters long and can include alphanumeric and the
        /// following characters: <c>_@-</c>.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// The Amazon Resource Name (ARN) that identifies the stack that
        /// contains the custom resource.
        ///
        /// Combining the StackId with the RequestId forms a value that you
        /// can use to uniquely identify a request on a particular custom
        /// resource.
        /// </summary>
        public string StackId { get; set; }

        /// <summary>
        /// The template developer-chosen name (logical ID) of the custom
        /// resource in the AWS CloudFormation template. This is provided to
        /// facilitate communication between the custom resource provider and
        /// the template developer.
        /// </summary>
        public string LogicalResourceId { get; set; }

        /// <summary>
        /// A required custom resource provider-defined physical ID that is
        /// unique for that provider.
        /// </summary>
        public string PhysicalResourceId { get; set; }

        /// <summary>
        /// This field contains the contents of the Properties object sent by
        /// the template developer. Its contents are defined by the custom
        /// resource provider.
        /// </summary>
        public TProperties ResourceProperties { get; set; }

        /// <summary>
        /// Used only for Update requests. Contains the resource properties
        /// that were declared previous to the update request.
        /// </summary>
        public TProperties OldResourceProperties { get; set; }
    }
}
