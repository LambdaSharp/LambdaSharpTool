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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using LambdaSharp.Build.Internal;
using NJsonSchema;
using NJsonSchema.Generation;

namespace LambdaSharp.Build.CSharp {

    public class LambdaSharpTool : ABuildEventsSource {

        //--- Types ---
        public class AssemblyMetadata {

            //--- Properties ---
            public string? ModuleVersionId { get; set; }
        }

        private class ProcessTargetInvocationException : Exception {

            //--- Constructors ---
            public ProcessTargetInvocationException(string message) : base(message) { }
        }

        //--- Constructors ---
        public LambdaSharpTool(BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) { }

        //--- Methods ---
        public bool RunLashTool(string workingDirectory, IEnumerable<string> arguments, bool showOutput, Func<string, string?>? processOutputLine = null) {

            // check if lambdasharp is installed or if we need to run it using dotnet
            var lambdaSharpFolder = Environment.GetEnvironmentVariable("LAMBDASHARP");
            bool success;
            if(lambdaSharpFolder == null) {

                // check if lash executable exists (it should since we're running)
                var lash = ProcessLauncher.Lash;
                if(string.IsNullOrEmpty(lash)) {
                    LogError("failed to find the \"lash\" executable in path.");
                    return false;
                }
                success = new ProcessLauncher(BuildEventsConfig).Execute(
                    lash,
                    arguments,
                    workingDirectory,
                    showOutput,
                    processOutputLine
                );
            } else {

                // check if dotnet executable exists
                var dotNetExe = ProcessLauncher.DotNetExe;
                if(string.IsNullOrEmpty(dotNetExe)) {
                    LogError("failed to find the \"dotnet\" executable in path.");
                    return false;
                }
                success = new ProcessLauncher(BuildEventsConfig).Execute(
                    dotNetExe,
                    new[] {
                        "run", "-p", $"{lambdaSharpFolder}/src/LambdaSharp.Tool", "--"
                    }.Union(arguments).ToList(),
                    workingDirectory,
                    showOutput,
                    processOutputLine
                );
            }
            return success;
        }

        public async Task CreateInvocationTargetSchemasAsync(
            string directory,
            string rootNamespace,
            IEnumerable<string> methodReferences,
            string outputFile
        ) {
            const string ASYNC_SUFFIX = "Async";
            var schemas = new Dictionary<string, InvocationTargetDefinition>();

            // create a list of nested namespaces from the root namespace
            var namespaces = new List<string>();
            if(!string.IsNullOrEmpty(rootNamespace)) {
                var parts = rootNamespace.Split(".");
                for(var i = 0; i < parts.Length; ++i) {
                    namespaces.Add(string.Join(".", parts.Take(i + 1)) + ".");
                }
            }
            namespaces.Add("");
            namespaces.Reverse();

            // enumerate type methods
            Console.WriteLine($"Inspecting method invocation targets in {directory}");
            foreach(var methodReference in methodReferences.Distinct()) {
                InvocationTargetDefinition? entryPoint = null;
                try {

                    // extract class and method names from method reference
                    if(!StringEx.TryParseAssemblyClassMethodReference(methodReference, out var assemblyName, out var typeName, out var methodName)) {
                        throw new ProcessTargetInvocationException($"method reference '{methodReference}' is not well formed");
                    }

                    // load assembly
                    Assembly assembly;
                    var assemblyFilepath = Path.Combine(directory, assemblyName + ".dll");
                    try {
                        assembly = Assembly.LoadFrom(assemblyFilepath);
                    } catch(FileNotFoundException) {
                        throw new ProcessTargetInvocationException($"could not find assembly '{assemblyFilepath}'");
                    } catch(Exception e) {
                        throw new ProcessTargetInvocationException($"error loading assembly '{assemblyFilepath}': {e.Message}");
                    }

                    // find type in assembly
                    var type = namespaces.Select(ns => assembly.GetType(ns + typeName)).Where(t => t != null).FirstOrDefault();
                    if(type == null) {
                        throw new ProcessTargetInvocationException($"could not find type for '{methodReference}' in assembly '{assembly.FullName}'");
                    }

                    // find method, optionally with 'Async' suffix
                    var method = type.GetMethod(methodName);
                    if((method == null) && !methodName.EndsWith(ASYNC_SUFFIX, StringComparison.Ordinal)) {
                        methodName += ASYNC_SUFFIX;
                        method = type.GetMethod(methodName);
                    }
                    if(method == null) {
                        throw new ProcessTargetInvocationException($"could not find method '{methodName}' in type '{type.FullName}'");
                    }
                    var resolvedMethodReference = $"{assemblyName}::{type.FullName}::{method.Name}";
                    var operationName = methodName.EndsWith(ASYNC_SUFFIX, StringComparison.Ordinal)
                        ? methodName.Substring(0, methodName.Length - ASYNC_SUFFIX.Length)
                        : methodName;

                    // process method parameters
                    ParameterInfo? requestParameter = null;
                    ParameterInfo? proxyRequestParameter = null;
                    var uriParameters = new List<KeyValuePair<string, bool>>();
                    var parameters = method.GetParameters();
                    foreach(var parameter in parameters) {

                        // check if [FromUri] or [FromBody] attributes are present
                        var customAttributes = parameter.GetCustomAttributes(true);
                        var hasFromUriAttribute = customAttributes.Any(attribute => attribute.GetType().FullName == "LambdaSharp.ApiGateway.FromUriAttribute");
                        var hasFromBodyAttribute = customAttributes.Any(attribute => attribute.GetType().FullName == "LambdaSharp.ApiGateway.FromBodyAttribute");
                        if(hasFromUriAttribute && hasFromBodyAttribute) {
                            throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameter '{parameter.Name}' cannot have both [FromUri] and [FromBody] attributes");
                        }

                        // check if parameter is a proxy request
                        var isProxyRequest =
                            (parameter.ParameterType.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest")
                            || (parameter.ParameterType.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest");
                        if(isProxyRequest) {
                            if(hasFromUriAttribute || hasFromBodyAttribute) {
                                throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameter '{parameter.Name}' of type 'APIGatewayProxyRequest' or 'APIGatewayHttpApiV2ProxyRequest' cannot have [FromUri] or [FromBody] attribute");
                            }
                            if(proxyRequestParameter != null) {
                                throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameters '{proxyRequestParameter.Name}' and '{parameter.Name}' conflict on proxy request");
                            }
                            proxyRequestParameter = parameter;
                            continue;
                        }

                        // check if parameter needs to deserialized from URI or BODY
                        var isSimpleType = parameter.ParameterType.IsValueType || (parameter.ParameterType == typeof(string));
                        if((isSimpleType && !hasFromBodyAttribute) || hasFromUriAttribute) {

                            // check if parameter is read from URI string directly or if its members are read from the URI string
                            if(isSimpleType) {

                                // parameter is required only if it does not have an optional value and is not nullable
                                uriParameters.Add(new KeyValuePair<string, bool>(parameter.Name ?? throw new InvalidOperationException("missing parameter name"), !parameter.IsOptional && (Nullable.GetUnderlyingType(parameter.ParameterType) == null) && (parameter.ParameterType.IsValueType || parameter.ParameterType == typeof(string))));
                            } else {
                                var queryParameterType = parameter.ParameterType;

                                // add complex-type properties
                                foreach(var property in queryParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                                    var name =
                                        property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name
                                        ?? property.GetCustomAttribute<System.Runtime.Serialization.DataMemberAttribute>()?.Name
                                        ?? property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.PropertyName
                                        ?? property.Name;
                                    var required = (
                                            (Nullable.GetUnderlyingType(property.PropertyType) == null)
                                            && (property.PropertyType.IsValueType || (property.PropertyType == typeof(string)))
                                            && (property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required != Newtonsoft.Json.Required.Default)
                                            && (property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required != Newtonsoft.Json.Required.DisallowNull)
                                        )
                                        || (property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() != null)
                                        || (property.GetCustomAttribute<Newtonsoft.Json.JsonRequiredAttribute>() != null)
                                        || (property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required == Newtonsoft.Json.Required.Always)
                                        || (property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required == Newtonsoft.Json.Required.AllowNull);
                                    uriParameters.Add(new KeyValuePair<string, bool>(name, required));
                                }

                                // TODO (2020-08-10, bjorg): System.Text.Json does not deserialize fields; check which deserializer is used

                                // add complex-type fields
                                foreach(var field in queryParameterType.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                                    var name = field.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.PropertyName ?? field.Name;
                                    var required = (
                                            (Nullable.GetUnderlyingType(field.FieldType) == null)
                                            && (field.FieldType.IsValueType || (field.FieldType == typeof(string)))
                                            && (field.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required != Newtonsoft.Json.Required.Default)
                                            && (field.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required != Newtonsoft.Json.Required.DisallowNull)
                                        )
                                        || (field.GetCustomAttribute<Newtonsoft.Json.JsonRequiredAttribute>() != null)
                                        || (field.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required == Newtonsoft.Json.Required.Always)
                                        || (field.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.Required == Newtonsoft.Json.Required.AllowNull);
                                    uriParameters.Add(new KeyValuePair<string, bool>(name, required));
                                }
                            }
                        } else {
                            if(requestParameter != null) {
                                throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameters '{requestParameter.Name}' and '{parameter.Name}' conflict on request body");
                            }
                            requestParameter = parameter;
                        }
                    }

                    // check if no specific request parameter was present, but the method also takes a proxy request
                    if((requestParameter == null) && (proxyRequestParameter != null)) {
                        requestParameter = proxyRequestParameter;
                    }

                    // process method request type
                    var requestSchemaAndContentType = await AddSchema(methodReference, $"for parameter '{requestParameter?.Name}'", requestParameter?.ParameterType);

                    // process method response type
                    var responseType = (method.ReturnType.IsGenericType) && (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                        ? method.ReturnType.GetGenericArguments()[0]
                        : method.ReturnType;
                    var responseSchemaAndContentType = await AddSchema(method.Name, "as return value", responseType);
                    entryPoint = new InvocationTargetDefinition {
                        Assembly = assemblyName,
                        Type = type.FullName,
                        Method = methodName,
                        OperationName = operationName,
                        RequestContentType = requestSchemaAndContentType?.Item2,
                        RequestSchema = requestSchemaAndContentType?.Item1,
                        RequestSchemaName = requestParameter?.ParameterType.FullName,
                        UriParameters  = uriParameters.Any() ? new Dictionary<string, bool>(uriParameters) : null,
                        ResponseContentType = responseSchemaAndContentType?.Item2,
                        ResponseSchema = responseSchemaAndContentType?.Item1,
                        ResponseSchemaName = responseType?.FullName
                    };

                    // write result
                    Console.WriteLine($"... {resolvedMethodReference}({string.Join(", ", uriParameters.Select(kv => kv.Key))}) {entryPoint.GetRequestSchemaName()} -> {entryPoint.GetResponseSchemaName()}");
                } catch(ProcessTargetInvocationException e) {
                    entryPoint = new InvocationTargetDefinition {
                        Error = e.Message
                    };
                } catch(Exception e) {
                    entryPoint = new InvocationTargetDefinition {
                        Error = $"internal error: {e.Message}",
                        StackTrace = e.StackTrace
                    };
                }
                if(entryPoint != null) {
                    schemas.Add(methodReference, entryPoint);
                } else {
                    schemas.Add(methodReference, new InvocationTargetDefinition {
                        Error = "internal error: missing target definition"
                    });
                }
            }

            // create json document
            try {
                var output = JsonSerializer.Serialize(schemas, new JsonSerializerOptions {
                    IgnoreNullValues = false,
                    WriteIndented = true
                });
                if(outputFile != null) {
                    File.WriteAllText(outputFile, output);
                } else {
                    Console.WriteLine(output);
                }
            } catch(Exception e) {
                LogError("unable to write schema", e);
            }

            // local functions
            async Task<Tuple<object, string?>> AddSchema(string methodReference, string parameterName, Type? messageType) {

                // check if there is no request type
                if(messageType == null) {
                    return Tuple.Create((object)"Void", (string?)null);
                }

                // check if there is no response type
                if(
                    (messageType == typeof(void))
                    || (messageType == typeof(Task))
                ) {
                    return Tuple.Create((object)"Void", (string?)null);
                }

                // check if request/response type is not supported
                if(
                    (messageType == typeof(string))
                    || messageType.IsValueType
                ) {
                    throw new ProcessTargetInvocationException($"{methodReference} has unsupported type {parameterName}");
                }

                // check if request/response type is inside 'Task<T>'
                if(messageType.IsGenericType && messageType.GetGenericTypeDefinition() == typeof(Task<>)) {
                    messageType = messageType.GetGenericArguments()[0];
                }

                // check if request/response has an open-ended schema
                if(
                    (messageType == typeof(object))
                    || (messageType == typeof(Newtonsoft.Json.Linq.JObject))
                ) {
                    return Tuple.Create((object)"Object", (string?)"application/json");
                }

                // check if request/response is not a proxy request/response
                if(
                    (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest")
                    && (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse")
                    && (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest")
                    && (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse")
                ) {
                    var schema = await JsonSchema4.FromTypeAsync(messageType, new JsonSchemaGeneratorSettings {
                        FlattenInheritanceHierarchy = true,

#pragma warning disable CS0618
                        // we prefer enums to be handled as strings (NOTE: trying to set this in SerializerSettings causes an NRE in JsonSchema4FromTypeAsync call)
                        DefaultEnumHandling = EnumHandling.String
#pragma warning restore CS0618
                    });

                    // NOTE (2019-04-03, bjorg): we need to allow additional properties, because Swagger doesn't support: "additionalProperties": false
                    schema.AllowAdditionalProperties = true;
                    foreach(var definition in schema.Definitions) {
                        definition.Value.AllowAdditionalProperties = true;
                    }

                    // NOTE (2019-08-16, bjorg): don't emit "x-enumNames" as it is not supported by API Gateway
                    foreach(var definition in schema.Definitions) {
                        definition.Value.EnumerationNames = null;
                    }

                    // return JSON schema document
                    return Tuple.Create((object)(JsonToNativeConverter.ParseObject(schema.ToJson()) ?? throw new InvalidDataException("schema is not a valid JSON object")), (string?)"application/json");
                }
                return Tuple.Create((object)"Proxy", (string?)null);
            }
        }

        public void ExtractAssemblyMetadata(string assemblyFilepath, string outputFilepath) {

            // load assembly
            Assembly assembly;
            try {
                assembly = Assembly.LoadFrom(assemblyFilepath);
            } catch(FileNotFoundException) {
                throw new ProcessTargetInvocationException($"could not find assembly '{assemblyFilepath}'");
            } catch(Exception e) {
                throw new ProcessTargetInvocationException($"error loading assembly '{assemblyFilepath}': {e.Message}");
            }

            // store/show assembly metadata
            if(outputFilepath != null) {
                var metadata = new AssemblyMetadata {
                    ModuleVersionId = assembly.ManifestModule.ModuleVersionId.ToString()
                };
                File.WriteAllText(outputFilepath, System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions {
                    IgnoreNullValues = true,
                    WriteIndented = true
                }));
            } else {
                Console.WriteLine();
                Console.WriteLine($"ModuleVersionId: {assembly.ManifestModule.ModuleVersionId}");
            }
        }
    }
}