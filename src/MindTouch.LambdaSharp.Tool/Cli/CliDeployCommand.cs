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
using Humidifier.Json;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliDeployCommand : ACliCommand {

        //--- Class Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("deploy", cmd => {
                cmd.HelpOption();
                cmd.Description = "Deploy LambdaSharp module";
                var altModuleNameOption = cmd.Option("--name", "(optional) Specify an alternate module name for the deployment (default: module name)", CommandOptionType.SingleOrNoValue);
                var inputsFileOption = cmd.Option("--inputs|-I <FILE>", "(optional) Specify module inputs (default: none)", CommandOptionType.SingleValue);
                var inputKeyOption = cmd.Option("--key <PARAMETER>=<VALUE>", "(optional) Specify module input key with value (default: none)", CommandOptionType.MultipleValue);
                var skipFunctionBuildOption = cmd.Option("--skip-function-build", "(optional) Do not build the function projects", CommandOptionType.NoValue);
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputCloudFormationFilePathOption = cmd.Option("--cf-output <FILE>", "(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
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

                    // reading module inputs
                    var inputs = new Dictionary<string, string>();
                    if(inputsFileOption.HasValue()) {
                        inputs = ReadInputParametersFiles(inputsFileOption.Value());
                        if(HasErrors) {
                            return;
                        }
                    }
                    foreach(var inputKeyValue in inputKeyOption.Values) {
                        var keyValue = inputKeyValue.Split('=', 2);
                        if(keyValue.Length != 2) {
                            AddError($"bad format for input parameter: {inputKeyValue}");
                        } else {
                            inputs[keyValue[0]] = keyValue[1];
                        }
                    }
                    if(HasErrors) {
                        return;
                    }

                    // deploying module
                    Console.WriteLine($"Readying module for deployment tier '{settingsCollection.First().Tier}'");
                    foreach(var settings in settingsCollection) {
                        if(!await Deploy(
                            settings,
                            dryRun,
                            outputCloudFormationFilePathOption.Value() ?? Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                            altModuleNameOption.Value(),
                            allowDataLossOption.HasValue(),
                            protectStackOption.HasValue(),
                            skipAssemblyValidationOption.HasValue(),
                            skipFunctionBuildOption.HasValue() || (dryRun == DryRunLevel.CloudFormation),
                            inputs
                        )) {
                            break;
                        }
                    }
                });
            });
        }

        public async Task<bool> Deploy(
            Settings settings,
            DryRunLevel? dryRun,
            string outputCloudFormationFilePath,
            string altModuleName,
            bool allowDataLoos,
            bool protectStack,
            bool skipAssemblyValidation,
            bool skipFunctionBuild,
            Dictionary<string, string> inputs
        ) {
            if(!await new CliBuildCommand().Build(
                settings,
                outputCloudFormationFilePath,
                skipAssemblyValidation,
                skipFunctionBuild
            )) {
                return false;
            }
            var manifestFile = File.ReadAllText(Path.Combine(settings.OutputDirectory, "manifest.json"));
            var manifest = JsonConvert.DeserializeObject<ModuleManifest>(manifestFile);

            // reset settings when the 'LambdaSharp` module is being deployed
            await PopulateEnvironmentSettingsAsync(settings);
            if(!manifest.HasPragma("no-environment-check")) {
                if(settings.EnvironmentVersion == null) {

                    // check that LambdaSharp Environment & Tool versions match
                    AddError("could not determine the LambdaSharp Environment version", new LambdaSharpDeploymentTierSetupException(settings.Tier));
                } else {
                    if(settings.EnvironmentVersion != settings.ToolVersion) {
                        AddError($"LambdaSharp tool (v{settings.ToolVersion}) and environment (v{settings.EnvironmentVersion}) versions do not match", new LambdaSharpDeploymentTierSetupException(settings.Tier));
                    }
                }
            }
            if(HasErrors) {
                return false;
            }

            // upload assets
            await new ModelUploader(settings).ProcessAsync(manifest, settings.OutputDirectory, skipUpload: dryRun != null);

            // serialize stack to disk
            var result = true;
            try {
                if(dryRun == null) {
                    result = await new ModelUpdater(settings).DeployAsync(
                        manifest,
                        altModuleName,
                        outputCloudFormationFilePath,
                        allowDataLoos,
                        protectStack,
                        inputs
                    );
                    if(settings.OutputDirectory == settings.WorkingDirectory) {
                        try {
                            File.Delete(outputCloudFormationFilePath);
                        } catch { }
                    }
                }
            } catch(Exception e) {
                AddError(e);
                return false;
            }
            return result;
        }

        private Dictionary<string, string> ReadInputParametersFiles(string filename) {
            if(!File.Exists(filename)) {
                AddError("cannot find inputs file");
                return null;
            }
            switch(Path.GetExtension(filename).ToLowerInvariant()) {
            case ".yml":
            case ".yaml":
                try {
                    var parameters = new DeserializerBuilder()
                        .WithNamingConvention(new PascalCaseNamingConvention())
                        .Build()
                        .Deserialize<Dictionary<string, object>>(File.ReadAllText(filename));
                    return parameters.ToDictionary(
                        kv => kv.Key,
                        kv => (kv.Value is string text)
                            ? text
                            : string.Join(",", (IList<object>)kv.Value)
                    );
                } catch(YamlDotNet.Core.YamlException e) {
                    AddError($"parsing error near {e.Message}");
                } catch(Exception e) {
                    AddError(e);
                }
                return null;
            default:
                AddError("incompatible inputs file format");
                return null;
            }

        }
    }
}
