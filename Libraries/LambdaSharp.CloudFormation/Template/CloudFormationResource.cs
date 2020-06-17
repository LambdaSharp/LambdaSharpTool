/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LambdaSharp.CloudFormation.Template.Serialization;

namespace LambdaSharp.CloudFormation.Template {

    [JsonConverter(typeof(CloudFormationResourceConverter))]
    public class CloudFormationResource {

        //--- Constructors ---
        public CloudFormationResource(string type)
            => Type = type ?? throw new ArgumentNullException(nameof(type));

        //--- Operators ---
        public ACloudFormationExpression this[string key] {
            get => Properties[key];
            set => Properties[key] = value;
        }

        //--- Properties ---
        public string Type { get; set; }
        public CloudFormationObject Properties { get; set; } = new CloudFormationObject();
        public List<string> DependsOn { get; set; } = new List<string>();
        public Dictionary<string, ACloudFormationExpression> Metadata { get; set; } = new Dictionary<string, ACloudFormationExpression>();
        public string? Condition { get; set; }
        public string? DeletionPolicy { get; set; }
    }
}