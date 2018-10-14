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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliBundleCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("bundle", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create LambdaSharp module package";
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var packageNameOption = cmd.Option("--name", "TBD", CommandOptionType.SingleValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // read settings and validate them
                    var settingsCollection = await initSettingsCallback();
                    if(!(settingsCollection?.Any() ?? false)) {
                        return;
                    }
                    await Package(
                        settingsCollection,
                        Path.Combine(Directory.GetCurrentDirectory(), packageNameOption.Value() ?? "package.zip"),
                        skipAssemblyValidationOption.HasValue()
                    );
                });
            });
        }

        public async Task Package(
            IEnumerable<Settings> settingsCollection,
            string packageName,
            bool skipAssemblyValidation
        ) {
            File.Delete(packageName);

            // create deployment for all modules
            var buildCommand = new CliBuildCommand();
            var compiledModulesWithSettings = new List<(Module Module, Settings Settings)>();
            foreach(var settings in settingsCollection) {
                var compiledModule = await buildCommand.Build(
                    settings,
                    DryRunLevel.Everything,
                    Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                    skipAssemblyValidation,
                    skipFunctionBuild: false
                );
                if(compiledModule == null) {
                    return;
                }
                compiledModulesWithSettings.Add((compiledModule, settings));
            }

            // combine packages into an archive
            Console.WriteLine();
            Console.WriteLine("Creating bundle");
            var fileCount = 0;
            using(var packageStream = File.Create(packageName))
            using(var packageArchive = new ZipArchive(packageStream, ZipArchiveMode.Create)) {
                var moduleCount = 0;
                foreach(var compiledModuleWithSettings in compiledModulesWithSettings) {
                    ++moduleCount;
                    foreach(var file in Directory.GetFiles(compiledModuleWithSettings.Settings.OutputDirectory)) {
                        var filename = Path.GetFileName(file);
                        if(filename == "cloudformation.json") {
                            var suffix = settingsCollection.First().GitSha ?? ("UTC" + DateTime.UtcNow.ToString("yyyyMMddhhmmss"));
                            filename = $"cloudformation-{suffix}.json";
                        }
                        var archiveName = $"{moduleCount:0#}-{compiledModuleWithSettings.Module.Name}/{filename}";
                        packageArchive.CreateEntryFromFile(file, archiveName);
                        ++fileCount;
                    }
                }
            }
            Console.WriteLine($"=> Created package: {Path.GetRelativePath(Directory.GetCurrentDirectory(), packageName)} ({fileCount:N0} files)");
        }
    }
}
