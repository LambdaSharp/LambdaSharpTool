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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LambdaSharp.ConfigSource;
using LambdaSharp.Internal;

namespace LambdaSharp {

    public abstract class ALambdaApiGatewayFunction : ALambdaFunction<APIGatewayProxyRequest, APIGatewayProxyResponse> {

        //--- Types ---
        private class ALambdaRestApiAbortException : Exception {

            //--- Fields ---
            public readonly APIGatewayProxyResponse Response;

            //--- Constructors ---
            public ALambdaRestApiAbortException(APIGatewayProxyResponse response) : base("Abort message request") {
                Response = response ?? throw new ArgumentNullException(nameof(response));
            }
        }

        private class APIGatewayDispatchMappings {

            //--- Properties ---
            public List<APIGatewayDispatchMapping> Mappings { get; set; }
        }

        private class APIGatewayDispatchMapping {

            //--- Properties ---
            public string RestApi;
            public string WebSocket;
            public string Method { get; set; }
        }

        //--- Fields ---
        private APIGatewayDispatchTable _dispatchTable;
        private APIGatewayProxyRequest _currentRequest;
        private IAmazonApiGatewayManagementApi _webSocketClient;

        //--- Constructors ---
        protected ALambdaApiGatewayFunction() : this(LambdaFunctionConfiguration.Instance) { }

        protected ALambdaApiGatewayFunction(LambdaFunctionConfiguration configuration) : base(configuration) { }

        //--- Properties ---
        protected APIGatewayProxyRequest CurrentRequest => _currentRequest;

        //--- Methods ---
        protected override async Task InitializeAsync(ILambdaConfigSource envSource, ILambdaContext context) {
            await base.InitializeAsync(envSource, context);

            // read optional api-gateway-mappings file
            _dispatchTable = new APIGatewayDispatchTable(GetType());
            if(File.Exists("api-mappings.json")) {
                var mappings = DeserializeJson<APIGatewayDispatchMappings>(File.ReadAllText("api-mappings.json"));
                foreach(var mapping in mappings.Mappings) {
                    if(mapping.RestApi != null) {
                        LogInfo($"Mapping REST API '{mapping.RestApi}' to {mapping.Method}");
                        _dispatchTable.Add(mapping.RestApi, mapping.Method);
                    } else if(mapping.WebSocket != null) {
                        LogInfo($"Mapping WebSocket route '{mapping.WebSocket}' to {mapping.Method}");
                        _dispatchTable.Add(mapping.WebSocket, mapping.Method);
                    } else {
                        throw new InvalidDataException("");
                    }
                }
            }

            // initialize WebSocket client if environment variable is set for it
            var webSocketUrl = envSource.Read("WEBSOCKET_URL");
            if(webSocketUrl != null) {
                _webSocketClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                    ServiceURL = webSocketUrl
                });
            }
        }

        public override async Task<APIGatewayProxyResponse> ProcessMessageAsync(APIGatewayProxyRequest request, ILambdaContext context) {
            _currentRequest = request;

// TODO: remove
LogInfo($"request:\n{SerializeJson(request)}");

            APIGatewayProxyResponse response;
            var signature = "<null>";
            try {
                APIGatewayDispatchTable.Dispatcher dispatcher;
                var requestContext = request.RequestContext;
                if(requestContext.ResourcePath != null) {

                    // this is a REST API request
                    signature = $"{requestContext.HttpMethod}:{requestContext.ResourcePath}";
                    if(!_dispatchTable.TryGetDispatcher(signature, out dispatcher)) {

                        // check if a generic HTTP method entry exists
                        _dispatchTable.TryGetDispatcher($"ANY:{requestContext.ResourcePath}", out dispatcher);
                    }
                } else if(requestContext.RouteKey != null) {

                    // this is a WebSocket request
                    signature = requestContext.RouteKey;
                    _dispatchTable.TryGetDispatcher(signature, out dispatcher);
                } else {

                    // could not determine the request type
                    signature = "<undetermined>";
                    dispatcher = null;
                }

                // invoke dispatcher or derived handler
                LogInfo($"dispatching route '{signature}'");
                if(dispatcher != null) {
                    response = await dispatcher(this, request);
                } else {
                    response = await HandleRequestAsync(request, context);
                }
                LogInfo($"finished with status code {response.StatusCode}");
            } catch(APIGatewayDispatchBadParameterException e) {
                LogInfo($"bad parameter '{e.ParameterName}': {e.Message}");
                response = CreateBadParameterResponse(request, e.ParameterName, e.Message);
            } catch(ALambdaRestApiAbortException e) {
                LogInfo($"aborted with status code {e.Response.StatusCode}\n{e.Response.Body}");
                response = e.Response;
            } catch(Exception e) {
                LogError(e, $"route '{signature}' threw {e.GetType()}");
                response = CreateInvocationExceptionResponse(request, e);
            } finally {
                _currentRequest = null;
            }
            return response;
        }

        public virtual Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context) {

            // NOTE: the default implementation returns NotFound since the derived method is only called when no signature match was found
            var signature = request.RequestContext.RouteKey ?? $"{request.RequestContext.HttpMethod}:{request.RequestContext.ResourcePath}";
            LogInfo($"route '{signature}' not found");
            return Task.FromResult(CreateRouteNotFoundResponse(request, signature));
        }

        protected Task PostToConnectionAsync(string connectionId, object message)
            => PostToConnectionAsync(connectionId, Encoding.UTF8.GetBytes(SerializeJson(message)));

        protected Task PostToConnectionAsync(string connectionId, byte[] bytes) {
            if(_webSocketClient == null) {
                throw new ApplicationException("WebSocket client is not configured for this function");
            }
            return _webSocketClient.PostToConnectionAsync(new PostToConnectionRequest {
                ConnectionId = connectionId,
                Data = new MemoryStream(bytes)
            });
        }

        protected virtual APIGatewayProxyResponse CreateRouteNotFoundResponse(APIGatewayProxyRequest request, string signature)
            => CreateStatusResponse(404, $"Route '{signature}' not found");

        protected virtual APIGatewayProxyResponse CreateBadParameterResponse(APIGatewayProxyRequest request, string parameterName, string message)
            => (parameterName == "request")
                ? CreateStatusResponse(400, $"Bad request body: {message}")
                : CreateStatusResponse(400, $"Bad request parameter '{parameterName}': {message}");

        protected virtual APIGatewayProxyResponse CreateInvocationExceptionResponse(APIGatewayProxyRequest request, Exception exception)
            => CreateStatusResponse(500, "Internal Error (see logs for details)");

        protected virtual Exception Abort(APIGatewayProxyResponse response)
            => throw new ALambdaRestApiAbortException(response);

        protected virtual Exception AbortBadRequest(string message)
            => Abort(CreateStatusResponse(400, message));

        protected virtual Exception AbortForbidden(string message)
            => Abort(CreateStatusResponse(403, message));

        protected virtual Exception AbortNotFound(string message)
            => Abort(CreateStatusResponse(404, message));

        protected virtual APIGatewayProxyResponse CreateStatusResponse(int statusCode, string message)
            => new APIGatewayProxyResponse {
                StatusCode = statusCode,
                Body = SerializeJson(new {
                    StatusCode = statusCode,
                    Summary = message,
                    RequestId = CurrentRequest.RequestContext.RequestId
                }),
                Headers = new Dictionary<string, string> {
                    ["ContentType"] = "application/json"
                }
            };
    }
}
