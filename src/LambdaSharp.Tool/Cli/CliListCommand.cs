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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {

    public class CliListCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("list", cmd => {
                cmd.HelpOption();
                cmd.Description = "List deployed LambdaSharp modules";

                // command options
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    await List(settings);
                });
            });
        }

        public async Task List(Settings settings) {
            var cfClient = new AmazonCloudFormationClient();
            var request = new ListStacksRequest {
                StackStatusFilter = new List<string> {
                    "CREATE_IN_PROGRESS",
                    "CREATE_FAILED",
                    "CREATE_COMPLETE",
                    "ROLLBACK_IN_PROGRESS",
                    "ROLLBACK_FAILED",
                    "ROLLBACK_COMPLETE",
                    "DELETE_IN_PROGRESS",
                    "DELETE_FAILED",
                    // "DELETE_COMPLETE",
                    "UPDATE_IN_PROGRESS",
                    "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS",
                    "UPDATE_COMPLETE",
                    "UPDATE_ROLLBACK_IN_PROGRESS",
                    "UPDATE_ROLLBACK_FAILED",
                    "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS",
                    "UPDATE_ROLLBACK_COMPLETE",
                    "REVIEW_IN_PROGRESS"
                }
            };

            // fetch all stacks
            var prefix = $"{settings.Tier}-";
            var stacks = new List<StackSummary>();
            do {
                var response = await cfClient.ListStacksAsync(request);
                stacks.AddRange(response.StackSummaries.Where(summary => summary.StackName.StartsWith(prefix, StringComparison.Ordinal)));
                request.NextToken = response.NextToken;
            } while(request.NextToken != null);

            // sort and format output
            if(stacks.Any()) {
                var moduleNameWidth = stacks.Max(stack => stack.StackName.Length) + 4 - prefix.Length;
                var statusWidth = stacks.Max(stack => stack.StackStatus.ToString().Length) + 4;
                Console.WriteLine();
                Console.WriteLine($"{"MODULE".PadRight(moduleNameWidth)}{"STATUS".PadRight(statusWidth)}DATE");
                foreach(var summary in stacks.Select(stack => new {
                    ModuleName = stack.StackName.Substring(prefix.Length),
                    StackStatus = stack.StackStatus,
                    Date = (stack.LastUpdatedTime > stack.CreationTime) ? stack.LastUpdatedTime : stack.CreationTime
                }).OrderBy(summary => summary.Date)) {
                    Console.WriteLine($"{summary.ModuleName.PadRight(moduleNameWidth)}{("[" + summary.StackStatus + "]").PadRight(statusWidth)}{summary.Date:yyyy-MM-dd HH:mm:ss}");
                }
                Console.WriteLine();
                Console.WriteLine($"Found {stacks.Count:N0} modules for deployment tier '{settings.Tier}'");
            } else {
                Console.WriteLine();
                Console.WriteLine($"Found no modules for deployment tier '{settings.Tier}'");
            }
        }
    }
}
