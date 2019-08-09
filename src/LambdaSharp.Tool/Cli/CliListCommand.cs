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
            await PopulateRuntimeSettingsAsync(settings);

            // gather module details
            var tierManager = new TierManager(settings);
            var moduleDetails = await tierManager.GetModuleDetailsAsync();

            // sort and format output
            tierManager.ShowModuleDetails(
                moduleDetails.OrderBy(module => module.DeploymentDate),
                (ColumnTitle: "NAME", GetColumnValue: module => module.ModuleDeploymentName),
                (ColumnTitle: "MODULE", GetColumnValue: module => module.ModuleReference),
                (ColumnTitle: "STATUS", GetColumnValue: module => module.StackStatus),
                (ColumnTitle: "DATE", GetColumnValue: module => module.DeploymentDate.ToString("yyyy-MM-dd HH:mm:ss"))
            );
        }
    }
}
