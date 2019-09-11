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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using LambdaSharp.Tool.Cli.Build;
using LambdaSharp.Tool.Cli.Deploy;
using LambdaSharp.Tool.Cli.Publish;

namespace LambdaSharp.Tool.Cli {

    public class CliBuildPublishDeployCommand : ACliCommand {

        //--- Class Methods ---
        public static CommandOption AddSkipAssemblyValidationOption(CommandLineApplication cmd)
            => cmd.Option("--no-assembly-validation", "(optional) Disable validating LambdaSharp assemblies", CommandOptionType.NoValue);

        public static CommandOption AddSkipDependencyValidationOption(CommandLineApplication cmd)
            => cmd.Option("--no-dependency-validation", "(optional) Disable validating LambdaSharp module dependencies", CommandOptionType.NoValue);

        public static CommandOption AddBuildConfigurationOption(CommandLineApplication cmd)
            => cmd.Option("--configuration|-c <CONFIGURATION>", "(optional) Build configuration for function projects (default: \"Release\")", CommandOptionType.SingleValue);

        public static CommandOption AddGitShaOption(CommandLineApplication cmd)
            => cmd.Option("--git-sha <VALUE>", "(optional) Git SHA of most recent git commit (default: invoke 'git rev-parse HEAD' command)", CommandOptionType.SingleValue);

        public static CommandOption AddGitBranchOption(CommandLineApplication cmd)
            => cmd.Option("--git-branch <VALUE>", "(optional) Git branch name (default: invoke 'git rev-parse --abbrev-ref HEAD' command)", CommandOptionType.SingleValue);

        public static CommandOption AddOutputPathOption(CommandLineApplication cmd)
            => cmd.Option("--output|-o <DIRECTORY>", "(optional) Path to output directory (default: bin)", CommandOptionType.SingleValue);

        public static CommandOption AddSelectorOption(CommandLineApplication cmd)
            => cmd.Option("--selector <NAME>", "(optional) Selector for resolving conditional compilation choices in module", CommandOptionType.SingleValue);

        public static CommandOption AddCloudFormationOutputOption(CommandLineApplication cmd)
            => cmd.Option("--cfn-output <PATH>", "(optional) Output location for generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);

        public static CommandOption AddDryRunOption(CommandLineApplication cmd)
            => cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output artifacts without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);

        public static CommandOption AddForcePublishOption(CommandLineApplication cmd)
            => cmd.Option("--force-publish", "(optional) Publish modules and their artifacts even when no changes were detected", CommandOptionType.NoValue);

        public static CommandOption AddModuleVersionOption(CommandLineApplication cmd)
            => cmd.Option("--module-version", "(optional) Override the module version", CommandOptionType.SingleValue);

        public static CommandOption AddModuleBuildDateOption(CommandLineApplication cmd)
            => cmd.Option("--module-build-date", "(optional) Override module build date [yyyyMMddHHmmss]", CommandOptionType.SingleValue);

        public static Dictionary<string, string> ReadInputParametersFiles(Settings settings, string filename) {
            if(!File.Exists(filename)) {
                LogError("cannot find parameters file");
                return null;
            }
            return new ParameterFileReader(settings, filename).ReadInputParametersFiles();
        }

        //--- Methods ---
        public void Register(CommandLineApplication app) {

            // NOTE (2018-10-16, bjorg): we're keeping the build/publish/deploy commands in a single
            //  class to make it easier to chain these commands consistently.

            // add 'build' command
            app.Command("build", cmd => {
                cmd.HelpOption();
                cmd.Description = "Build LambdaSharp module";

                // build options
                var modulesArgument = cmd.Argument("<NAME>", "(optional) Path to module definition/folder (default: Module.yml)", multipleValues: true);
                var skipAssemblyValidationOption = AddSkipAssemblyValidationOption(cmd);
                var skipDependencyValidationOption = AddSkipDependencyValidationOption(cmd);
                var buildConfigurationOption = AddBuildConfigurationOption(cmd);
                var gitShaOption = AddGitShaOption(cmd);
                var gitBranchOption = AddGitBranchOption(cmd);
                var outputDirectoryOption = AddOutputPathOption(cmd);
                var selectorOption = AddSelectorOption(cmd);
                var outputCloudFormationPathOption = AddCloudFormationOutputOption(cmd);
                var moduleVersionOption = AddModuleVersionOption(cmd);
                var moduleBuildDateOption = AddModuleBuildDateOption(cmd);

                // misc options
                var dryRunOption = AddDryRunOption(cmd);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // read settings and validate them
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    DryRunLevel? dryRun = null;
                    if(dryRunOption.HasValue()) {
                        DryRunLevel value;
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, DryRunLevel.Everything, out value)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                            return;
                        }
                        dryRun = value;
                    }

                    // check if one or more arguments have been specified
                    var arguments = modulesArgument.Values.Any()
                        ? modulesArgument.Values
                        : new List<string> { Directory.GetCurrentDirectory() };

                    // check if a module version number is supplied
                    VersionInfo moduleVersion = null;
                    if(moduleVersionOption.HasValue()) {
                        if(!VersionInfo.TryParse(moduleVersionOption.Value(), out moduleVersion)) {
                            LogError("--module-version is not a valid version number");
                            return;
                        }
                    }

                    // check if a module build time is supplied
                    if(moduleBuildDateOption.HasValue()) {
                        if(!DateTime.TryParseExact(moduleBuildDateOption.Value(), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var moduleBuildDate)) {
                            LogError("--module-build-date is not a valid date-time");
                            return;
                        }
                        settings.UtcNow = moduleBuildDate;
                    }

                    // run build step
                    foreach(var argument in arguments) {
                        string moduleSource;
                        if(Directory.Exists(argument)) {

                            // append default module filename
                            moduleSource = Path.Combine(Path.GetFullPath(argument), "Module.yml");
                        } else {
                            moduleSource = Path.GetFullPath(argument);
                        }
                        settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                        settings.OutputDirectory = outputDirectoryOption.HasValue()
                            ? Path.GetFullPath(outputDirectoryOption.Value())
                            : Path.Combine(settings.WorkingDirectory, "bin");
                        settings.NoDependencyValidation = skipDependencyValidationOption.HasValue();
                        if(!await BuildStepAsync(
                            settings,
                            GetOutputFilePath(settings, outputCloudFormationPathOption, moduleSource),
                            skipAssemblyValidationOption.HasValue(),
                            dryRun == DryRunLevel.CloudFormation,
                            gitShaOption.Value() ?? GetGitShaValue(settings.WorkingDirectory),
                            gitBranchOption.Value() ?? GetGitBranch(settings.WorkingDirectory, showWarningOnFailure: false),
                            buildConfigurationOption.Value() ?? "Release",
                            selectorOption.Value(),
                            moduleSource,
                            moduleVersion
                        )) {
                            break;
                        }
                    }
                });
            });

            // add 'publish' command
            app.Command("publish", cmd => {
                cmd.HelpOption();
                cmd.Description = "Publish LambdaSharp module";

                // publish options
                var forcePublishOption = AddForcePublishOption(cmd);
                var moduleOriginOption = cmd.Option("--module-origin", "(optional) Set alternative module origin when publishing", CommandOptionType.SingleValue);

                // build options
                var compiledModulesArgument = cmd.Argument("<NAME>", "(optional) Path to module or artifacts folder (default: Module.yml)", multipleValues: true);
                var skipAssemblyValidationOption = AddSkipAssemblyValidationOption(cmd);
                var skipDependencyValidationOption = AddSkipDependencyValidationOption(cmd);
                var buildConfigurationOption = AddBuildConfigurationOption(cmd);
                var gitShaOption = AddGitShaOption(cmd);
                var gitBranchOption = AddGitBranchOption(cmd);
                var outputDirectoryOption = AddOutputPathOption(cmd);
                var selectorOption = AddSelectorOption(cmd);
                var outputCloudFormationPathOption = AddCloudFormationOutputOption(cmd);
                var moduleVersionOption = AddModuleVersionOption(cmd);
                var moduleBuildDateOption = AddModuleBuildDateOption(cmd);

                // misc options
                var dryRunOption = AddDryRunOption(cmd);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // read settings and validate them
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    DryRunLevel? dryRun = null;
                    if(dryRunOption.HasValue()) {
                        DryRunLevel value;
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, DryRunLevel.Everything, out value)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                            return;
                        }
                        dryRun = value;
                    }

                    // check if one or more arguments have been specified
                    var arguments = compiledModulesArgument.Values.Any()
                        ? compiledModulesArgument.Values
                        : new List<string> { Directory.GetCurrentDirectory() };

                    // check if a module version number is supplied
                    VersionInfo moduleVersion = null;
                    if(moduleVersionOption.HasValue()) {
                        if(!VersionInfo.TryParse(moduleVersionOption.Value(), out moduleVersion)) {
                            LogError("--module-version is not a valid version number");
                            return;
                        }
                    }

                    // check if a module build time is supplied
                    if(moduleBuildDateOption.HasValue()) {
                        if(!DateTime.TryParseExact(moduleBuildDateOption.Value(), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var moduleBuildDate)) {
                            LogError("--module-build-date is not a valid date-time");
                            return;
                        }
                        settings.UtcNow = moduleBuildDate;
                    }

                    // run build & publish steps
                    foreach(var argument in arguments) {
                        string moduleSource = null;
                        ModuleInfo moduleInfo = null;
                        if(Directory.Exists(argument)) {

                            // check if argument is pointing to a folder containing a cloudformation file
                            if(File.Exists(Path.Combine(argument, "cloudformation.json"))) {
                                settings.WorkingDirectory = Path.GetFullPath(argument);
                                settings.OutputDirectory = settings.WorkingDirectory;
                            } else {
                                moduleSource = Path.Combine(Path.GetFullPath(argument), "Module.yml");
                            }
                        } else if((Path.GetExtension(argument) == ".yml") || (Path.GetExtension(argument) == ".yaml")) {
                            moduleSource = Path.GetFullPath(argument);
                        } else if(Path.GetFileName(argument) == "cloudformation.json") {
                            settings.WorkingDirectory = Path.GetDirectoryName(argument);
                            settings.OutputDirectory = settings.WorkingDirectory;
                        } else if(ModuleInfo.TryParse(argument, out moduleInfo)) {
                            if(moduleInfo.Origin == null) {
                                LogError($"missing module origin for '{moduleInfo}'");
                                break;
                            }
                        } else {
                            LogError($"unrecognized argument: {argument}");
                            break;
                        }
                        if(moduleSource != null) {
                            settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                            settings.OutputDirectory = outputDirectoryOption.HasValue()
                                ? Path.GetFullPath(outputDirectoryOption.Value())
                                : Path.Combine(settings.WorkingDirectory, "bin");
                            settings.NoDependencyValidation = skipDependencyValidationOption.HasValue();
                            if(!await BuildStepAsync(
                                settings,
                                GetOutputFilePath(settings, outputCloudFormationPathOption, moduleSource),
                                skipAssemblyValidationOption.HasValue(),
                                dryRun == DryRunLevel.CloudFormation,
                                gitShaOption.Value() ?? GetGitShaValue(settings.WorkingDirectory),
                                gitBranchOption.Value() ?? GetGitBranch(settings.WorkingDirectory, showWarningOnFailure: false),
                                buildConfigurationOption.Value() ?? "Release",
                                selectorOption.Value(),
                                moduleSource,
                                moduleVersion
                            )) {
                                break;
                            }
                        }
                        if(dryRun == null) {
                            if(moduleSource != null) {
                                if(await PublishStepAsync(settings, forcePublishOption.HasValue(), moduleOriginOption.Value()) == null) {
                                    break;
                                }
                            } else if(moduleInfo != null) {
                                if(!await ImportStepAsync(settings, moduleInfo, forcePublishOption.HasValue())) {
                                    break;
                                }
                            }
                        }
                    }
                });
            });

            // add 'deploy' command
            app.Command("deploy", cmd => {
                cmd.HelpOption();
                cmd.Description = "Deploy LambdaSharp module";

                // deploy options
                var publishedModulesArgument = cmd.Argument("<NAME>", "(optional) Published module name, or path to artifacts folder, or module definition/folder (default: Module.yml)", multipleValues: true);
                var alternativeNameOption = cmd.Option("--name <NAME>", "(optional) Specify an alternative module name for the deployment (default: module name)", CommandOptionType.SingleValue);
                var parametersFileOption = cmd.Option("--parameters <FILE>", "(optional) Specify source filename for module parameters (default: none)", CommandOptionType.SingleValue);
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the deployed module", CommandOptionType.NoValue);
                var enableXRayTracingOption = cmd.Option("--xray[:<LEVEL>]", "(optional) Enable service-call tracing with AWS X-Ray for all resources in module  (0=Disabled, 1=RootModule, 2=AllModules; RootModule if LEVEL is omitted)", CommandOptionType.SingleOrNoValue);
                var forceDeployOption = cmd.Option("--force-deploy", "(optional) Force module deployment", CommandOptionType.NoValue);
                var promptAllParametersOption = cmd.Option("--prompt-all", "(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)", CommandOptionType.NoValue);

                // publish options
                var forcePublishOption = AddForcePublishOption(cmd);

                // build options
                var skipAssemblyValidationOption = AddSkipAssemblyValidationOption(cmd);
                var skipDependencyValidationOption = AddSkipDependencyValidationOption(cmd);
                var buildConfigurationOption = AddBuildConfigurationOption(cmd);
                var gitShaOption = AddGitShaOption(cmd);
                var gitBranchOption = AddGitBranchOption(cmd);
                var outputDirectoryOption = AddOutputPathOption(cmd);
                var selectorOption = AddSelectorOption(cmd);
                var moduleVersionOption = AddModuleVersionOption(cmd);
                var moduleBuildDateOption = AddModuleBuildDateOption(cmd);

                // misc options
                var dryRunOption = AddDryRunOption(cmd);
                var outputCloudFormationPathOption = AddCloudFormationOutputOption(cmd);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");

                    // read settings and validate them
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    DryRunLevel? dryRun = null;
                    if(dryRunOption.HasValue()) {
                        DryRunLevel value;
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, DryRunLevel.Everything, out value)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                            return;
                        }
                        dryRun = value;
                    }

                    // check x-ray settings
                    if(!TryParseEnumOption(enableXRayTracingOption, XRayTracingLevel.Disabled, XRayTracingLevel.RootModule, out var xRayTracingLevel)) {

                        // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                        return;
                    }

                    // check if one or more arguments have been specified
                    var arguments = publishedModulesArgument.Values.Any()
                        ? publishedModulesArgument.Values
                        : new List<string> { Directory.GetCurrentDirectory() };
                    Console.WriteLine($"Readying module for deployment tier '{settings.TierName}'");

                    // check if a module version number is supplied
                    VersionInfo moduleVersion = null;
                    if(moduleVersionOption.HasValue()) {
                        if(!VersionInfo.TryParse(moduleVersionOption.Value(), out moduleVersion)) {
                            LogError("--module-version is not a valid version number");
                            return;
                        }
                    }

                    // check if a module build time is supplied
                    if(moduleBuildDateOption.HasValue()) {
                        if(!DateTime.TryParseExact(moduleBuildDateOption.Value(), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var moduleBuildDate)) {
                            LogError("--module-build-date is not a valid date-time");
                            return;
                        }
                        settings.UtcNow = moduleBuildDate;
                    }

                    // read optional parameters file
                    var parameters = new Dictionary<string, string>();
                    if(parametersFileOption.HasValue()) {
                        parameters = ReadInputParametersFiles(settings, parametersFileOption.Value());
                    }
                    if(HasErrors) {
                        return;
                    }

                    // run build, publish, and deploy steps
                    foreach(var argument in arguments) {
                        ModuleInfo moduleInfo = null;
                        string moduleSource = null;
                        if(Directory.Exists(argument)) {

                            // check if argument is pointing to a folder containing a cloudformation file
                            if(File.Exists(Path.Combine(argument, "cloudformation.json"))) {
                                settings.WorkingDirectory = Path.GetFullPath(argument);
                                settings.OutputDirectory = settings.WorkingDirectory;
                            } else {
                                moduleSource = Path.Combine(Path.GetFullPath(argument), "Module.yml");
                            }
                        } else if((Path.GetExtension(argument) == ".yml") || (Path.GetExtension(argument) == ".yaml")) {
                            moduleSource = Path.GetFullPath(argument);
                        } else if(Path.GetFileName(argument) == "cloudformation.json") {
                            settings.WorkingDirectory = Path.GetDirectoryName(argument);
                            settings.OutputDirectory = settings.WorkingDirectory;
                        } else if(!ModuleInfo.TryParse(argument, out moduleInfo)) {
                            LogError($"unrecognized argument: {argument}");
                            break;
                        }
                        if(moduleSource != null) {
                            settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                            settings.OutputDirectory = outputDirectoryOption.HasValue()
                                ? Path.GetFullPath(outputDirectoryOption.Value())
                                : Path.Combine(settings.WorkingDirectory, "bin");
                            settings.NoDependencyValidation = skipDependencyValidationOption.HasValue();
                            if(!await BuildStepAsync(
                                settings,
                                GetOutputFilePath(settings, outputCloudFormationPathOption, moduleSource),
                                skipAssemblyValidationOption.HasValue(),
                                dryRun == DryRunLevel.CloudFormation,
                                gitShaOption.Value() ?? GetGitShaValue(settings.WorkingDirectory),
                                gitBranchOption.Value() ?? GetGitBranch(settings.WorkingDirectory, showWarningOnFailure: false),
                                buildConfigurationOption.Value() ?? "Release",
                                selectorOption.Value(),
                                moduleSource,
                                moduleVersion
                            )) {
                                break;
                            }
                        }
                        if(dryRun == null) {

                            // check if module needs to be published or imported first
                            if(moduleInfo == null) {
                                moduleInfo = await PublishStepAsync(settings, forcePublishOption.HasValue(), moduleOrigin: null);
                                if(moduleInfo == null) {
                                    break;
                                }
                            } else if(moduleInfo.Origin != null) {
                                if(!await ImportStepAsync(settings, moduleInfo, forcePublishOption.HasValue())) {
                                    break;
                                }
                            }
                            if(!await DeployStepAsync(
                                settings,
                                dryRun,
                                moduleInfo.ToString(),
                                alternativeNameOption.Value(),
                                allowDataLossOption.HasValue(),
                                protectStackOption.HasValue(),
                                parameters,
                                forceDeployOption.HasValue(),
                                promptAllParametersOption.HasValue(),
                                xRayTracingLevel,
                                deployOnlyIfExists: false
                            )) {
                                break;
                            }
                        }
                    }
                });
            });
        }

        public async Task<bool> BuildStepAsync(
            Settings settings,
            string outputCloudFormationFilePath,
            bool noAssemblyValidation,
            bool noPackageBuild,
            string gitSha,
            string gitBranch,
            string buildConfiguration,
            string selector,
            string moduleSource,
            VersionInfo moduleVersion
        ) {
            try {
                if(!await PopulateDeploymentTierSettingsAsync(settings)) {
                    return false;
                }
                return await new BuildStep(settings, moduleSource).DoAsync(
                    outputCloudFormationFilePath,
                    noAssemblyValidation,
                    noPackageBuild,
                    gitSha,
                    gitBranch,
                    buildConfiguration,
                    selector,
                    moduleVersion
                );
            } catch(Exception e) {
                LogError(e);
                return false;
            }
        }

        public async Task<ModuleInfo> PublishStepAsync(Settings settings, bool forcePublish, string moduleOrigin) {
            if(!await PopulateDeploymentTierSettingsAsync(settings)) {
                return null;
            }
            var cloudformationFile = Path.Combine(settings.OutputDirectory, "cloudformation.json");
            return await new PublishStep(settings, cloudformationFile).DoAsync(cloudformationFile, forcePublish, moduleOrigin);
        }

        public async Task<bool> ImportStepAsync(Settings settings, ModuleInfo moduleInfo, bool forcePublish) {
            if(!await PopulateDeploymentTierSettingsAsync(settings)) {
                return false;
            }
            if(moduleInfo.Origin == settings.DeploymentBucketName) {
                LogWarn($"skipping import of {moduleInfo} because origin matches deployment bucket");
                return true;
            }
            return await new PublishStep(settings, moduleInfo.ToString()).DoImportAsync(moduleInfo, forcePublish);
        }

        public async Task<bool> DeployStepAsync(
            Settings settings,
            DryRunLevel? dryRun,
            string moduleReference,
            string instanceName,
            bool allowDataLoos,
            bool protectStack,
            Dictionary<string, string> parameters,
            bool forceDeploy,
            bool promptAllParameters,
            XRayTracingLevel xRayTracingLevel,
            bool deployOnlyIfExists
        ) {
            try {
                if(!await PopulateDeploymentTierSettingsAsync(settings)) {
                    return false;
                }
                if(HasErrors) {
                    return false;
                }

                // reading module parameters
                return await new DeployStep(settings, moduleReference).DoAsync(
                    dryRun,
                    moduleReference,
                    instanceName,
                    allowDataLoos,
                    protectStack,
                    parameters,
                    forceDeploy,
                    promptAllParameters,
                    xRayTracingLevel,
                    deployOnlyIfExists
                );
            } catch(Exception e) {
                LogError(e);
                return false;
            }
        }

        public string GetOutputFilePath(Settings settings, CommandOption option, string moduleSource) {
            string result;
            if(option.HasValue()) {
                var outputPath = Path.GetFullPath(option.Value());
                if(Directory.Exists(outputPath)) {
                    var fileInfo = new FileInfo(moduleSource);
                    var filenameWithoutExtension = (fileInfo.Name == "Module.yml")
                        ? fileInfo.Directory.Name
                        : Path.GetFileNameWithoutExtension(moduleSource);
                    result = Path.Combine(outputPath, filenameWithoutExtension + ".json");
                } else {
                    result = option.Value();
            }
            } else {
                result = Path.Combine(settings.OutputDirectory, "cloudformation.json");
        }
            return result;
        }
    }
}
