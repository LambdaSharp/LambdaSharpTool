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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliSetupCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("setup", cmd => {
                cmd.HelpOption();
                cmd.Description = "Setup LambdaSharp environment";
                var tierOption = AddTierOption(cmd);
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var forceDeployOption = cmd.Option("--force-deploy", "(optional) Force module deployment", CommandOptionType.NoValue);
                var versionOption = cmd.Option("--version", "(optional) Specify version for LambdaSharp modules (default: same as tool version)", CommandOptionType.SingleValue);
                var localOption = cmd.Option("--local", "(optional) Provide a path to a local check-out of the LambdaSharp bootstrap modules (default: LAMBDASHARP environment variable)", CommandOptionType.SingleValue);
                var remoteOption = cmd.Option("--no-local", "(optional) Force the setup command to use the published LambdaSharp bootstrap modules", CommandOptionType.NoValue);

                // command options
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // initialize deployment tier value
                    var tier = tierOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_TIER");
                    if(string.IsNullOrEmpty(tier)) {
                        AddError("missing deployment tier name");
                        return;
                    }
                    if(tier == "Default") {
                        AddError("deployment tier cannot be 'Default' because it is a reserved name");
                        return;
                    }

                    // determine if we want to install modules for a local check-out
                    string localPath = null;
                    if(!remoteOption.HasValue()) {
                        if(localOption.HasValue()) {
                            localPath = localOption.Value();
                        } else {
                            var env = Environment.GetEnvironmentVariable("LAMBDASHARP");
                            if(env != null) {
                                localPath = Path.Combine(env, "Bootstrap");
                            }
                        }
                    }
                    await Setup(
                        settings,
                        allowDataLossOption.HasValue(),
                        protectStackOption.HasValue(),
                        tier,
                        forceDeployOption.HasValue(),
                        versionOption.HasValue() ? VersionInfo.Parse(versionOption.Value()) : Version,
                        localPath
                    );
                });
            });
        }

        public async Task Setup(
            Settings settings,
            bool allowDataLoos,
            bool protectStack,
            string tier,
            bool forceDeploy,
            VersionInfo version,
            string localPath
        ) {
            Console.WriteLine($"Readying module for deployment tier '{tier}'");
            foreach(var module in new[] {
                "LambdaSharp",
                "LambdaSharpRegistrar",
                "LambdaSharpS3Subscriber",
                "LambdaSharpS3PackageLoader"
            }) {
                var command = new CliBuildPublishDeployCommand();
                if(localPath != null) {
                    var moduleSource = Path.Combine(localPath, module, "Module.yml");
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
                    var moduleKey = await command.PublishStepAsync(settings);
                    if(moduleKey == null) {
                        break;
                    }

                    // deploy module
                    if(!await command.DeployStepAsync(
                        settings,
                        dryRun: null,
                        moduleKey: moduleKey,
                        instanceName: null,
                        allowDataLoos: allowDataLoos,
                        protectStack: protectStack,
                        inputs: new Dictionary<string, string>(),
                        tier: tier,
                        forceDeploy: forceDeploy
                    )) {
                        break;
                    }
                } else {

                    // deploy from published bucket
                    if(!await command.DeployStepAsync(
                        settings,
                        dryRun: null,
                        moduleKey: $"{module}:{version}",
                        instanceName: null,
                        allowDataLoos: allowDataLoos,
                        protectStack: protectStack,
                        inputs: new Dictionary<string, string>(),
                        tier: tier,
                        forceDeploy: forceDeploy
                    )) {
                        break;
                    }
                }
            }
        }
    }
}
