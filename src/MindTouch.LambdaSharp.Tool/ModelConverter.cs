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
using System.Linq;
using MindTouch.LambdaSharp.Tool.Model;
using MindTouch.LambdaSharp.Tool.Model.AST;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelParserException : Exception {

        //--- Constructors ---
        public ModelParserException(string message) : base(message) { }
    }

    public class ModelConverter : AModelProcessor {

        //--- Fields ---
        private Module _module;

        //--- Constructors ---
        public ModelConverter(Settings settings) : base(settings) { }

        //--- Methods ---
        public Module Process(ModuleNode module) {

            // convert module file
            try {
                return Convert(module);
            } catch(Exception e) {
                AddError($"internal error: {e.Message}", e);
                return null;
            }
        }

        private Module Convert(ModuleNode module) {

            // initialize module
            _module = new Module {
                Name = module.Name,
                Version = Version.Parse(module.Version),
                Settings = Settings,
                Description = module.Description,
                Functions = new List<Function>()
            };

            // append the version to the module description
            if(_module.Description != null) {
                _module.Description = _module.Description.TrimEnd() + $" (v{module.Version})";
            }

            // convert 'Version' attribute to implicit 'Version' parameter
            module.Parameters.Add(new ParameterNode {
                Name = "Version",
                Value = module.Version,
                Description = "LambdaSharp module version",
                Export = "Version"
            });

            // check if we need to add a 'RollbarToken' parameter node
            if(
                (Settings.RollbarCustomResourceTopicArn != null)
                && module.Functions.Any()
                && !module.Parameters.Any(param => param.Name == "RollbarToken")
            ) {
                module.Parameters.Add(new ParameterNode {
                    Name = "RollbarToken",
                    Description = "Rollbar project token",
                    Resource = new ResourceNode {
                        Type = "Custom::LambdaSharpRollbarProject",
                        Allow = "None",
                        Properties = new Dictionary<string, object> {
                            ["ServiceToken"] = Settings.RollbarCustomResourceTopicArn,
                            ["Tier"] = Settings.Tier,
                            ["Module"] = _module.Name,

                            // NOTE (2018-08-05, bjorg): set old values for backwards compatibility
                            ["Project"] = _module.Name,
                            ["Deployment"] = Settings.Tier
                        }
                    }
                });
            }

            // convert secrets
            var secretIndex = 0;
            _module.Secrets = AtLocation("Secrets", () => module.Secrets
                .Select(secret => ConvertSecret(++secretIndex, secret))
                .Where(secret => secret != null)
                .ToList()
            , new List<string>());

            // convert parameters
            if(module.Parameters.Any(p => p.Package != null)) {

                // check if a deployment bucket exists
                if(Settings.DeploymentBucketName == null) {
                    AddError("deploying packages requires a deployment bucket", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                }

                // check if S3 package loader topic arn exists
                if(Settings.S3PackageLoaderCustomResourceTopicArn == null) {
                    AddError("parameter package requires S3PackageLoader custom resource handler to be deployed", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                }
            }
            _module.Parameters = AtLocation("Parameters", () => ConvertParameters(module.Parameters), null) ?? new List<AParameter>();

            // create functions
            if(module.Functions.Any()) {

                // check if a dead-letter queue was specified
                if(Settings.DeadLetterQueueUrl == null) {
                    AddError("deploying functions requires a dead-letter queue", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                }

                // check if a logging topic was set
                if(Settings.LoggingTopicArn == null) {
                    AddError("deploying functions requires a logging topic", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                }
                var functionIndex = 0;
                _module.Functions = AtLocation("Functions", () => module.Functions
                    .Select(function => ConvertFunction(++functionIndex, function))
                    .Where(function => function != null)
                    .ToList()
                , null) ?? new List<Function>();
            }
            return _module;
        }

        public string ConvertSecret(int index, string secret) {
            return AtLocation($"[{index}]", () => {
                if(secret.StartsWith("arn:")) {

                    // decryption keys provided with their ARN can be added as is; no further steps required
                    return secret;
                }

                // assume key name is an alias and resolve it to its ARN
                try {
                    var response = Settings.KmsClient.DescribeKeyAsync($"alias/{secret}").Result;
                    return response.KeyMetadata.Arn;
                } catch(Exception e) {
                    AddError($"failed to resolve key alias: {secret}", e);
                    return null;
                }
            }, null);
        }

        public IList<AParameter> ConvertParameters(
            IList<ParameterNode> parameters,
            string resourcePrefix = ""
        ) {
            var resultList = new List<AParameter>();
            if((parameters == null) || !parameters.Any()) {
                return resultList;
            }

            // convert all parameters
            var index = 0;
            foreach(var parameter in parameters) {
                ++index;
                var parameterName = parameter.Name ?? $"[{index}]";
                AParameter result = null;
                var parameterFullName = resourcePrefix + parameter.Name;
                AtLocation(parameterName, () => {
                    if(parameter.Secret != null) {

                        // encrypted value
                        AtLocation("Secret", () => {
                            result = new SecretParameter {
                                Name = parameter.Name,
                                Description = parameter.Description,
                                Secret = parameter.Secret,
                                Export = parameter.Export,
                                EncryptionContext = parameter.EncryptionContext
                            };
                        });
                    } else if(parameter.Values != null) {

                        // list of values
                        AtLocation("Values", () => {
                            result = new StringListParameter {
                                Name = parameter.Name,
                                Description = parameter.Description,
                                Values = parameter.Values,
                                Export = parameter.Export
                            };
                        });

                        // TODO (2018-08-19, bjorg): this implementation creates unnecessary parameters
                        if(parameter.Resource != null) {
                            AtLocation("Resource", () => {

                                // enumerate individual values with resource definition for each
                                parameter.Parameters = new List<ParameterNode>();
                                for(var i = 1; i <= parameter.Values.Count; ++i) {
                                    parameter.Parameters.Add(new ParameterNode {
                                        Name = $"Index{i}",
                                        Value = parameter.Values[i - 1],
                                        Resource = parameter.Resource
                                    });
                                }
                            });
                        }
                    } else if(parameter.Package != null) {

                        // package value
                        var s3 = parameter.Package.S3Location.ToS3Info();
                        result = new PackageParameter {
                            Name = parameter.Name,
                            Description = parameter.Description,
                            DestinationBucketParameterName = parameter.Package.Bucket,
                            DestinationKeyPrefix = parameter.Package.Prefix ?? "",
                            PackageBucket = s3.Bucket,
                            PackageKey = s3.Key,
                        };
                    } else if(parameter.Value != null) {
                        if(parameter.Resource != null) {
                            AtLocation("Resource", () => {

                                // existing resource
                                var resource = ConvertResource(parameter.Value, parameter.Resource);
                                result = new ReferencedResourceParameter {
                                    Name = parameter.Name,
                                    Description = parameter.Description,
                                    Resource = resource
                                };
                            });
                        } else {

                            // plaintext value
                            result = new StringParameter {
                                Name = parameter.Name,
                                Description = parameter.Description,
                                Value = parameter.Value
                            };
                        }
                    } else if(parameter.Resource != null) {

                        // managed resource
                        AtLocation("Resource", () => {
                            result = new CloudFormationResourceParameter {
                                Name = parameter.Name,
                                Description = parameter.Description,
                                Resource = ConvertResource(null, parameter.Resource)
                            };
                        });
                    }
                });

                // check if there are nested parameters
                if(parameter.Parameters != null) {
                    AtLocation("Parameters", () => {
                        var nestedParameters = ConvertParameters(
                            parameter.Parameters,
                            parameterFullName
                        );

                        // keep nested parameters only if they have values
                        if(nestedParameters.Any()) {

                            // create empty string parameter if collection has no value
                            result = result ?? new StringParameter {
                                Name = parameter.Name,
                                Value = "",
                                Description = parameter.Description,
                                Export = parameter.Export
                            };
                            result.Parameters = nestedParameters;
                        }
                    });
                }

                // add parameter
                if(result != null) {
                    result.FullName = parameterFullName;
                    result.Export = parameter.Export;
                    resultList.Add(result);
                }
            }
            return resultList;
        }

        public Resource ConvertResource(string resourceArn, ResourceNode resource) {

            // parse resource type
            var resourceType = "<BAD>";
            if(resource.Type == null) {
                if(resourceArn != null) {
                    resource.Type = "AWS";
                } else {
                    AddError("missing Type field");
                }
            } else {
                resourceType = AtLocation("Type", () => {
                    if(
                        !resource.Type.StartsWith("Custom::")
                        && !Settings.ResourceMapping.IsResourceTypeSupported(resource.Type)
                    ) {
                        AddError($"unsupported resource type: {resource.Type}");
                        return "<BAD>";
                    }
                    return resource.Type;
                }, "<BAD>");
            }

            // parse resource allowed operations
            var allowList = new List<string>();
            if((resource.Type != null) && (resource.Allow != null)) {
                AtLocation("Allow", () => {
                    if(resource.Allow is string inlineValue) {

                        // inline values can be separated by `,`
                        allowList.AddRange(inlineValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    } else if(resource.Allow is IList<object> allowed) {
                        allowList = allowed.Cast<string>().ToList();
                    } else {
                        AddError("invalid allow value");
                        return;
                    }

                    // resolve shorthands and de-duplicated statements
                    var allowSet = new HashSet<string>();
                    foreach(var allowStatement in allowList) {
                        if(allowStatement == "None") {

                            // nothing to do
                        } else if(allowStatement.Contains(':')) {

                            // AWS permission statements always contain a `:` (e.g `ssm:GetParameter`)
                            allowSet.Add(allowStatement);
                        } else if(Settings.ResourceMapping.TryResolveAllowShorthand(resourceType, allowStatement, out IList<string> allowedList)) {
                            foreach(var allowed in allowedList) {
                                allowSet.Add(allowed);
                            }
                        } else {
                            AddError($"could not find IAM mapping for short-hand '{allowStatement}' on AWS type '{resource.Type}'");
                        }
                    }
                    allowList = allowSet.OrderBy(text => text).ToList();
                });
            }

            // ensure the local resource name is an ARN or wildcard
            if(resourceArn != null) {
                if(!resourceArn.StartsWith("arn:") && (resourceArn != "*")) {
                    AddError($"resource name must be a valid ARN or wildcard: {resourceArn}");
                }
                if(resource.Properties != null) {
                    AddError($"referenced resource '{resourceArn}' cannot set properties");
                }
            }
            return new Resource {
                Type = resourceType,
                ResourceArn = resourceArn,
                Allow = allowList,
                Properties = resource.Properties
            };
        }

        public Function ConvertFunction(int index, FunctionNode function) {
            return AtLocation(function.Name ?? $"[{index}]", () => {

                // initialize VPC configuration if provided
                FunctionVpc vpc = null;
                if(function.VPC?.Any() ?? false) {
                    if(
                        function.VPC.TryGetValue("SubnetIds", out var subnets)
                        && function.VPC.TryGetValue("SecurityGroupIds", out var securityGroups)
                    ) {
                        AtLocation("VPC", () => {
                            vpc = new FunctionVpc {
                                SubnetIds = subnets,
                                SecurityGroupIds = securityGroups
                            };
                        });
                    } else {
                        AddError("Lambda function contains a VPC definition that does not include SubnetIds or SecurityGroupIds");
                    }
                }

                // create function
                var eventIndex = 0;
                return new Function {
                    Name = function.Name ,
                    Description = function.Description,
                    Sources = AtLocation("Sources", () => function.Sources?.Select(source => ConvertFunctionSource(++eventIndex, source)).Where(evt => evt != null).ToList(), null) ?? new List<AFunctionSource>(),
                    S3Location = function.S3Location,
                    Handler = function.Handler,
                    Runtime = function.Runtime,
                    Memory = function.Memory,
                    Timeout = function.Timeout,
                    ReservedConcurrency = function.ReservedConcurrency,
                    VPC = vpc,
                    Environment = function.Environment ?? new Dictionary<string, string>(),
                    Export = function.Export
                };
            }, null);
        }

        public AFunctionSource ConvertFunctionSource(int index, FunctionSourceNode source) {
            return AtLocation<AFunctionSource>($"{index}", () => {
                if(source.Topic != null) {
                    return new TopicSource {
                        TopicName = source.Topic
                    };
                }
                if(source.Schedule != null) {
                    return AtLocation("Schedule", () => {
                        return new ScheduleSource {
                            Expression = source.Schedule,
                            Name = source.Name
                        };
                    }, null);
                }
                if(source.Api != null) {
                    return AtLocation("Api", () => {

                        // extract http method from route
                        var api = source.Api.Trim();
                        var pathSeparatorIndex = api.IndexOfAny(new[] { ':', ' ' });
                        if(pathSeparatorIndex < 0) {
                            AddError("invalid api format");
                            return new ApiGatewaySource {
                                Method = "ANY",
                                Path = new string[0],
                                Integration = ApiGatewaySourceIntegration.RequestResponse
                            };
                        }
                        var method = api.Substring(0, pathSeparatorIndex).ToUpperInvariant();
                        if(method == "*") {
                            method = "ANY";
                        }
                        var path = api.Substring(pathSeparatorIndex + 1).TrimStart().Split('/', StringSplitOptions.RemoveEmptyEntries);

                        // parse integration into a valid enum
                        var integration = AtLocation("Integration", () => Enum.Parse<ApiGatewaySourceIntegration>(source.Integration ?? "RequestResponse", ignoreCase: true), ApiGatewaySourceIntegration.Unsupported);
                        return new ApiGatewaySource {
                            Method = method,
                            Path = path,
                            Integration = integration
                        };
                    }, null);
                }
                if(source.SlackCommand != null) {
                    return AtLocation("SlackCommand", () => {

                        // parse integration into a valid enum
                        return new ApiGatewaySource {
                            Method = "POST",
                            Path = source.SlackCommand.Split('/', StringSplitOptions.RemoveEmptyEntries),
                            Integration = ApiGatewaySourceIntegration.SlackCommand
                        };
                    }, null);
                }
                if(source.S3 != null) {
                    return new S3Source {
                        Bucket = source.S3,
                        Events = source.Events ?? new List<string> {

                            // default S3 events to listen to
                            "s3:ObjectCreated:*"
                        },
                        Prefix = source.Prefix,
                        Suffix = source.Suffix
                    };
                }
                if(source.Sqs != null) {
                    return new SqsSource {
                        Queue = source.Sqs,
                        BatchSize = source.BatchSize ?? 10
                    };
                }
                if(source.Alexa != null) {
                    var alexaSkillId = string.IsNullOrWhiteSpace(source.Alexa) || (source.Alexa == "*")
                        ? null
                        : source.Alexa;
                    return new AlexaSource {
                        EventSourceToken = alexaSkillId
                    };
                }
                AddError("empty event");
                return null;
            }, null);
            throw new ModelParserException("invalid function event");
        }
    }
}