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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humidifier.Json;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliDeployCommand : ACliCommand {

        //--- Class Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("deploy", cmd => {
                cmd.HelpOption();
                cmd.Description = "Deploy LambdaSharp module";
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputFilename = cmd.Option("--output <FILE>", "(optional) Name of generated CloudFormation template file (default: cloudformation.json)", CommandOptionType.SingleValue);
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // read settings and validate them
                    var settingsCollection = await initSettingsCallback();
                    if(settingsCollection == null) {
                        return;
                    }
                    foreach(var settings in settingsCollection) {
                        if((settings.ModuleFileName == null) && File.Exists("Deploy.yml")) {
                            settings.ModuleFileName = Path.GetFullPath("Deploy.yml");
                        } else if((settings.ModuleFileName == null) || !File.Exists(settings.ModuleFileName)) {
                            AddError($"could not find '{settings.ModuleFileName ?? Path.GetFullPath("Deploy.yml")}'");
                        }
                    }
                    if(ErrorCount > 0) {
                        return;
                    }
                    DryRunLevel? dryRun = null;
                    if(dryRunOption.HasValue()) {
                        DryRunLevel value;
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, out value)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by `TryParseEnumOption`
                            return;
                        }
                        dryRun = value;
                    }
                    Console.WriteLine($"Readying module for deployment tier '{settingsCollection.First().Tier}'");
                    foreach(var settings in settingsCollection) {
                        if(!await Deploy(
                            settings,
                            dryRun,
                            outputFilename.Value() ?? "cloudformation.json",
                            allowDataLossOption.HasValue(),
                            protectStackOption.HasValue()
                        )) {
                            break;
                        }
                    }
                });
            });
        }

        private async Task<bool> Deploy(
            Settings settings,
            DryRunLevel? dryRun,
            string outputFilename,
            bool allowDataLoos,
            bool protectStack
        ) {
            var stopwatch = Stopwatch.StartNew();

            // read input file
            Console.WriteLine();
            Console.WriteLine($"Processing module: {settings.ModuleFileName}");
            var source = await File.ReadAllTextAsync(settings.ModuleFileName);

            // preprocess file
            var tokenStream = new ModelPreprocessor(settings).Preprocess(source);
            if(ErrorCount > 0) {
                return false;
            }

            // parse yaml module file
            var module = new ModelParser(settings).Parse(tokenStream, skipCompile: dryRun == DryRunLevel.CloudFormation);
            if(ErrorCount > 0) {
                return false;
            }

            // generate cloudformation template
            var stack = new ModelGenerator().Generate(module);
            if(ErrorCount > 0) {
                return false;
            }

            // serialize stack to disk
            var result = true;
            var outputPath = Path.Combine(settings.WorkingDirectory, outputFilename);
            var template = new JsonStackSerializer().Serialize(stack);
            File.WriteAllText(outputPath, template);
            if(dryRun == null) {
                result = await new StackUpdater().Deploy(module, template, allowDataLoos, protectStack);
                try {
                    File.Delete(outputPath);
                } catch { }
            }
            Console.WriteLine($"Done (duration: {stopwatch.Elapsed:c})");
            return result;
        }
    }
}
