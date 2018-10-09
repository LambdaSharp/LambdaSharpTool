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
using System.Text.RegularExpressions;
using MindTouch.LambdaSharp.Tool.Model;
using MindTouch.LambdaSharp.Tool.Model.AST;

namespace MindTouch.LambdaSharp.Tool {
    using Fn = Humidifier.Fn;

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
                Version = module.Version,
                Description = module.Description,
                Functions = new List<Function>()
            };

            // append the version to the module description
            if(_module.Description != null) {
                _module.Description = _module.Description.TrimEnd() + $" (v{module.Version})";
            }

            // convert secrets
            var secretIndex = 0;
            _module.Secrets = AtLocation("Secrets", () => module.Secrets
                .Select(secret => ConvertSecret(++secretIndex, secret))
                .Where(secret => secret != null)
                .ToList()
            , new List<string>());

            // convert inputs
            var inputIndex = 0;
            _module.Inputs = AtLocation("Inputs", () => module.Inputs
                .Select(input => ConvertInput(++inputIndex, input))
                .Where(input => input != null)
                .ToList()
            , null) ?? new List<Input>();

            // convert parameters
            _module.Variables = AtLocation("Variables", () => ConvertParameters(module.Variables), null) ?? new List<AParameter>();
            _module.Parameters = AtLocation("Parameters", () => ConvertParameters(module.Parameters), null) ?? new List<AParameter>();

            // create functions
            var functionIndex = 0;
            _module.Functions = AtLocation("Functions", () => module.Functions
                .Select(function => ConvertFunction(++functionIndex, function))
                .Where(function => function != null)
                .ToList()
            , null) ?? new List<Function>();

            // convert outputs
            var outputIndex = 0;
            _module.Outputs = AtLocation("Outputs", () => module.Outputs
                .Select(output => ConvertOutput(++outputIndex, output))
                .Where(output => output != null)
                .ToList()
            , null) ?? new List<AOutput>();

            // add module variables
            _module.Variables.Add(new ValueParameter {
                Name = "Module",
                ResourceName = "Module",
                Description = "LambdaSharp module information",
                Parameters = new List<AParameter> {
                    new ValueParameter {
                        Name = "Id",
                        ResourceName = "ModuleId",
                        Description = "LambdaSharp module id",
                        Value = Fn.Ref("ModuleId")
                    },
                    new ValueParameter {
                        Name = "Name",
                        ResourceName = "ModuleName",
                        Description = "LambdaSharp module name",
                        Value = _module.Name
                    },
                    new ValueParameter {
                        Name = "Version",
                        ResourceName = "ModuleVersion",
                        Description = "LambdaSharp module version",
                        Value = _module.Version
                    }

                    // TODO (2010-10-05, bjorg): add `Module::RestApi` as well?
                    //  Fn.Sub("https://${ModuleRestApi}.execute-api.${AWS::Region}.${AWS::URLSuffix}/LATEST/")
                }
            });
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

        public Input ConvertInput(int index, InputNode input) {
            return AtLocation(input.Name ?? $"[{index}]", () => {

                // TODO (2018-10-04, bjorg): convert import into proper format (Module.ExportName --> ???)

                object reference;
                if(input.Import != null) {

                    // If condition is set, the parameter uses the `!ImportValue` function, otherwise it's just a `!Ref`
                    //  UseFoo: FooIsImport ? ($Tier + "-" + split($Foo, "!Import:")[1]) : $Foo
                    reference = Fn.If($"{input.Name}IsImport", Fn.ImportValue(Fn.Join("-", Fn.Ref("Tier"), Fn.Select("1", Fn.Split("!Import:", Fn.Ref(input.Name))))), Fn.Ref(input.Name));
                } else {
                    reference = Fn.Ref(input.Name);
                }
                return new Input {
                    Name = input.Name,
                    Description = input.Description,
                    Type = input.Type,
                    Default = input.Default,
                    Import = input.Import,
                    Reference = reference
                };
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
                                EncryptionContext = parameter.EncryptionContext
                            };
                        });
                    } else if(parameter.Values != null) {
                        if(parameter.Resource != null) {
                            AtLocation("Resource", () => {

                                // list of existing resources
                                var resource = ConvertResource(parameter.Values, parameter.Resource);
                                result = new ReferencedResourceParameter {
                                    Name = parameter.Name,
                                    Description = parameter.Description,
                                    Resource = resource
                                };
                            });
                        } else {

                            // list of values
                            AtLocation("Values", () => {
                                result = new ValueListParameter {
                                    Name = parameter.Name,
                                    Description = parameter.Description,
                                    Values = parameter.Values
                                };
                            });
                        }
                    } else if(parameter.Package != null) {

                        // package value
                        result = new PackageParameter {
                            Name = parameter.Name,
                            Description = parameter.Description,
                            DestinationBucketParameterName = parameter.Package.Bucket,
                            DestinationKeyPrefix = parameter.Package.Prefix ?? "",
                            PackagePath = parameter.Package.PackagePath
                        };
                    } else if(parameter.Value != null) {
                        if(parameter.Resource != null) {
                            AtLocation("Resource", () => {

                                // existing resource
                                var resource = ConvertResource(new List<object> { parameter.Value }, parameter.Resource);
                                result = new ReferencedResourceParameter {
                                    Name = parameter.Name,
                                    Description = parameter.Description,
                                    Resource = resource
                                };
                            });
                        } else {
                            result = new ValueParameter {
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
                                Resource = ConvertResource(new List<object>(), parameter.Resource)
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
                            result = result ?? new ValueParameter {
                                Name = parameter.Name,
                                Value = "",
                                Description = parameter.Description
                            };
                            result.Parameters = nestedParameters;
                        }
                    });
                }

                // add parameter
                if(result != null) {
                    result.ResourceName = parameterFullName;
                    resultList.Add(result);
                }
            }
            return resultList;
        }

        public Resource ConvertResource(IList<object> resourceReferences, ResourceNode resource) {

            // parse resource allowed operations
            var allowList = new List<string>();
            if(resource.Allow != null) {
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
                        } else if(Settings.ResourceMapping.TryResolveAllowShorthand(resource.Type, allowStatement, out IList<string> allowedList)) {
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

            // parse resource name as `{MODULE}::{TYPE}` pattern to import the custom resource topic name
            AtLocation("Type", () => {
                var customResourceHandlerAndType = resource.Type.Split("::", 2);
                if((customResourceHandlerAndType[0] != "AWS") && (customResourceHandlerAndType[0] != "Custom")) {
                    if(customResourceHandlerAndType.Length != 2) {
                        AddError("custom resource type must have format {MODULE}::{TYPE}");
                        return;
                    }

                    // check if custom resource needs a service token to be imported
                    if(resource.Properties == null) {
                        resource.Properties = new Dictionary<string, object>();
                    }
                    if(!resource.Properties.ContainsKey("ServiceToken")) {
                        resource.Properties["ServiceToken"] = Fn.ImportValue(Fn.Sub($"${{Tier}}-CustomResource-{resource.Type}"));
                    }

                    // convert type name to a custom AWS resource type
                    resource.Type = "Custom::" + resource.Type.Replace("::", "");
                }
            });
            return new Resource {
                Type = resource.Type,
                ResourceReferences = resourceReferences,
                Allow = allowList,
                Properties = resource.Properties,
                DependsOn = resource.DependsOn
            };
        }

        public Function ConvertFunction(int index, FunctionNode function) {
            return AtLocation(function.Name ?? $"[{index}]", () => {

                // append the version to the function description
                if(function.Description != null) {
                    function.Description = function.Description.TrimEnd() + $" (v{_module.Version})";
                }

                // initialize VPC configuration if provided
                FunctionVpc vpc = null;
                if(function.VPC?.Any() == true) {
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
                    Name = function.Name,
                    Description = function.Description,
                    Sources = AtLocation("Sources", () => function.Sources?.Select(source => ConvertFunctionSource(function, ++eventIndex, source)).Where(evt => evt != null).ToList(), null) ?? new List<AFunctionSource>(),
                    PackagePath = function.PackagePath,
                    Handler = function.Handler,
                    Runtime = function.Runtime,
                    Memory = function.Memory,
                    Timeout = function.Timeout,
                    ReservedConcurrency = function.ReservedConcurrency,
                    VPC = vpc,
                    Environment = function.Environment ?? new Dictionary<string, object>()
                };
            }, null);
        }

        public AFunctionSource ConvertFunctionSource(FunctionNode function, int index, FunctionSourceNode source) {
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
                            Integration = integration,
                            OperationName = source.OperationName,
                            ApiKeyRequired = source.ApiKeyRequired
                        };
                    }, null);
                }
                if(source.SlackCommand != null) {
                    return AtLocation("SlackCommand", () => {

                        // parse integration into a valid enum
                        return new ApiGatewaySource {
                            Method = "POST",
                            Path = source.SlackCommand.Split('/', StringSplitOptions.RemoveEmptyEntries),
                            Integration = ApiGatewaySourceIntegration.SlackCommand,
                            OperationName = source.OperationName
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
                if(source.DynamoDB != null) {
                    return new DynamoDBSource {
                        DynamoDB = source.DynamoDB,
                        BatchSize = source.BatchSize ?? 100,
                        StartingPosition = source.StartingPosition ?? "LATEST"
                    };
                }
                if(source.Kinesis != null) {
                    return new KinesisSource {
                        Kinesis = source.Kinesis,
                        BatchSize = source.BatchSize ?? 100,
                        StartingPosition = source.StartingPosition ?? "LATEST"
                    };
                }
                if(source.Macro != null) {
                    var macroName = source.Macro;
                    if((macroName == "") || (macroName == "*")) {
                        macroName = function.Name;
                    }
                    return new MacroSource {
                        MacroName = macroName
                    };
                }
                return null;
            }, null);
        }

        private AOutput ConvertOutput(int index, OutputNode output) {
            return AtLocation<AOutput>(output.Name ?? $"[{index}]", () => {
                if(output.Name != null) {
                    return new StackOutput {
                        Name = output.Name,
                        Description = output.Description,
                        Value = output.Value
                    };
                }
                if(output.Export != null) {
                    return new ExportOutput {
                        Name = output.Name,
                        Description = output.Description,
                        Value = output.Value
                    };
                }
                if(output.CustomResource != null) {
                    return new CustomResourceHandlerOutput {
                        CustomResourceName = output.CustomResource,
                        Description = output.Description,
                        Handler = output.Handler
                    };
                }
                throw new ModelParserException("invalid output type");
            }, null);
        }
    }
}