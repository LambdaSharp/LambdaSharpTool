/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using LambdaSharp.Tool.Cli.Tier;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class CliTierCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("tier", cmd => {
                cmd.HelpOption();
                cmd.Description = "Update settings module in LambdaSharp tier";

                // function sub-command
                cmd.Command("coreservices", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Enable/disable LambdaSharp.Core services for all modules in tier";
                    var enabledOption = subCmd.Option("--enabled", "(optional) Enable LambdaSharp.Core services for all modules", CommandOptionType.NoValue);
                    var disabledOption = subCmd.Option("--disabled", "(optional) Disable LambdaSharp.Core services for all modules", CommandOptionType.NoValue);
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
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

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
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
                foreach(var details in moduleDetails) {
                    if(details.CoreServices == null) {
                        LogError($"${details.ModuleDeploymentName} does not support enabling/disabling LambdaSharp.Core services");
                    } else if(!enabled.Value && details.HasDefaultSecretKeyParameter) {
                        LogError($"${details.ModuleDeploymentName} cannot disable LambdaSharp.Core services, because it depends on DefaultSecretKey");
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
            Console.WriteLine($"=> {(enabled.Value ? "Enabling" : "Disabling")} modules in deployment tier '{settings.TierName}'");
            var parameters = new Dictionary<string, string> {
                ["LambdaSharpCoreServices"] = coreServicesParameter
            };
            foreach(var module in modulesToUpdate) {
                await UpdateStackParameters(settings, module, parameters);
            }
            Console.WriteLine();

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
            tierManager.ShowModuleDetails(
                moduleDetails.OrderBy(module => module.ModuleDeploymentName),
                (ColumnTitle: "NAME", GetColumnValue: module => module.ModuleDeploymentName),
                (ColumnTitle: "MODULE", GetColumnValue: module => module.ModuleReference),
                (ColumnTitle: "STATUS", GetColumnValue: module => module.StackStatus),
                (ColumnTitle: "CORE-SERVICES", GetColumnValue: module => module.CoreServices?.ToUpperInvariant() ?? "N/A")
            );
        }

        private async Task UpdateStackParameters(Settings settings, TierModuleDetails module, Dictionary<string, string> parameters) {

            // keep all original parameter values except for 'LambdaSharpCoreServices'
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

            // retrieve name mappings for template
            var template = (await settings.CfnClient.GetTemplateAsync(new GetTemplateRequest {
                StackName = module.Stack.StackName
            })).TemplateBody;
            var nameMappings = new ModelManifestLoader(settings, module.Stack.StackName).GetNameMappingsFromTemplate(template);

            // create change-set
            var now = DateTime.UtcNow;
            var mostRecentStackEventId = await settings.CfnClient.GetMostRecentStackEventIdAsync(module.StackName);
            var changeSetName = $"{module.ModuleDeploymentName}-{now:yyyy-MM-dd-hh-mm-ss}";
            Console.WriteLine();
            Console.WriteLine($"=> Stack update initiated for {module.StackName}");
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
                var outcome = await settings.CfnClient.TrackStackUpdateAsync(module.StackName, response.StackId, mostRecentStackEventId, nameMappings, LogError);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack update finished");
                } else {
                    Console.WriteLine($"=> Stack update FAILED");
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
