/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

namespace LambdaSharp.CloudFormation.Syntax.Validation {

    public class CloudFormationLimits {

        // NOTE: AWS CloudFormation quotas: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cloudformation-limits.html

        //--- Constants ---
        public const int MAX_TEMPLATE_DESCRIPTION_LENGTH = 1024;
        public const int MAX_PARAMETER_VALUE_LENGTH = 4096;

        // TODO: use these limits
        public const int DECLARATION_MAX_NAME_LENGTH = 255;
        public const int PARAMETERS_MAX_COUNT = 200;
        public const int MAPPINGS_MAX_COUNT = 200;
        public const int MAPPING_MAX_ATTRIBUTE_COUNT = 200;
        public const int OUTPUTS_MAX_COUNT = 200;
        public const int RESOURCE_MAX_COUNT = 500;
    }
}