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
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using LambdaSharp.Tool.Internal;
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
            Dictionary<string, string> parameters = null;
            await PopulateRuntimeSettingsAsync(settings, optional: true);
            if(HasErrors) {
                return false;
            }

            // check if installation needs to be upgraded
            var install = (settings.TierVersion == null);
            var update = false;
            if(!install) {
                var tierToToolVersionComparison = settings.TierVersion.CompareToVersion(settings.ToolVersion);
                if(tierToToolVersionComparison == 0) {

                    // versions are identical; nothing to do, unless it's a pre-release, which always need to be updated
                    update = settings.ToolVersion.IsPreRelease;
                } else if(tierToToolVersionComparison < 0) {

                    // tier is older; let's only upgrade it if we can
                    update = true;
                } else if(tierToToolVersionComparison > 0) {

                    // tier is newer; tool needs to get updated
                    LogError($"LambdaSharp tool is out of date (tool: {settings.ToolVersion}, tier: {settings.TierVersion})", new LambdaSharpToolOutOfDateException(settings.TierVersion));
                    return false;
                } else if(!forceDeploy) {
                    LogError($"Could not determine if LambdaSharp tool is compatible (tool: {settings.ToolVersion}, tier: {settings.TierVersion}); use --force-deploy to proceed anyway");
                    return false;
                } else {

                    // force deploy it is!
                    update = true;
                }
            }

            // check if bootstrap tier needs to be installed or upgraded
            if(install || (update && (settings.CoreServices == CoreServices.Disabled))) {

                // initialize stack with seed CloudFormation template
                var template = ReadResource("LambdaSharpCore.yml", new Dictionary<string, string> {
                    ["VERSION"] = settings.ToolVersion.ToString()
                });

                // check if bootstrap template is being updated or installed
                if(install) {
                    Console.WriteLine($"Creating LambdaSharp tier");

                    // prompt for profile name
                    if(settings.Tier == null) {
                        if(promptsAsErrors) {
                            LogError($"must provide a tier name with --tier option");
                            return false;
                        }

                        // confirm that the implicit name is the desired name
                        settings.Tier = PromptString("LambdaSharp tier name", "Default");
                    }
                } else {
                    Console.WriteLine($"Updating LambdaSharp tier");
                }

                // create lambdasharp CLI bootstrap stack
                var stackName = $"{settings.TierPrefix}LambdaSharp-Core";
                parameters = (parametersFilename != null)
                    ? CliBuildPublishDeployCommand.ReadInputParametersFiles(settings, parametersFilename)
                    : new Dictionary<string, string>();
                if(HasErrors) {
                    return false;
                }
                var templateParameters = await PromptMissingTemplateParameters(
                    settings.CfnClient,
                    promptsAsErrors,
                    stackName,
                    new Dictionary<string, string>(parameters) {
                        ["TierName"] = settings.Tier
                    },
                    template
                );
                if(HasErrors) {
                    return false;
                }

                // create/update cloudformation stack
                if(install) {
                    Console.WriteLine($"=> Stack creation initiated for {stackName}");
                    await settings.CfnClient.CreateStackAsync(new CreateStackRequest {
                        StackName = stackName,
                        Capabilities = new List<string> { },
                        OnFailure = OnFailure.DELETE,
                        Parameters = templateParameters,
                        EnableTerminationProtection = protectStack,
                        TemplateBody = template
                    });
                    var created = await settings.CfnClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId: null, logError: LogError);
                    if(created.Success) {
                        Console.WriteLine("=> Stack creation finished");
                    } else {
                        Console.WriteLine("=> Stack creation FAILED");
                        return false;
                    }
                } else {
                    Console.WriteLine($"=> Stack update initiated for {stackName}");
                    try {
                        var mostRecentStackEventId = await settings.CfnClient.GetMostRecentStackEventIdAsync(stackName);
                        await settings.CfnClient.UpdateStackAsync(new UpdateStackRequest {
                            StackName = stackName,
                            Capabilities = new List<string> { },
                            Parameters = templateParameters,
                            TemplateBody = template
                        });
                        var created = await settings.CfnClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId, logError: LogError);
                        if(created.Success) {
                            Console.WriteLine("=> Stack update finished");
                        } else {
                            Console.WriteLine("=> Stack update FAILED");
                            return false;
                        }
                    } catch(AmazonCloudFormationException e) when(e.Message == "No updates are to be performed.") {

                        // this error is thrown when no required updates where found
                        Console.WriteLine("=> No stack update required");
                    }
                }
                await PopulateRuntimeSettingsAsync(settings);
                if(HasErrors) {
                    return false;
                }
            }

            // check if API Gateway role needs to be set or updated
            await CheckApiGatewayRole(settings);
            if(HasErrors) {
                return false;
            }

            // standard modules
            var standardModules = new[] {
                "LambdaSharp.Core",
                "LambdaSharp.S3.IO",
                "LambdaSharp.S3.Subscriber"
            };

            // check if the module must be built and published first
            var buildPublishDeployCommand = new CliBuildPublishDeployCommand();
            if(lambdaSharpPath != null) {
                Console.WriteLine($"Building LambdaSharp modules");

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
                    if(!await buildPublishDeployCommand.BuildStepAsync(
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
                    var moduleReference = await buildPublishDeployCommand.PublishStepAsync(settings, forcePublish, forceModuleOrigin: "lambdasharp");
                    if(moduleReference == null) {
                        return false;
                    }
                }
            }

            // check if operating services need to be installed/updated
            if(settings.CoreServices == CoreServices.Disabled) {
                return true;
            }
            if(install) {
                Console.WriteLine($"Creating new deployment tier '{settings.Tier}'");
            } else if(update) {
                Console.WriteLine($"Creating new deployment tier '{settings.Tier}'");
            } else {
                return true;
            }

            // read parameters if they haven't been read yet
            if(parameters == null) {
                parameters = (parametersFilename != null)
                    ? CliBuildPublishDeployCommand.ReadInputParametersFiles(settings, parametersFilename)
                    : new Dictionary<string, string>();
                if(HasErrors) {
                    return false;
                }
            }

            // deploy LambdaSharp module
            foreach(var module in standardModules) {
                var isLambdaSharpCoreModule = (module == "LambdaSharp.Core");
                if(!await buildPublishDeployCommand.DeployStepAsync(
                    settings,
                    dryRun: null,
                    moduleReference: $"{module}:{version}@lambdasharp",
                    instanceName: null,
                    allowDataLoos: allowDataLoos,
                    protectStack: protectStack,
                    parameters: parameters,
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

        private async Task<List<Parameter>> PromptMissingTemplateParameters(
            IAmazonCloudFormation cfnClient,
            bool promptsAsErrors,
            string stackName,
            IDictionary<string, string> providedParameters,
            string templateBody
        ) {

            // get summary of new template
            GetTemplateSummaryResponse templateSummary;
            try {
                templateSummary = await cfnClient.GetTemplateSummaryAsync(new GetTemplateSummaryRequest {
                    TemplateBody = templateBody
                });
            } catch(AmazonCloudFormationException e) {
                LogError(e.Message);
                return null;
            }

            // find configuration for existing stack
            Stack existing = null;
            if(stackName != null) {
                try {
                    existing = (await cfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = stackName
                    })).Stacks.First();
                } catch(AmazonCloudFormationException) { }
            }
            var result = new List<Parameter>();
            var missingParameters = new List<ParameterDeclaration>();
            foreach(var templateParameter in templateSummary.Parameters) {
                if(providedParameters.TryGetValue(templateParameter.ParameterKey, out var providedValue)) {

                    // use the provided parameter value
                    result.Add(new Parameter {
                        ParameterKey = templateParameter.ParameterKey,
                        ParameterValue = providedValue
                    });
                } else if(existing?.Parameters.Any(existingParam => existingParam.ParameterKey == templateParameter.ParameterKey) == true) {

                    // re-use the existing parameter value
                    result.Add(new Parameter {
                        ParameterKey = templateParameter.ParameterKey,
                        UsePreviousValue = true
                    });
                } else {

                    // add parameter to missing parameters
                    missingParameters.Add(templateParameter);
                }
            }

            // ask user for missing values
            if(missingParameters.Any()) {
                if(promptsAsErrors) {
                    foreach(var missingParameter in missingParameters) {
                        LogError($"template requires value for parameter '{missingParameter.ParameterKey}'");
                    }
                    return null;
                }
                Console.WriteLine();
                Console.WriteLine($"Configuring {templateSummary.Description} Parameters");
                foreach(var missingParameter in missingParameters) {
                    if(missingParameter.ParameterConstraints?.AllowedValues.Any() ?? false) {
                        var enteredValue = PromptChoice(
                            $"{missingParameter.Description ?? missingParameter.ParameterKey}",
                            missingParameter.ParameterConstraints.AllowedValues
                        );
                        result.Add(new Parameter {
                            ParameterKey = missingParameter.ParameterKey,
                            ParameterValue = enteredValue
                        });
                    } else {
                        var enteredValue = PromptString($"{missingParameter.Description ?? missingParameter.ParameterKey}", missingParameter.DefaultValue) ?? "";
                        result.Add(new Parameter {
                            ParameterKey = missingParameter.ParameterKey,
                            ParameterValue = enteredValue
                        });
                    }
                }
                Console.WriteLine();
            }

            // NOTE (2019-06-06, bjorg): extraneous parameters are ignored as they might be relevant to the LambdaSharp.Core initialization

            // return the collected paramaters
            return result;
        }
    }
}
