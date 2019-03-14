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
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace LambdaSharp.Internal {

    public class APIGatewayDispatchBadParameterException : Exception {

        //--- Fields ---
        public readonly string ParameterName;

        //--- Constructors ---
        public APIGatewayDispatchBadParameterException(string message, string parameterName) : base(message) {
            ParameterName = parameterName;
        }
    }

    public class APIGatewayDispatchTable {

        //--- Types ---
        public delegate Task<APIGatewayProxyResponse> Dispatcher(object target, APIGatewayProxyRequest request);

        //--- Class Methods ---
        private static string SerializeJson(object value) => JsonConvert.SerializeObject(value);

        // TODO (2019-03-09, bjorg): we should use the Lambda serializer, but there is no-non generic version available
        private static object DeserializeJson(string json, Type type) => JsonConvert.DeserializeObject(json, type);

        private static Func<APIGatewayProxyRequest, object> CreateParameterResolver(ParameterInfo parameter) {
            Func<APIGatewayProxyRequest, object> resolver;

            if(parameter.ParameterType == typeof(APIGatewayProxyRequest)) {

                // parameter is the proxy request itself
                resolver = request => request;
            } else if(parameter.Name == "request") {

                // parameter represents the body of the request
                resolver = request => {
                    try {
                        return DeserializeJson(request.Body, parameter.ParameterType);
                    } catch {
                        throw new APIGatewayDispatchBadParameterException("invalid JSON document", "request");
                    }
                };
            } else {

                // create function for getting default parameter value
                Func<object> getDefaultValue;
                if(parameter.IsOptional) {
                    getDefaultValue = () => parameter.DefaultValue;
                } else if(parameter.ParameterType.IsValueType) {

                    // TODO (2019-03-13, bjorg): or we could throw an exception when missing
                    getDefaultValue = () => Activator.CreateInstance(parameter.ParameterType);
                } else {
                    getDefaultValue = () => null;
                }

                // create function to resolve parameter
                resolver = request => {
                    string value = null;

                    // attempt to resolve the parameter from stage variables, path parameters, and query string parameters
                    var success = (request.StageVariables?.TryGetValue(parameter.Name, out value) ?? false)
                        || (request.PathParameters?.TryGetValue(parameter.Name, out value) ?? false)
                        || (request.QueryStringParameters?.TryGetValue(parameter.Name, out value) ?? false);

                    // if resolved, return the converted value; otherwise the default value
                    if(success) {
                        try {
                            return Convert.ChangeType(value, Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType);
                        } catch(FormatException) {
                            throw new APIGatewayDispatchBadParameterException("invalid parameter format", parameter.Name);
                        }
                    }
                    return getDefaultValue();
                };
            }
            return resolver;
        }

        private static Dispatcher CreateMethodDelegate(MethodInfo method) {

            // create function to resolve all method parameters
            var resolvers = method.GetParameters().Select(CreateParameterResolver).ToArray();

            // create adapter to invoke custom method
            Dispatcher methodAdapter;
            if(method.ReturnType == typeof(Task<APIGatewayProxyResponse>)) {
                methodAdapter = async (object target, APIGatewayProxyRequest request) => {
                    try {
                        return await (Task<APIGatewayProxyResponse>)method.Invoke(target, resolvers.Select(resolver => resolver(request)).ToArray());
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType == typeof(APIGatewayProxyResponse)) {
                methodAdapter = async (object target, APIGatewayProxyRequest request) => {
                    try {
                        return (APIGatewayProxyResponse)method.Invoke(target, resolvers.Select(resolver => resolver(request)).ToArray());
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) {
                var resolveReturnValue = method.ReturnType.GetProperty("Result");
                methodAdapter = async (object target, APIGatewayProxyRequest request) => {
                    try {
                        var task = (Task)method.Invoke(target, resolvers.Select(resolver => resolver(request)).ToArray());
                        await task;
                        var result = resolveReturnValue.GetValue(task);
                        return new APIGatewayProxyResponse {
                            StatusCode = 200,
                            Body = SerializeJson(result),
                            Headers = new Dictionary<string, string> {
                                ["ContentType"] = "application/json"
                            }
                        };
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType == typeof(Task)) {
                methodAdapter = async (object target, APIGatewayProxyRequest request) => {
                    try {
                        var task = (Task)method.Invoke(target, resolvers.Select(resolver => resolver(request)).ToArray());
                        await task;
                        return new APIGatewayProxyResponse {
                            StatusCode = 200
                        };
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType == typeof(void)) {
                methodAdapter = async (object target, APIGatewayProxyRequest request) => {
                    try {
                        method.Invoke(target, resolvers.Select(resolver => resolver(request)).ToArray());
                        return new APIGatewayProxyResponse {
                            StatusCode = 200
                        };
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else {
                methodAdapter = async (object target, APIGatewayProxyRequest request) => {
                    try {
                        var result = method.Invoke(target, resolvers.Select(resolver => resolver(request)).ToArray());
                        return new APIGatewayProxyResponse {
                            StatusCode = 200,
                            Body = SerializeJson(result),
                            Headers = new Dictionary<string, string> {
                                ["ContentType"] = "application/json"
                            }
                        };
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw new Exception("should never happen");
                    }
                };
            }
            return methodAdapter;
        }

        //--- Fields ---
        private readonly Type _targetType;
        private readonly Dictionary<string, Dispatcher> _mappings = new Dictionary<string, Dispatcher>();

        //--- Constructors ---
        public APIGatewayDispatchTable(Type targetType) {
            _targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        //--- Methods ---
        public void Add(string key, string methodName) => _mappings.Add(key, CreateMethodDelegate(_targetType.GetMethod(methodName)));
        public bool TryGetDispatcher(string key, out Dispatcher dispatcher) => _mappings.TryGetValue(key, out dispatcher);
    }
}
