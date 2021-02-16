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
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using LambdaSharp.Modules;
using LambdaSharp.Tool.Cli.Tier;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;
    using ModuleInfo = LambdaSharp.Modules.ModuleInfo;

    public class CliTierCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("tier", cmd => {
                cmd.HelpOption();
                cmd.Description = "Tier utility commands";

                // function sub-command
                cmd.Command("coreservices", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Enable/disable LambdaSharp.Core services for all modules in tier";

                    // command options
                    var enabledOption = subCmd.Option("--enabled", "(optional) Enable LambdaSharp.Core services for all modules", CommandOptionType.NoValue);
                    var disabledOption = subCmd.Option("--disabled", "(optional) Disable LambdaSharp.Core services for all modules", CommandOptionType.NoValue);
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return;
                        }
                        if(enabledOption.HasValue() && disabledOption.HasValue()) {
                            LogError("--enabled and --disabled options are mutually exclusive");
                            return;
                        }
                        bool? enabled = null;
                        if(enabledOption.HasValue()) {
                            enabled = true;
                        } else if(disabledOption.HasValue()) {
                            enabled = false;
                        }
                        await UpdateCoreServicesAsync(settings, enabled, showModules: true);
                    });
                });

                // check tier version
                cmd.Command("version", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Check Tier Version";
                    var minVersionOption = subCmd.Option("--min-version", "(optional) Minimum expected version", CommandOptionType.SingleValue);
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    AddStandardCommandOptions(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return -1;
                        }

                        // fetch tier information
                        if(!await PopulateDeploymentTierSettingsAsync(settings, optional: true)) {
                            if(!Program.Quiet) {
                                Console.WriteLine();
                                Console.WriteLine($"No deployment tier found {Settings.OutputColor}[ExitCode: 2]{Settings.ResetColor}");
                            }
                            return 2;
                        }

                        // validate options
                        if(minVersionOption.Value() == null) {
                            Console.WriteLine();
                            Console.WriteLine($"Deployment tier version: {Settings.InfoColor}{settings.TierVersion}{Settings.ResetColor}");
                            return 0;
                        } else {
                            if(!VersionInfo.TryParse(minVersionOption.Value(), out var minVersion)) {
                                LogError("invalid value for --min-version option");
                                return -1;
                            }

                            // compare version numbers
                            var exitCode = settings.TierVersion.IsGreaterOrEqualThanVersion(minVersion) ? 0 : 1;
                            if(!Program.Quiet) {
                                Console.WriteLine();
                                Console.WriteLine($"Deployment tier version: {Settings.InfoColor}{settings.TierVersion} {Settings.OutputColor}[ExitCode: {exitCode}]{Settings.ResetColor}");
                            }
                            return exitCode;
                        }
                    });
                });

                // check tier version
                cmd.Command("list", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "List all available deployment tiers";

                    // command options
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return;
                        }

                        // gather module details
                        var tierManager = new TierManager(settings);
                        var tierDetails = await tierManager.GetDeploymentTierDetailsAsync();
                        if(tierDetails.Any()) {
                            Console.WriteLine();
                            Console.WriteLine($"Found {tierDetails.Count():N0} deployment tiers");
                            Console.WriteLine();
                            tierManager.ShowModuleDetails(
                                tierDetails.OrderBy(module => module.DeploymentTierName),
                                (ColumnTitle: "TIER", GetColumnValue: module => module.DeploymentTierName),
                                (ColumnTitle: "VERSION", GetColumnValue: module => ModuleInfo.TryParse(module.ModuleReference, out var moduleInfo)
                                    ? moduleInfo.Version.ToString()
                                    : module.ModuleReference),
                                (ColumnTitle: "STATUS", GetColumnValue: module => module.StackStatus),
                                (ColumnTitle: "CORE-SERVICES", GetColumnValue: module => module.CoreServices?.ToUpperInvariant() ?? "N/A")
                            );
                        } else {
                            Console.WriteLine();
                            Console.WriteLine($"Found no deployment tiers");
                        }
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    Program.ShowHelp = true;
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        public async Task UpdateCoreServicesAsync(Settings settings, bool? enabled, bool showModules) {
            if(!await PopulateDeploymentTierSettingsAsync(settings, requireBucketName: false, requireCoreServices: false, requireVersionCheck: false)) {
                return;
            }

            // gather module details
            var tierManager = new TierManager(settings);
            var moduleDetails = (await tierManager.GetModuleDetailsAsync())
                .OrderBy(details => details.ModuleDeploymentName)
                .ToList();
            if(showModules) {
                ShowModuleDetails(tierManager, moduleDetails);
            }

            // check if tier has any stacks
            if(!moduleDetails.Any()) {
                return;
            }

            // validate that all modules in tier can enable/disable core services
            if(enabled.HasValue) {

                // check if LambdaSharp.Core has Core Services enabled
                if(enabled.Value && (settings.CoreServices != CoreServices.Enabled)) {
                    LogError($"{settings.TierName} does not have Core Services enabled; run 'lash init --core-services enabled' first");
                    return;
                }

                // check if deployed modules support Core Services
                foreach(var details in moduleDetails) {
                    if(details.CoreServices == null) {
                        LogError($"{details.ModuleDeploymentName} does not support enabling/disabling LambdaSharp.Core services");
                    } else if(!enabled.Value && details.HasDefaultSecretKeyParameter) {
                        LogError($"{details.ModuleDeploymentName} cannot disable LambdaSharp.Core services, because it depends on DefaultSecretKey");
                    }
                }
            }
            if(!enabled.HasValue || HasErrors) {
                return;
            }

            // update core services for each affected root module
            var coreServicesParameter = enabled.Value ? "Enabled" : "Disabled";
            var modulesToUpdate = moduleDetails
                .Where(module => module.CoreServices != coreServicesParameter)
                .Where(module => module.IsRoot)
                .ToList();
            if(!modulesToUpdate.Any()) {
                return;
            }
            Console.WriteLine();
            Console.WriteLine($"=> {(enabled.Value ? "Enabling" : "Disabling")} core services in deployment tier {Settings.InfoColor}{settings.TierName}{Settings.ResetColor}");
            var parameters = new Dictionary<string, string> {
                ["LambdaSharpCoreServices"] = coreServicesParameter,

                // NOTE (2020-04-23, bjorg): deployment bucket might change if the LambdaSharp.Core is recreated
                ["DeploymentBucketName"] = settings.DeploymentBucketName
            };
            foreach(var module in modulesToUpdate) {
                await UpdateStackParameters(settings, module, parameters);
            }

            // show updated state
            if(showModules) {
                ShowModuleDetails(tierManager, await tierManager.GetModuleDetailsAsync());
            }
        }

        private async Task<List<Change>> WaitForChangeSetAsync(Settings settings, string changeSetId) {

            // wait until change-set if available
            var changeSetRequest = new DescribeChangeSetRequest {
                ChangeSetName = changeSetId
            };
            var changes = new List<Change>();
            while(true) {
                await Task.Delay(TimeSpan.FromSeconds(3));
                var changeSetResponse = await settings.CfnClient.DescribeChangeSetAsync(changeSetRequest);
                if(changeSetResponse.Status == ChangeSetStatus.CREATE_PENDING) {

                    // wait until the change-set is CREATE_COMPLETE
                    continue;
                }
                if(changeSetResponse.Status == ChangeSetStatus.CREATE_IN_PROGRESS) {

                    // wait until the change-set is CREATE_COMPLETE
                    continue;
                }
                if(changeSetResponse.Status == ChangeSetStatus.CREATE_COMPLETE) {
                    changes.AddRange(changeSetResponse.Changes);
                    if(changeSetResponse.NextToken != null) {
                        changeSetRequest.NextToken = changeSetResponse.NextToken;
                        continue;
                    }
                    return changes;
                }
                if(changeSetResponse.Status == ChangeSetStatus.FAILED) {
                    if(changeSetResponse.StatusReason.StartsWith("The submitted information didn't contain changes.", StringComparison.Ordinal)) {
                        return new List<Change>();
                    }
                    LogError($"change-set failed: {changeSetResponse.StatusReason}");
                    return null;
                }
                LogError($"unexpected change-set status: {changeSetResponse.ExecutionStatus}");
                return null;
            }
        }

        private void ShowModuleDetails(TierManager tierManager, IEnumerable<TierModuleDetails> moduleDetails) {
            if(moduleDetails.Any()) {
                Console.WriteLine();
                Console.WriteLine($"Found {moduleDetails.Count():N0} modules for deployment tier {Settings.InfoColor}{tierManager.TierName}{Settings.ResetColor}");
                Console.WriteLine();
                tierManager.ShowModuleDetails(
                    moduleDetails.OrderBy(module => module.ModuleDeploymentName),
                    (ColumnTitle: "NAME", GetColumnValue: module => module.ModuleDeploymentName),
                    (ColumnTitle: "MODULE", GetColumnValue: module => module.ModuleReference),
                    (ColumnTitle: "STATUS", GetColumnValue: module => module.StackStatus),
                    (ColumnTitle: "CORE-SERVICES", GetColumnValue: module => module.CoreServices?.ToUpperInvariant() ?? "N/A")
                );
            } else {
                Console.WriteLine();
                Console.WriteLine($"Found no modules for deployment tier {Settings.InfoColor}{tierManager.TierName}{Settings.ResetColor}");
            }
        }

        private async Task UpdateStackParameters(Settings settings, TierModuleDetails module, Dictionary<string, string> parameters) {

            // keep all original parameter values except for 'LambdaSharpCoreServices' and 'DeploymentBucketName'
            var stackParameters = module.Stack.Parameters
                .Select(parameter => {
                    if(parameters.TryGetValue(parameter.ParameterKey, out var value)) {
                        return new CloudFormationParameter {
                            ParameterKey = parameter.ParameterKey,
                            ParameterValue = value
                        };
                    }
                    return new CloudFormationParameter {
                        ParameterKey = parameter.ParameterKey,
                        UsePreviousValue = true
                    };
                }).ToList();

            // retrieve name mappings and artifacts for template
            var template = (await settings.CfnClient.GetTemplateAsync(new GetTemplateRequest {
                StackName = module.Stack.StackName,
                TemplateStage = TemplateStage.Original
            })).TemplateBody;
            var manifestLoader = new ModelManifestLoader(settings, module.Stack.StackName);
            var nameMappings = manifestLoader.GetNameMappingsFromTemplate(template);
            var artifacts = manifestLoader.GetArtifactsFromTemplate(template);

            // ensure that all artifacts exist before updating (otherwise, the update will fail)
            if(artifacts?.Any() ?? false) {
                foreach(var artifact in artifacts) {
                    if(!await settings.S3Client.DoesS3ObjectExistAsync(settings.DeploymentBucketName, artifact)) {
                        LogError($"cannot update CloudFormation stack due to missing artifact; re-publish original artifacts or re-deploy module with new artifacts (missing: {artifact})");
                    }
                }
                if(HasErrors) {
                    return;
                }
            }

            // create change-set
            var now = DateTime.UtcNow;
            var mostRecentStackEventId = await settings.CfnClient.GetMostRecentStackEventIdAsync(module.StackName);
            var changeSetName = $"{module.ModuleDeploymentName}-{now:yyyy-MM-dd-hh-mm-ss}";
            Console.WriteLine();
            Console.WriteLine($"=> Stack update initiated for {Settings.InfoColor}{module.StackName}{Settings.ResetColor}");
            var response = await settings.CfnClient.CreateChangeSetAsync(new CreateChangeSetRequest {
                Capabilities = module.Stack.Capabilities,
                ChangeSetName = changeSetName,
                ChangeSetType = (mostRecentStackEventId != null) ? ChangeSetType.UPDATE : ChangeSetType.CREATE,
                Description = $"Stack parameters update for {module.ModuleReference}",
                Parameters = stackParameters,
                StackName = module.StackName,
                UsePreviousTemplate = true,
                Tags = module.Stack.Tags
            });
            try {
                var changes = await WaitForChangeSetAsync(settings, response.Id);
                if(changes == null) {
                    LogError($"unable to apply changes to ${module.ModuleDeploymentName}");
                    return;
                }
                if(!changes.Any()) {
                    Console.WriteLine("=> No stack update required");
                    return;
                }

                // execute change-set
                await settings.CfnClient.ExecuteChangeSetAsync(new ExecuteChangeSetRequest {
                    ChangeSetName = changeSetName,
                    StackName = module.StackName
                });
                var outcome = await settings.CfnClient.TrackStackUpdateAsync(
                    module.StackName,
                    response.StackId,
                    mostRecentStackEventId,
                    nameMappings,
                    oldNameMappings: null,
                    LogError
                );
                if(outcome.Success) {
                    Console.WriteLine("=> Stack update finished");
                } else {
                    Console.WriteLine("=> Stack update FAILED");
                    LogError($"unable to update {module.ModuleDeploymentName}");
                }
            } finally {
                try {
                    await settings.CfnClient.DeleteChangeSetAsync(new DeleteChangeSetRequest {
                        ChangeSetName = response.Id
                    });
                } catch { }
            }
        }
    }
}
