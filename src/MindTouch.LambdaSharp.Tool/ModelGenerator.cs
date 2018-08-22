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
using Humidifier;
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model;
using Newtonsoft.Json;

namespace MindTouch.LambdaSharp.Tool {
    using ApiGateway = Humidifier.ApiGateway;
    using Events = Humidifier.Events;
    using IAM = Humidifier.IAM;
    using Lambda = Humidifier.Lambda;
    using SNS = Humidifier.SNS;
    using SSM = Humidifier.SSM;

    public class ModelGenerator : AModelProcessor {

        //--- Types ---
        private class ApiRoute {

            //--- Properties ---
            public string Method { get; set; }
            public string[] Path { get; set; }
            public ApiGatewaySourceIntegration Integration { get; set; }
            public Function Function { get; set; }
        }

        //--- Fields ---

        private Module _module;
        private Stack _stack;
        private List<Statement> _resourceStatements;
        private List<ApiRoute> _apiGatewayRoutes;

        //--- Constructors ---
        public ModelGenerator(Settings settings) : base(settings) { }

        //--- Methods ---
        public Stack Generate(Module module) {
            _module = module;

            // stack header
            _stack = new Stack {
                AWSTemplateFormatVersion = "2010-09-09",
                Description = _module.Description
            };

            // create generic resource statement; additional resource statements can be added by resources
            _resourceStatements = new List<Statement> {
                new Statement {
                    Sid = "LambdaLoggingWrite",
                    Effect = "Allow",
                    Resource = "arn:aws:logs:*:*:*",
                    Action = new List<string> {
                        "logs:CreateLogStream",
                        "logs:PutLogEvents"
                    }
                },
                new Statement {
                    Sid = "LambdaLoggingCreate",
                    Effect = "Allow",
                    Resource = "*",
                    Action = new List<string> {
                        "logs:CreateLogGroup"
                    }
                }
            };

            // add decryption permission for requested keys
            if(_module.Secrets.Any()) {
                _resourceStatements.Add(new Statement {
                    Sid = "LambdaSecretsDecryption",
                    Effect = "Allow",
                    Resource = _module.Secrets,
                    Action = "kms:Decrypt"
                });
            }

            // add parameters
            var environmentRefVariables = new Dictionary<string, object>();
            foreach(var parameter in _module.Parameters) {
                AddParameter(parameter, "", environmentRefVariables);
            }

            // check if we need to create a module IAM role (only needed by functions)
            if(_module.Functions.Any()) {
                _apiGatewayRoutes = new List<ApiRoute>();

                // permissions needed for dead-letter queue
                if(Settings.DeadLetterQueueArn != null) {
                    _resourceStatements.Add(new Statement {
                        Sid = "LambdaDeadLetterQueueLogging",
                        Effect = "Allow",
                        Resource = Settings.DeadLetterQueueArn,
                        Action = new List<string> {
                            "sqs:SendMessage"
                        }
                    });
                }

                // permissions needed for logging topic
                if(Settings.LoggingTopicArn != null) {
                    _resourceStatements.Add(new Statement {
                        Sid = "LambdaSnsLogging",
                        Effect = "Allow",
                        Resource = Settings.LoggingTopicArn,
                        Action = new List<string> {
                            "sns:Publish"
                        }
                    });
                }

                // permissions needed for lambda functions to exist in a VPC
                if(_module.Functions.Any(function => function.VPC != null)) {
                    _resourceStatements.Add(new Statement {
                        Sid = "LambdaVpcNetworkInterfaces",
                        Effect = "Allow",
                        Resource = "*",
                        Action = new List<string> {
                            "ec2:DescribeNetworkInterfaces",
                            "ec2:CreateNetworkInterface",
                            "ec2:DeleteNetworkInterface"
                        }
                    });
                }

                // create module IAM role used by all functions
                _stack.Add("ModuleRole", new IAM.Role {
                    AssumeRolePolicyDocument = new PolicyDocument {
                        Statement = new List<Statement> {
                            new Statement {
                                Sid = "LambdaInvocation",
                                Effect = "Allow",
                                Principal = new {
                                    Service = "lambda.amazonaws.com"
                                },
                                Action = "sts:AssumeRole"
                            }
                        }
                    },
                    Policies = new List<IAM.Policy> {
                        new IAM.Policy {
                            PolicyName = $"{Settings.Tier}-{_module.Name}-policy",
                            PolicyDocument = new PolicyDocument {

                                // NOTE: additional resource statements can be added by resources
                                Statement = _resourceStatements
                            }
                        }
                    }
                });
                foreach(var function in _module.Functions) {
                    AddFunction(function, environmentRefVariables);
                }

                // check if an API gateway needs to be created
                if(_apiGatewayRoutes.Any()) {

                    // create a RestApi
                    var restApiName = "ModuleRestApi";
                    var restApiDescription = $"{_module.Name} API (v{_module.Version})";
                    _stack.Add(restApiName, new ApiGateway.RestApi {
                        Name = $"{_module.Name} API ({Settings.Tier})",
                        Description = restApiDescription,
                        FailOnWarnings = true
                    });

                    // add output parameter to easily located API
                    _stack.Add("ModuleRestApi", new Output {
                        Description = restApiDescription,
                        Value = Fn.Join(
                            "",
                            "https://",
                            Fn.Ref(restApiName),
                            ".execute-api.",
                            Fn.Ref("AWS::Region"),
                            ".",
                            Fn.Ref("AWS::URLSuffix"),
                            "/LATEST/"
                        )
                    });

                    // create a RestApi role that can write logs
                    var restApiRoleName = restApiName + "Role";
                    _stack.Add(restApiRoleName, new IAM.Role {
                        AssumeRolePolicyDocument = new PolicyDocument {
                            Statement = new List<Statement> {
                                new Statement {
                                    Sid = "LambdaRestApiInvocation",
                                    Effect = "Allow",
                                    Principal = new {
                                        Service = "apigateway.amazonaws.com"
                                    },
                                    Action = "sts:AssumeRole"
                                }
                            }
                        },
                        Policies = new List<IAM.Policy> {
                            new IAM.Policy {
                                PolicyName = $"{_module.Name}RestApiRolePolicy",
                                PolicyDocument = new PolicyDocument {
                                    Statement = new List<Statement> {
                                        new Statement {
                                            Sid = "LambdaRestApiLogging",
                                            Effect = "Allow",
                                            Action = new List<string> {
                                                "logs:CreateLogGroup",
                                                "logs:CreateLogStream",
                                                "logs:DescribeLogGroups",
                                                "logs:DescribeLogStreams",
                                                "logs:PutLogEvents",
                                                "logs:GetLogEvents",
                                                "logs:FilterLogEvents"
                                            },
                                            Resource = "*"
                                        }
                                    }
                                }
                            }
                        }
                    });

                    // create a RestApi account which uses the RestApi role
                    var restApiAccountName = restApiName + "Account";
                    _stack.Add(restApiAccountName, new ApiGateway.Account {
                        CloudWatchRoleArn = Fn.GetAtt(restApiRoleName, "Arn")
                    });

                    // recursively create resources as needed
                    var apiMethods = new List<KeyValuePair<string, ApiGateway.Method>>();
                    AddApiResource(restApiName, Fn.Ref(restApiName), Fn.GetAtt(restApiName, "RootResourceId"), 0, _apiGatewayRoutes, apiMethods);

                    // RestApi deployment depends on all methods and their hash (to force redeployment in case of change)
                    var methodSignature = string.Join("\n", apiMethods
                        .OrderBy(kv => kv.Key)
                        .Select(kv => JsonConvert.SerializeObject(kv.Value))
                    );
                    string methodsHash = methodSignature.ToMD5Hash();
                    var restApiDeploymentName = restApiName + "Deployment" + methodsHash;

                    // NOTE (2018-06-21, bjorg): the RestApi deployment resource depends on ALL methods resources having been created
                    _stack.Add(restApiDeploymentName, new ApiGateway.Deployment {
                        RestApiId = Fn.Ref(restApiName),
                        Description = $"{_module.Name} API ({Settings.Tier}) [{methodsHash}]"
                    }, dependsOn: apiMethods.Select(kv => kv.Key).ToArray());

                    // RestApi stage depends on API gateway deployment and API gateway account
                    // NOTE (2018-06-21, bjorg): the stage resource depends on the account resource having been granted
                    // the necessary permissions for logging
                    var restApiStageName = restApiName + "Stage";
                    _stack.Add(restApiStageName, new ApiGateway.Stage {
                        RestApiId = Fn.Ref(restApiName),
                        DeploymentId = Fn.Ref(restApiDeploymentName),
                        StageName = "LATEST",
                        MethodSettings = new List<ApiGateway.StageTypes.MethodSetting> {
                            new ApiGateway.StageTypes.MethodSetting {
                                DataTraceEnabled = true,
                                HttpMethod = "*",
                                LoggingLevel = "INFO",
                                ResourcePath = "/*"
                            }
                        }
                    }, dependsOn: new[] { restApiAccountName });
                }
            }
            return _stack;

            // local functions
            void AddApiResource(string parentPrefix, object restApiId, object parentId, int level, IEnumerable<ApiRoute> routes, List<KeyValuePair<string, ApiGateway.Method>> apiMethods) {

                // attach methods to resource id
                var methods = routes.Where(route => route.Path.Length == level).ToArray();
                foreach(var method in methods) {
                    var methodName = parentPrefix + method.Method;
                    ApiGateway.Method apiMethod;
                    switch(method.Integration) {
                    case ApiGatewaySourceIntegration.RequestResponse:
                        apiMethod = new ApiGateway.Method {
                            AuthorizationType = "NONE",
                            HttpMethod = method.Method,
                            ResourceId = parentId,
                            RestApiId = restApiId,
                            Integration = new ApiGateway.MethodTypes.Integration {
                                Type = "AWS_PROXY",
                                IntegrationHttpMethod = "POST",
                                Uri = Fn.Sub(
                                    "arn:aws:apigateway:${AWS::Region}:lambda:path/2015-03-31/functions/${Arn}/invocations",
                                    new Dictionary<string, dynamic> {
                                        ["Arn"] = Fn.GetAtt(method.Function.Name, "Arn")
                                    }
                                )
                            }
                        };
                        break;
                    case ApiGatewaySourceIntegration.SlackCommand:

                        // NOTE (2018-06-06, bjorg): Slack commands have a 3sec timeout on invocation, which is rarely good enough;
                        // instead we wire Slack command requests up as asynchronous calls; this way, we can respond with
                        // a callback later and the integration works well all the time.
                        apiMethod = new ApiGateway.Method {
                            AuthorizationType = "NONE",
                            HttpMethod = method.Method,
                            ResourceId = parentId,
                            RestApiId = restApiId,
                            Integration = new ApiGateway.MethodTypes.Integration {
                                Type = "AWS",
                                IntegrationHttpMethod = "POST",
                                Uri = Fn.Sub(
                                    "arn:aws:apigateway:${AWS::Region}:lambda:path/2015-03-31/functions/${Arn}/invocations",
                                    new Dictionary<string, dynamic> {
                                        ["Arn"] = Fn.GetAtt(method.Function.Name, "Arn")
                                    }
                                ),
                                RequestParameters = new Dictionary<string, dynamic> {
                                    ["integration.request.header.X-Amz-Invocation-Type"] = "'Event'"
                                },
                                RequestTemplates = new Dictionary<string, dynamic> {
                                    ["application/x-www-form-urlencoded"] =
@"{
    #foreach($token in $input.path('$').split('&'))
        #set($keyVal = $token.split('='))
        #set($keyValSize = $keyVal.size())
        #if($keyValSize == 2)
            #set($key = $util.escapeJavaScript($util.urlDecode($keyVal[0])))
            #set($val = $util.escapeJavaScript($util.urlDecode($keyVal[1])))
            ""$key"": ""$val""#if($foreach.hasNext),#end
        #end
    #end
}"
                                },
                                IntegrationResponses = new List<ApiGateway.MethodTypes.IntegrationResponse> {
                                    new ApiGateway.MethodTypes.IntegrationResponse {
                                        StatusCode = 200,
                                        ResponseTemplates = new Dictionary<string, dynamic> {
                                            ["application/json"] =
@"{
    ""response_type"": ""in_channel"",
    ""text"": """"
}"
                                        }
                                    }
                                }
                            },
                            MethodResponses = new List<ApiGateway.MethodTypes.MethodResponse> {
                                new ApiGateway.MethodTypes.MethodResponse {
                                    StatusCode = 200,
                                    ResponseModels = new Dictionary<string, dynamic> {
                                        ["application/json"] = "Empty"
                                    }
                                }
                            }
                        };
                        break;
                    default:
                        throw new NotImplementedException($"api integration {method.Integration} is not supported");
                    }
                    apiMethods.Add(new KeyValuePair<string, ApiGateway.Method>(methodName, apiMethod));
                    _stack.Add(methodName, apiMethod);
                    _stack.Add($"{method.Function.Name}{methodName}Permission", new Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        FunctionName = Fn.GetAtt(method.Function.Name, "Arn"),
                        Principal = "apigateway.amazonaws.com",
                        SourceArn = Fn.Sub(
                            $"arn:aws:execute-api:{Settings.AwsRegion}:{Settings.AwsAccountId}:${{RestApi}}/LATEST/{method.Method}/{string.Join("/", method.Path)}",
                            new Dictionary<string, dynamic> {
                                ["RestApi"] = Fn.Ref("ModuleRestApi")
                            }
                        )
                    });
                }

                // create new resource for each route with a common path segment
                var subRoutes = routes.Where(route => route.Path.Length > level).ToLookup(route => route.Path[level]);
                foreach(var subRoute in subRoutes) {

                    // remove special character from path segment and capitalize it
                    var partName = new string(subRoute.Key.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    partName = char.ToUpperInvariant(partName[0]) + partName.Substring(1);

                    // create a new resource
                    var newResourceName = parentPrefix + partName + "Resource";
                    _stack.Add(newResourceName, new ApiGateway.Resource {
                        RestApiId = restApiId,
                        ParentId = parentId,
                        PathPart = subRoute.Key
                    });
                    AddApiResource(parentPrefix + partName, restApiId, Fn.Ref(newResourceName), level + 1, subRoute, apiMethods);
                }
            }
        }

        private void AddFunction(Function function, IDictionary<string, object> environmentRefVariables) {
            var environmentVariables = function.Environment.ToDictionary(kv => kv.Key, kv => (dynamic)kv.Value);
            environmentVariables["TIER"] = Settings.Tier;
            environmentVariables["MODULE"] = _module.Name;
            environmentVariables["DEADLETTERQUEUE"] = Settings.DeadLetterQueueUrl;
            environmentVariables["LOGGINGTOPIC"] = Settings.LoggingTopicArn;
            environmentVariables["LAMBDARUNTIME"] = function.Runtime;
            foreach(var environmentRefVariable in environmentRefVariables) {
                environmentVariables[environmentRefVariable.Key] = environmentRefVariable.Value;
            }

            // check if function as a VPC configuration
            Lambda.FunctionTypes.VpcConfig vpcConfig = null;
            if(function.VPC != null) {
                vpcConfig = new Lambda.FunctionTypes.VpcConfig {
                    SubnetIds = function.VPC.SubnetIds,
                    SecurityGroupIds = function.VPC.SecurityGroupIds
                };
            }

            // create function definition
            var s3 = function.S3Location.ToS3Info();
            _stack.Add(function.Name, new Lambda.Function {
                FunctionName = ToAppResourceName(function.Name),
                Description = function.Description,
                Runtime = function.Runtime,
                Handler = function.Handler,
                Timeout = function.Timeout,
                MemorySize = function.Memory,
                ReservedConcurrentExecutions = function.ReservedConcurrency,
                Role = Fn.GetAtt("ModuleRole", "Arn"),
                Code = new Lambda.FunctionTypes.Code {
                    S3Bucket = s3.Bucket,
                    S3Key = s3.Key
                },
                DeadLetterConfig = new Lambda.FunctionTypes.DeadLetterConfig {
                    TargetArn = Settings.DeadLetterQueueArn
                },
                Environment = new Lambda.FunctionTypes.Environment {
                    Variables = environmentVariables
                },
                VpcConfig = vpcConfig,
                Tags = new List<Tag> {
                    new Tag {
                        Key = "lambdasharp:tier",
                        Value = Settings.Tier
                    },
                    new Tag {
                        Key = "lambdasharp:module",
                        Value = _module.Name
                    }
                }
            });

            // check if function is exported
            if(function.Export != null) {
                var export = function.Export.StartsWith("/")
                    ? function.Export
                    : $"/{Settings.Tier}/{_module.Name}/{function.Export}";
                _stack.Add(function.Name + "SsmParameter", new SSM.Parameter {
                    Name = export,
                    Description = function.Description,
                    Type = "String",
                    Value = Fn.Ref(function.Name)
                });
            }

            // check if function has any SNS topic event sources
            var topicSources = function.Sources.OfType<TopicSource>();
            if(topicSources.Any()) {
                foreach(var topicSource in topicSources) {

                    // find the resource that matches the declared topic name
                    var parameter = _module.Parameters
                        .OfType<AResourceParameter>()
                        .FirstOrDefault(p => p.Name == topicSource.TopicName);

                    // determine how to reference the resource
                    object resourceReference;
                    switch(parameter) {
                    case ReferencedResourceParameter reference:
                        resourceReference = reference.Resource.ResourceArn;
                        break;
                    case CloudFormationResourceParameter cloudformation:
                        resourceReference = Fn.Ref(topicSource.TopicName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(parameter),
                            parameter?.GetType().Name ?? "<null>",
                            "parameter resource type must be either ReferencedResourceParameter or CloudFormationResourceParameter"
                        );
                    }
                    _stack.Add($"{function.Name}{topicSource.TopicName}SnsPermission", new Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        SourceArn = resourceReference,
                        FunctionName = Fn.GetAtt(function.Name, "Arn"),
                        Principal = "sns.amazonaws.com"
                    });
                    _stack.Add($"{function.Name}{topicSource.TopicName}Subscription", new SNS.Subscription {
                        Endpoint = Fn.GetAtt(function.Name, "Arn"),
                        Protocol = "lambda",
                        TopicArn = resourceReference
                    });
                }
            }

            // check if function has any API gateway event sources
            var scheduleSources = function.Sources.OfType<ScheduleSource>().ToList();
            if(scheduleSources.Any()) {
                for(var i = 0; i < scheduleSources.Count; ++i) {
                    var name = function.Name + "ScheduleEvent" + (i + 1).ToString("00");
                    _stack.Add(name, new Events.Rule {
                        ScheduleExpression = scheduleSources[i].Expression,
                        Targets = new List<Events.RuleTypes.Target> {
                            new Events.RuleTypes.Target {
                                Id = ToAppResourceName(name),
                                Arn = Fn.GetAtt(function.Name, "Arn"),
                                InputTransformer = new Events.RuleTypes.InputTransformer {
                                    InputPathsMap = new Dictionary<string, dynamic> {
                                        ["version"] = "$.version",
                                        ["id"] = "$.id",
                                        ["source"] = "$.source",
                                        ["account"] = "$.account",
                                        ["time"] = "$.time",
                                        ["region"] = "$.region"
                                    },
                                    InputTemplate =
@"{
  ""Version"": <version>,
  ""Id"": <id>,
  ""Source"": <source>,
  ""Account"": <account>,
  ""Time"": <time>,
  ""Region"": <region>,
  ""tName"": """ + scheduleSources[i].Name + @"""
}"
                                }
                            }
                        }
                    });
                }
                _stack.Add(function.Name + "ScheduleEventPermission", new Lambda.Permission {
                    Action = "lambda:InvokeFunction",
                    SourceAccount = Settings.AwsAccountId,
                    FunctionName = Fn.GetAtt(function.Name, "Arn"),
                    Principal = "events.amazonaws.com"
                });
            }

            // check if function has any API gateway event sources
            var apiSources = function.Sources.OfType<ApiGatewaySource>().ToList();
            if(apiSources.Any()) {
                foreach(var apiEvent in apiSources) {
                    _apiGatewayRoutes.Add(new ApiRoute {
                        Method = apiEvent.Method,
                        Path = apiEvent.Path,
                        Integration = apiEvent.Integration,
                        Function = function
                    });
                }
            }

            // check if function has any S3 event sources
            var s3Sources = function.Sources.OfType<S3Source>().ToList();
            if(s3Sources.Any()) {
                foreach(var source in s3Sources.Distinct(source => source.BucketArn)) {
                    var permissionLogicalId = $"{function.Name}{source.Bucket}S3Permission";
                    _stack.Add(permissionLogicalId, new Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        SourceAccount = Settings.AwsAccountId,
                        SourceArn = source.BucketArn,
                        FunctionName = Fn.GetAtt(function.Name, "Arn"),
                        Principal = "s3.amazonaws.com"
                    });
                    _stack.AddDependsOn(source.Bucket, permissionLogicalId);
                }
            }

            // check if function has any SQS event sources
            var sqsSources = function.Sources.OfType<SqsSource>().ToList();
            if(sqsSources.Any()) {
                foreach(var source in sqsSources) {
                    _stack.Add($"{function.Name}{source.Queue}EventMapping", new Lambda.EventSourceMapping {
                        BatchSize = source.BatchSize,
                        Enabled = true,
                        EventSourceArn = Fn.GetAtt(source.Queue, "Arn"),
                        FunctionName = Fn.Ref(function.Name)
                    });
                }
            }

            // check if function has any Alexa event sources
            var alexaSources = function.Sources.OfType<AlexaSource>().ToList();
            if(alexaSources.Any()) {
                var index = 0;
                foreach(var source in alexaSources) {
                    ++index;
                    var suffix = (source.EventSourceToken != null)
                        ? source.EventSourceToken.ToMD5Hash().Substring(0, 7)
                        : index.ToString();
                    _stack.Add($"{function.Name}AlexaPermission{suffix}", new Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        FunctionName = Fn.GetAtt(function.Name, "Arn"),
                        Principal = "alexa-appkit.amazon.com",
                        EventSourceToken = source.EventSourceToken
                    });
                }
            }
        }

        private void AddParameter(
            AParameter parameter,
            string envPrefix,
            IDictionary<string, object> environmentRefVariables
        ) {
            object exportValue = null;
            var fullEnvName = envPrefix + parameter.Name.ToUpperInvariant();
            switch(parameter) {
            case SecretParameter secretParameter:
                if(secretParameter.EncryptionContext?.Any() == true) {
                    environmentRefVariables["SEC_" + fullEnvName] = $"{secretParameter.Secret}|{string.Join("|", secretParameter.EncryptionContext.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"))}";
                } else {
                    environmentRefVariables["SEC_" + fullEnvName] = secretParameter.Secret;
                }
                break;
            case StringParameter stringParameter:
                environmentRefVariables["STR_" + fullEnvName] = stringParameter.Value;
                exportValue = stringParameter.Value;

                // add literal string parameter value as CloudFormation parameter so it can be referenced
                _stack.Add(stringParameter.FullName, new Parameter {
                    Type = "String",
                    Default = stringParameter.Value,
                    Description = stringParameter.Description
                });
                break;
            case StringListParameter stringListParameter: {
                    var commaDelimitedValue = string.Join(",", stringListParameter.Values);
                    environmentRefVariables["STR_" + fullEnvName] = commaDelimitedValue;
                    exportValue = commaDelimitedValue;
                }

                // add literal string list parameter value as CloudFormation parameter so it can be referenced
                _stack.Add(stringListParameter.FullName, new Parameter {
                    Type = "CommaDelimitedList",
                    Default = string.Join(",", stringListParameter.Values),
                    Description = stringListParameter.Description
                });
                break;
            case PackageParameter packageParameter:
                environmentRefVariables["STR_" + fullEnvName] = Fn.GetAtt(parameter.FullName, "Result");
                _stack.Add(packageParameter.FullName, new Model.CustomResource("Custom::LambdaSharpS3PackageLoader", new Dictionary<string, object> {
                    ["ServiceToken"] = Settings.S3PackageLoaderCustomResourceTopicArn,
                    ["DestinationBucketName"] = Humidifier.Fn.Ref(packageParameter.DestinationBucketParameterName),
                    ["DestinationKeyPrefix"] = packageParameter.DestinationKeyPrefix,
                    ["SourceBucketName"] = packageParameter.PackageBucket,
                    ["SourcePackageKey"] = packageParameter.PackageKey,
                }));
                break;
            case ReferencedResourceParameter referenceResourceParameter: {
                    var resource = referenceResourceParameter.Resource;
                    environmentRefVariables["STR_" + fullEnvName] = resource.ResourceArn;
                    exportValue = resource.ResourceArn;

                    // add permissions for resource
                    if(resource.Allow?.Any() == true) {
                        _resourceStatements.Add(new Statement {
                            Sid = parameter.FullName,
                            Effect = "Allow",
                            Resource = resource.ResourceArn,
                            Action = resource.Allow
                        });
                    }

                    // add reference resource parameter value as CloudFormation parameter so it can be referenced
                    _stack.Add(referenceResourceParameter.FullName, new Parameter {
                        Type = "String",
                        Default = resource.ResourceArn,
                        Description = referenceResourceParameter.Description
                    });
                }
                break;
            case CloudFormationResourceParameter cloudFormationResourceParameter: {
                    var resource = cloudFormationResourceParameter.Resource;
                    var resourceName = parameter.FullName;
                    object resourceArn;
                    object resourceParamFn;
                    Humidifier.Resource resourceTemplate;
                    if(resource.Type.StartsWith("Custom::")) {
                        resourceArn = null;
                        resourceParamFn = Fn.GetAtt(resourceName, "Result");
                        resourceTemplate = new Model.CustomResource(resource.Type, resource.Properties);
                    } else if(!Settings.ResourceMapping.TryParseResourceProperties(
                        resource.Type,
                        resourceName,
                        resource.Properties,
                        out resourceArn,
                        out resourceParamFn,
                        out resourceTemplate
                    )) {
                        throw new NotImplementedException($"resource type is not supported: {resource.Type}");
                    }

                    // for S3 buckets, we need to check if any functions use the bucket as an event source
                    if(resource.Type == "AWS::S3::Bucket") {
                        var s3Template = (Humidifier.S3.Bucket)resourceTemplate;
                        var s3Sources = _module.Functions.SelectMany(function => function.Sources
                                .OfType<S3Source>()
                                .Where(source => source.Bucket == resourceName)
                                .Select(source => new {
                                    Function = function,
                                    Source = source
                                })
                            )
                            .ToList();
                        if(s3Sources.Any()) {

                            // NOTE (2018-06-28, bjorg): for S3 buckets, we need to jump through a few hoops to make
                            //      everything work as expected; first, we need to know the final name of the bucket;
                            //      second, we cannot use `Ref()` to get its name, because it introduces a circular dependency.

                            // check if we need to create a hashed bucket name and set `BucketName` in template
                            if(s3Template.BucketName == null) {

                                // NOTE (2018-08-16, bjorg): bucket names must be lowercase
                                var bucketName = $"{Settings.Tier}-{_module.Name}-{resourceName}-".ToLowerInvariant();
                                bucketName += $"{Settings.AwsAccountId}-{Settings.AwsRegion}-{bucketName}".ToMD5Hash().Substring(0, 7).ToLowerInvariant();
                                s3Template.BucketName = bucketName;
                                var arn = $"arn:aws:s3:::{bucketName}";
                                resourceArn = new object[] {
                                    arn,
                                    arn + "/*"
                                };
                                foreach(var src in s3Sources) {
                                    src.Source.BucketArn = arn;
                                }
                            }

                            // use the hashed bucket name as environment variable
                            resourceParamFn = s3Template.BucketName;

                            // add notification configuration to template
                            s3Template.NotificationConfiguration = new Humidifier.S3.BucketTypes.NotificationConfiguration {
                                LambdaConfigurations = s3Sources.SelectMany(src => src.Source.Events.Select(evt => new Humidifier.S3.BucketTypes.LambdaConfiguration {
                                    Event = evt,
                                    Filter = ConvertFilter(src.Source.Prefix, src.Source.Suffix),
                                    Function = Fn.GetAtt(src.Function.Name, "Arn")
                                })).ToList()
                            };
                        }

                        // local function
                        Humidifier.S3.BucketTypes.NotificationFilter ConvertFilter(string prefix, string suffix) {
                            if((prefix == null) && (suffix == null)) {
                                return null;
                            }
                            var rules = new List<Humidifier.S3.BucketTypes.FilterRule>();
                            if(prefix != null) {
                                rules.Add(new Humidifier.S3.BucketTypes.FilterRule {
                                    Name = "prefix",
                                    Value = prefix
                                });
                            }
                            if(suffix != null) {
                                rules.Add(new Humidifier.S3.BucketTypes.FilterRule {
                                    Name = "suffix",
                                    Value = suffix
                                });
                            }
                            return new Humidifier.S3.BucketTypes.NotificationFilter {
                                S3Key = new Humidifier.S3.BucketTypes.S3KeyFilter {
                                    Rules = rules
                                }
                            };
                        }
                    }
                    _stack.Add(resourceName, resourceTemplate);
                    exportValue = resourceParamFn;

                    // only add parameters that the lambda functions are allowed to access
                    if(resource.Type.StartsWith("Custom::") || (resource.Allow?.Any() == true)) {
                        environmentRefVariables["STR_" + fullEnvName] = resourceParamFn;
                    }

                    // add permissions for resource
                    if((resourceArn != null) && (resource.Allow?.Any() == true)) {
                        _resourceStatements.Add(new Statement {
                            Sid = parameter.FullName,
                            Effect = "Allow",
                            Resource = resourceArn,
                            Action = resource.Allow
                        });
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(parameter), parameter, "unknown parameter type");
            }

            // check if nested parameters need to be added
            if(parameter.Parameters?.Any() == true) {
                foreach(var nestedResource in parameter.Parameters) {
                    AddParameter(
                        nestedResource,
                        fullEnvName + "_",
                        environmentRefVariables
                    );
                }
            }

            // check if resource name should be exported
            if(parameter.Export != null) {
                var export = parameter.Export.StartsWith("/")
                    ? parameter.Export
                    : $"/{Settings.Tier}/{_module.Name}/{parameter.Export}";
                _stack.Add(parameter.FullName + "SsmParameter", new SSM.Parameter {
                    Name = export,
                    Description = parameter.Description,
                    Type = "String",
                    Value = exportValue
                });
            }
        }

        private string ToAppResourceName(string name) => (name != null) ? $"{Settings.Tier}-{_module.Name}-{name}" : null;
   }
}