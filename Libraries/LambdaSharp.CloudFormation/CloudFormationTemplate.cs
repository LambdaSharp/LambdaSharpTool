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

    [JsonConverter(typeof(CloudFormationTemplateConverter))]
    public class CloudFormationTemplate {

        //--- Properties ---
        public string AWSTemplateFormatVersion { get; set; } = "2010-09-09";
        public string? Description { get; set; }
        public List<string> Transforms { get; set; } = new List<string>();
        public Dictionary<string, CloudFormationParameter> Parameters { get; set; } = new Dictionary<string, CloudFormationParameter>();
        public Dictionary<string, Dictionary<string, string>> Mappings { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, CloudFormationObject> Conditions { get; set; } = new Dictionary<string, CloudFormationObject>();
        public Dictionary<string, CloudFormationResource> Resources { get; set; } = new Dictionary<string, CloudFormationResource>();
        public Dictionary<string, CloudFormationOutput> Outputs { get; set; } = new Dictionary<string, CloudFormationOutput>();
        public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();
    }
}