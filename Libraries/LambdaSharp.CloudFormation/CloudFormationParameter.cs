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

namespace LambdaSharp.CloudFormation {

    public class CloudFormationParameter {

        //--- Properties ---
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? AllowedPattern { get; set; }
        public List<string>? AllowedValues { get; set; }
        public string? ConstraintDescription { get; set; }
        public string? Default { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public bool? NoEcho { get; set; }
    }
}