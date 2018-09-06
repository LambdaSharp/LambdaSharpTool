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
            var assembly = typeof(ResourceMapping).GetTypeInfo().Assembly;
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

        public bool TryParseResourceProperties(
            string awsType,
            string logicalId,
            object properties,
            out object resourceArnFn,
            out object resourceParamFn,
            out Humidifier.Resource resourceTemplate
        ) {
            var type = GetHumidifierType(awsType);
            if(type == null) {
                resourceArnFn = null;
                resourceParamFn = null;
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
                resourceArnFn = Fn.Ref(logicalId);
                resourceParamFn = Fn.Ref(logicalId);
                break;
            case "AWS::S3::Bucket":

                // S3 Bucket resources must be granted permissions on the bucket AND the keys
                resourceArnFn = new object[] {
                    Fn.GetAtt(logicalId, "Arn"),
                    Fn.Join("", Fn.GetAtt(logicalId, "Arn"), "/*")
                };
                resourceParamFn = Fn.Ref(logicalId);
                break;
            case "AWS::DynamoDB::Table":

                // DynamoDB resources must be granted permissions on the table AND the stream
                resourceArnFn = new object[] {
                    Fn.GetAtt(logicalId, "Arn"),
                    Fn.Join("", Fn.GetAtt(logicalId, "Arn"), "/stream/*")
                };
                resourceParamFn = Fn.Ref(logicalId);
                break;
            default:

                // most AWS resources expose an `Arn` attribute that we need to use
                resourceArnFn = Fn.GetAtt(logicalId, "Arn");
                resourceParamFn = Fn.Ref(logicalId);
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
