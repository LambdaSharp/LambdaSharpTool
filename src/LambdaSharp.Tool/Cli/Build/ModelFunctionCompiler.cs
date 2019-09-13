/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
                    reference: FnSub($"arn:aws:s3:::${{DeploymentBucketName}}/{ModuleInfo.MODULE_ORIGIN_PLACEHOLDER}/${{Module::Namespace}}/${{Module::Name}}/.artifacts/*"),
                    allow: "s3:GetObject",
                    condition: null
                );

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
                value: FnSub($"https://${{{restDomainName.FullName}}}/LATEST"),
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
                            ValidateRequestBody = true,
                            ValidateRequestParameters = true
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: null,
                        pragmas: null
                    ).DiscardIfNotReachable = true;
            }

            // create log-group for REST API
            var restLogGroup = _builder.AddResource(
                parent: restApi,
                name: "LogGroup",
                description: null,
                scope: null,
                resource: new Humidifier.Logs.LogGroup {
                    LogGroupName = FnSub($"API-Gateway-Execution-Logs_${{{restApi.FullName}}}/LATEST"),
                    RetentionInDays = FnRef("Module::LogRetentionInDays")
                },
                resourceExportAttribute: null,
                dependsOn: null,
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
                    }.ToList(),
                    TracingEnabled = FnIf("XRayIsEnabled", true, false)
                },
                resourceExportAttribute: null,
                dependsOn: new[] { restLogGroup.FullName },
                condition: null,
                pragmas: null
            );
        }

        private void AddWebSocketResources(IEnumerable<FunctionItem> functions) {
            var moduleItem = _builder.GetItem("Module");

            // give permission to the Lambda functions to communicate back over the WebSocket
            _builder.AddGrant(
                name: "WebSocketConnections",
                awsType: null,
                reference: FnSub("arn:aws:execute-api:${AWS::Region}:${AWS::AccountId}:${Module::WebSocket}/LATEST/POST/@connections/*"),
                allow: new[] {
                    "execute-api:ManageConnections"
                },
                condition: null
            );

            // read WebSocket configuration
            if(!_builder.TryGetOverride("Module::WebSocket.RouteSelectionExpression", out var routeSelectionExpression)) {
                routeSelectionExpression = "$request.body.action";
            }

            // create a WebSocket API
            var webSocket = _builder.AddResource(
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
            var webSocketResources = new Dictionary<string, object>();
            foreach(var webSocketRouteByFunction in _webSocketRoutes.GroupBy(route => route.Function.FullName)) {
                var function = webSocketRouteByFunction.First().Function;

                // add integration resource (only need one per end-point function)
                var integrationResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::Integration") {
                    ["ApiId"] = FnRef(webSocket.FullName),
                    ["Description"] = $"WebSocket Integration for '{function.FullName}'",
                    ["IntegrationType"] = "AWS_PROXY",
                    ["IntegrationUri"] = FnSub($"arn:aws:apigateway:${{AWS::Region}}:lambda:path/2015-03-31/functions/${{{function.FullName}.Arn}}/invocations"),
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
                    pragmas: null
                );
                webSocketResources.Add(integration.FullName, integrationResource);

                // create resources per route
                foreach(var webSocketRoute in webSocketRouteByFunction.OrderBy(route => route.Source.RouteKey)) {

                    // remove special character from path segment and capitalize it
                    var routeName = webSocketRoute.Source.RouteKey.ToPascalIdentifier();

                    // add route resource
                    var routeResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::Route") {
                        ["ApiId"] = FnRef(webSocket.FullName),
                        ["RouteKey"] = webSocketRoute.Source.RouteKey,
                        ["AuthorizationType"] = "NONE",
                        ["OperationName"] = webSocketRoute.Source.OperationName,
                        ["Target"] = FnSub($"integrations/${{{integration.FullName}}}")
                    };
                    var route = _builder.AddResource(
                        parent: webSocketRoute.Function,
                        name: routeName + "Route",
                        description: $"WebSocket Route for '{webSocketRoute.Source.RouteKey}'",
                        scope: null,
                        resource: routeResource,
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: webSocketRoute.Function.Condition,
                        pragmas: null
                    );
                    webSocketResources.Add(route.FullName, routeResource);

                    // add optional request/response models
                    if(!webSocketRoute.Source.RouteKey.StartsWith("$", StringComparison.Ordinal)) {

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
                                    ["ApiId"] = FnRef(webSocket.FullName),
                                    ["ContentType"] = webSocketRoute.Source.RequestContentType,
                                    ["Name"] = $"{webSocketRoute.Source.OperationName}Request",
                                    ["Schema"] = webSocketRoute.Source.RequestSchema
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: function.Condition,
                                pragmas: null
                            );
                            webSocketResources.Add(model.FullName, webSocketRoute.Source.RequestSchema);

                            // update the route to require request validation
                            routeResource["ModelSelectionExpression"] = "default";
                            routeResource["RequestModels"] = new Dictionary<string, object> {
                                ["default"] = $"{webSocketRoute.Source.OperationName}Request"
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
                                ["ApiId"] = FnRef(webSocket.FullName),
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
                                pragmas: null
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
                                    ["ApiId"] = FnRef(webSocket.FullName),
                                    ["ContentType"] = webSocketRoute.Source.ResponseContentType,
                                    ["Name"] = $"{webSocketRoute.Source.OperationName}Response",
                                    ["Schema"] = webSocketRoute.Source.ResponseSchema
                                },
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: function.Condition,
                                pragmas: null
                            );
                            webSocketResources.Add(model.FullName, webSocketRoute.Source.ResponseSchema);

                            // add route response resource
                            routeResponseResource = new Humidifier.CustomResource("AWS::ApiGatewayV2::RouteResponse") {
                                ["ApiId"] = FnRef(webSocket.FullName),
                                ["RouteId"] = FnRef(route.FullName),
                                ["RouteResponseKey"] = "$default",
                                ["ModelSelectionExpression"] = "default",
                                ["ResponseModels"] = new Dictionary<string, object> {
                                    ["default"] = $"{webSocketRoute.Source.OperationName}Response"
                                }
                            };
                            routeResponse = _builder.AddResource(
                                parent: webSocketRoute.Function,
                                name: routeName + "RouteResponse",
                                description: $"WebSocket Route Response for '{webSocketRoute.Source.RouteKey}'",
                                scope: null,
                                resource: routeResponseResource,
                                resourceExportAttribute: null,
                                dependsOn: null,
                                condition: webSocketRoute.Function.Condition,
                                pragmas: null
                            );
                            webSocketResources.Add(routeResponse.FullName, routeResponseResource);

                            // update the route to require route response
                            routeResource["RouteResponseSelectionExpression"] = "$default";
                            break;
                        default:
                            throw new ApplicationException($"unrecognized response schema type: {webSocketRoute.Source.ResponseSchema} [{webSocketRoute.Source.ResponseSchema.GetType()}]");
                        }
                    }

                    // add lambda invocation permission resource
                    _builder.AddResource(
                        parent: webSocketRoute.Function,
                        name: routeName + "Permission",
                        description: $"WebSocket invocation permission for '{webSocketRoute.Source.RouteKey}'",
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
            }

            // WebSocket deployment depends on all methods and their hash (to force redeployment in case of change)
            var resourcesSignature = string.Join("\n", webSocketResources
                .OrderBy(kv => kv.Key)
                .Select(kv => JsonConvert.SerializeObject(kv.Value))
            );
            string methodsHash = resourcesSignature.ToMD5Hash();

            // add WebSocket url
            var webSocketDomainName = _builder.AddVariable(
                parent: webSocket,
                name: "DomainName",
                description: "Module WebSocket Domain Name",
                type: "String",
                scope: null,
                value: FnSub($"${{{webSocket.FullName}}}.execute-api.${{AWS::Region}}.${{AWS::URLSuffix}}"),
                allow: null,
                encryptionContext: null
            );
            _builder.AddVariable(
                parent: webSocket,
                name: "Url",
                description: "Module WebSocket URL",
                type: "String",
                scope: new List<string> { "all" },
                value: FnSub($"wss://${{{webSocketDomainName.FullName}}}/LATEST"),
                allow: null,
                encryptionContext: null
            );

            // NOTE (2018-06-21, bjorg): the WebSocket deployment depends on ALL route resources having been created;
            //  a new name is used for the deployment to force the stage to be updated
            var deploymentWithHash = _builder.AddResource(
                parent: webSocket,
                name: "Deployment" + methodsHash,
                description: "Module WebSocket Deployment",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Deployment") {
                    ["ApiId"] = FnRef("Module::WebSocket"),
                    ["Description"] = FnSub($"${{AWS::StackName}} WebSocket [{methodsHash}]")
                },
                resourceExportAttribute: null,
                dependsOn: webSocketResources.Select(kv => kv.Key).OrderBy(key => key).ToArray(),
                condition: null,
                pragmas: null
            );
            var deployment = _builder.AddVariable(
                parent: webSocket,
                name: "Deployment",
                description: "Module WebSocket Deployment",
                type: "String",
                scope: null,
                value: FnRef(deploymentWithHash.FullName),
                allow: null,
                encryptionContext: null
            );

            // create log-group for WebSocket
            var webSocketLogGroup = _builder.AddResource(
                parent: webSocket,
                name: "LogGroup",
                description: null,
                scope: null,
                resource: new Humidifier.Logs.LogGroup {
                    RetentionInDays = FnRef("Module::LogRetentionInDays")
                },
                resourceExportAttribute: null,
                dependsOn: null,
                condition: null,
                pragmas: null
            );

            // WebSocket stage depends on deployment
            _builder.AddResource(
                parent: webSocket,
                name: "Stage",
                description: "Module WebSocket Stage",
                scope: null,
                resource: new Humidifier.CustomResource("AWS::ApiGatewayV2::Stage") {
                    ["AccessLogSettings"] = new Dictionary<string, dynamic> {
                        ["DestinationArn"] = FnSub($"arn:aws:logs:${{AWS::Region}}:${{AWS::AccountId}}:log-group:${{{webSocketLogGroup.FullName}}}"),
                        ["Format"] = JsonConvert.SerializeObject(JObject.Parse(GetType().Assembly.ReadManifestResource("LambdaSharp.Tool.Resources.WebSocketLogging.json")), Formatting.None)
                    },
                    ["ApiId"] = FnRef("Module::WebSocket"),
                    ["StageName"] = "LATEST",
                    ["Description"] = "Module WebSocket LATEST Stage",
                    ["DeploymentId"] = FnRef(deployment.FullName)
                },
                resourceExportAttribute: null,
                dependsOn: new[] { webSocketLogGroup.FullName },
                condition: null,
                pragmas: null
            );
        }

        private void AddRestApiResource(AModuleItem parent, object restApiId, object parentId, int level, IEnumerable<(FunctionItem Function, RestApiSource Source)> routes, Dictionary<string, object> apiMethodDeclarations) {

            // create methods at this route level to parent id
            foreach(var route in routes.Where(route => route.Source.Path.Length == level)) {
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
                    pragmas: null
                );
                apiMethodDeclarations.Add(method.FullName, apiMethodResource);
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
                        resource: new Humidifier.ApiGateway.Model {
                            ContentType = route.Source.RequestContentType,
                            Name = $"{route.Source.OperationName}Request",
                            RestApiId = restApiId,
                            Schema = route.Source.RequestSchema
                        },
                        resourceExportAttribute: null,
                        dependsOn: null,
                        condition: route.Function.Condition,
                        pragmas: null
                    );
                    apiMethodDeclarations.Add(model.FullName, route.Source.RequestSchema);

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
                    integration.RequestTemplates = new Dictionary<string, dynamic> {
                        ["application/json"] = LambdaRestApiRequestTemplate
                    };
                    integration.Type = "AWS";
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
                        resource: new Humidifier.ApiGateway.Model {
                            ContentType = route.Source.ResponseContentType,
                            Name = $"{route.Source.OperationName}Response",
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
                    if(route.Source.ResponseContentType != null) {
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
                    _builder.AddDependencyAsync(new ModuleInfo("LambdaSharp", "S3.Subscriber", Settings.CoreServicesVersion, "lambdasharp"), ModuleManifestDependencyType.Shared).Wait();
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