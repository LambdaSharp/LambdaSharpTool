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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {

    public class CliDebugCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {

            // nuke a deployment tier
            app.Command("debug", cmd => {
                cmd.HelpOption();
                cmd.Description = "Debug a LambdaSharp function";
                var assemblyPathOption = cmd.Option("--assembly <ASSEMBLY>", "", CommandOptionType.SingleValue);
                var classNameOption = cmd.Option("--class", "", CommandOptionType.SingleValue);
                var environmentPathOption = cmd.Option("--environment <FILE>", "", CommandOptionType.SingleValue);
                var payloadPathOption = cmd.Option("--payload <FILE>", "", CommandOptionType.SingleValue);

                // TODO:
                //  * build project for debugging
                //  * load compilation output
                //  * locate class
                //  * instantiate class (assume ALambdaFunction base class)
                //  * invoke `Task InitializeAsync(LambdaConfig config)`
                //  * invoke `Task<Stream> ProcessMessageStreamAsync(Stream stream)`

                // misc options
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                AddStandardCommandOptions(cmd);
                cmd.OnExecute(async () => {
                    ExecuteCommandActions(cmd);

                    // read settings and validate them
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // load assembly
                    if(!assemblyPathOption.HasValue()) {
                        LogError("missing assembly information");
                        return;
                    }
                    Assembly assembly;
                    try {
                        var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), assemblyPathOption.Value());
                        assembly = Assembly.LoadFrom(assemblyPath);
                    } catch(Exception e) {
                        LogError($"unable to load assembly: {assemblyPathOption.Value()}", e);
                        return;
                    }

                    // resolve function class
                    if(!classNameOption.HasValue()) {
                        LogError("missing class name information");
                        return;
                    }
                    Type functionType;
                    try {
                        functionType = assembly.GetType(classNameOption.Value(), throwOnError: true);
                    } catch(Exception e) {
                        LogError($"unable to resolve class name: {classNameOption.Value()}", e);
                        return;
                    }

                    // load environment variables
                    if(!environmentPathOption.HasValue()) {
                        LogError("missing environment variables information");
                        return;
                    }
                    Dictionary<string, string> environmentVariables;
                    try {
                        environmentVariables = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(environmentPathOption.Value()));
                    } catch(Exception e) {
                        LogError($"unable to load environment variables: {environmentPathOption.Value()}", e);
                        return;
                    }

                    // load payload
                    if(!payloadPathOption.HasValue()) {
                        LogError("missing payload information");
                        return;
                    }
                    string payload;
                    try {
                        payload = File.ReadAllText(payloadPathOption.Value());
                    } catch(Exception e) {
                        LogError($"unable to load payload: {payloadPathOption.Value()}", e);
                        return;
                    }

                    await DebugAsync(settings, assembly, functionType, environmentVariables, payload);
                });
            });
        }

        public Task<Stream> DebugAsync(Settings settings, Assembly assembly, Type functionType, Dictionary<string, string> environmentVariables, string payload) {
            var lambdaSharpAssemblyName = assembly.GetReferencedAssemblies().First(referencedAssembly => referencedAssembly.Name == "LambdaSharp");
            var lambdaSharpAssembly = Assembly.Load(lambdaSharpAssemblyName);

            // TODO: the dependency provider depends on the function type!
            var debugLambdaFunctionDependencyProviderType = lambdaSharpAssembly.GetType("LambdaSharp.Debug.DebugLambdaFunctionDependencyProvider", throwOnError: true);
            var debugLambdaContextType = lambdaSharpAssembly.GetType("LambdaSharp.Debug.DebugLambdaContext", throwOnError: true);

            // create function
            dynamic function = Activator.CreateInstance(functionType);
            dynamic provider = Activator.CreateInstance(debugLambdaFunctionDependencyProviderType, environmentVariables);
            function.Provider = provider;

            // create invocation context
            dynamic context = Activator.CreateInstance(debugLambdaContextType, provider);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            return DebugAsync(settings, function, context, stream);
        }

        public Task<Stream> DebugAsync(Settings settings, dynamic function, dynamic context, Stream stream)
            => function.FunctionHandlerAsync(stream, context);
    }
}
