/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliInitCommand : ACliCommand {

        //--- Class Methods ---
        private static string ReadResource(string resourceName, IDictionary<string, string> substitutions = null) {
            string result;
            var assembly = typeof(CliNewCommand).Assembly;
            using(var resource = assembly.GetManifestResourceStream($"MindTouch.LambdaSharp.Tool.Resources.{resourceName}"))
            using(var reader = new StreamReader(resource, Encoding.UTF8)) {
                result = reader.ReadToEnd();
            }
            if(substitutions != null) {
                foreach(var kv in substitutions) {
                    result = result.Replace($"%%{kv.Key}%%", kv.Value);
                }
            }
            return result;
        }

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("init", cmd => {
                cmd.HelpOption();
                cmd.Description = "Initialize LambdaSharp deployment tier";

                // init options
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var forceDeployOption = cmd.Option("--force-deploy", "(optional) Force module deployment", CommandOptionType.NoValue);
                var versionOption = cmd.Option("--version <VERSION>", "(optional) Specify version for LambdaSharp modules (default: same as CLI version)", CommandOptionType.SingleValue);
                var localOption = cmd.Option("--local <PATH>", "(optional) Provide a path to a local check-out of the LambdaSharp bootstrap modules (default: LAMBDASHARP environment variable)", CommandOptionType.SingleValue);
                var usePublishedOption = cmd.Option("--use-published", "(optional) Force the init command to use the published LambdaSharp bootstrap modules", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // determine if we want to install modules from a local check-out
                    await Init(
                        settings,
                        allowDataLossOption.HasValue(),
                        protectStackOption.HasValue(),
                        forceDeployOption.HasValue(),
                        versionOption.HasValue() ? VersionInfo.Parse(versionOption.Value()) : Version,
                        localOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP")
                    );
                });
            });
        }

        public async Task Init(
            Settings settings,
            bool allowDataLoos,
            bool protectStack,
            bool forceDeploy,
            VersionInfo version,
            string lambdaSharpPath
        ) {
            var command = new CliBuildPublishDeployCommand();
            Console.WriteLine($"Creating new deployment tier '{settings.Tier}'");
            foreach(var module in new[] {
                "LambdaSharpS3PackageLoader",
                "LambdaSharpS3Subscriber",
                "LambdaSharpRegistrar",
                "LambdaSharp"
            }) {
                var moduleKey = $"{module}:{version}";

                // check if the module must be built and published first
                if(lambdaSharpPath != null) {
                    var moduleSource = Path.Combine(lambdaSharpPath, "Bootstrap", module, "Module.yml");
                    settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                    settings.OutputDirectory = Path.Combine(settings.WorkingDirectory, "bin");

                    // build local module
                    if(!await command.BuildStepAsync(
                        settings,
                        Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                        skipAssemblyValidation: true,
                        skipFunctionBuild: false,
                        gitsha: null,
                        buildConfiguration: "Release",
                        selector: null,
                        moduleSource: moduleSource
                    )) {
                        break;
                    }

                    // publish module
                    moduleKey = await command.PublishStepAsync(settings);
                    if(moduleKey == null) {
                        break;
                    }
                }
            }

            // deploy LambdaSharp module
            await command.DeployStepAsync(
                settings,
                dryRun: null,
                moduleKey: $"LambdaSharp:{version}",
                instanceName: null,
                allowDataLoos: allowDataLoos,
                protectStack: protectStack,
                inputs: new Dictionary<string, string>(),
                forceDeploy: forceDeploy
            );
        }
    }
}
