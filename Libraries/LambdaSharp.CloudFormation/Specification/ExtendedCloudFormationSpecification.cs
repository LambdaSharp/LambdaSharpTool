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

using System.Collections.Generic;

namespace LambdaSharp.CloudFormation.Specification {

    public sealed class ExtendedCloudFormationSpecification {

        // CloudFormation specification: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html
        // And then extended with https://github.com/aws-cloudformation/cfn-python-lint

        //--- Properties ---
        public string? ResourceSpecificationVersion { get; set; }
        public Dictionary<string, ResourceType> ResourceTypes { get; set; } = new Dictionary<string, ResourceType>();
        public Dictionary<string, ResourceType> PropertyTypes { get; set; } = new Dictionary<string, ResourceType>();
        public Dictionary<string, IntrinsicFunctionType> IntrinsicTypes { get; set; } = new Dictionary<string, IntrinsicFunctionType>();
        public Dictionary<string, List<string>> ParameterTypes { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, ValueType> ValueTypes { get; set; } = new Dictionary<string, ValueType>();
    }
}
