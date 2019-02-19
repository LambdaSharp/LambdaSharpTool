/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using LambdaSharp.Tool.Model.AST;
using Newtonsoft.Json;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelFunctionProcessor : AModelProcessor {

        //--- Types ---
        private class ApiRoute {

            //--- Properties ---
            public string Method { get; set; }
            public string[] Path { get; set; }
            public ApiGatewaySourceIntegration Integration { get; set; }
            public FunctionItem Function { get; set; }
            public string OperationName { get; set; }
            public bool? ApiKeyRequired { get; set; }
        }

        //--- Fields ---
        private ModuleBuilder _builder;
        private List<ApiRoute> _apiGatewayRoutes = new List<ApiRoute>();

        //--- Constructors ---
        public ModelFunctionProcessor(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Process(ModuleBuilder builder) {
            _builder = builder;

            // create module IAM role used by all functions
            var functions = _builder.Items.OfType<FunctionItem>().ToList();
            if(functions.Any()) {

                // add functions
                foreach(var function in functions) {
                    AddFunction(function);
                }

                // check if an API gateway needs to be created
                if(_apiGatewayRoutes.Any()) {
                    var moduleItem = _builder.GetItem("Module");

                    // create a RestApi
                    var restApiItem = _builder.AddResource(
                        parent: moduleItem,
                        name: "RestApi",
                        description: "Module REST API",
                        scope: null,
                        resource: new Humidifier.ApiGateway.RestApi {
                            Name = FnSub("${AWS::StackName} Module API"),
                            Description = "${Module::FullName} API (v${Module::Version})",
                            FailOnWarnings = true
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: null,
                        pragmas: null
                    );

                    // recursively create resources as needed
                    var apiMethods = new List<KeyValuePair<string, object>>();
                    AddApiResource(restApiItem, FnRef(restApiItem.FullName), FnGetAtt(restApiItem.FullName, "RootResourceId"), 0, _apiGatewayRoutes, apiMethods);

                    // RestApi deployment depends on all methods and their hash (to force redeployment in case of change)
                    var methodSignature = string.Join("\n", apiMethods
                        .OrderBy(kv => kv.Key)
                        .Select(kv => JsonConvert.SerializeObject(kv.Value))
                    );
                    string methodsHash = methodSignature.ToMD5Hash();

                    // add RestApi url
                    _builder.AddVariable(
                        parent: restApiItem,
                        name: "Url",
                        description: "Module REST API URL",
                        type: "String",
                        scope: null,
                        value: FnSub("https://${Module::RestApi}.execute-api.${AWS::Region}.${AWS::URLSuffix}/LATEST"),
                        allow: null,
                        encryptionContext: null
                    );

                    // create a RestApi role that can write logs
                    _builder.AddResource(
                        parent: restApiItem,
                        name: "Role",
                        description: "Module REST API Role",
                        scope: null,
                        resource: new Humidifier.IAM.Role {
                            AssumeRolePolicyDocument = new Humidifier.PolicyDocument {
                                Version = "2012-10-17",
                                Statement = new[] {
                                    new Humidifier.Statement {
                                        Sid = "ModuleRestApiPrincipal",
                                        Effect = "Allow",
                                        Principal = new Humidifier.Principal {
                                            Service = "apigateway.amazonaws.com"
                                        },
                                        Action = "sts:AssumeRole"
                                    }
                                }.ToList()
                            },
                            Policies = new[] {
                                new Humidifier.IAM.Policy {
                                    PolicyName = FnSub("${AWS::StackName}ModuleRestApiPolicy"),
                                    PolicyDocument = new Humidifier.PolicyDocument {
                                        Version = "2012-10-17",
                                        Statement = new[] {
                                            new Humidifier.Statement {
                                                Sid = "ModuleRestApiLogging",
                                                Effect = "Allow",
                                                Action = new[] {
                                                    "logs:CreateLogGroup",
                                                    "logs:CreateLogStream",
                                                    "logs:DescribeLogGroups",
                                                    "logs:DescribeLogStreams",
                                                    "logs:PutLogEvents",
                                                    "logs:GetLogEvents",
                                                    "logs:FilterLogEvents"
                                                },
                                                Resource = "arn:aws:logs:*:*:*"
                                            }
                                        }.ToList()
                                    }
                                }
                            }.ToList()
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: null,
                        pragmas: null
                    );

                    // create a RestApi account which uses the RestApi role
                    _builder.AddResource(
                        parent: restApiItem,
                        name: "Account",
                        description: "Module REST API Account",
                        scope: null,
                        resource: new Humidifier.ApiGateway.Account {
                            CloudWatchRoleArn = FnGetAtt("Module::RestApi::Role", "Arn")
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: null,
                        pragmas: null
                    );

                    // NOTE (2018-06-21, bjorg): the RestApi deployment resource depends on ALL methods resources having been created;
                    //  a new name is used for the deployment to force the stage to be updated
                    var deploymentWithHash = _builder.AddResource(
                        parent: restApiItem,
                        name: "Deployment" + methodsHash,
                        description: "Module REST API Deployment",
                        scope: null,
                        resource: new Humidifier.ApiGateway.Deployment {
                            RestApiId = FnRef("Module::RestApi"),
                            Description = FnSub($"${{AWS::StackName}} API [{methodsHash}]")
                        },
                        resourceExportAttribute: null,
                        dependsOn: apiMethods.Select(kv => kv.Key).ToArray(),
                        condition: null,
                        pragmas: null
                    );
                    var deployment = _builder.AddVariable(
                        parent: restApiItem,
                        name: "Deployment",
                        description: "Module REST API Deployment",
                        type: "String",
                        scope: null,
                        value: FnRef(deploymentWithHash.FullName),
                        allow: null,
                        encryptionContext: null
                    );


                    // RestApi stage depends on API gateway deployment and API gateway account
                    // NOTE (2018-06-21, bjorg): the stage resource depends on the account resource having been granted
                    //  the necessary permissions for logging
                    _builder.AddResource(
                        parent: restApiItem,
                        name: "Stage",
                        description: "Module REST API Stage",
                        scope: null,
                        resource: new Humidifier.ApiGateway.Stage {
                            RestApiId = FnRef("Module::RestApi"),
                            DeploymentId = FnRef(deployment.FullName),
                            StageName = "LATEST",
                            MethodSettings = new[] {
                                new Humidifier.ApiGateway.StageTypes.MethodSetting {
                                    DataTraceEnabled = true,
                                    HttpMethod = "*",
                                    LoggingLevel = "INFO",
                                    ResourcePath = "/*"
                                }
                            }.ToList()
                        },
                        resourceExportAttribute: null,
                        dependsOn: new[] { "Module::RestApi::Account" },
                        condition: null,
                        pragmas: null
                    );
                }
            }
        }

        private void AddApiResource(AModuleItem parent, object restApiId, object parentId, int level, IEnumerable<ApiRoute> routes, List<KeyValuePair<string, object>> apiMethods) {

            // create methods at this route level to parent id
            var methods = routes.Where(route => route.Path.Length == level).ToArray();
            foreach(var method in methods) {
                Humidifier.ApiGateway.Method apiMethod;
                switch(method.Integration) {
                case ApiGatewaySourceIntegration.RequestResponse:
                    apiMethod = CreateRequestResponseApiMethod(method);
                    break;
                case ApiGatewaySourceIntegration.SlackCommand:
                    apiMethod = CreateSlackRequestApiMethod(method);
                    break;
                default:
                    AddError($"api integration {method.Integration} is not supported");
                    continue;
                }

                // add API method item
                var methodItem = _builder.AddResource(
                    parent: parent,
                    name: method.Method,
                    description: null,
                    scope: null,
                    resource: apiMethod,
                    resourceExportAttribute: null,
                    dependsOn: null,

                    // TODO (2018-12-28, bjorg): handle conditional function
                    condition: null,
                    pragmas: null
                );

                // add permission to API method to invoke lambda
                _builder.AddResource(
                    parent: methodItem,
                    name: "Permission",
                    description: null,
                    scope: null,
                    resource: new Humidifier.Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        FunctionName = FnGetAtt(method.Function.FullName, "Arn"),
                        Principal = "apigateway.amazonaws.com",
                        SourceArn = FnSub($"arn:aws:execute-api:${{AWS::Region}}:${{AWS::AccountId}}:${{Module::RestApi}}/LATEST/{method.Method}/{string.Join("/", method.Path)}")
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: method.Function.Condition,
                    pragmas: null
                );
                apiMethods.Add(new KeyValuePair<string, object>(methodItem.FullName, apiMethod));
            }

            // find sub-routes and group common sub-route prefix
            var subRoutes = routes.Where(route => route.Path.Length > level).ToLookup(route => route.Path[level]);
            foreach(var subRoute in subRoutes) {

                // remove special character from path segment and capitalize it
                var partName = subRoute.Key.ToIdentifier();
                partName = char.ToUpperInvariant(partName[0]) + ((partName.Length > 1) ? partName.Substring(1) : "");

                // create a new parent resource to attach methods or sub-resource to
                var resource = _builder.AddResource(
                    parent: parent,
                    name: partName + "Resource",
                    description: null,
                    scope: null,
                    resource: new Humidifier.ApiGateway.Resource {
                        RestApiId = restApiId,
                        ParentId = parentId,
                        PathPart = subRoute.Key
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,

                    // TODO (2018-12-28, bjorg): handle conditional function
                    condition: null,
                    pragmas: null
                );
                AddApiResource(resource, restApiId, FnRef(resource.FullName), level + 1, subRoute, apiMethods);
            }

            Humidifier.ApiGateway.Method CreateRequestResponseApiMethod(ApiRoute method) {
                return new Humidifier.ApiGateway.Method {
                    AuthorizationType = "NONE",
                    HttpMethod = method.Method,
                    OperationName = method.OperationName,
                    ApiKeyRequired = method.ApiKeyRequired,
                    ResourceId = parentId,
                    RestApiId = restApiId,
                    Integration = new Humidifier.ApiGateway.MethodTypes.Integration {
                        Type = "AWS_PROXY",
                        IntegrationHttpMethod = "POST",
                        Uri = FnSub($"arn:aws:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{method.Function.FullName}.Arn}}/invocations")
                    }
                };
            }

            Humidifier.ApiGateway.Method CreateSlackRequestApiMethod(ApiRoute method) {

                // NOTE (2018-06-06, bjorg): Slack commands have a 3sec timeout on invocation, which is rarely good enough;
                // instead we wire Slack command requests up as asynchronous calls; this way, we can respond with
                // a callback later and the integration works well all the time.
                return new Humidifier.ApiGateway.Method {
                    AuthorizationType = "NONE",
                    HttpMethod = method.Method,
                    OperationName = method.OperationName,
                    ApiKeyRequired = method.ApiKeyRequired,
                    ResourceId = parentId,
                    RestApiId = restApiId,
                    Integration = new Humidifier.ApiGateway.MethodTypes.Integration {
                        Type = "AWS",
                        IntegrationHttpMethod = "POST",
                        Uri = FnSub($"arn:aws:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{method.Function.FullName}.Arn}}/invocations"),
                        RequestParameters = new Dictionary<string, object> {
                            ["integration.request.header.X-Amz-Invocation-Type"] = "'Event'"
                        },
                        RequestTemplates = new Dictionary<string, object> {
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
                        IntegrationResponses = new[] {
                            new Humidifier.ApiGateway.MethodTypes.IntegrationResponse {
                                StatusCode = 200,
                                ResponseTemplates = new Dictionary<string, object> {
                                    ["application/json"] =
@"{
""response_type"": ""in_channel"",
""text"": """"
}"
                                }
                            }
                        }.ToList()
                    },
                    MethodResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                            StatusCode = 200,
                            ResponseModels = new Dictionary<string, object> {
                                ["application/json"] = "Empty"
                            }
                        }
                    }.ToList()
                };
            }
        }

        private void AddFunction(FunctionItem function) {

            // add function sources
            for(var sourceIndex = 0; sourceIndex < function.Sources.Count; ++sourceIndex) {
                var source = function.Sources[sourceIndex];
                var sourceSuffix = (sourceIndex + 1).ToString();
                switch(source) {
                case TopicSource topicSource:
                    Enumerate(topicSource.TopicName, (suffix, arn) => {
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Subscription{suffix}",
                            description: null,
                            scope: null,
                            resource: new Humidifier.SNS.Subscription {
                                Endpoint = FnGetAtt(function.FullName, "Arn"),
                                Protocol = "lambda",
                                TopicArn = arn,
                                FilterPolicy = (topicSource.Filters != null)
                                    ? JsonConvert.SerializeObject(topicSource.Filters)
                                    : null
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission{suffix}",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.Permission {
                                Action = "lambda:InvokeFunction",
                                FunctionName = FnGetAtt(function.FullName, "Arn"),
                                Principal = "sns.amazonaws.com",
                                SourceArn = arn
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                    });
                    break;
                case ScheduleSource scheduleSource: {

                        // NOTE (2019-01-30, bjorg): we need the source suffix to support multiple sources
                        //  per function; however, we cannot exceed 64 characters in length for the ID.
                        var id = function.LogicalId;
                        if(id.Length > 61) {
                            id += id.Substring(0, 61) + "-" + sourceSuffix;
                        } else {
                            id += "-" + sourceSuffix;
                        }
                        var schedule = _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}ScheduleEvent",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Events.Rule {
                                ScheduleExpression = scheduleSource.Expression,
                                Targets = new[] {
                                    new Humidifier.Events.RuleTypes.Target {
                                        Id = id,
                                        Arn = FnGetAtt(function.FullName, "Arn"),
                                        InputTransformer = new Humidifier.Events.RuleTypes.InputTransformer {
                                            InputPathsMap = new Dictionary<string, object> {
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
    ""tName"": """ + scheduleSource.Name + @"""
}"
                                        }
                                    }
                                }.ToList()
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.Permission {
                                Action = "lambda:InvokeFunction",
                                FunctionName = FnGetAtt(function.FullName, "Arn"),
                                Principal = "events.amazonaws.com",
                                SourceArn = FnGetAtt(schedule.FullName, "Arn")
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                    }
                    break;
                case ApiGatewaySource apiGatewaySource:
                    _apiGatewayRoutes.Add(new ApiRoute {
                        Method = apiGatewaySource.Method,
                        Path = apiGatewaySource.Path,
                        Integration = apiGatewaySource.Integration,
                        Function = function,
                        OperationName = apiGatewaySource.OperationName,
                        ApiKeyRequired = apiGatewaySource.ApiKeyRequired
                    });
                    break;
                case S3Source s3Source:
                    _builder.AddDependency("LambdaSharp.S3.Subscriber", Settings.ToolVersion.GetCompatibleBaseVersion(), maxVersion: null, bucketName: null);
                    Enumerate(s3Source.Bucket, (suffix, arn) => {
                        var permission = _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.Permission {
                                Action = "lambda:InvokeFunction",
                                FunctionName = FnGetAtt(function.FullName, "Arn"),
                                Principal = "s3.amazonaws.com",
                                SourceAccount = FnRef("AWS::AccountId"),
                                SourceArn = arn
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Subscription",
                            description: null,
                            type: "LambdaSharp::S3::Subscription",
                            scope: null,
                            allow: null,
                            properties: new Dictionary<string, object> {
                                ["Bucket"] = arn,
                                ["Function"] = FnGetAtt(function.FullName, "Arn"),
                                ["Filters"] = new List<object> {

                                    // TODO (2018-11-18, bjorg): we need to group filters from the same function for the same bucket
                                    ConvertS3Source()
                                }
                            },
                            dependsOn: new[] { permission.FullName },
                            arnAttribute: null,
                            condition: function.Condition,
                            pragmas: null
                        );

                        // local function
                        Dictionary<string, object> ConvertS3Source() {
                            var filter = new Dictionary<string, object> {
                                ["Events"] = s3Source.Events
                            };
                            if(s3Source.Prefix != null) {
                                filter["Prefix"] = s3Source.Prefix;
                            }
                            if(s3Source.Suffix != null) {
                                filter["Suffix"] = s3Source.Suffix;
                            }
                            return filter;
                        }
                    });
                    break;
                case SqsSource sqsSource:
                    Enumerate(sqsSource.Queue, (suffix, arn) => {
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}EventMapping{suffix}",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.EventSourceMapping {
                                BatchSize = sqsSource.BatchSize,
                                Enabled = true,
                                EventSourceArn = arn,
                                FunctionName = FnRef(function.FullName)
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                    });
                    break;
                case AlexaSource alexaSource: {

                        // check if we need to create a conditional expression for a non-literal token
                        var eventSourceToken = alexaSource.EventSourceToken;
                        if(eventSourceToken is string token) {
                            if(token == "*") {
                                eventSourceToken = null;
                            }
                        } else if(
                            (eventSourceToken != null)
                            && TryGetFnRef(eventSourceToken, out var refKey)
                            && _builder.TryGetItem(refKey, out var item)
                            && item is ParameterItem
                        ) {

                            // create conditional expression to allow "*" values
                            var condition = _builder.AddCondition(
                                parent: function,
                                name: $"Source{sourceSuffix}AlexaIsBlank",
                                description: null,
                                value: FnEquals(alexaSource.EventSourceToken, "*")
                            );
                            eventSourceToken = FnIf(
                                condition.FullName,
                                FnRef("AWS::NoValue"),
                                alexaSource.EventSourceToken
                            );
                        }
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}AlexaPermission",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.Permission {
                                Action = "lambda:InvokeFunction",
                                FunctionName = FnGetAtt(function.FullName, "Arn"),
                                Principal = "alexa-appkit.amazon.com",
                                EventSourceToken = eventSourceToken
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                    }
                    break;
                case DynamoDBSource dynamoDbSource:
                    Enumerate(dynamoDbSource.DynamoDB, (suffix, arn) => {
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}EventMapping{suffix}",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.EventSourceMapping {
                                BatchSize = dynamoDbSource.BatchSize,
                                StartingPosition = dynamoDbSource.StartingPosition,
                                Enabled = true,
                                EventSourceArn = arn,
                                FunctionName = FnRef(function.FullName)
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                    }, item => FnGetAtt(item.FullName, "StreamArn"));
                    break;
                case KinesisSource kinesisSource:
                    Enumerate(kinesisSource.Kinesis, (suffix, arn) => {
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}EventMapping{suffix}",
                            description: null,
                            scope: null,
                            resource: new Humidifier.Lambda.EventSourceMapping {
                                BatchSize = kinesisSource.BatchSize,
                                StartingPosition = kinesisSource.StartingPosition,
                                Enabled = true,
                                EventSourceArn = arn,
                                FunctionName = FnRef(function.FullName)
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null
                        );
                    });
                    break;
                default:
                    throw new ApplicationException($"unrecognized function source type '{source?.GetType()}' for source #{sourceSuffix}");
                }
            }
        }

        private void Enumerate(object value, Action<string, object> action, Func<AResourceItem, object> getReference = null) {
            if(value is string fullName) {
                if(!_builder.TryGetItem(fullName, out var item)) {
                    AddError($"could not find function source: '{fullName}'");
                    return;
                }
                if(item is AResourceItem resource) {
                    action("", getReference?.Invoke(resource) ?? item.GetExportReference());
                } else if(item.Reference is IList list) {
                    switch(list.Count) {
                    case 0:
                        break;
                    case 1:
                        action("", list[0]);
                        break;
                    default:
                        for(var i = 0; i < list.Count; ++i) {
                            action((i + 1).ToString(), list[i]);
                        }
                        break;
                    }
                } else {
                    action("", item.GetExportReference());
                }
            } else {
                action("", value);
           }
        }
   }
}