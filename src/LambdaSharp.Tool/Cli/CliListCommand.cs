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

            // fetch all stacks
            var prefix = $"{settings.Tier}-";
            var stacks = new List<Stack>();
            var request = new DescribeStacksRequest();
            do {
                var response = await settings.CfnClient.DescribeStacksAsync(request);
                stacks.AddRange(response.Stacks.Where(summary => summary.StackName.StartsWith(prefix, StringComparison.Ordinal)));
                request.NextToken = response.NextToken;
            } while(request.NextToken != null);

            // sort and format output
            if(stacks.Any()) {

                // gather summaries
                var summaries = stacks.Select(stack => new {
                    ModuleName = stack.StackName.Substring(prefix.Length),
                    StackStatus = stack.StackStatus.ToString(),
                    Date = (stack.LastUpdatedTime > stack.CreationTime) ? stack.LastUpdatedTime : stack.CreationTime,
                    Description = stack.Description,
                    ModuleReference = stack.Outputs.FirstOrDefault(o => o.OutputKey == "Module")?.OutputValue
                }).OrderBy(summary => summary.Date).ToList();

                var moduleNameWidth = summaries.Max(stack => stack.ModuleName.Length) + 4;
                var moduleReferenceWidth = summaries.Max(stack => stack.ModuleReference.Length + 4);
                var statusWidth = summaries.Max(stack => stack.StackStatus.Length) + 4;
                Console.WriteLine();
                Console.WriteLine($"Found {stacks.Count:N0} modules for deployment tier '{settings.Tier}'");
                Console.WriteLine();
                Console.WriteLine($"{"NAME".PadRight(moduleNameWidth)}{"MODULE".PadRight(moduleReferenceWidth)}{"STATUS".PadRight(statusWidth)}DATE");
                foreach(var summary in summaries) {
                    Console.WriteLine($"{summary.ModuleName.PadRight(moduleNameWidth)}{summary.ModuleReference.PadRight(moduleReferenceWidth)}{summary.StackStatus.PadRight(statusWidth)}{summary.Date:yyyy-MM-dd HH:mm:ss}");
                }
            } else {
                Console.WriteLine();
                Console.WriteLine($"Found no modules for deployment tier '{settings.Tier}'");
            }
        }
    }
}
