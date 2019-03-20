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

        //--- Fields ---
        private ModuleBuilder _builder;
        private List<(FunctionItem Function, RestApiSource Source)> _restApiRoutes = new List<(FunctionItem Function, RestApiSource Source)>();
        private List<(FunctionItem Function, WebSocketSource Source)> _webSocketRoutes = new List<(FunctionItem Function, WebSocketSource Source)>();

        //--- Constructors ---
        public ModelFunctionProcessor(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Process(ModuleBuilder builder) {
            _builder = builder;
            var functions = _builder.Items.OfType<FunctionItem>().ToList();
            if(functions.Any()) {

                // add functions
                foreach(var function in functions) {
                    AddFunctionSources(function);
                }

                // check if a REST API gateway needs to be created
                if(_restApiRoutes.Any()) {
                    AddRestApiResources(functions);
                }

                // check if a WebSocket API gateway needs to be created
                if(_webSocketRoutes.Any()) {
                    AddWebSocketResources(functions);
                }
            }
        }

        private void AddRestApiResources(IEnumerable<FunctionItem> functions) {
            var moduleItem = _builder.GetItem("Module");

            // create a REST API
            var restApi = _builder.AddResource(
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
            var apiMethodDeclarations = new Dictionary<string, object>();
            AddRestApiResource(restApi, FnRef(restApi.FullName), FnGetAtt(restApi.FullName, "RootResourceId"), 0, _restApiRoutes, apiMethodDeclarations);

            // RestApi deployment depends on all methods and their hash (to force redeployment in case of change)
            string apiMethodDeclarationsHash = string.Join("\n", apiMethodDeclarations
                .OrderBy(kv => kv.Key)
                .Select(kv => JsonConvert.SerializeObject(kv.Value))
            ).ToMD5Hash();

            // add RestApi url
            _builder.AddVariable(
                parent: restApi,
                name: "Url",
                description: "Module REST API URL",
                type: "String",
                scope: null,
                value: FnSub("https://${Module::RestApi}.execute-api.${AWS::Region}.${AWS::URLSuffix}/LATEST"),
                allow: null,
                encryptionContext: null
            );

            // optionally, add request validation resource if there is a request schema
            var allSources = functions.SelectMany(f => f.Sources).ToList();
            if(
                allSources.OfType<RestApiSource>().Any(source => source.RequestSchema != null)
                || allSources.OfType<WebSocketSource>().Any(source => source.RequestSchema != null)
            ) {

                    // create request validator
                    _builder.AddResource(
                        parent: restApi,
                        name: "RequestValidator",
                        description: null,
                        scope: null,
                        resource: new Humidifier.ApiGateway.RequestValidator {
                            RestApiId = FnRef(restApi.FullName),
                            ValidateRequestBody = true

                            // TODO (2019-03-19, bjorg): add support for validatiting path/query parameters and request headers
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: null,
                        pragmas: null
                    ).DiscardIfNotReachable = true;
            }

            // create a RestApi role that can write logs
            _builder.AddResource(
                parent: restApi,
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

            // create log-group for API
            var restLogGroup = _builder.AddResource(
                parent: restApi,
                name: "LogGroup",
                description: null,
                scope: null,
                resource: new Humidifier.Logs.LogGroup {
                    LogGroupName = FnSub($"API-Gateway-Execution-Logs_${{{restApi.FullName}}}/LATEST"),

                    // TODO (2019-03-18, bjorg): make retention configurable
                    //  see https://docs.aws.amazon.com/AmazonCloudWatchLogs/latest/APIReference/API_PutRetentionPolicy.html
                    RetentionInDays = 30
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null
            );

            // create a RestApi account which uses the RestApi role
            var restAccount = _builder.AddResource(
                parent: restApi,
                name: "Account",
                description: "Module REST API Account",
                scope: null,
                resource: new Humidifier.ApiGateway.Account {
                    CloudWatchRoleArn = FnGetAtt("Module::RestApi::Role", "Arn")
                },
                resourceExportAttribute: null,
                dependsOn: new[] { restLogGroup.FullName },
                condition: null,
                pragmas: null
            );

            // NOTE (2018-06-21, bjorg): the RestApi deployment resource depends on ALL methods resources having been created;
            //  a new name is used for the deployment to force the stage to be updated
            var deploymentWithHash = _builder.AddResource(
                parent: restApi,
                name: "Deployment" + apiMethodDeclarationsHash,
                description: "Module REST API Deployment",
                scope: null,
                resource: new Humidifier.ApiGateway.Deployment {
                    RestApiId = FnRef("Module::RestApi"),
                    Description = FnSub($"${{AWS::StackName}} API [{apiMethodDeclarationsHash}]")
                },
                resourceExportAttribute: null,
                dependsOn: apiMethodDeclarations.Select(kv => kv.Key).OrderBy(key => key).ToArray(),
                condition: null,
                pragmas: null
            );
            var deployment = _builder.AddVariable(
                parent: restApi,
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
                parent: restApi,
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
                dependsOn: new[] { restAccount.FullName },
                condition: null,
                pragmas: null
            );
        }

        private void AddWebSocketResources(IEnumerable<FunctionItem> functions) {
            var moduleItem = _builder.GetItem("Module");

            // give permission to the Lambda functions to communicate back over the websocket
            _builder.AddGrant(
                sid: "ModuleWebSocketConnections",
                awsType: null,
                reference: FnSub("arn:aws:execute-api:${AWS::Region}:${AWS::AccountId}:${Module::WebSocket}/LATEST/POST/@connections/*"),
                allow: new[] {
                    "execute-api:ManageConnections"
                }
            );

            // add websocket URL to all function environments
            foreach(var function in functions) {
                function.Function.Environment.Variables["WEBSOCKET_URL"] = FnSub("https://${Module::WebSocket}.execute-api.${AWS::Region}.amazonaws.com/LATEST");
            }

            // read websocket configuration
            if(!_builder.TryGetOverride("Module::WebSocket.RouteSelectionExpression", out var routeSelectionExpression)) {
                routeSelectionExpression = "$request.body.action";
            }

            // create a WebSocket API
            var webSocketItem = _builder.AddResource(
                parent: moduleItem,
                name: "WebSocket",
                description: "Module WebSocket",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Api") {
                    ["Name"] = FnSub("${AWS::StackName} Module WebSocket"),
                    ["ProtocolType"] = "WEBSOCKET",
                    ["Description"] = "${Module::FullName} WebSocket (v${Module::Version})",
                    ["RouteSelectionExpression"] = routeSelectionExpression
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null
            );

            // create resources as needed
            var webSocketResources = new List<KeyValuePair<string, object>>();
            foreach(var webSocketRoute in _webSocketRoutes) {

                // remove special character from path segment and capitalize it
                var routeName = webSocketRoute.Source.RouteKey.ToPascalIdentifier();

                // add integration resource
                var integrationResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::Integration") {
                    ["ApiId"] = FnRef(webSocketItem.FullName),
                    ["Description"] = $"WebSocket Integration for `{webSocketRoute.Source.RouteKey}`",
                    ["IntegrationType"] = "AWS_PROXY",
                    ["IntegrationUri"] = FnSub($"arn:aws:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{webSocketRoute.Function.FullName}.Arn}}/invocations")
                };
                var integration = _builder.AddResource(
                    parent: webSocketRoute.Function,
                    name: routeName + "Integration",
                    description: $"WebSocket Integration for `{webSocketRoute.Source.RouteKey}`",
                    scope: null,
                    resource: integrationResource,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: webSocketRoute.Function.Condition,
                    pragmas: null
                );
                webSocketResources.Add(new KeyValuePair<string, object>(integration.FullName, integrationResource));

                // add route resource
                var routeResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::Route") {
                    ["ApiId"] = FnRef(webSocketItem.FullName),
                    ["RouteKey"] = webSocketRoute.Source.RouteKey,
                    ["AuthorizationType"] = "NONE",
                    ["OperationName"] = webSocketRoute.Source.OperationName,
                    ["RouteResponseSelectionExpression"] = "$default",
                    ["Target"] = FnSub($"integrations/${{{integration.FullName}}}")
                };
                var route = _builder.AddResource(
                    parent: webSocketRoute.Function,
                    name: routeName + "Route",
                    description: $"WebSocket Route for `{webSocketRoute.Source.RouteKey}`",
                    scope: null,
                    resource: routeResource,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: webSocketRoute.Function.Condition,
                    pragmas: null
                );
                webSocketResources.Add(new KeyValuePair<string, object>(route.FullName, routeResource));

                // add route response resource
                var routeResponseResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::RouteResponse") {
                    ["ApiId"] = FnRef(webSocketItem.FullName),
                    ["RouteId"] = FnRef(route.FullName),
                    ["RouteResponseKey"] = "$default"
                };
                var routeResponse = _builder.AddResource(
                    parent: webSocketRoute.Function,
                    name: routeName + "RouteResponse",
                    description: $"WebSocket Route Response for `{webSocketRoute.Source.RouteKey}`",
                    scope: null,
                    resource: routeResponseResource,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: webSocketRoute.Function.Condition,
                    pragmas: null
                );
                webSocketResources.Add(new KeyValuePair<string, object>(routeResponse.FullName, routeResponseResource));

                // add lambda invocation permission resource
                _builder.AddResource(
                    parent: webSocketRoute.Function,
                    name: routeName + "Permission",
                    description: $"WebSocket invocation permission for `{webSocketRoute.Source.RouteKey}`",
                    scope: null,
                    resource: new Humidifier.Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        FunctionName = FnRef(webSocketRoute.Function.FullName),
                        Principal = "apigateway.amazonaws.com",
                        SourceArn = FnSub($"arn:aws:execute-api:${{AWS::Region}}:${{AWS::AccountId}}:${{Module::WebSocket}}/LATEST/{webSocketRoute.Source.RouteKey}")
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: webSocketRoute.Function.Condition,
                    pragmas: null
                );
            }

            // WebSocket deployment depends on all methods and their hash (to force redeployment in case of change)
            var resourcesSignature = string.Join("\n", webSocketResources
                .OrderBy(kv => kv.Key)
                .Select(kv => JsonConvert.SerializeObject(kv.Value))
            );
            string methodsHash = resourcesSignature.ToMD5Hash();

            // add WebSocket url
            _builder.AddVariable(
                parent: webSocketItem,
                name: "Url",
                description: "Module WebSocket URL",
                type: "String",
                scope: null,
                value: FnSub("wss://${Module::WebSocket}.execute-api.${AWS::Region}.amazonaws.com/${Module::WebSocket::Stage}"),
                allow: null,
                encryptionContext: null
            );

            // NOTE (2018-06-21, bjorg): the WebSocket deployment depends on ALL route resources having been created;
            //  a new name is used for the deployment to force the stage to be updated
            var deploymentWithHash = _builder.AddResource(
                parent: webSocketItem,
                name: "Deployment" + methodsHash,
                description: "Module WebSocket Deployment",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Deployment") {
                    ["ApiId"] = FnRef("Module::WebSocket"),
                    ["Description"] = FnSub($"${{AWS::StackName}} WebSocket [{methodsHash}]")
                },
                resourceExportAttribute: null,
                dependsOn: webSocketResources.Select(kv => kv.Key).ToArray(),
                condition: null,
                pragmas: null
            );
            var deployment = _builder.AddVariable(
                parent: webSocketItem,
                name: "Deployment",
                description: "Module WebSocket Deployment",
                type: "String",
                scope: null,
                value: FnRef(deploymentWithHash.FullName),
                allow: null,
                encryptionContext: null
            );

            // WebSocket stage depends on deployment
            _builder.AddResource(
                parent: webSocketItem,
                name: "Stage",
                description: "Module WebSocket Stage",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Stage") {
                    ["ApiId"] = FnRef("Module::WebSocket"),
                    ["StageName"] = "LATEST",
                    ["Description"] = "Module WebSocket LATEST Stage",
                    ["DeploymentId"] = FnRef(deployment.FullName)
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null
            );
        }

        private void AddRestApiResource(AModuleItem parent, object restApiId, object parentId, int level, IEnumerable<(FunctionItem Function, RestApiSource Source)> routes, Dictionary<string, object> apiMethodDeclarations) {

            // create methods at this route level to parent id
            foreach(var route in routes.Where(route => route.Source.Path.Length == level)) {
                Humidifier.ApiGateway.Method apiMethod;
                switch(route.Source.Integration) {
                case ApiGatewaySourceIntegration.RequestResponse:
                    apiMethod = CreateRequestResponseApiMethod(route.Function, route.Source);
                    break;
                case ApiGatewaySourceIntegration.SlackCommand:
                    apiMethod = CreateSlackRequestApiMethod(route.Function, route.Source);
                    break;
                default:
                    LogError($"api integration {route.Source.Integration} is not supported");
                    continue;
                }

                // add API method item
                var method = _builder.AddResource(
                    parent: parent,
                    name: route.Source.HttpMethod,
                    description: null,
                    scope: null,
                    resource: apiMethod,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: route.Function.Condition,
                    pragmas: null
                );
                apiMethodDeclarations.Add(method.FullName, apiMethod);

                // check if method has a request schema
                if(route.Source.RequestSchema != null) {

                    // create request model
                    var model = _builder.AddResource(
                        parent: method,
                        name: "RequestModel",
                        description: null,
                        scope: null,
                        resource: new Humidifier.ApiGateway.Model {
                            ContentType = "application/json",
                            Name = $"{route.Source.InvokeMethod}Request",
                            RestApiId = restApiId,
                            Schema = route.Source.RequestSchema
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: route.Function.Condition,
                        pragmas: null
                    );
                    apiMethodDeclarations.Add(model.FullName, route.Source.ResponseSchema);

                    // update the API method to require request validation
                    apiMethod.Integration.PassthroughBehavior = "NEVER";
                    apiMethod.RequestValidatorId = FnRef("Module::RestApi::RequestValidator");
                    if(apiMethod.RequestModels == null) {
                        apiMethod.RequestModels = new Dictionary<string, dynamic>();
                    }
                    apiMethod.RequestModels.Add("application/json", FnRef(model.FullName));
                }

                // check if method has a response schema
                if(route.Source.ResponseSchema != null)  {

                    // create request model
                    var model = _builder.AddResource(
                        parent: method,
                        name: "ResponseModel",
                        description: null,
                        scope: null,
                        resource: new Humidifier.ApiGateway.Model {
                            ContentType = "application/json",
                            Name = $"{route.Source.InvokeMethod}Response",
                            RestApiId = restApiId,
                            Schema = route.Source.ResponseSchema
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: route.Function.Condition,
                        pragmas: null
                    );
                    apiMethodDeclarations.Add(model.FullName, route.Source.ResponseSchema);

                    // update the API method with the response schema
                    if(apiMethod.MethodResponses == null) {
                        apiMethod.MethodResponses = new List<Humidifier.ApiGateway.MethodTypes.MethodResponse>();
                    }
                    apiMethod.MethodResponses.Add(new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                        StatusCode = 200,
                        ResponseModels = new Dictionary<string, dynamic> {
                            ["application/json"] = FnRef(model.FullName)
                        }
                    });
                }

                // add permission to API method to invoke lambda
                _builder.AddResource(
                    parent: method,
                    name: "Permission",
                    description: null,
                    scope: null,
                    resource: new Humidifier.Lambda.Permission {
                        Action = "lambda:InvokeFunction",
                        FunctionName = FnGetAtt(route.Function.FullName, "Arn"),
                        Principal = "apigateway.amazonaws.com",
                        SourceArn = FnSub($"arn:aws:execute-api:${{AWS::Region}}:${{AWS::AccountId}}:${{Module::RestApi}}/LATEST/{route.Source.HttpMethod}/{string.Join("/", route.Source.Path)}")
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: route.Function.Condition,
                    pragmas: null
                );
            }

            // find sub-routes and group common sub-route prefix
            var subRoutes = routes.Where(route => route.Source.Path.Length > level).ToLookup(route => route.Source.Path[level]);
            foreach(var subRoute in subRoutes) {

                // remove special character from path segment and capitalize it
                var partName = subRoute.Key.ToPascalIdentifier();

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
                AddRestApiResource(resource, restApiId, FnRef(resource.FullName), level + 1, subRoute, apiMethodDeclarations);
            }

            Humidifier.ApiGateway.Method CreateRequestResponseApiMethod(FunctionItem function, RestApiSource source) {
                return new Humidifier.ApiGateway.Method {
                    AuthorizationType = "NONE",
                    HttpMethod = source.HttpMethod,
                    OperationName = source.OperationName,
                    ApiKeyRequired = source.ApiKeyRequired,
                    ResourceId = parentId,
                    RestApiId = restApiId,
                    Integration = new Humidifier.ApiGateway.MethodTypes.Integration {
                        Type = "AWS_PROXY",
                        IntegrationHttpMethod = "POST",
                        Uri = FnSub($"arn:aws:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{function.FullName}.Arn}}/invocations")
                    }
                };
            }

            Humidifier.ApiGateway.Method CreateSlackRequestApiMethod(FunctionItem function, RestApiSource source) {

                // NOTE (2018-06-06, bjorg): Slack commands have a 3sec timeout on invocation, which is rarely good enough;
                // instead we wire Slack command requests up as asynchronous calls; this way, we can respond with
                // a callback later and the integration works well all the time.
                return new Humidifier.ApiGateway.Method {
                    AuthorizationType = "NONE",
                    HttpMethod = source.HttpMethod,
                    OperationName = source.OperationName,
                    ApiKeyRequired = source.ApiKeyRequired,
                    ResourceId = parentId,
                    RestApiId = restApiId,
                    Integration = new Humidifier.ApiGateway.MethodTypes.Integration {
                        Type = "AWS",
                        IntegrationHttpMethod = "POST",
                        Uri = FnSub($"arn:aws:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{function.FullName}.Arn}}/invocations"),
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

        private void AddFunctionSources(FunctionItem function) {

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
                case RestApiSource apiGatewaySource:
                    _restApiRoutes.Add((Function: function, Source: apiGatewaySource));
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
                case WebSocketSource webSocketSource:
                    _webSocketRoutes.Add((Function: function, Source: webSocketSource));
                    break;
                default:
                    throw new ApplicationException($"unrecognized function source type '{source?.GetType()}' for source #{sourceSuffix}");
                }
            }
        }

        private void Enumerate(object value, Action<string, object> action, Func<AResourceItem, object> getReference = null) {
            if(value is string fullName) {
                if(!_builder.TryGetItem(fullName, out var item)) {
                    LogError($"could not find function source: '{fullName}'");
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