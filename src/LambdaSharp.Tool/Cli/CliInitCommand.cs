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
                var enableXRayTracingOption = cmd.Option("--xray", "(optional) Enable service-call tracing with AWS X-Ray for all resources in module", CommandOptionType.NoValue);
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

                    // check x-ray settings
                    if(!TryParseEnumOption(enableXRayTracingOption, XRayTracingLevel.Disabled, XRayTracingLevel.RootModule, out var xRayTracingLevel)) {

                        // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
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
                        parametersFileOption.Value(),
                        forcePublishOption.HasValue(),
                        promptAllParametersOption.HasValue(),
                        promptsAsErrorsOption.HasValue(),
                        xRayTracingLevel
                    );
                });
            });
        }

        public async Task<bool> Init(
            Settings settings,
            bool allowDataLoos,
            bool protectStack,
            bool forceDeploy,
            VersionInfo version,
            string lambdaSharpPath,
            string parametersFilename,
            bool forcePublish,
            bool promptAllParameters,
            bool promptsAsErrors,
            XRayTracingLevel xRayTracingLevel
        ) {
            var command = new CliBuildPublishDeployCommand();
            Console.WriteLine($"Creating new deployment tier '{settings.Tier}'");

            // standard modules
            var standardModules = new[] {
                "LambdaSharp.Core",
                "LambdaSharp.S3.IO",
                "LambdaSharp.S3.Subscriber"
            };

            // check if the module must be built and published first
            if(lambdaSharpPath != null) {

                // attempt to parse the tool version from environment variables
                if(!VersionInfo.TryParse(Environment.GetEnvironmentVariable("LAMBDASHARP_VERSION"), out var moduleVersion)) {
                    LogError("unable to parse module version from LAMBDASHARP_VERSION");
                    return false;
                }
                foreach(var module in standardModules) {
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
                        moduleSource: moduleSource,
                        moduleVersion: moduleVersion
                    )) {
                        return false;
                    }

                    // publish module
                    var moduleReference = await command.PublishStepAsync(settings, forcePublish);
                    if(moduleReference == null) {
                        return false;
                    }
                }
            }

            // deploy LambdaSharp module
            foreach(var module in standardModules) {
                var isLambdaSharpCoreModule = (module == "LambdaSharp.Core");
                if(!await command.DeployStepAsync(
                    settings,
                    dryRun: null,
                    moduleReference: $"{module}:{version}",
                    instanceName: null,
                    allowDataLoos: allowDataLoos,
                    protectStack: protectStack,
                    parametersFilename: parametersFilename,
                    forceDeploy: forceDeploy,
                    promptAllParameters: promptAllParameters,
                    promptsAsErrors: promptsAsErrors,
                    xRayTracingLevel: xRayTracingLevel,
                    deployOnlyIfExists: !isLambdaSharpCoreModule
                )) {
                    return false;
                }

                // reset tier version if core module was deployed
                if(isLambdaSharpCoreModule) {
                    settings.TierVersion = null;
                }
            }
            return true;
        }
    }
}
