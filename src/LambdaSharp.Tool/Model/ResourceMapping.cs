/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LambdaSharp.Tool.Model {
    using static ModelFunctions;

    public static class ResourceMapping {

        //--- Fields ---
        private static readonly IDictionary<string, IDictionary<string, IList<string>>> _iamMappings;
        private static readonly HashSet<string> _cloudFormationParameterTypes;

        //--- Constructors ---
        static ResourceMapping() {

            // read short-hand for IAM mappings from embedded resource
            var assembly = typeof(ResourceMapping).Assembly;
            using(var iamResource = assembly.GetManifestResourceStream("LambdaSharp.Tool.Resources.IAM-Mappings.yml"))
            using(var reader = new StreamReader(iamResource, Encoding.UTF8)) {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
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
            foreach(var awsType in new[] {
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
            }) {
                _cloudFormationParameterTypes.Add(awsType);
                _cloudFormationParameterTypes.Add($"List<{awsType}>");
                _cloudFormationParameterTypes.Add($"AWS::SSM::Parameter::Value<{awsType}>");
                _cloudFormationParameterTypes.Add($"AWS::SSM::Parameter::Value<List<{awsType}>>");
            }
        }

        //--- Class Methods ---
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

        public static string ToCloudFormationParameterType(string type)
            => _cloudFormationParameterTypes.Contains(type) ? type : "String";
    }
}
