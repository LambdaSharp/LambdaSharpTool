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
using System.Reflection;
using System.Text;
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
                var entrypointOption = cmd.Option("--entrypoint <ASSEMBLY-NAME>::<CLASS-NAME>::<FUNCTION-NAME>", "", CommandOptionType.SingleValue);
                var environmentOption = cmd.Option("--environment <FILEPATH>", "", CommandOptionType.SingleValue);
                var payloadOption = cmd.Option("--payload <FILEPATH>", "", CommandOptionType.SingleValue);

                // TODO:
                //  * build project for debugging
                //  * load compilation output
                //  * locate class
                //  * instantiate class (assume ALambdaFunction base class)
                //  * invoke `Task InitializeAsync(LambdaConfig config)`
                //  * invoke `Task<Stream> ProcessMessageStreamAsync(Stream stream)`
            });
        }

        public async Task DebugAsync(Settings settings, string project, string className, Dictionary<string, string> environment, string payload) {
            throw new NotImplementedException();
        }

        public Task<Stream> DebugAsync(Settings settings, Assembly assembly, Type functionType, Dictionary<string, string> environmentVariables, string payload) {
            var debugLambdaFunctionDependencyProviderType = assembly.GetType("LambdaSharp.Debug.DebugLambdaFunctionDependencyProvider");
            var debugLambdaContextType = assembly.GetType("LambdaSharp.Debug.DebugLambdaContext");

            // create function
            var provider = Activator.CreateInstance(debugLambdaFunctionDependencyProviderType, environmentVariables);
            var function = Activator.CreateInstance(functionType, provider);
            var context = Activator.CreateInstance(debugLambdaContextType);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            return DebugAsync(settings, function, context, stream);
        }

        public Task<Stream> DebugAsync(Settings settings, dynamic function, object context, Stream stream)
            => function.FunctionHandlerAsync(stream, context);
    }
}
