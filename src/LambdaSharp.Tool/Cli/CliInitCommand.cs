/*
 * MindTouch λ#
 * Copyright (C) 2006-2018-2019 MindTouch, Inc.
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

namespace LambdaSharp.Tool.Cli {

    public class CliInitCommand : ACliCommand {

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
                var localOption = cmd.Option("--local <PATH>", "(optional) Provide a path to a local check-out of the LambdaSharp modules (default: LAMBDASHARP environment variable)", CommandOptionType.SingleValue);
                var usePublishedOption = cmd.Option("--use-published", "(optional) Force the init command to use the published LambdaSharp modules", CommandOptionType.NoValue);
                var parametersFileOption = cmd.Option("--parameters <FILE>", "(optional) Specify source filename for module parameters (default: none)", CommandOptionType.SingleValue);
                var forcePublishOption = CliBuildPublishDeployCommand.AddForcePublishOption(cmd);
                var promptAllParametersOption = cmd.Option("--prompt-all", "(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)", CommandOptionType.NoValue);
                var promptsAsErrorsOption = cmd.Option("--prompts-as-errors", "(optional) Missing parameters cause an error instead of a prompts (use for CI/CD to avoid unattended prompts)", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // reading module inputs
                    var inputs = new Dictionary<string, string>();
                    if(parametersFileOption.HasValue()) {
                        inputs = CliBuildPublishDeployCommand.ReadInputParametersFiles(settings, parametersFileOption.Value());
                        if(HasErrors) {
                            return;
                        }
                    }
                    if(HasErrors) {
                        return;
                    }

                    // determine if we want to install modules from a local check-out
                    await Init(
                        settings,
                        allowDataLossOption.HasValue(),
                        protectStackOption.HasValue(),
                        forceDeployOption.HasValue(),
                        versionOption.HasValue() ? VersionInfo.Parse(versionOption.Value()) : Version,
                        usePublishedOption.HasValue()
                            ? null
                            : (localOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP")),
                        inputs,
                        forcePublishOption.HasValue(),
                        promptAllParametersOption.HasValue(),
                        promptsAsErrorsOption.HasValue()
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
            string lambdaSharpPath,
            Dictionary<string, string> inputs,
            bool forcePublish,
            bool promptAllParameters,
            bool promptsAsErrors
        ) {
            var command = new CliBuildPublishDeployCommand();
            Console.WriteLine($"Creating new deployment tier '{settings.Tier}'");

            // check if the module must be built and published first
            if(lambdaSharpPath != null) {
                foreach(var module in new[] {
                    "LambdaSharp.Core",
                    "LambdaSharp.S3.IO",
                    "LambdaSharp.S3.Subscriber"
                }) {
                    var moduleSource = Path.Combine(lambdaSharpPath, "Modules", module, "Module.yml");
                    settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                    settings.OutputDirectory = Path.Combine(settings.WorkingDirectory, "bin");

                    // build local module
                    if(!await command.BuildStepAsync(
                        settings,
                        Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                        noAssemblyValidation: true,
                        noPackageBuild: false,
                        gitSha: GetGitShaValue(settings.WorkingDirectory, showWarningOnFailure: false),
                        gitBranch: GetGitBranch(settings.WorkingDirectory, showWarningOnFailure: false),
                        buildConfiguration: "Release",
                        selector: null,
                        moduleSource: moduleSource
                    )) {
                        break;
                    }

                    // publish module
                    var moduleReference = await command.PublishStepAsync(settings, forcePublish);
                    if(moduleReference == null) {
                        break;
                    }
                }
            }

            // deploy LambdaSharp module
            await command.DeployStepAsync(
                settings,
                dryRun: null,
                moduleReference: $"LambdaSharp.Core:{version}",
                instanceName: null,
                allowDataLoos: allowDataLoos,
                protectStack: protectStack,
                inputs: inputs,
                forceDeploy: forceDeploy,
                promptAllParameters: promptAllParameters,
                promptsAsErrors: promptsAsErrors,
                enableXRayTracing: false
            );
        }
    }
}
