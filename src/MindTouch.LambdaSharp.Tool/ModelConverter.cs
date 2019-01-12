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
using System.Text;
using System.Text.RegularExpressions;
using MindTouch.LambdaSharp.Tool.Model;
using MindTouch.LambdaSharp.Tool.Model.AST;

namespace MindTouch.LambdaSharp.Tool {
    using Fn = Humidifier.Fn;
    using Condition = Humidifier.Condition;

    public class ModelParserException : Exception {

        //--- Constructors ---
        public ModelParserException(string message) : base(message) { }
    }

    public class ModelConverter : AModelProcessor {

        //--- Constants ---
        private const string CUSTOM_RESOURCE_PREFIX = "Custom::";

        //--- Fields ---
        private Module _module;

        //--- Constructors ---
        public ModelConverter(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public Module Process(ModuleNode module) {

            // convert module definition
            try {
                return Convert(module);
            } catch(Exception e) {
                AddError(e);
                return null;
            }
        }

        private Module Convert(ModuleNode module) {

            // initialize module
            _module = new Module {
                Name = module.Module,
                Version = VersionInfo.Parse(module.Version),
                Description = module.Description,
                Pragmas = module.Pragmas,
                Functions = new List<Function>()
            };

            // append the version to the module description
            if(_module.Description != null) {
                _module.Description = _module.Description.TrimEnd() + $" (v{module.Version})";
            }

            // convert parameters
            var parameters = new List<AParameter>();
            parameters.AddRange(AtLocation("Inputs", () => ConvertInputs(module, module.Inputs), null) ?? new List<AParameter>());

            // add LambdaSharp Module Options
            var section = "LambdaSharp Module Options";
            parameters.AddRange(AtLocation("Inputs", () => ConvertInputs(module, new InputNode[] {
                new InputNode {
                    Parameter = "Secrets",
                    Section = section,
                    Label = "Secret Keys (ARNs)",
                    Description = "Comma-separated list of optional secret keys",
                    Default = ""
                }
            }), null) ?? new List<AParameter>());

            // add standard parameters (unless requested otherwise)
            if(!_module.HasPragma("no-lambdasharp-dependencies")) {

                // add LambdaSharp Module Internal Dependencies
                section = "LambdaSharp Dependencies";
                parameters.AddRange(AtLocation("Inputs", () => ConvertInputs(module, new InputNode[] {
                    new InputNode {
                        Import = "LambdaSharp::DeadLetterQueueArn",
                        Section = section,
                        Label = "Dead Letter Queue (ARN)",
                        Description = "Dead letter queue for functions"
                    },
                    new InputNode {
                        Import = "LambdaSharp::LoggingStreamArn",
                        Section = section,
                        Label = "Logging Stream (ARN)",
                        Description = "Logging kinesis stream for functions"
                    },
                    new InputNode {
                        Import = "LambdaSharp::DefaultSecretKeyArn",
                        Section = section,
                        Label = "Secret Key (ARN)",
                        Description = "Default secret key for functions"
                    }
                }), null) ?? new List<AParameter>());
            }

            // add LambdaSharp Deployment Settings
            section = "LambdaSharp Deployment Settings (DO NOT MODIFY)";
            parameters.AddRange(AtLocation("Inputs", () => ConvertInputs(module, new InputNode[] {
                new InputNode {
                    Parameter = "DeploymentBucketName",
                    Section = section,
                    Label = "Deployment S3 Bucket",
                    Description = "Source deployment S3 bucket name"
                },
                new InputNode {
                    Parameter = "DeploymentPrefix",
                    Section = section,
                    Label = "Deployment Prefix",
                    Description = "Module deployment prefix"
                },
                new InputNode {
                    Parameter = "DeploymentPrefixLowercase",
                    Section = section,
                    Label = "Deployment Prefix (lowercase)",
                    Description = "Module deployment prefix (lowercase)"
                },
                new InputNode {
                    Parameter = "DeploymentParent",
                    Section = section,
                    Label = "Parent Stack Name",
                    Description = "Parent stack name for nested deployments, blank otherwise",
                    Default = ""
                },
            }), null) ?? new List<AParameter>());
            parameters.AddRange(AtLocation("Variables", () => ConvertParameters(module, module.Variables), null) ?? new List<AParameter>());
            _module.Parameters = parameters;

            // convert secrets (NOTE: must happen AFTER parameters are initialized)
            var secretIndex = 0;
            _module.Secrets = AtLocation("Secrets", () => module.Secrets
                .Select(secret => ConvertSecret(++secretIndex, secret))
                .Where(secret => secret != null)
                .ToList()
            , new List<object>());

            // add default secrets key that is imported from the input parameters
            if(!_module.HasPragma("no-lambdasharp-dependencies")) {
                _module.Secrets.Add(_module.GetParameter("LambdaSharp::DefaultSecretKeyArn").Reference);
            }

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
            var moduleParameters = new List<AParameter> {
                new ValueParameter {
                    Scope = new List<string>(),
                    Name = "Id",
                    ResourceName = "ModuleId",
                    Description = "LambdaSharp module id",
                    Reference = FnRef("AWS::StackName")
                },
                new ValueParameter {
                    Scope = new List<string>(),
                    Name = "Name",
                    ResourceName = "ModuleName",
                    Description = "Module name",
                    Reference = _module.Name
                },
                new ValueParameter {
                    Scope = new List<string>(),
                    Name = "Version",
                    ResourceName = "ModuleVersion",
                    Description = "Module version",
                    Reference = _module.Version.ToString()
                }
            };
            if(!module.Pragmas.Contains("no-lambdasharp-dependencies")) {
                moduleParameters.AddRange(new List<AParameter> {
                    new ValueParameter {
                        Scope = new List<string>(),
                        Name = "DeadLetterQueueArn",
                        ResourceName = "ModuleDeadLetterQueueArn",
                        Description = "LambdaSharp Dead Letter Queue",
                        Reference = FnRef("LambdaSharp::DeadLetterQueueArn")
                    },
                    new ValueParameter {
                        Scope = new List<string>(),
                        Name = "LoggingStreamArn",
                        ResourceName = "ModuleLoggingStreamArn",
                        Description = "LambdaSharp Logging Stream",
                        Reference = FnRef("LambdaSharp::LoggingStreamArn")
                    }
                });
            }
            if(module.Functions.Any(function => function.Sources.Any(source => (source.Api != null) || (source.SlackCommand != null)))) {
                moduleParameters.AddRange(new List<AParameter> {

                    // TODO (2010-10-19, bjorg): figure out how to make this work

                    // new CloudFormationResourceParameter {
                    //     Scope = new List<string>(),
                    //     Name = "RestApi",
                    //     ResourceName = "ModuleRestApi",
                    //     Description = $"{_module.Name} API (v{_module.Version})",
                    //     Reference = FnRef("ModuleRestApi"),
                    //     Resource = new Resource {
                    //         Type = "AWS::ApiGateway::RestApi",
                    //         ResourceReferences = new List<object>(),
                    //         Properties = new Dictionary<string, object> {
                    //             ["Name"] = FnSub("${AWS::StackName} Module API"),
                    //             ["Description"] = $"{_module.Name} API (v{_module.Version})",
                    //             ["FailOnWarnings"] = true
                    //         }
                    //     }
                    // },

                    // // TODO (2018-10-30, bjorg): convert to a resource
                    // new ValueParameter {
                    //     Scope = new List<string>(),
                    //     Name = "RestApiStage",
                    //     ResourceName = "ModuleRestApiStage",
                    //     Description = "LambdaSharp module REST API",
                    //     Reference = FnRef("ModuleRestApiStage")
                    // },
                    // new ValueParameter {
                    //     Scope = new List<string>(),
                    //     Name = "RestApiUrl",
                    //     ResourceName = "ModuleRestApiUrl",
                    //     Description = "LambdaSharp module REST API URL",
                    //     Reference = FnSub("https://${Module::RestApi}.execute-api.${AWS::Region}.${AWS::URLSuffix}/LATEST/")
                    // }
                });
            }
            _module.Parameters.Add(new ValueParameter {
                Scope = new List<string>(),
                Name = "Module",
                ResourceName = "Module",
                Description = "LambdaSharp module information",
                Reference = "",
                Parameters = moduleParameters
            });
            return _module;
        }

        private object ConvertSecret(int index, string secret) {
            return AtLocation($"[{index}]", () => {
                if(secret.StartsWith("arn:")) {

                    // decryption keys provided with their ARN can be added as is; no further steps required
                    return secret;
                }

                // assume key name is an alias and resolve it to its ARN
                try {
                    var response = Settings.KmsClient.DescribeKeyAsync(secret).Result;
                    return response.KeyMetadata.Arn;
                } catch(Exception e) {
                    AddError($"failed to resolve key alias: {secret}", e);
                    return null;
                }
            }, null);
        }

        private IList<AParameter> ConvertInputs(ModuleNode module, IList<InputNode> inputs) {
            var resultList = new List<AParameter>();
            if((inputs == null) || !inputs.Any()) {
                return resultList;
            }
            var index = 0;
            foreach(var input in inputs) {
                ++index;
                AtLocation(input.Parameter ?? input.Import, () => {
                    AInputParameter result = null;
                    if(input.Import != null) {
                        var parts = input.Import.Split("::", 2);

                        // find or create parent collection node
                        var parentParameter = resultList.FirstOrDefault(p => p.Name == parts[0]);
                        if(parentParameter == null) {
                            parentParameter = new ValueParameter {
                                Scope = new List<string>(),
                                Name = parts[0],
                                ResourceName = parts[0],
                                Description = $"{parts[0]} cross-module references",
                                Reference = "",
                                Parameters = new List<AParameter>()
                            };
                            resultList.Add(parentParameter);
                        }

                        // create imported input
                        var resourceName = input.Import.Replace("::", "");
                        result = new ImportInputParameter {
                            Name = parts[1],
                            Type = input.Type,
                            ResourceName = resourceName,
                            Reference = FnIf(
                                $"{resourceName}IsImport",
                                FnImportValue(FnSub("${DeploymentPrefix}${Import}", new Dictionary<string, object> {
                                    ["Import"] = FnSelect("1", FnSplit("$", FnRef(resourceName)))
                                })),
                                FnRef(resourceName)
                            ),
                            Import = input.Import
                        };

                        // check if a resource definition is associated with the import statement
                        if(input.Resource != null) {
                            result.Resource = ConvertResource(new List<object> { result.Reference }, input.Resource);
                        }
                        parentParameter.Parameters.Add(result);
                    } else {

                        // create regular input
                        result = new ValueInputParameter {
                            Name = input.Parameter,
                            ResourceName = input.Parameter,
                            Reference = FnRef(input.Parameter),
                            Default = input.Default,
                            ConstraintDescription = input.ConstraintDescription,
                            AllowedPattern = input.AllowedPattern,
                            AllowedValues = input.AllowedValues,
                            MaxLength = input.MaxLength,
                            MaxValue = input.MaxValue,
                            MinLength = input.MinLength,
                            MinValue = input.MinValue
                        };

                        // check if a resource definition is associated with the input statement
                        if(input.Resource != null) {
                            if(input.Default != null) {
                                result.Reference = FnIf(
                                    $"{result.Name}Created",
                                    ResourceMapping.GetArnReference(input.Resource.Type, $"{result.Name}CreatedInstance"),
                                    FnRef(result.Name)
                                );
                            }
                            result.Resource = ConvertResource(new List<object> { result.Reference }, input.Resource);
                        }
                    }
                    if(result != null) {

                        // set AParameter fields
                        result.Scope = ConvertScope(module, input.Scope);
                        result.Description = input.Description;

                        // set AInputParamete fields
                        result.Type = input.Type ?? "String";
                        result.Section = input.Section ?? "Module Settings";
                        result.Label = input.Label ?? PrettifyLabel(input.Import ?? input.Parameter);
                        result.NoEcho = input.NoEcho;

                        // add result, unless it's an cross-module reference
                        if(input.Import == null) {
                            resultList.Add(result);
                        }

                        // local function
                        string PrettifyLabel(string name) {
                            var builder = new StringBuilder();
                            var isUppercase = true;
                            foreach(var c in name) {
                                if(char.IsDigit(c)) {
                                    if(!isUppercase) {
                                        builder.Append(' ');
                                    }
                                    isUppercase = true;
                                    builder.Append(c);
                                } else if(char.IsLetter(c)) {
                                    if(isUppercase) {
                                        isUppercase = char.IsUpper(c);
                                        builder.Append(c);
                                    } else {
                                        if(isUppercase = char.IsUpper(c)) {
                                            builder.Append(' ');
                                        }
                                        builder.Append(c);
                                    }
                                }
                            }
                            return builder.ToString();
                        }
                    }
                });
            }
            return resultList;
        }

        private IList<string> ConvertScope(ModuleNode module, object scope) {
            return AtLocation("Scope", () => {
                if(scope == null) {
                    return new List<string>();
                }

                // resolve scope wildcard
                var scopeNames = ConvertToStringList(scope);
                if(scopeNames.Contains("*")) {
                    scopeNames.Remove("*");
                    scopeNames.AddRange(module.Functions.Select(item => item.Function));
                }
                return scopeNames.Distinct()
                    .OrderBy(item => item)
                    .ToList();
            }, new List<string>());
        }

        private IList<AParameter> ConvertParameters(
            ModuleNode module,
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
                var parameterName = parameter.Var ?? $"[{index}]";
                AParameter result = null;
                var resourceName = resourcePrefix + parameter.Var;
                AtLocation(parameterName, () => {
                    if(parameter.Secret != null) {

                        // encrypted value
                        AtLocation("Secret", () => {
                            result = new SecretParameter {
                                Scope = ConvertScope(module, parameter.Scope),
                                Name = parameter.Var,
                                Description = parameter.Description,
                                Secret = parameter.Secret,
                                EncryptionContext = parameter.EncryptionContext,
                                Reference = parameter.Secret
                            };
                        });
                    } else if(parameter.Package != null) {

                        // package value
                        result = new PackageParameter {
                            Scope = ConvertScope(module, parameter.Scope),
                            Name = parameter.Package,
                            Description = parameter.Description,
                            DestinationBucketParameterName = parameter.Bucket,
                            DestinationKeyPrefix = parameter.Prefix ?? "",
                            PackagePath = parameter.PackagePath,
                            Reference = FnGetAtt(resourceName, "Url")
                        };
                    } else if(parameter.Value != null) {
                        if(parameter.Resource != null) {
                            AtLocation("Resource", () => {

                                // existing resource
                                var resource = ConvertResource((parameter.Value as IList<object>) ?? new List<object> { parameter.Value }, parameter.Resource);
                                result = new ReferencedResourceParameter {
                                    Scope = ConvertScope(module, parameter.Scope),
                                    Name = parameter.Var,
                                    Description = parameter.Description,
                                    Resource = resource,
                                    Reference = FnJoin(",", resource.ResourceReferences)
                                };
                            });
                        } else {
                            result = new ValueParameter {
                                Scope = ConvertScope(module, parameter.Scope),
                                Name = parameter.Var,
                                Description = parameter.Description,
                                Reference = (parameter.Value is IList<object> values)
                                    ? FnJoin(",", values)
                                    : parameter.Value
                            };
                        }
                    } else if(parameter.Resource != null) {

                        // managed resource
                        AtLocation("Resource", () => {
                            var reference = (parameter.Resource.ArnAttribute != null)
                                ? FnGetAtt(resourceName, parameter.Resource.ArnAttribute)
                                : ResourceMapping.GetArnReference(parameter.Resource.Type, resourceName);
                            result = new CloudFormationResourceParameter {
                                Scope = ConvertScope(module, parameter.Scope),
                                Name = parameter.Var,
                                Description = parameter.Description,
                                Resource = ConvertResource(new List<object>(), parameter.Resource),
                                Reference = reference
                            };
                        });
                    }
                });

                // check if there are nested parameters
                if(parameter.Variables != null) {
                    AtLocation("Variables", () => {
                        var nestedParameters = ConvertParameters(
                            module,
                            parameter.Variables,
                            resourceName
                        );

                        // keep nested parameters only if they have values
                        if(nestedParameters.Any()) {

                            // create empty string parameter if collection has no value
                            result = result ?? new ValueParameter {
                                Scope = ConvertScope(module, parameter.Scope),
                                Name = parameter.Var,
                                ResourceName = resourcePrefix + parameter.Var,
                                Description = parameter.Description,
                                Reference = ""
                            };
                            result.Parameters = nestedParameters;
                        }
                    });
                }

                // add parameter
                if(result != null) {
                    result.ResourceName = resourceName;
                    resultList.Add(result);
                }
            }
            return resultList;
        }

        private Resource ConvertResource(IList<object> resourceReferences, ResourceNode resource) {

            // parse resource allowed operations
            var allowList = new List<string>();
            if(resource.Allow != null) {
                AtLocation("Allow", () => {
                    allowList.AddRange(ConvertToStringList(resource.Allow));

                    // resolve shorthands and de-duplicated statements
                    var allowSet = new HashSet<string>();
                    foreach(var allowStatement in allowList) {
                        if(allowStatement == "None") {

                            // nothing to do
                        } else if(allowStatement.Contains(':')) {

                            // AWS permission statements always contain a `:` (e.g `ssm:GetParameter`)
                            allowSet.Add(allowStatement);
                        } else if(ResourceMapping.TryResolveAllowShorthand(resource.Type, allowStatement, out IList<string> allowedList)) {
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

            // check if custom resource needs a service token to be imported
            AtLocation("Type", () => {
                if(!resource.Type.StartsWith("AWS::", StringComparison.Ordinal)) {
                    var customResourceName = resource.Type.StartsWith(CUSTOM_RESOURCE_PREFIX, StringComparison.Ordinal)
                        ? resource.Type.Substring(CUSTOM_RESOURCE_PREFIX.Length)
                        : resource.Type;
                    if(resource.Properties == null) {
                        resource.Properties = new Dictionary<string, object>();
                    }
                    if(!resource.Properties.ContainsKey("ServiceToken")) {
                        resource.Properties["ServiceToken"] = FnImportValue(FnSub($"${{DeploymentPrefix}}CustomResource-{customResourceName}"));
                    }

                    // convert type name to a custom AWS resource type
                    resource.Type = "Custom::" + customResourceName.Replace("::", "");
                }
            });
            return new Resource {
                Type = resource.Type,
                ResourceReferences = resourceReferences,
                Allow = allowList,
                Properties = resource.Properties,
                DependsOn = ConvertToStringList(resource.DependsOn)
            };
        }

        private Function ConvertFunction(int index, FunctionNode function) {
            return AtLocation(function.Function ?? $"[{index}]", () => {

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
                    Name = function.Function,
                    Description = function.Description,
                    Sources = AtLocation("Sources", () => function.Sources?.Select(source => ConvertFunctionSource(function, ++eventIndex, source)).Where(evt => evt != null).ToList(), null) ?? new List<AFunctionSource>(),
                    PackagePath = function.PackagePath,
                    Handler = function.Handler,
                    Runtime = function.Runtime,
                    Language = function.Language,
                    Memory = function.Memory,
                    Timeout = function.Timeout,
                    ReservedConcurrency = function.ReservedConcurrency,
                    VPC = vpc,
                    Environment = function.Environment.ToDictionary(kv => "STR_" + kv.Key.Replace("::", "_").ToUpperInvariant(), kv => kv.Value) ?? new Dictionary<string, object>(),
                    Pragmas = function.Pragmas
                };
            }, null);
        }

        private AFunctionSource ConvertFunctionSource(FunctionNode function, int index, FunctionSourceNode source) {
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
                    return new AlexaSource {
                        EventSourceToken = source.Alexa
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
                return null;
            }, null);
        }

        private AOutput ConvertOutput(int index, OutputNode output) {
            return AtLocation<AOutput>(output.Export ?? output.CustomResource ?? $"[{index}]", () => {
                if(output.Export != null) {
                    var value = output.Value;
                    var description = output.Description;
                    if(value == null) {


                        // NOTE: if no value is provided, we expect the export name to correspond to a
                        //  parameter name; if it does, we export the ARN value of that parameter; in
                        //  addition, we assume its description if none is provided.

                        var parameter = _module.Parameters.First(p => p.Name == output.Export);
                        if(parameter is AInputParameter) {

                            // input parameters are always expected to be in ARN format
                            value = FnRef(parameter.Name);
                        } else {
                            value = ResourceMapping.GetArnReference((parameter as AResourceParameter)?.Resource?.Type, parameter.ResourceName);
                        }
                        if(description == null) {
                            description = parameter.Description;
                        }
                    }
                    return new ExportOutput {
                        Name = output.Export,
                        Description = description,
                        Value = value
                    };
                }
                if(output.CustomResource != null) {
                    return new CustomResourceHandlerOutput {
                        CustomResourceName = output.CustomResource,
                        Description = output.Description,
                        Handler = output.Handler
                    };
                }
                if(output.Macro != null) {
                    return new MacroOutput {
                        Macro = output.Macro,
                        Description = output.Description,
                        Handler = output.Handler
                    };
                }
                throw new ModelParserException("invalid output type");
            }, null);
        }
    }
}