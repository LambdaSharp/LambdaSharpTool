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
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LambdaSharp.ConfigSource;
using LambdaSharp.Internal;

namespace LambdaSharp {

    public abstract class ALambdaRestApiFunction : ALambdaFunction<APIGatewayProxyRequest, APIGatewayProxyResponse> {

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
            public List<APIGatewayDispatchMapping> Mappings = new List<APIGatewayDispatchMapping>();
        }

        private class APIGatewayDispatchMapping {

            //--- Properties ---
            public string Signature;
            public string Method;
        }

        //--- Fields ---
        private readonly APIGatewayDispatchTable _dispatchTable;
        private APIGatewayProxyRequest _currentRequest;

        //--- Constructors ---
        protected ALambdaRestApiFunction() : this(LambdaFunctionConfiguration.Instance) { }

        protected ALambdaRestApiFunction(LambdaFunctionConfiguration configuration) : base(configuration) {
            _dispatchTable = new APIGatewayDispatchTable(GetType());

            // TODO (2019-03-09, bjorg): initialize methods dispatch information
        }

        //--- Properties ---
        protected APIGatewayProxyRequest CurrentRequest => _currentRequest;

        //--- Methods ---
        protected override async Task InitializeAsync(ILambdaConfigSource envSource, ILambdaContext context) {
            await base.InitializeAsync(envSource, context);

            // read optional api-gateway-mappings file
            if(File.Exists("api-gateway-mappings.json")) {
                var mappings = DeserializeJson<APIGatewayDispatchMappings>(File.ReadAllText("api-gateway-mappings.json"));
                foreach(var mapping in mappings.Mappings) {
                    LogInfo($"Mapping {mapping.Signature} to {mapping.Method}");
                    _dispatchTable.Add(mapping.Signature, mapping.Method);
                }
            }
        }

        public override async Task<APIGatewayProxyResponse> ProcessMessageAsync(APIGatewayProxyRequest request, ILambdaContext context) {
            _currentRequest = request;
            try {
                var signature = $"{request.HttpMethod}:{request.Resource}";
                APIGatewayProxyResponse response;

                // determine if resource signature has an associated dispatcher
                APIGatewayDispatchTable.Dispatcher dispatcher;
                if(
                    !_dispatchTable.TryGetDispatcher(signature, out dispatcher)
                    && !_dispatchTable.TryGetDispatcher($"ANY:{request.Resource}", out dispatcher)
                ) {
                    response = CreateRouteNotFoundResponse(request);
                    LogInfo($"route {signature} not found");
                } else {

                    // invoke dispatcher
                    try {
                        LogInfo($"dispatching route {signature}");
                        try {
                            response = await dispatcher(this, request);
                        } catch(TargetInvocationException e) {

                            // rethrow inner exception caused by reflection invocation
                            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                            throw new Exception("should never happen");
                        }
                        LogInfo($"finished with status code {response.StatusCode}");
                    } catch(ALambdaRestApiAbortException e) {
                        response = e.Response;
                        LogInfo($"aborted with status code {response.StatusCode}");
                    } catch(Exception e) {
                        LogError(e, $"route {signature} threw {e.GetType()}");
                        response = CreateExceptionResponse(request, e);
                    }
                }
                return response;
            } finally {
                _currentRequest = null;
            }
        }

        protected virtual APIGatewayProxyResponse CreateExceptionResponse(APIGatewayProxyRequest request, Exception exception)
            => CreateAbortResponse(500, "Internal Error (see logs for details)");

        protected virtual APIGatewayProxyResponse CreateRouteNotFoundResponse(APIGatewayProxyRequest request)
            => CreateAbortResponse(404, $"Route {request.HttpMethod}:{request.Resource} not found");

        protected virtual Exception Abort(APIGatewayProxyResponse response)
            => throw new ALambdaRestApiAbortException(response);

        protected virtual Exception AbortBadRequest(string message)
            => Abort(CreateAbortResponse(400, message));

        protected virtual Exception AbortForbidden(string message)
            => Abort(CreateAbortResponse(403, message));

        protected virtual Exception AbortNotFound(string message)
            => Abort(CreateAbortResponse(404, message));

        protected virtual APIGatewayProxyResponse CreateAbortResponse(int statusCode, string message)
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
