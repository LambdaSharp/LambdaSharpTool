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
using Newtonsoft.Json;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliBuildCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("build", cmd => {
                cmd.HelpOption();
                cmd.Description = "Build LambdaSharp module";
                var skipFunctionBuildOption = cmd.Option("--skip-function-build", "(optional) Do not build the function projects", CommandOptionType.NoValue);
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputCloudFormationFilePathOption = cmd.Option("--cf-output <FILE>", "(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);
                var verboseLevelOption = cmd.Option("--verbose|-V:<LEVEL>", "(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)", CommandOptionType.SingleOrNoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // initialize logging level
                    if(verboseLevelOption.HasValue()) {
                        if(!TryParseEnumOption(verboseLevelOption, VerboseLevel.Detailed, out Settings.VerboseLevel)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by `TryParseEnumOption`
                            return;
                        }
                    }

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
                        if(!await Build(
                            settings,
                            outputCloudFormationFilePathOption.Value() ?? Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                            skipAssemblyValidationOption.HasValue(),
                            skipFunctionBuildOption.HasValue() || (dryRun == DryRunLevel.CloudFormation)
                        )) {
                            break;
                        }
                    }
                });
            });
        }

        public async Task<bool> Build(
            Settings settings,
            string outputCloudFormationFilePath,
            bool skipAssemblyValidation,
            bool skipFunctionBuild
        ) {
            try {
                if(!File.Exists(settings.ModuleSource)) {
                    AddError($"could not find '{settings.ModuleSource}'");
                    return false;
                }

                // read input file
                Console.WriteLine();
                Console.WriteLine($"Processing module: {settings.ModuleSource}");
                var source = await File.ReadAllTextAsync(settings.ModuleSource);

                // preprocess file
                var tokenStream = new ModelPreprocessor(settings).Preprocess(source);
                if(HasErrors) {
                    return false;
                }

                // parse yaml module file
                var parsedModule = new ModelParser(settings).Parse(tokenStream);
                if(HasErrors) {
                    return false;
                }

                // validate module
                new ModelValidation(settings).Process(parsedModule);
                if(HasErrors) {
                    return false;
                }

                // TODO (2018-10-04, bjorg): refactor all model processing to use the strict model instead of the parsed model

                // package all functions
                new ModelFunctionPackager(settings).Process(
                    parsedModule,
                    settings.ToolVersion,
                    skipCompile: skipFunctionBuild,
                    skipAssemblyValidation: skipAssemblyValidation
                );

                // package all files
                new ModelFilesPackager(settings).Process(parsedModule);

                // compile module file
                var module = new ModelConverter(settings).Process(parsedModule);
                if(HasErrors) {
                    return false;
                }

                // resolve all parameter references
                new ModelReferenceResolver(settings).Resolve(module);
                if(HasErrors) {
                    return false;
                }

                // generate & save cloudformation template
                var template = new ModelGenerator(settings).Generate(module);
                if(HasErrors) {
                    return false;
                }
                var outputCloudFormationDirectory = Path.GetDirectoryName(outputCloudFormationFilePath);
                if(outputCloudFormationDirectory != "") {
                    Directory.CreateDirectory(outputCloudFormationDirectory);
                }
                File.WriteAllText(outputCloudFormationFilePath, template);

                // create & save module manifest
                var assets = new List<ModuleManifestAsset>();
                assets.AddRange(module.Functions.Where(f => f.PackagePath != null).Select(f => new ModuleManifestAsset {
                    Type = "Lambda function",
                    Path = Path.GetRelativePath(settings.OutputDirectory, f.PackagePath)
                }));
                assets.AddRange(module.Parameters.OfType<PackageParameter>().Select(p => new ModuleManifestAsset {
                    Type = "package",
                    Path = Path.GetRelativePath(settings.OutputDirectory, p.PackagePath)
                }));
                var manifest = new ModuleManifest {
                    Name = module.Name,
                    Version = module.Version,
                    GitSha = settings.GitSha,
                    Pragmas = module.Pragmas,
                    Assets = assets
                };
                var manifestFilePath = Path.Combine(settings.OutputDirectory, "manifest.json");
                if(!Directory.Exists(settings.OutputDirectory)) {
                    Directory.CreateDirectory(settings.OutputDirectory);
                }
                File.WriteAllText(manifestFilePath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
                Console.WriteLine("=> Module processing done");
                return true;
            } catch(Exception e) {
                AddError(e);
                return false;
            }
        }
    }
}
