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
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.Tool.Cli.Tier;
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
                AddStandardCommandOptions(cmd);
                cmd.OnExecute(async () => {
                    ExecuteCommandActions(cmd);
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    await ListAsync(settings);
                });
            });
        }

        public async Task ListAsync(Settings settings) {
            if(!await PopulateDeploymentTierSettingsAsync(settings, requireBucketName: false, requireCoreServices: false, requireVersionCheck: false)) {
                return;
            }

            // gather module details
            var tierManager = new TierManager(settings);
            var moduleDetails = await tierManager.GetModuleDetailsAsync();

            // sort and format output
            if(moduleDetails.Any()) {
                Console.WriteLine();
                Console.WriteLine($"Found {moduleDetails.Count():N0} modules for deployment tier {Settings.InfoColor}{tierManager.TierName}{Settings.ResetColor}");
                Console.WriteLine();
                tierManager.ShowModuleDetails(
                    moduleDetails.OrderBy(module => module.DeploymentDate),
                    (ColumnTitle: "NAME", GetColumnValue: module => module.ModuleDeploymentName),
                    (ColumnTitle: "MODULE", GetColumnValue: module => module.ModuleReference),
                    (ColumnTitle: "STATUS", GetColumnValue: module => module.StackStatus),
                    (ColumnTitle: "DATE", GetColumnValue: module => module.DeploymentDate.ToString("yyyy-MM-dd HH:mm:ss"))
                );
            } else {
                Console.WriteLine();
                Console.WriteLine($"Found no modules for deployment tier {Settings.InfoColor}{tierManager.TierName}{Settings.ResetColor}");
            }
        }
    }
}
