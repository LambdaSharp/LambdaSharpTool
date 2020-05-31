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
using System.Text.Json.Serialization;
using LambdaSharp.CloudFormation.Serialization;

namespace LambdaSharp.CloudFormation {

    [JsonConverter(typeof(CloudFormationResourceConverter))]
    public class CloudFormationResource {

        //--- Properties ---
        public string? Type { get; set; }
        public CloudFormationObjectExpression Properties { get; set; } = new CloudFormationObjectExpression();
        public List<string> DependsOn { get; set; } = new List<string>();
        public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();
        public string? Condition { get; set; }
        public string? DeletionPolicy { get; set; }
    }
}