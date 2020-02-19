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

#nullable disable

using System;
using System.Collections.Generic;

namespace LambdaSharp.Tool.Model {

    public class CloudFormationSpec {

        // SEE: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html

        //--- Properties ---
        public string ResourceSpecificationVersion { get; set; }
        public Dictionary<string, ResourceType> ResourceTypes { get; set; }
        public Dictionary<string, ResourceType> PropertyTypes { get; set; }

        //--- Methods ---
        public bool IsAwsType(string awsType) => ResourceTypes.ContainsKey(awsType);

        public bool HasProperty(string awsType, string property) {

            // for 'Custom::', allow any property
            if(awsType.StartsWith("Custom::", StringComparison.Ordinal)) {
                return true;
            }

            // check if type exists and contains property
            return ResourceTypes.TryGetValue(awsType, out var resource)
                && (resource.Properties?.ContainsKey(property) == true);
        }

        public bool HasAttribute(string awsType, string attribute) {

            // for 'AWS::CloudFormation::Stack', allow attributes starting with "Outputs."
            if((awsType == "AWS::CloudFormation::Stack") && attribute.StartsWith("Outputs.", StringComparison.Ordinal)) {
                return true;
            }

            // for 'Custom::', allow any attribute
            if(awsType.StartsWith("Custom::", StringComparison.Ordinal)) {
                return true;
            }

            // check if type exists and contains attribute
            return ResourceTypes.TryGetValue(awsType, out var resource)
                && (resource.Attributes?.ContainsKey(attribute) == true);
        }
    }

    public class ResourceType {

        //--- Properties ---
        public string Documentation { get; set; }
        public Dictionary<string, AttributeType> Attributes { get; set; }
        public Dictionary<string, PropertyType> Properties { get; set; }
    }

    public class AttributeType {

        //--- Properties ---
        public string ItemType { get; set; }
        public string PrimitiveItemType { get; set; }
        public string PrimitiveType { get; set; }
        public string Type { get; set; }
    }

    public class PropertyType {

        //--- Properties ---

        public bool DuplicatesAllowed { get; set; }
        public string ItemType { get; set; }
        public string PrimitiveItemType { get; set; }
        public string PrimitiveType { get; set; }
        public bool Required { get; set; }
        public string Type { get; set; }
    }
}
