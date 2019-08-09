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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Humidifier;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LambdaSharp.Tool.Model {
    using System.IO.Compression;
    using static ModelFunctions;

    public class CloudFormationSpec {

        // SEE: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html

        //--- Properties ---
        public string ResourceSpecificationVersion { get; set; }
        public IDictionary<string, ResourceType> ResourceTypes { get; set; }
        public IDictionary<string, ResourceType> PropertyTypes { get; set; }
    }

    public class ResourceType {

        //--- Properties ---
        public string Documentation { get; set; }
        public IDictionary<string, AttributeType> Attributes { get; set; }
        public IDictionary<string, PropertyType> Properties { get; set; }
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

    public static class ResourceMapping {

        //--- Fields ---
        public static readonly CloudFormationSpec CloudformationSpec;
        private static readonly IDictionary<string, IDictionary<string, IList<string>>> _iamMappings;
        private static readonly HashSet<string> _cloudFormationParameterTypes;


        //--- Constructors ---
        static ResourceMapping() {

            // read short-hand for IAM mappings from embedded resource
            var assembly = typeof(ResourceMapping).Assembly;
            using(var iamResource = assembly.GetManifestResourceStream("LambdaSharp.Tool.Resources.IAM-Mappings.yml"))
            using(var reader = new StreamReader(iamResource, Encoding.UTF8)) {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new NullNamingConvention())
                    .Build();
                _iamMappings = deserializer.Deserialize<IDictionary<string, IDictionary<string, IList<string>>>>(reader);
            }

            // create list of natively supported CloudFormation types
            _cloudFormationParameterTypes = new HashSet<string> {
                "String",
                "Number",
                "List<Number>",
                "CommaDelimitedList",
                "AWS::SSM::Parameter::Name",
                "AWS::SSM::Parameter::Value<String>",
                "AWS::SSM::Parameter::Value<List<String>>",
                "AWS::SSM::Parameter::Value<CommaDelimitedList>"
            };
            var awsTypes = new[] {
                "AWS::EC2::AvailabilityZone::Name",
                "AWS::EC2::Image::Id",
                "AWS::EC2::Instance::Id",
                "AWS::EC2::KeyPair::KeyName",
                "AWS::EC2::SecurityGroup::GroupName",
                "AWS::EC2::SecurityGroup::Id",
                "AWS::EC2::Subnet::Id",
                "AWS::EC2::Volume::Id",
                "AWS::EC2::VPC::Id",
                "AWS::Route53::HostedZone::Id"
            };
            foreach(var awsType in awsTypes) {
                _cloudFormationParameterTypes.Add(awsType);
                _cloudFormationParameterTypes.Add($"List<{awsType}>");
                _cloudFormationParameterTypes.Add($"AWS::SSM::Parameter::Value<{awsType}>");
                _cloudFormationParameterTypes.Add($"AWS::SSM::Parameter::Value<List<{awsType}>>");
            }

            // read CloudFormation specification
            using(var specResource = assembly.GetManifestResourceStream("LambdaSharp.Tool.Resources.CloudFormationResourceSpecification.json.gz"))
            using(var specGzipStream = new GZipStream(specResource, CompressionMode.Decompress))
            using(var specReader = new StreamReader(specGzipStream)) {
                CloudformationSpec = JsonConvert.DeserializeObject<CloudFormationSpec>(specReader.ReadToEnd());
            }
        }

        //--- Methods ---
        public static bool TryResolveAllowShorthand(string awsType, string shorthand, out IList<string> allowed) {
            allowed = null;
            return _iamMappings.TryGetValue(awsType, out var awsTypeShorthands)
                && awsTypeShorthands.TryGetValue(shorthand, out allowed);
        }

        public static object ExpandResourceReference(string awsType, object arnReference) {

            // NOTE (2018-12-11, bjorg): some AWS resources require additional sub-resource reference
            //  to properly apply permissions across the board.

            switch(awsType) {
            case "AWS::S3::Bucket":

                // S3 Bucket resources must be granted permissions on the bucket AND the keys
                return LiftArnReference().SelectMany(reference => new object[] {
                    reference,
                    FnJoin("", new List<object> { reference, "/*" })
                }).ToList();
            case "AWS::DynamoDB::Table":

                // DynamoDB resources must be granted permissions on the table AND the stream
                return LiftArnReference().SelectMany(reference => new object[] {
                    reference,
                    FnJoin("/", new List<object> { reference, "stream/*" }),
                    FnJoin("/", new List<object> { reference, "index/*" })
                }).ToList();
            default:
                return arnReference;
            }

            // local functions
            IList<object> LiftArnReference()
                => (arnReference is IList<object> arnReferences)
                    ? arnReferences
                    : new object[] { arnReference };
        }

        public static bool HasProperty(string awsType, string property) {

            // for 'Custom::', allow any property
            if(awsType.StartsWith("Custom::", StringComparison.Ordinal)) {
                return true;
            }

            // check if type exists and contains property
            return CloudformationSpec.ResourceTypes.TryGetValue(awsType, out var resource)
                && (resource.Properties?.ContainsKey(property) == true);
        }

        public static bool HasAttribute(string awsType, string attribute) {

            // for 'AWS::CloudFormation::Stack', allow attributes starting with "Outputs."
            if((awsType == "AWS::CloudFormation::Stack") && attribute.StartsWith("Outputs.", StringComparison.Ordinal)) {
                return true;
            }

            // for 'Custom::', allow any attribute
            if(awsType.StartsWith("Custom::", StringComparison.Ordinal)) {
                return true;
            }

            // check if type exists and contains attribute
            return CloudformationSpec.ResourceTypes.TryGetValue(awsType, out var resource)
                && (resource.Attributes?.ContainsKey(attribute) == true);
        }

        public static bool IsCloudFormationType(string awsType) => CloudformationSpec.ResourceTypes.ContainsKey(awsType);

        public static string ToCloudFormationParameterType(string type)
            => _cloudFormationParameterTypes.Contains(type) ? type : "String";

        public static bool TryGetPropertyItemType(string rootAwsType, string itemTypeName, out ResourceType type)
            => ResourceMapping.CloudformationSpec.PropertyTypes.TryGetValue(itemTypeName, out type)
                || ResourceMapping.CloudformationSpec.PropertyTypes.TryGetValue(rootAwsType + "." + itemTypeName, out type);
    }
}
