﻿/*
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Metadata;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelFunctionProcessor : AModelProcessor {

        //--- Class Fields ---
        private static readonly string LambdaRestApiRequestTemplate = typeof(ModelFunctionProcessor).Assembly.ReadManifestResource("LambdaSharp.Tool.Resources.LambdaRestApiRequest.vtl");
        private static readonly string LambdaRestApiResponseTemplate = typeof(ModelFunctionProcessor).Assembly.ReadManifestResource("LambdaSharp.Tool.Resources.LambdaRestApiResponse.vtl");

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

                // grant read access to deployment bucket
                _builder.AddGrant(
                    name: "DeploymentBucketReadOnly",
                    awsType: null,
                    reference: FnSub($"arn:${{AWS::Partition}}:s3:::${{DeploymentBucketName}}/{_builder.ModuleInfo.Origin ?? ModuleInfo.MODULE_ORIGIN_PLACEHOLDER}/${{Module::Namespace}}/${{Module::Name}}/.artifacts/*"),
                    allow: "s3:GetObject",
                    condition: null
                );

                // add functions
                foreach(var function in functions) {
                    AddEventSources(function);
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

            // read WebSocket configuration
            _builder.TryGetOverride("Module::RestApi.EndpointConfiguration", out var endpointConfiguration);
            _builder.TryGetOverride("Module::RestApi.Policy", out var policy);

            // create a REST API
            var restApi = _builder.AddResource(
                parent: moduleItem,
                name: "RestApi",
                description: "Module REST API",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGateway::RestApi") {
                    ["Name"] = FnSub("${AWS::StackName} Module API"),
                    ["Description"] = FnSub("${Module::FullName} API (v${Module::Version})"),
                    ["FailOnWarnings"] = true,
                    ["EndpointConfiguration"] = endpointConfiguration,
                    ["Policy"] = policy
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null,
                deletionPolicy: null
            );

            // create variable to hold stage name
            _builder.TryGetOverride("Module::RestApi::StageName", out var stageName);
            _builder.AddVariable(
                parent: restApi,
                name: "StageName",
                description: "Module REST API Stage Name",
                type: "String",
                scope: null,
                value: stageName ?? "LATEST",
                allow: null,
                encryptionContext: null
            );

            // add RestApi url
            var restDomainName = _builder.AddVariable(
                parent: restApi,
                name: "DomainName",
                description: "Module REST API Domain Name",
                type: "String",
                scope: null,
                value: FnSub($"${{{restApi.FullName}}}.execute-api.${{AWS::Region}}.${{AWS::URLSuffix}}"),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: restApi,
                name: "Url",
                description: "Module REST API URL",
                type: "String",
                scope: null,
                value: FnSub($"https://${{{restDomainName.FullName}}}/${{Module::RestApi::StageName}}"),
                allow: null,
                encryptionContext: null
            );

            // add CORS Origin variable
            _builder.TryGetOverride("Module::RestApi::CorsOrigin", out var moduleRestApiCorsOrigin);
            object moduleRestApiCorsOriginExpression;
            if(moduleRestApiCorsOrigin != null) {
                _builder.AddVariable(
                    parent: restApi,
                    name: "CorsOrigin",
                    description: "CORS Origin for REST API",
                    type: "String",
                    scope: null,
                    value: moduleRestApiCorsOrigin,
                    allow: null,
                    encryptionContext: null
                );

                // NOTE (2021-04-01, bjorg): we should always assign the CORS_ORIGIN environment variable even
                //  when the we know the value is `!Ref AWS::NoValue`. The `lash` compiler should be smart enough
                //  to remove these properties again and simplify the datastructure accordingly.

                // add CORS origin value to Lambda environment variables
                foreach(var restApiRoute in _restApiRoutes) {

                    // check if Environment needs to be initialized
                    if(restApiRoute.Function.Function.Environment == null) {
                        restApiRoute.Function.Function.Environment = new Humidifier.Lambda.FunctionTypes.Environment();
                    }

                    // check if Environment.Variables needs to be initialized
                    if(restApiRoute.Function.Function.Environment.Variables == null) {
                        restApiRoute.Function.Function.Environment.Variables = new Dictionary<string, dynamic>();
                    }

                    // add CORS_ORIGIN environment variable to Lambda function
                    restApiRoute.Function.Function.Environment.Variables.TryAdd("CORS_ORIGIN", moduleRestApiCorsOrigin);
                }

                // convert into a quoted expression
                moduleRestApiCorsOriginExpression = FnSub("'${CorsOrigin}'", new Dictionary<string, object> {
                    ["CorsOrigin"] = moduleRestApiCorsOrigin
                });
            } else {

                // add placeholder variable
                _builder.AddVariable(
                    parent: restApi,
                    name: "CorsOrigin",
                    description: "CORS Origin for REST API",
                    type: "String",
                    scope: null,
                    value: FnRef("AWS::NoValue"),
                    allow: null,
                    encryptionContext: null
                );
                moduleRestApiCorsOriginExpression = null;
            }

            // recursively create resources as needed
            var apiDeclarations = new Dictionary<string, object>();
            AddRestApiResource(restApi, FnRef(restApi.FullName), FnGetAtt(restApi.FullName, "RootResourceId"), 0, _restApiRoutes, apiDeclarations, moduleRestApiCorsOriginExpression);

            // RestApi deployment depends on all methods and their hash (to force redeployment in case of change)
            string apiDeclarationsChecksum = string.Join("\n", apiDeclarations
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={StringEx.GetJsonChecksum(JsonConvert.SerializeObject(kv.Value))}")
            ).ToMD5Hash();

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
                        resource: new Humidifier.CustomResource("AWS::ApiGateway::RequestValidator") {
                            ["RestApiId"] = FnRef(restApi.FullName),
                            ["ValidateRequestBody"] = true,
                            ["ValidateRequestParameters"] = true
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: null,
                        pragmas: null,
                        deletionPolicy: null
                    ).DiscardIfNotReachable = true;
            }

            // create log-group for REST API
            var restLogGroup = _builder.AddResource(
                parent: restApi,
                name: "LogGroup",
                description: null,
                scope: null,
                resource: new Humidifier.CustomResource("AWS::Logs::LogGroup") {
                    ["LogGroupName"] = FnSub($"API-Gateway-Execution-Logs_${{{restApi.FullName}}}/${{Module::RestApi::StageName}}"),
                    ["RetentionInDays"] = FnRef("Module::LogRetentionInDays")
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null,
                deletionPolicy: null
            );

            // NOTE (2018-06-21, bjorg): the RestApi deployment resource depends on ALL methods resources having been created;
            //  a new name is used for the deployment to force the stage to be updated
            var deploymentWithChecksum = _builder.AddResource(
                parent: restApi,
                name: "Deployment" + apiDeclarationsChecksum,
                description: "Module REST API Deployment",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGateway::Deployment") {
                    ["RestApiId"] = FnRef("Module::RestApi"),
                    ["Description"] = FnSub($"${{AWS::StackName}} API [{apiDeclarationsChecksum}]")
                },
                resourceExportAttribute: null,
                dependsOn: apiDeclarations.Select(kv => kv.Key).OrderBy(key => key).ToArray(),
                condition: null,
                pragmas: null,
                deletionPolicy: null
            );
            var deployment = _builder.AddVariable(
                parent: restApi,
                name: "Deployment",
                description: "Module REST API Deployment",
                type: "String",
                scope: null,
                value: FnRef(deploymentWithChecksum.FullName),
                allow: null,
                encryptionContext: null
            );

            // RestApi stage depends on API gateway deployment and API gateway account
            _builder.TryGetOverride("Module::RestApi::LoggingLevel", out var restApiStageLoggingLevel);
            _builder.AddResource(
                parent: restApi,
                name: "Stage",
                description: "Module REST API Stage",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGateway::Stage") {
                    ["RestApiId"] = FnRef("Module::RestApi"),
                    ["Description"] =  FnSub("Module REST API ${Module::RestApi::StageName} Stage"),
                    ["DeploymentId"] = FnRef(deployment.FullName),
                    ["StageName"] = FnRef("Module::RestApi::StageName"),
                    ["MethodSettings"] = new[] {
                        new Humidifier.ApiGateway.StageTypes.MethodSetting {
                            DataTraceEnabled = true,
                            HttpMethod = "*",
                            LoggingLevel = restApiStageLoggingLevel ?? "INFO",
                            ResourcePath = "/*",
                            MetricsEnabled = true
                        }
                    }.ToList(),
                    ["TracingEnabled"] = FnIf("XRayIsEnabled", true, false)
                },
                resourceExportAttribute: null,
                dependsOn: new[] { restLogGroup.FullName },
                condition: null,
                pragmas: null,
                deletionPolicy: null
            );
        }

        private void AddWebSocketResources(IEnumerable<FunctionItem> functions) {
            var moduleItem = _builder.GetItem("Module");

            // check if we need to generate the websocket or if we're given a reference to it
            AModuleItem webSocket = null;
            var generateWebSocket = !_builder.TryGetOverride("Module::WebSocket", out var webSocketFullName);
            if(generateWebSocket) {

                // create a WebSocket API
                _builder.TryGetOverride("Module::WebSocket.RouteSelectionExpression", out var routeSelectionExpression);
                _builder.TryGetOverride("Module::WebSocket.ApiKeySelectionExpression", out var apiKeySelectionExpression);
                webSocket = _builder.AddResource(
                    parent: moduleItem,
                    name: "WebSocket",
                    description: "Module WebSocket",
                    scope: null,
                    resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Api") {
                        ["Name"] = FnSub("${AWS::StackName} Module WebSocket"),
                        ["ProtocolType"] = "WEBSOCKET",
                        ["Description"] = FnSub("${Module::FullName} WebSocket (v${Module::Version})"),
                        ["RouteSelectionExpression"] = routeSelectionExpression ?? "$request.body.action",
                        ["ApiKeySelectionExpression"] = apiKeySelectionExpression
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: null,
                    pragmas: null,
                    deletionPolicy: null
                );
                webSocketFullName = FnRef(webSocket.FullName);
            } else {

                // create variable to hold the web socket reference
                webSocket = _builder.AddVariable(
                    parent: moduleItem,
                    name: "WebSocket",
                    description: "Module WebSocket (reference)",
                    type: "String",
                    scope: null,
                    value: webSocketFullName,
                    allow: null,
                    encryptionContext: null
                );
            }

            // create variable to hold stage name
            _builder.TryGetOverride("Module::WebSocket::StageName", out var stageName);
            _builder.AddVariable(
                parent: webSocket,
                name: "StageName",
                description: "Module WebSocket Stage Name",
                type: "String",
                scope: null,
                value: stageName ?? "LATEST",
                allow: null,
                encryptionContext: null
            );

            // give permission to the Lambda functions to communicate back over the WebSocket
            _builder.AddGrant(
                name: "WebSocketConnections",
                awsType: null,
                reference: FnSub("arn:${AWS::Partition}:execute-api:${AWS::Region}:${AWS::AccountId}:${Module::WebSocket}/${Module::WebSocket::StageName}/POST/@connections/*"),
                allow: new[] {
                    "execute-api:ManageConnections"
                },
                condition: null
            );

            // create resources as needed
            var webSocketResources = new Dictionary<string, object>();
            foreach(var webSocketRouteByFunction in _webSocketRoutes.GroupBy(route => route.Function.FullName)) {
                var function = webSocketRouteByFunction.First().Function;

                // add integration resource (only need one per end-point function)
                var integrationResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::Integration") {
                    ["ApiId"] = webSocketFullName,
                    ["Description"] = $"WebSocket Integration for '{function.FullName}'",
                    ["IntegrationType"] = "AWS_PROXY",
                    ["IntegrationUri"] = FnSub($"arn:${{AWS::Partition}}:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{function.FullName}.Arn}}/invocations"),
                    ["PassthroughBehavior"] = "WHEN_NO_TEMPLATES"
                };
                var integration = _builder.AddResource(
                    parent: function,
                    name: "WebSocketIntegration",
                    description: $"WebSocket Integration for '{function.FullName}'",
                    scope: null,
                    resource: integrationResource,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: function.Condition,
                    pragmas: null,
                    deletionPolicy: null
                );
                webSocketResources.Add(integration.FullName, integrationResource);

                // create resources per route
                foreach(var webSocketRoute in webSocketRouteByFunction.OrderBy(route => route.Source.RouteKey)) {

                    // remove special character from path segment and capitalize it
                    var routeName = webSocketRoute.Source.RouteKey.ToPascalIdentifier();

                    // add route resource
                    var routeResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::Route") {
                        ["ApiId"] = webSocketFullName,
                        ["RouteKey"] = webSocketRoute.Source.RouteKey,
                        ["ApiKeyRequired"] = webSocketRoute.Source.ApiKeyRequired,
                        ["AuthorizationType"] = webSocketRoute.Source.AuthorizationType ?? "NONE",
                        ["AuthorizationScopes"] =  webSocketRoute.Source.AuthorizationScopes,
                        ["AuthorizerId"] = webSocketRoute.Source.AuthorizerId,
                        ["OperationName"] = webSocketRoute.Source.OperationName,
                        ["Target"] = FnSub($"integrations/${{{integration.FullName}}}")
                    };
                    var route = (ResourceItem)_builder.AddResource(
                        parent: webSocketRoute.Function,
                        name: routeName + "Route",
                        description: $"WebSocket Route for '{webSocketRoute.Source.RouteKey}'",
                        scope: null,
                        resource: routeResource,
                        resourceExportAttribute: null,
                        dependsOn: new List<string>(),
                        condition: webSocketRoute.Function.Condition,
                        pragmas: null,
                        deletionPolicy: null
                    );
                    webSocketResources.Add(route.FullName, routeResource);

                    // add optional request/response models
                    if(
                        !webSocketRoute.Source.RouteKey.Equals("$connect", StringComparison.Ordinal)
                        && !webSocketRoute.Source.RouteKey.Equals("$disconnect", StringComparison.Ordinal)
                    ) {

                        // add request model
                        switch(webSocketRoute.Source.RequestSchema) {
                        case null:

                            // nothing to do
                            break;
                        case "Object":

                            // nothing to do; object requests allow any JSON object and there is no built-in 'Empty' model
                            break;
                        case "Proxy":

                            // nothing to do; proxy requests have no validation
                            break;
                        case "Void":

                            // not-applicable
                            break;
                        case IDictionary _:

                            // create request model
                            var model = _builder.AddResource(
                                parent: route,
                                name: "RequestModel",
                                description: null,
                                scope: null,
                                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Model") {
                                    ["ApiId"] = webSocketFullName,
                                    ["ContentType"] = webSocketRoute.Source.RequestContentType,
                                    ["Name"] = $"{route.LogicalId}RequestModel",
                                    ["Schema"] = webSocketRoute.Source.RequestSchema
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: function.Condition,
                                pragmas: null,
                                deletionPolicy: null
                            );
                            webSocketResources.Add(model.FullName, webSocketRoute.Source.RequestSchema);
                            route.DependsOn.Add(model.FullName);

                            // update the route to require request validation
                            routeResource["ModelSelectionExpression"] = "default";
                            routeResource["RequestModels"] = new Dictionary<string, object> {
                                ["default"] = $"{route.LogicalId}RequestModel"
                            };
                            break;
                        default:
                            throw new ApplicationException($"unrecognized request schema type: {webSocketRoute.Source.RequestSchema} [{webSocketRoute.Source.RequestSchema.GetType()}]");
                        }

                        // add response model
                        switch(webSocketRoute.Source.ResponseSchema) {
                        case null:

                            // nothing to do
                            break;
                        case "Object":

                            // add route response resource to return non-validated JSON response
                            var routeResponseResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::RouteResponse") {
                                ["ApiId"] = webSocketFullName,
                                ["RouteId"] = FnRef(route.FullName),
                                ["RouteResponseKey"] = "$default"
                            };
                            var routeResponse = _builder.AddResource(
                                parent: webSocketRoute.Function,
                                name: routeName + "RouteResponse",
                                description: $"WebSocket Route Response for '{webSocketRoute.Source.RouteKey}'",
                                scope: null,
                                resource: routeResponseResource,
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: webSocketRoute.Function.Condition,
                                pragmas: null,
                                deletionPolicy: null
                            );
                            webSocketResources.Add(routeResponse.FullName, routeResponseResource);

                            // update the route to require route response
                            routeResource["RouteResponseSelectionExpression"] = "$default";
                            break;
                        case "Proxy":

                            // nothing to do; proxy responses have no validation
                            break;
                        case "Void":

                            // nothing to do
                            break;
                        case IDictionary _:

                            // create response model
                            var model = _builder.AddResource(
                                parent: route,
                                name: "ResponseModel",
                                description: null,
                                scope: null,
                                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Model") {
                                    ["ApiId"] = webSocketFullName,
                                    ["ContentType"] = webSocketRoute.Source.ResponseContentType,
                                    ["Name"] = $"{route.LogicalId}ResponseModel",
                                    ["Schema"] = webSocketRoute.Source.ResponseSchema
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: function.Condition,
                                pragmas: null,
                                deletionPolicy: null
                            );
                            webSocketResources.Add(model.FullName, webSocketRoute.Source.ResponseSchema);

                            // add route response resource
                            routeResponseResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::RouteResponse") {
                                ["ApiId"] = webSocketFullName,
                                ["RouteId"] = FnRef(route.FullName),
                                ["RouteResponseKey"] = "$default",
                                ["ModelSelectionExpression"] = "default",
                                ["ResponseModels"] = new Dictionary<string, object> {
                                    ["default"] = $"{route.LogicalId}ResponseModel"
                                }
                            };
                            routeResponse = _builder.AddResource(
                                parent: webSocketRoute.Function,
                                name: routeName + "RouteResponse",
                                description: $"WebSocket Route Response for '{webSocketRoute.Source.RouteKey}'",
                                scope: null,
                                resource: routeResponseResource,
                                resourceExportAttribute: null,
                                dependsOn: new[] { model.FullName },
                                condition: webSocketRoute.Function.Condition,
                                pragmas: null,
                                deletionPolicy: null
                            );
                            webSocketResources.Add(routeResponse.FullName, routeResponseResource);

                            // update the route to require route response
                            routeResource["RouteResponseSelectionExpression"] = "$default";
                            break;
                        default:
                            throw new ApplicationException($"unrecognized response schema type: {webSocketRoute.Source.ResponseSchema} [{webSocketRoute.Source.ResponseSchema.GetType()}]");
                        }
                    }

                    // check if a lambda permission for the WebSocket already exists for this function
                    if(!_builder.TryGetItem($"{webSocketRoute.Function.FullName}::WebSocketPermission", out _)) {

                        // add lambda invocation permission resource
                        _builder.AddResource(
                            parent: webSocketRoute.Function,
                            name: "WebSocketPermission",
                            description: "WebSocket invocation permission",
                            scope: null,
                            resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                                ["Action"] = "lambda:InvokeFunction",
                                ["FunctionName"] = FnRef(webSocketRoute.Function.FullName),
                                ["Principal"] = "apigateway.amazonaws.com",
                                ["SourceArn"] = FnSub("arn:${AWS::Partition}:execute-api:${AWS::Region}:${AWS::AccountId}:${Module::WebSocket}/${Module::WebSocket::StageName}/*")
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: webSocketRoute.Function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                    }
                }
            }

            // add WebSocket url
            var webSocketDomainName = _builder.AddVariable(
                parent: webSocket,
                name: "DomainName",
                description: "Module WebSocket Domain Name",
                type: "String",
                scope: null,
                value: FnSub($"${{WebSocketFullName}}.execute-api.${{AWS::Region}}.${{AWS::URLSuffix}}", new Dictionary<string, object> {
                    ["WebSocketFullName"] = webSocketFullName
                }),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: webSocket,
                name: "Url",
                description: "Module WebSocket URL",
                type: "String",
                scope: new List<string> { "all" },
                value: FnSub($"wss://${{Module::WebSocket::DomainName}}/${{Module::WebSocket::StageName}}"),
                allow: null,
                encryptionContext: null
            );

            // create additional resources for the generated web socket
            if(generateWebSocket) {

                // create log-group for WebSocket
                var webSocketLogGroup = _builder.AddResource(
                    parent: webSocket,
                    name: "LogGroup",
                    description: null,
                    scope: null,
                    resource: new Humidifier.CustomResource("AWS::Logs::LogGroup") {
                        ["RetentionInDays"] = FnRef("Module::LogRetentionInDays")
                    },
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: null,
                    pragmas: null,
                    deletionPolicy: null
                );

                // WebSocket stage depends on Module::WebSocket::LogGroup
                _builder.AddResource(
                    parent: webSocket,
                    name: "Stage",
                    description: "Module WebSocket Stage",
                    scope: null,
                    resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Stage") {
                        ["AccessLogSettings"] = new Dictionary<string, dynamic> {
                            ["DestinationArn"] = FnSub($"arn:${{AWS::Partition}}:logs:${{AWS::Region}}:${{AWS::AccountId}}:log-group:${{{webSocketLogGroup.FullName}}}"),
                            ["Format"] = JsonConvert.SerializeObject(JObject.Parse(GetType().Assembly.ReadManifestResource("LambdaSharp.Tool.Resources.WebSocketLogging.json")), Formatting.None)
                        },
                        ["ApiId"] = FnRef("Module::WebSocket"),
                        ["StageName"] = FnRef("Module::WebSocket::StageName"),
                        ["Description"] = FnSub("Module WebSocket ${Module::WebSocket::StageName} Stage"),
                        ["AutoDeploy"] = true,
                        ["DefaultRouteSettings"] = new Dictionary<string, dynamic> {
                            ["DataTraceEnabled"] = true,
                            ["DetailedMetricsEnabled"] = true,
                            ["LoggingLevel"] = "INFO"
                        }
                    },
                    resourceExportAttribute: null,
                    dependsOn: new[] { webSocketLogGroup.FullName },
                    condition: null,
                    pragmas: null,
                    deletionPolicy: null
                );
            } else {

                // add blank placeholders so that reference can resolve during code analysis
                _builder.AddVariable(
                    parent: webSocket,
                    name: "Stage",
                    description: "Module WebSocket Stage",
                    type: "String",
                    scope: null,
                    value: "",
                    allow: null,
                    encryptionContext: null
                );
            }
        }

        private void AddRestApiResource(AModuleItem parent, object restApiId, object parentId, int level, IEnumerable<(FunctionItem Function, RestApiSource Source)> routes, Dictionary<string, object> apiDeclarations, object moduleRestApiCorsOrigin) {

            // create methods at this route level to parent id
            var localRoutes = routes.Where(route => route.Source.Path.Length == level).ToList();
            foreach(var route in localRoutes) {
                Humidifier.ApiGateway.Method apiMethodResource;
                switch(route.Source.Integration) {
                case ApiGatewaySourceIntegration.RequestResponse:
                    apiMethodResource = CreateRequestResponseApiMethod(route.Function, route.Source);
                    break;
                case ApiGatewaySourceIntegration.SlackCommand:
                    apiMethodResource = CreateSlackRequestApiMethod(route.Function, route.Source);
                    break;
                default:
                    LogError($"api integration {route.Source.Integration} is not supported");
                    continue;
                }
                var integration = apiMethodResource.Integration;

                // add API method item
                var method = _builder.AddResource(
                    parent: parent,
                    name: route.Source.HttpMethod,
                    description: null,
                    scope: null,
                    resource: apiMethodResource,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: route.Function.Condition,
                    pragmas: null,
                    deletionPolicy: null
                );
                apiDeclarations.Add(method.FullName, apiMethodResource);
                integration.PassthroughBehavior = "WHEN_NO_TEMPLATES";

                // set list of expected query parameters (if any)
                if(route.Source.QueryStringParameters != null) {
                    if(apiMethodResource.RequestParameters == null) {
                        apiMethodResource.RequestParameters = new Dictionary<string, bool>();
                    }
                    foreach(var queryParameter in route.Source.QueryStringParameters) {
                        apiMethodResource.RequestParameters.Add($"method.request.querystring.{queryParameter.Key}", queryParameter.Value);
                    }
                }

                // check if method has a request schema
                switch(route.Source.RequestSchema) {
                case null:

                    // nothing to do
                    break;
                case "Object":

                    // object requests use the built-in 'Empty' model which allows any valid JSON object
                    apiMethodResource.RequestModels = new Dictionary<string, dynamic> {
                        [route.Source.RequestContentType] = "Empty"
                    };
                    apiMethodResource.RequestValidatorId = FnRef("Module::RestApi::RequestValidator");
                    break;
                case "Proxy":

                    // nothing to do; proxy requests have no validation
                    break;
                case "Void":

                    // NOTE (2019-04-02, bjorg): unfortunately, this does not enforce that the request has no payload
                    apiMethodResource.RequestValidatorId = FnRef("Module::RestApi::RequestValidator");

                    // TODO (2019-05-25, bjorg): check request verb; only GET and OPTIONS should be able to have no request body

                    break;
                case IDictionary _:

                    // create request model
                    var model = _builder.AddResource(
                        parent: method,
                        name: "RequestModel",
                        description: null,
                        scope: null,
                        resource: new Humidifier.CustomResource("AWS::ApiGateway::Model") {
                            ["ContentType"] = route.Source.RequestContentType,
                            ["RestApiId"] = restApiId,
                            ["Schema"] = route.Source.RequestSchema
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: route.Function.Condition,
                        pragmas: null,
                        deletionPolicy: null
                    );
                    apiDeclarations.Add(model.FullName, route.Source.RequestSchema);

                    // update API method to require request validation
                    apiMethodResource.RequestModels = new Dictionary<string, dynamic> {
                        [route.Source.RequestContentType] = FnRef(model.FullName)
                    };
                    apiMethodResource.RequestValidatorId = FnRef("Module::RestApi::RequestValidator");
                    apiMethodResource.MethodResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                            StatusCode = 200,
                            ResponseModels = new Dictionary<string, object> {
                                ["application/json"] = "Empty"
                            }
                        }
                    }.ToList();

                    // set integration to AWS to enable payload manipulation
                    integration.Type = "AWS";
                    integration.RequestTemplates = new Dictionary<string, dynamic> {
                        ["application/json"] = LambdaRestApiRequestTemplate
                    };
                    integration.IntegrationResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.IntegrationResponse {
                            StatusCode = 200,
                            ResponseTemplates = new Dictionary<string, object> {
                                ["application/json"] = LambdaRestApiResponseTemplate
                            }
                        }
                    }.ToList();
                    break;
                default:
                    throw new ApplicationException($"unrecognized request schema type: {route.Source.RequestSchema} [{route.Source.RequestSchema.GetType()}]");
                }

                // check if method has a response schema
                switch(route.Source.ResponseSchema) {
                case null:

                    // nothing to do
                    break;
                case "Object":

                    // object responses use the built-in 'Empty' model which allows any valid JSON object
                    apiMethodResource.MethodResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                            StatusCode = 200,
                            ResponseModels = new Dictionary<string, object> {
                                [route.Source.ResponseContentType] = "Empty"
                            }
                        }
                    }.ToList();
                    break;
                case "Proxy":

                    // nothing to do

                    break;
                case "Void":

                    // TODO (2019-04-02, bjorg): allow specifying the default response
                    var defaultResponseContentType = "application/json";
                    var defaultResponsePayload = "";

                    // ensure GET/OPTIONS is not mapped to an asynchronous method
                    if((route.Source.HttpMethod == "GET") || (route.Source.HttpMethod == "OPTIONS")) {
                        LogError($"{route.Source.HttpMethod}:/{string.Join("/", route.Source.Path)} cannot be mapped to an asynchronous invocation method");
                    }

                    // void responses are mapped to asynchronous lambda requests with an empty response
                    if(integration.Type != "AWS") {
                        integration.Type = "AWS";
                        integration.RequestTemplates = new Dictionary<string, dynamic> {
                            ["application/json"] = LambdaRestApiRequestTemplate
                        };
                        integration.IntegrationHttpMethod = "POST";
                    }
                    if(integration.RequestParameters == null) {
                        integration.RequestParameters = new Dictionary<string, dynamic>();
                    }
                    integration.RequestParameters["integration.request.header.X-Amz-Invocation-Type"] = "'Event'";
                    integration.IntegrationResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.IntegrationResponse {
                            StatusCode = 202,
                            ResponseTemplates = new Dictionary<string, object> {
                                [defaultResponseContentType] = defaultResponsePayload
                            }
                        }
                    }.ToList();
                    apiMethodResource.MethodResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                            StatusCode = 202,
                            ResponseModels = new Dictionary<string, object> {
                                [defaultResponseContentType] = "Empty"
                            }
                        }
                    }.ToList();
                    break;
                case IDictionary _:

                    // create response model
                    var model = _builder.AddResource(
                        parent: method,
                        name: "ResponseModel",
                        description: null,
                        scope: null,
                        resource: new Humidifier.CustomResource("AWS::ApiGateway::Model") {
                            ["ContentType"] = route.Source.ResponseContentType,
                            ["RestApiId"] = restApiId,
                            ["Schema"] = route.Source.ResponseSchema
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: route.Function.Condition,
                        pragmas: null,
                        deletionPolicy: null
                    );
                    apiDeclarations.Add(model.FullName, route.Source.ResponseSchema);

                    // update the API method with the response schema
                    if(route.Source.ResponseContentType != null) {

                        // set method response
                        apiMethodResource.MethodResponses = new[] {
                            new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                                StatusCode = 200,
                                ResponseModels = new Dictionary<string, object> {
                                    [route.Source.ResponseContentType] = FnRef(model.FullName)
                                }
                            }
                        }.ToList();
                    }
                    break;
                default:
                    throw new ApplicationException($"unrecognized request response type: {route.Source.ResponseSchema} [{route.Source.ResponseSchema.GetType()}]");
                }

                // check if CORS origin header needs to be added
                if(moduleRestApiCorsOrigin != null) {

                    // ensure there is an method response definition
                    if(apiMethodResource.MethodResponses == null) {
                        apiMethodResource.MethodResponses = new[] {
                            new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                                StatusCode = 200,
                                ResponseModels = new Dictionary<string, object> {
                                    ["application/json"] = "Empty"
                                }
                            }
                        }.ToList();
                    }

                    // add CORS header method settings
                    foreach(var methodResponse in apiMethodResource.MethodResponses) {
                        if(methodResponse.ResponseParameters == null) {
                            methodResponse.ResponseParameters = new Dictionary<string, bool>();
                        }

                        // only add CORS origin header settings if none are provided
                        methodResponse.ResponseParameters.TryAdd("method.response.header.Access-Control-Allow-Origin", false);
                    }
                }

                // check if a lambda permission for the REST API already exists for this function
                if(!_builder.TryGetItem($"{route.Function.FullName}::RestApiPermission", out _)) {

                    // add permission to API method to invoke lambda
                    _builder.AddResource(
                        parent: route.Function,
                        name: "RestApiPermission",
                        description: "RestApi invocation permission",
                        scope: null,
                        resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                            ["Action"] = "lambda:InvokeFunction",
                            ["FunctionName"] = FnRef(route.Function.FullName),
                            ["Principal"] = "apigateway.amazonaws.com",
                            ["SourceArn"] = FnSub("arn:${AWS::Partition}:execute-api:${AWS::Region}:${AWS::AccountId}:${Module::RestApi}/${Module::RestApi::StageName}/*")
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: route.Function.Condition,
                        pragmas: null,
                        deletionPolicy: null
                    );
                }
            }

            // determine if an OPTIONS method needs to be created
            if(
                (moduleRestApiCorsOrigin != null)
                && localRoutes.Any()
                && !localRoutes.Any(route => route.Source.HttpMethod == "OPTIONS")
            ) {
                Humidifier.ApiGateway.Method optionsMethodResource = CreateCorsOptionsMethod(localRoutes.Select(route => route.Source.HttpMethod), moduleRestApiCorsOrigin);

                // add API method item
                var method = _builder.AddResource(
                    parent: parent,
                    name: "OPTIONS",
                    description: null,
                    scope: null,
                    resource: optionsMethodResource,
                    resourceExportAttribute: null,
                    dependsOn: null,
                    condition: localRoutes.First().Function.Condition,
                    pragmas: null,
                    deletionPolicy: null
                );
                apiDeclarations.Add(method.FullName, optionsMethodResource);
            }


            // find sub-routes and group common sub-route prefix
            var subRoutes = routes.Where(route => route.Source.Path.Length > level).ToLookup(route => route.Source.Path[level]);
            foreach(var subRoute in subRoutes) {

                // remove special character from path segment and capitalize it
                var partName = subRoute.Key.ToPascalIdentifier();

                // create a new parent resource to attach methods or sub-resource to
                var apiResourceResource = new Humidifier.CustomResource("AWS::ApiGateway::Resource") {
                    ["RestApiId"] = restApiId,
                    ["ParentId"] = parentId,
                    ["PathPart"] = subRoute.Key
                };
                var resource = _builder.AddResource(
                    parent: parent,
                    name: partName + "Resource",
                    description: null,
                    scope: null,
                    resource: apiResourceResource,
                    resourceExportAttribute: null,
                    dependsOn: null,

                    // TODO (2018-12-28, bjorg): handle conditional function
                    condition: null,
                    pragmas: null,
                    deletionPolicy: null
                );
                apiDeclarations.Add(resource.FullName, apiResourceResource);
                AddRestApiResource(resource, restApiId, FnRef(resource.FullName), level + 1, subRoute, apiDeclarations, moduleRestApiCorsOrigin);
            }

            // local functions
            Humidifier.ApiGateway.Method CreateRequestResponseApiMethod(FunctionItem function, RestApiSource source) {
                return new Humidifier.ApiGateway.Method {
                    HttpMethod = source.HttpMethod,
                    OperationName = source.OperationName,
                    ApiKeyRequired = source.ApiKeyRequired,
                    AuthorizationType = source.AuthorizationType ?? "NONE",
                    AuthorizationScopes = source.AuthorizationScopes,
                    AuthorizerId = source.AuthorizerId,
                    ResourceId = parentId,
                    RestApiId = restApiId,
                    Integration = new Humidifier.ApiGateway.MethodTypes.Integration {
                        Type = "AWS_PROXY",
                        IntegrationHttpMethod = "POST",
                        Uri = FnSub($"arn:${{AWS::Partition}}:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{function.FullName}.Arn}}/invocations")
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
                        Uri = FnSub($"arn:${{AWS::Partition}}:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{function.FullName}.Arn}}/invocations"),
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

            Humidifier.ApiGateway.Method CreateCorsOptionsMethod(IEnumerable<string> httpMethods, object moduleRestApiCorsOrigin) {
                return new Humidifier.ApiGateway.Method {
                    HttpMethod = "OPTIONS",
                    AuthorizationType = "NONE",
                    ResourceId = parentId,
                    RestApiId = restApiId,
                    Integration = new Humidifier.ApiGateway.MethodTypes.Integration {
                        Type = "MOCK",
                        PassthroughBehavior = "WHEN_NO_MATCH",
                        IntegrationResponses = new[] {
                            new Humidifier.ApiGateway.MethodTypes.IntegrationResponse {
                                StatusCode = 204,
                                ResponseParameters = new Dictionary<string, dynamic> {
                                    ["method.response.header.Access-Control-Allow-Headers"] = "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token'",
                                    ["method.response.header.Access-Control-Allow-Methods"] = $"'{string.Join(",", httpMethods.Append("OPTIONS").OrderBy(httpMethod => httpMethod))}'",
                                    ["method.response.header.Access-Control-Allow-Origin"] = moduleRestApiCorsOrigin,
                                    ["method.response.header.Access-Control-Max-Age"] = "'600'"
                                },
                                ResponseTemplates = new Dictionary<string, dynamic> {
                                    ["application/json"] = ""
                                }
                            }
                        }.ToList(),
                        RequestTemplates = new Dictionary<string, dynamic> {
                            ["application/json"] = "{\"statusCode\": 200}"
                        }
                    },
                    MethodResponses = new[] {
                        new Humidifier.ApiGateway.MethodTypes.MethodResponse {
                            StatusCode = 204,
                            ResponseModels = new Dictionary<string, dynamic> {
                                ["application/json"] = "Empty"
                            },
                            ResponseParameters = new Dictionary<string, bool> {
                                ["method.response.header.Access-Control-Allow-Headers"] = false,
                                ["method.response.header.Access-Control-Allow-Methods"] = false,
                                ["method.response.header.Access-Control-Allow-Origin"] = false,
                                ["method.response.header.Access-Control-Max-Age"] = false
                            }
                        }
                    }.ToList()
                };
            }
        }

        private void AddEventSources(FunctionItem function) {

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
                            resource: new Humidifier.CustomResource("AWS::SNS::Subscription") {
                                ["Endpoint"] = FnGetAtt(function.FullName, "Arn"),
                                ["Protocol"] = "lambda",
                                ["TopicArn"] = arn,
                                ["FilterPolicy"] = (topicSource.Filters != null)
                                    ? JsonConvert.SerializeObject(topicSource.Filters)
                                    : null
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission{suffix}",
                            description: null,
                            scope: null,
                            resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                                ["Action"] = "lambda:InvokeFunction",
                                ["FunctionName"] = FnRef(function.FullName),
                                ["Principal"] = "sns.amazonaws.com",
                                ["SourceArn"] = arn
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
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
                            resource: new Humidifier.CustomResource("AWS::Events::Rule") {
                                ["ScheduleExpression"] = scheduleSource.Expression,
                                ["Targets"] = new[] {
                                    new Humidifier.Events.RuleTypes.Target {
                                        Id = id,
                                        Arn = FnGetAtt(function.FullName, "Arn"),
                                        InputTransformer = new Humidifier.Events.RuleTypes.InputTransformer {
                                            InputPathsMap = new Dictionary<string, object> {
                                                ["id"] = "$.id",
                                                ["time"] = "$.time"
                                            },
                                            InputTemplate =
@"{
    ""Id"": <id>,
    ""Time"": <time>,
    ""Name"": """ + scheduleSource.Name + @"""
}"
                                        }
                                    }
                                }.ToList()
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission",
                            description: null,
                            scope: null,
                            resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                                ["Action"] = "lambda:InvokeFunction",
                                ["FunctionName"] = FnRef(function.FullName),
                                ["Principal"] = "events.amazonaws.com",
                                ["SourceArn"] = FnGetAtt(schedule.FullName, "Arn")
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                    }
                    break;
                case RestApiSource apiGatewaySource:
                    _restApiRoutes.Add((Function: function, Source: apiGatewaySource));
                    break;
                case S3Source s3Source:
                    _builder.AddDependencyAsync(new ModuleInfo("LambdaSharp", "S3.Subscriber", Settings.CoreServicesReferenceVersion, "lambdasharp"), ModuleManifestDependencyType.Shared).Wait();
                    Enumerate(s3Source.Bucket, (suffix, arn) => {
                        var permission = _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission",
                            description: null,
                            scope: null,
                            resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                                ["Action"] = "lambda:InvokeFunction",
                                ["FunctionName"] = FnRef(function.FullName),
                                ["Principal"] = "s3.amazonaws.com",
                                ["SourceAccount"] = FnRef("AWS::AccountId"),
                                ["SourceArn"] = arn
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
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
                            pragmas: null,
                            deletionPolicy: null
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
                            resource: new Humidifier.CustomResource("AWS::Lambda::EventSourceMapping") {
                                ["BatchSize"] = sqsSource.BatchSize,
                                ["Enabled"] = true,
                                ["EventSourceArn"] = arn,
                                ["FunctionName"] = FnRef(function.FullName)
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
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
                            resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                                ["Action"] = "lambda:InvokeFunction",
                                ["FunctionName"] = FnRef(function.FullName),
                                ["Principal"] = "alexa-appkit.amazon.com",
                                ["EventSourceToken"] = eventSourceToken
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
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
                            resource: new Humidifier.CustomResource("AWS::Lambda::EventSourceMapping") {
                                ["BatchSize"] = dynamoDbSource.BatchSize,
                                ["StartingPosition"] = dynamoDbSource.StartingPosition,
                                ["Enabled"] = true,
                                ["EventSourceArn"] = arn,
                                ["FunctionName"] = FnRef(function.FullName)
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
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
                            resource: new Humidifier.CustomResource("AWS::Lambda::EventSourceMapping") {
                                ["BatchSize"] = kinesisSource.BatchSize,
                                ["StartingPosition"] = kinesisSource.StartingPosition,
                                ["Enabled"] = true,
                                ["EventSourceArn"] = arn,
                                ["FunctionName"] = FnRef(function.FullName)
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                    });
                    break;
                case WebSocketSource webSocketSource:
                    _webSocketRoutes.Add((Function: function, Source: webSocketSource));
                    break;
                case CloudWatchEventSource cloudWatchRuleSource: {

                        // NOTE (2019-01-30, bjorg): we need the source suffix to support multiple sources
                        //  per function; however, we cannot exceed 64 characters in length for the ID.
                        var id = function.LogicalId;
                        if(id.Length > 61) {
                            id += id.Substring(0, 61) + "-" + sourceSuffix;
                        } else {
                            id += "-" + sourceSuffix;
                        }
                        var rule = _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Event",
                            description: null,
                            scope: null,
                            resource: new Humidifier.CustomResource("AWS::Events::Rule") {
                                ["EventPattern"] = cloudWatchRuleSource.Pattern,
                                ["EventBusName"] = cloudWatchRuleSource.EventBus,
                                ["Targets"] = new[] {
                                    new Humidifier.Events.RuleTypes.Target {
                                        Id = id,
                                        Arn = FnGetAtt(function.FullName, "Arn")
                                    }
                                }.ToList()
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                        _builder.AddResource(
                            parent: function,
                            name: $"Source{sourceSuffix}Permission",
                            description: null,
                            scope: null,
                            resource: new Humidifier.CustomResource("AWS::Lambda::Permission") {
                                ["Action"] = "lambda:InvokeFunction",
                                ["FunctionName"] = FnRef(function.FullName),
                                ["Principal"] = "events.amazonaws.com",
                                ["SourceArn"] = FnGetAtt(rule.FullName, "Arn")
                            },
                            resourceExportAttribute: null,
                            dependsOn: null,
                            condition: function.Condition,
                            pragmas: null,
                            deletionPolicy: null
                        );
                    }
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