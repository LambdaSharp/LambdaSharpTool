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

namespace MindTouch.LambdaSharp.CustomResource {

    public class Response<TProperties> {

        //--- Properties ---

        /// This value should be an identifier unique to the custom resource
        /// vendor, and can be up to 1 Kb in size. The value must be a
        /// non-empty string and must be identical for all responses for the
        /// same resource.
        public string PhysicalResourceId { get; set; }

        /// Optional. Indicates whether to mask the output of the custom
        /// resource when retrieved by using the Fn::GetAtt function. If set to
        /// true, all returned values are masked with asterisks (*****). The
        /// default value is false.
        public bool NoEcho;

        /// Optional. The custom resource provider-defined name-value pairs to
        /// send with the response. You can access the values provided here by
        /// name in the template with Fn::GetAtt.
        public TProperties Properties;
    }
}
