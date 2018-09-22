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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Humidifier.Json;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliBuildCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("build", cmd => {
                cmd.HelpOption();
                cmd.Description = "Build LambdaSharp module";
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputCloudFormationFilePathOption = cmd.Option("--cf-output <FILE>", "(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // read settings and validate them
                    var settingsCollection = await initSettingsCallback();
                    if(!(settingsCollection?.Any() ?? false)) {
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
                    foreach(var settings in settingsCollection) {
                        if(await Build(
                            settings,
                            dryRun,
                            outputCloudFormationFilePathOption.Value() ?? Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                            skipAssemblyValidationOption.HasValue()
                        ) == null) {
                            break;
                        }
                    }
                });
            });
        }

        public async Task<Module> Build(
            Settings settings,
            DryRunLevel? dryRun,
            string outputCloudFormationFilePath,
            bool skipAssemblyValidation
        ) {
            try {
                if(!File.Exists(settings.ModuleSource)) {
                    AddError($"could not find '{settings.ModuleSource}'");
                }

                // read input file
                Console.WriteLine();
                Console.WriteLine($"Processing module: {settings.ModuleSource}");
                var source = await File.ReadAllTextAsync(settings.ModuleSource);

                // preprocess file
                var tokenStream = new ModelPreprocessor(settings).Preprocess(source);
                if(HasErrors) {
                    return null;
                }

                // parse yaml module file
                var module = new ModelParser().Parse(tokenStream);
                if(HasErrors) {
                    return null;
                }

                // reset settings when the 'LambdaSharp` module is being deployed
                settings.ModuleName = module.Name;
                if(module.Name == "LambdaSharp") {
                    settings.Reset();
                } else {
                    await PopulateEnvironmentSettingsAsync(settings);
                    if(settings.EnvironmentVersion == null) {

                        // check that LambdaSharp Environment & Tool versions match
                        AddError("could not determine the LambdaSharp Environment version", new LambdaSharpDeploymentTierSetupException(settings.Tier));
                    } else {
                        if(settings.EnvironmentVersion != settings.ToolVersion) {
                            AddError($"LambdaSharp Tool (v{settings.ToolVersion}) and Environment (v{settings.EnvironmentVersion}) versions do not match", new LambdaSharpDeploymentTierSetupException(settings.Tier));
                        }
                    }
                }

                // validate module
                new ModelValidation(settings).Process(module);
                if(HasErrors) {
                    return null;
                }

                // resolve all imported parameters
                new ModelImportProcessor(settings).Process(module);

                // package all functions
                new ModelFunctionPackager(settings).Process(
                    module,
                    settings.ToolVersion,
                    skipCompile: dryRun == DryRunLevel.CloudFormation,
                    skipAssemblyValidation: skipAssemblyValidation
                );

                // package all files
                new ModelFilesPackager(settings).Process(module);

                // compile module file
                var compiledModule = new ModelConverter(settings).Process(module);
                if(HasErrors) {
                    return null;
                }

                // generate cloudformation template
                var stack = new ModelGenerator(settings).Generate(compiledModule);

                // upload assets
                if(HasErrors) {
                    return null;
                }

                // serialize stack to disk
                var template = new JsonStackSerializer().Serialize(stack);
                var outputCloudFormationDirectory = Path.GetDirectoryName(outputCloudFormationFilePath);
                if(outputCloudFormationDirectory != "") {
                    Directory.CreateDirectory(outputCloudFormationDirectory);
                }
                File.WriteAllText(outputCloudFormationFilePath, template);
                Console.WriteLine("=> Module processing done");
                return compiledModule;
            } catch(Exception e) {
                AddError(e);
                return null;
            }
        }
    }
}
