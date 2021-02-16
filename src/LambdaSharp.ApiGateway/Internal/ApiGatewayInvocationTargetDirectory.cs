/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using LambdaSharp.Exceptions;
using LambdaSharp.Serialization;

namespace LambdaSharp.ApiGateway.Internal {

    internal class ApiGatewayInvocationTargetDirectory {

        //--- Types ---
        public delegate Task<APIGatewayProxyResponse> InvocationTargetDelegate(APIGatewayProxyRequest request);
        public delegate object CreateTargetInstanceDelegate(Type type);

        internal class ReturnNullException : Exception {

            //--- Constructors ---
            public ReturnNullException(MethodInfo method) { }
        }

        //--- Class Methods ---
        internal class InvocationTargetState {

            //--- Properties ---
            public string? PathQueryParametersJson { get; set; }
        }

        private static Func<APIGatewayProxyRequest, InvocationTargetState, object?> CreateParameterResolver(ILambdaJsonSerializer serializer, MethodInfo method, ParameterInfo parameter) {

            // check if [FromUri] or [FromBody] attributes are present
            var hasFromUriAttribute = parameter.GetCustomAttribute<FromUriAttribute>() != null;
            var hasFromBodyAttribute = parameter.GetCustomAttribute<FromBodyAttribute>() != null;
            if(hasFromUriAttribute && hasFromBodyAttribute) {
                throw new ApiGatewayInvocationTargetParameterException(method, parameter, "cannot have both [FromUri] and [FromBody] attributes");
            }

            // check if parameter is a proxy request
            var isProxyRequest = parameter.ParameterType == typeof(APIGatewayProxyRequest);
            if(isProxyRequest) {

                // parameter is the proxy request itself
                return (request, state) => request;
            }

            // check if parameter needs to deserialized from URI or BODY
            var isSimpleType = parameter.ParameterType.IsValueType || (parameter.ParameterType == typeof(string));
            if((isSimpleType && !hasFromBodyAttribute) || hasFromUriAttribute) {

                // check if parameter is read from URI string directly or if its members are read from the URI string
                if(isSimpleType) {

                    // create function for getting default parameter value
                    Func<object?> getDefaultValue;
                    if(parameter.IsOptional) {
                        getDefaultValue = () => parameter.DefaultValue;
                    } else if((Nullable.GetUnderlyingType(parameter.ParameterType) == null) && (parameter.ParameterType.IsValueType || parameter.ParameterType == typeof(string))) {
                        getDefaultValue = () => throw new ApiGatewayInvocationTargetParameterException(method, parameter, "missing value");
                    } else {
                        getDefaultValue = () => null;
                    }

                    // create function to resolve parameter
                    return (request, state) => {
                        string? value = null;

                        // attempt to resolve the parameter from stage variables, path parameters, and query string parameters
                        var success = (request.PathParameters?.TryGetValue(parameter.Name, out value) ?? false)
                            || (request.QueryStringParameters?.TryGetValue(parameter.Name, out value) ?? false);

                        // if resolved, return the converted value; otherwise the default value
                        if(success) {
                            try {
                                return Convert.ChangeType(value, parameter.ParameterType);
                            } catch(FormatException) {
                                throw new ApiGatewayInvocationTargetParameterException(method, parameter, "invalid parameter format");
                            }
                        }
                        return getDefaultValue();
                    };
                } else {
                    var queryParameterType = parameter.ParameterType;

                    // parameter represents the query-string key-value pairs
                    return (request, state) => {
                        try {

                            // construct a unified JSON representation of the path and query-string parameters
                            if(state.PathQueryParametersJson == null) {
                                var pathParameters = request.PathParameters ?? Enumerable.Empty<KeyValuePair<string, string>>();
                                var queryStringParameters = request.QueryStringParameters ?? Enumerable.Empty<KeyValuePair<string, string>>();
                                state.PathQueryParametersJson = serializer.Serialize(
                                    pathParameters.Union(queryStringParameters)
                                        .GroupBy(kv => kv.Key)
                                        .Select(grouping => grouping.First())
                                        .ToDictionary(kv => kv.Key, kv => kv.Value)
                                );
                            }
                            return serializer.Deserialize(state.PathQueryParametersJson, parameter.ParameterType);
                        } catch {
                            throw new ApiGatewayInvocationTargetParameterException(method, parameter, "invalid path/query-string parameters format");
                        }
                    };
                }
            } else {

                // parameter represents the body of the request
                return (request, state) => {
                    try {
                        return serializer.Deserialize(request.Body, parameter.ParameterType);
                    } catch {
                        throw new ApiGatewayInvocationTargetParameterException(method, parameter, "invalid JSON document in request body");
                    }
                };
            }
        }

        //--- Fields ---
        private readonly CreateTargetInstanceDelegate _createInstance;
        private readonly Dictionary<string, (InvocationTargetDelegate Delegate, bool IsAsync)> _mappings = new Dictionary<string, (InvocationTargetDelegate, bool)>();
        private readonly Dictionary<Type, object> _targets = new Dictionary<Type, object>();
        private readonly ILambdaJsonSerializer _serializer;

        //--- Constructors ---
        public ApiGatewayInvocationTargetDirectory(CreateTargetInstanceDelegate createInstance, ILambdaJsonSerializer serializer) {
            _createInstance = createInstance ?? throw new ArgumentNullException(nameof(createInstance));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        //--- Methods ---
        public void Add(string key, string methodReference) {

            // validate parameters
            var methodReferenceParts = methodReference.Split(new[] { "::" }, 3, StringSplitOptions.None);
            if(methodReferenceParts.Length != 3) {
                throw new ArgumentException("invalid method reference", nameof(methodReference));
            }
            var assemblyName = methodReferenceParts[0];
            var typeName = methodReferenceParts[1];
            var methodName = methodReferenceParts[2];

            // check if an invocation target already exists; otherwise create it
            var type = Assembly.Load(assemblyName).GetType(typeName);
            if(type == null) {
                throw new ArgumentException($"could not find type '{typeName}'", nameof(methodReference));
            }
            if(!_targets.TryGetValue(type, out var target)) {
                target = _createInstance(type);
                _targets[type] = target;
            }

            // find method
            var method = target.GetType().GetMethod(methodName);
            if(method == null) {
                throw new ArgumentException($"could not find method '{methodName}' in type '{typeName}'", nameof(methodReference));
            }

            // add invocation delegate
            var methodDelegate = CreateMethodDelegate(target, method, out var isAsync);

            // NOTE (2020-10-09, bjorg): WebSocket routes starting with '$' are never treated as async
            _mappings.Add(key, (methodDelegate, isAsync && !key.StartsWith("$", StringComparison.Ordinal)));
        }

        public bool TryGetInvocationTarget(string key, out InvocationTargetDelegate? invocationTarget, out bool isAsync) {
            if(_mappings.TryGetValue(key, out var tuple)) {
                (invocationTarget, isAsync) = tuple;
                return true;
            } else {
                (invocationTarget, isAsync) = (null, false);
                return false;
            }
        }

        private InvocationTargetDelegate CreateMethodDelegate(object target, MethodInfo method, out bool isAsync) {

            // create resolver function for each method parameter
            var resolvers = method.GetParameters().Select(parameter => CreateParameterResolver(_serializer, method, parameter)).ToArray();

            // create method adapter based on method return type
            InvocationTargetDelegate methodAdapter;
            if(method.ReturnType == typeof(Task<APIGatewayProxyResponse>)) {
                isAsync = false;
                methodAdapter = async (APIGatewayProxyRequest request) => {
                    try {
                        var state = new InvocationTargetState();
                        return await (Task<APIGatewayProxyResponse>)(method.Invoke(target, resolvers.Select(resolver => resolver(request, state)).ToArray()) ?? throw new ReturnNullException(method));
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType == typeof(APIGatewayProxyResponse)) {
                isAsync = false;
                methodAdapter = async (APIGatewayProxyRequest request) => {
                    try {
                        var state = new InvocationTargetState();
                        return (APIGatewayProxyResponse)(method.Invoke(target, resolvers.Select(resolver => resolver(request, state)).ToArray()) ?? throw new ReturnNullException(method));
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) {
                isAsync = false;
                var resolveReturnValue = method.ReturnType.GetProperty("Result") ?? throw new ShouldNeverHappenException("could not fetch 'Result' property of Task<> type");
                methodAdapter = async (APIGatewayProxyRequest request) => {
                    try {
                        var state = new InvocationTargetState();
                        var task = (Task)(method.Invoke(target, resolvers.Select(resolver => resolver(request, state)).ToArray()) ?? throw new ReturnNullException(method));
                        await task;
                        var result = resolveReturnValue.GetValue(task);
                        return (result != null)
                            ? new APIGatewayProxyResponse {
                                StatusCode = 200,
                                Body = _serializer.Serialize<object>(result),
                                Headers = new Dictionary<string, string> {
                                    ["ContentType"] = "application/json"
                                }
                            }
                            : new APIGatewayProxyResponse {
                                StatusCode = 200
                            };
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else if(method.ReturnType == typeof(Task)) {
                isAsync = true;
                methodAdapter = async (APIGatewayProxyRequest request) => {
                    try {
                        var state = new InvocationTargetState();
                        var task = (Task)(method.Invoke(target, resolvers.Select(resolver => resolver(request, state)).ToArray()) ?? throw new ReturnNullException(method));
                        await task;
                        return new APIGatewayProxyResponse {
                            StatusCode = 200
                        };
                    } catch(TargetInvocationException e) {

                        // rethrow target exception as an asynchronous endpoint exception
                        throw new ApiGatewayAsyncEndpointException(e.InnerException ?? e);
                    }
                };
            } else if(method.ReturnType == typeof(void)) {
                isAsync = true;
                methodAdapter = async (APIGatewayProxyRequest request) => {
                    try {
                        var state = new InvocationTargetState();
                        method.Invoke(target, resolvers.Select(resolver => resolver(request, state)).ToArray());
                        return new APIGatewayProxyResponse {
                            StatusCode = 200
                        };
                    } catch(TargetInvocationException e) {

                        // rethrow target exception as an asynchronous endpoint exception
                        throw new ApiGatewayAsyncEndpointException(e.InnerException ?? e);
                    }
                };
            } else if(!method.ReturnType.IsValueType && (method.ReturnType != typeof(string))) {
                isAsync = false;
                methodAdapter = async (APIGatewayProxyRequest request) => {
                    try {
                        var state = new InvocationTargetState();
                        var result = method.Invoke(target, resolvers.Select(resolver => resolver(request, state)).ToArray());
                        return (result != null)
                            ? new APIGatewayProxyResponse {
                                StatusCode = 200,
                                Body = _serializer.Serialize<object>(result),
                                Headers = new Dictionary<string, string> {
                                    ["ContentType"] = "application/json"
                                }
                            }
                            : new APIGatewayProxyResponse {
                                StatusCode = 200
                            };
                    } catch(TargetInvocationException e) {

                        // rethrow inner exception caused by reflection invocation
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                        throw new Exception("should never happen");
                    }
                };
            } else {
                throw new ApiGatewayInvocationTargetReturnException(method, $"unsupported type '{method.ReturnType.FullName}'");
            }
            return methodAdapter;
        }
    }
}
