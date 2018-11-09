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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Humidifier;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MindTouch.LambdaSharp.Tool.Model {

    public class ResourceMapping {

        //--- Fields ---
        private readonly IDictionary<string, IDictionary<string, IList<string>>> _iamMappings;

        //--- Constructors ---
        public ResourceMapping() {

            // read short-hand for IAM mappings from embedded resource
            var assembly = typeof(ResourceMapping).Assembly;
            using(var resource = assembly.GetManifestResourceStream("MindTouch.LambdaSharp.Tool.Resources.IAM-Mappings.yml"))
            using(var reader = new StreamReader(resource, Encoding.UTF8)) {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new NullNamingConvention())
                    .Build();
                _iamMappings = deserializer.Deserialize<IDictionary<string, IDictionary<string, IList<string>>>>(reader);
            }
        }

        //--- Methods ---
        public bool TryResolveAllowShorthand(string awsType, string shorthand, out IList<string> allowed) {
            allowed = null;
            return _iamMappings.TryGetValue(awsType, out IDictionary<string, IList<string>> awsTypeShorthands)
                && awsTypeShorthands.TryGetValue(shorthand, out allowed);
        }

        public object ExpandResourceReference(string awsType, object arnReference) {

            // NOTE: some AWS resources require additional sub-resource reference
            //  to properly apply permissions across the board.

            switch(awsType) {
            case "AWS::S3::Bucket":

                // S3 Bucket resources must be granted permissions on the bucket AND the keys
                return new object[] {
                    arnReference,
                    AModelProcessor.FnJoin("", new List<object> { arnReference, "/*" })
                };
            case "AWS::DynamoDB::Table":

                // DynamoDB resources must be granted permissions on the table AND the stream
                return new object[] {
                    arnReference,
                    AModelProcessor.FnJoin("/", new List<object> { arnReference, "stream", "*" }),
                    AModelProcessor.FnJoin("/", new List<object> { arnReference, "index", "*" })
                };
            default:
                return arnReference;
            }
        }

        public object GetArnReference(string awsType, string logicalId) {
            if(awsType == null) {

                // use !Ref for non-resource type references
                return AModelProcessor.FnRef(logicalId);
            }
            var type = GetHumidifierType(awsType);
            if(type == null) {

                // don't reference custom types
                return AModelProcessor.FnRef("AWS::NoValue");
            }
            switch(awsType) {
            case "AWS::ApplicationAutoScaling::ScalingPolicy":
            case "AWS::AutoScaling::ScalingPolicy":
            case "AWS::Batch::ComputeEnvironment":
            case "AWS::Batch::JobDefinition":
            case "AWS::Batch::JobQueue":
            case "AWS::CertificateManager::Certificate":
            case "AWS::CloudFormation::Stack":
            case "AWS::CloudFormation::WaitCondition":
            case "AWS::ECS::Service":
            case "AWS::ECS::TaskDefinition":
            case "AWS::ElasticLoadBalancingV2::Listener":
            case "AWS::ElasticLoadBalancingV2::ListenerRule":
            case "AWS::ElasticLoadBalancingV2::LoadBalancer":
            case "AWS::ElasticLoadBalancingV2::TargetGroup":
            case "AWS::IAM::ManagedPolicy":
            case "AWS::Lambda::Alias":
            case "AWS::Lambda::Version":
            case "AWS::OpsWorks::UserProfile":
            case "AWS::SNS::Topic":
            case "AWS::StepFunctions::Activity":
            case "AWS::StepFunctions::StateMachine":

                // these AWS resources return their ARN using `!Ref`
                return AModelProcessor.FnRef(logicalId);
            default:

                // most AWS resources expose an `Arn` attribute that we need to use
                return AModelProcessor.FnGetAtt(logicalId, "Arn");
            }
        }

        public bool TryParseResourceProperties(
            string awsType,
            object arnReference,
            object properties,
            out object resourceAsStatementFn,
            out Humidifier.Resource resourceTemplate
        ) {
            var type = GetHumidifierType(awsType);
            if(type == null) {
                resourceAsStatementFn = null;
                resourceTemplate = null;
                return false;
            }
            if(properties == null) {
                resourceTemplate = (Humidifier.Resource)Activator.CreateInstance(type);
            } else {
                if(properties is IDictionary<string, object> dictionary) {

                    // NOTE (2018-09-05, bjorg): Humidifier appends a '_' to property names
                    //  that conflict with the typename. This mimics the behavior by doing the
                    // thing before we attempt to deserialize into the target type.
                    var typeName = type.Name;
                    if(dictionary.TryGetValue(typeName, out object value)) {
                        dictionary.Remove(typeName);
                        dictionary[typeName + "_"] = value;
                    }
                }
                resourceTemplate = (Humidifier.Resource)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(properties), type);
            }

            // determine how we can get the ARN for the resource, which is used when we grant IAM permissions
            switch(awsType) {
            case "AWS::S3::Bucket":

                // S3 Bucket resources must be granted permissions on the bucket AND the keys
                resourceAsStatementFn = new object[] {
                    arnReference,
                    AModelProcessor.FnJoin("", new List<object> { arnReference, "/*" })
                };
                break;
            case "AWS::DynamoDB::Table":

                // DynamoDB resources must be granted permissions on the table AND the stream AND the index
                resourceAsStatementFn = new object[] {
                    arnReference,
                    AModelProcessor.FnJoin("/", new List<object> { arnReference, "stream/*" }),
                    AModelProcessor.FnJoin("/", new List<object> { arnReference, "index/*" })
                };
                break;
            default:

                // most AWS resources just require the ARN reference
                resourceAsStatementFn = arnReference;
                break;
            }
            return true;
        }

        public bool IsResourceTypeSupported(string awsType) => GetHumidifierType(awsType) != null;

        private Type GetHumidifierType(string awsType) {
            const string AWS_PREFIX = "AWS::";
            if(!awsType.StartsWith(AWS_PREFIX)) {
                return null;
            }
            var typeName = "Humidifier." + awsType.Substring(AWS_PREFIX.Length).Replace("::", ".");
            return typeof(Humidifier.Resource).Assembly.GetType(typeName, throwOnError: false);
        }
    }
}
