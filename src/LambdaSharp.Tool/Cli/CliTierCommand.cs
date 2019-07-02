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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli.Tier {
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class CliTierCommand : ACliCommand {

        //--- Types ---
        private class ModuleSummary {

            //--- Properties ---
            public string StackName { get; set; }
            public string ModuleDeploymentName { get; set; }
            public string StackStatus { get; set; }
            public DateTime DeploymentDate { get; set; }
            public Stack Stack { get; set; }
            public string ModuleReference { get; set; }
            public string CoreServices { get; set; }

        }

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("tier", cmd => {
                cmd.HelpOption();
                cmd.Description = "Update settings module in LambdaSharp tier";

                // function sub-command
                cmd.Command("coreservices", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Enable/Disable LambdaSharp.Core services for all modules in tier";
                    var enableOption = subCmd.Option("--enable", "(optional) Enable LambdaSharp.Core services for all modules", CommandOptionType.NoValue);
                    var disableOption = subCmd.Option("--disable", "(optional) Disable LambdaSharp.Core services for all modules", CommandOptionType.NoValue);
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        var settings = await initSettingsCallback();
                        if(enableOption.HasValue() && disableOption.HasValue()) {
                            LogError("--enable and --disable options are mutually exclusive");
                            return;
                        }
                        bool? enable = null;
                        if(enableOption.HasValue()) {
                            enable = true;
                        } else if(disableOption.HasValue()) {
                            enable = false;
                        }
                        await UpdateCoreServicesAsync(settings, enable);
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        private async Task UpdateCoreServicesAsync(Settings settings, bool? enable) {

            // fetch all stacks
            var prefix = $"{settings.Tier}-";

            // gather summaries
            var summaries = await GetModuleSummaries(settings);

            // check if tier has any stacks
            if(summaries.Any()) {
                ShowSummaries(settings, summaries);

                // validate that all modules in tier can enable/disable core services
                if(enable.HasValue) {
                    foreach(var summary in summaries.Where(s => s.CoreServices == null)) {
                        LogError($"${summary.ModuleDeploymentName} does not support enabling/disabling LambdaSharp.Core services");
                    }
                }
                if(!enable.HasValue || HasErrors) {
                    return;
                }

                // update core services for each affected module
                var coreServicesParameter = enable.Value ? "Enabled" : "Disabled";
                var parameters = new Dictionary<string, string> {
                    ["LambdaSharpCoreServices"] = coreServicesParameter
                };
                foreach(var summary in summaries
                    .Where(summary => summary.Stack.RootId == null)
                    .Where(s => (s.CoreServices != coreServicesParameter))
                ) {
                    await UpdateStackParameters(settings, summary, parameters);
                }

                // show updated state
                summaries = await GetModuleSummaries(settings);
                ShowSummaries(settings, summaries);
            } else {
                Console.WriteLine();
                Console.WriteLine($"Found no modules for deployment tier '{settings.Tier}'");
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

        private async Task<IEnumerable<ModuleSummary>> GetModuleSummaries(Settings settings) {
            var prefix = $"{settings.Tier}-";
            var stacks = new List<Stack>();
            var request = new DescribeStacksRequest();
            do {
                var response = await settings.CfnClient.DescribeStacksAsync(request);
                stacks.AddRange(response.Stacks.Where(summary => summary.StackName.StartsWith(prefix, StringComparison.Ordinal)));
                request.NextToken = response.NextToken;
            } while(request.NextToken != null);
            return stacks.Select(stack => new ModuleSummary {
                StackName = stack.StackName,
                ModuleDeploymentName = stack.StackName.Substring(prefix.Length),
                StackStatus = stack.StackStatus.ToString(),
                DeploymentDate = (stack.LastUpdatedTime > stack.CreationTime) ? stack.LastUpdatedTime : stack.CreationTime,
                Stack = stack,
                ModuleReference = stack.Outputs.FirstOrDefault(o => o.OutputKey == "Module")?.OutputValue ?? "",
                CoreServices = stack.Parameters
                    .FirstOrDefault(parameter => parameter.ParameterKey == "LambdaSharpCoreServices")
                    ?.ParameterValue
            })
                .Where(summary => !summary.ModuleReference.StartsWith("LambdaSharp.Core", StringComparison.Ordinal))
                .OrderBy(summary => summary.DeploymentDate)
                .ToList();
        }

        private void ShowSummaries(Settings settings, IEnumerable<ModuleSummary> summaries) {

            // validate that all modules in tier can enable/disable core services
            var moduleNameWidth = summaries.Max(stack => stack.ModuleDeploymentName.Length) + 4;
            var moduleReferenceWidth = summaries.Max(stack => stack.ModuleReference.Length + 4);
            var statusWidth = summaries.Max(stack => stack.StackStatus.Length) + 4;
            Console.WriteLine();
            Console.WriteLine($"Found {summaries.Count():N0} modules for deployment tier '{settings.Tier}'");
            Console.WriteLine();
            Console.WriteLine($"{"NAME".PadRight(moduleNameWidth)}{"MODULE".PadRight(moduleReferenceWidth)}{"STATUS".PadRight(statusWidth)}CORE-SERVICES");
            foreach(var summary in summaries) {
                Console.WriteLine($"{summary.ModuleDeploymentName.PadRight(moduleNameWidth)}{summary.ModuleReference.PadRight(moduleReferenceWidth)}{summary.StackStatus.PadRight(statusWidth)}{summary.CoreServices?.ToUpperInvariant() ?? "N/A"}");
            }
        }

        private async Task UpdateStackParameters(Settings settings, ModuleSummary summary, Dictionary<string, string> parameters) {

            // set optional notification topics for cloudformation operations
            var notificationArns =  new List<string>();
            if(settings.DeploymentNotificationsTopic != null) {
                notificationArns.Add(settings.DeploymentNotificationsTopic);
            }

            // keep all original parameter values except for 'LambdaSharpCoreServices'
            var stackParameters = summary.Stack.Parameters
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

            // create change-set
            var now = DateTime.UtcNow;
            var mostRecentStackEventId = await settings.CfnClient.GetMostRecentStackEventIdAsync(summary.StackName);
            var changeSetName = $"{summary.ModuleDeploymentName}-{now:yyyy-MM-dd-hh-mm-ss}";
            Console.WriteLine();
            Console.WriteLine($"=> Stack update initiated for {summary.StackName}");
            var response = await settings.CfnClient.CreateChangeSetAsync(new CreateChangeSetRequest {
                Capabilities = summary.Stack.Capabilities,
                ChangeSetName = changeSetName,
                ChangeSetType = (mostRecentStackEventId != null) ? ChangeSetType.UPDATE : ChangeSetType.CREATE,
                Description = $"Stack parameters update for {summary.ModuleReference}",
                NotificationARNs = notificationArns,
                Parameters = stackParameters,
                StackName = summary.StackName,
                UsePreviousTemplate = true,
                Tags = summary.Stack.Tags
            });
            try {
                var changes = await WaitForChangeSetAsync(settings, response.Id);
                if(changes == null) {
                    LogError($"unable to apply changes to ${summary.ModuleDeploymentName}");
                    return;
                }
                if(!changes.Any()) {
                    Console.WriteLine("=> No stack update required");
                    return;
                }

                // execute change-set
                await settings.CfnClient.ExecuteChangeSetAsync(new ExecuteChangeSetRequest {
                    ChangeSetName = changeSetName,
                    StackName = summary.StackName
                });
                var outcome = await settings.CfnClient.TrackStackUpdateAsync(summary.StackName, mostRecentStackEventId, logError: LogError);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack update finished");
                } else {
                    Console.WriteLine($"=> Stack update FAILED");
                    LogError($"unable to update {summary.ModuleDeploymentName}");
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
