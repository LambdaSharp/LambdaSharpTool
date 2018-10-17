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
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement;
using Humidifier.Json;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliBuildPublishDeployCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {

            // NOTE (2018-10-16, bjorg): we're keeping the build/publish/deploy commands in a single
            //  class to make it easier to chain these commands consistently.

            // add 'build' command
            app.Command("build", cmd => {
                cmd.HelpOption();
                cmd.Description = "Build LambdaSharp module";

                // build options
                var modulesArgument = cmd.Argument("<NAME>", "(optional) Path to module file/folder (default: Module.yml)", multipleValues: true);
                var skipFunctionBuildOption = cmd.Option("--skip-function-build", "(optional) Do not build the function projects", CommandOptionType.NoValue);
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var buildConfigurationOption = cmd.Option("-c|--configuration <CONFIGURATION>", "(optional) Build configuration for function projects (default: \"Release\")", CommandOptionType.SingleValue);
                var gitShaOption = cmd.Option("--gitsha <VALUE>", "(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)", CommandOptionType.SingleValue);
                var outputDirectoryOption = cmd.Option("-o|--output <DIRECTORY>", "(optional) Path to output directory (default: bin)", CommandOptionType.SingleValue);
                var selectorOption = cmd.Option("--selector <NAME>", "(optional) Selector for resolving conditional compilation choices in module", CommandOptionType.SingleValue);
                var outputCloudFormationFilePathOption = cmd.Option("--cf-output <FILE>", "(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);

                // misc options
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd, requireAwsProfile: false);
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
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, out value)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by `TryParseEnumOption`
                            return;
                        }
                        dryRun = value;
                    }

                    // check if one or more arguments have been specified
                    var arguments = modulesArgument.Values.Any()
                        ? modulesArgument.Values
                        : new List<string> { Directory.GetCurrentDirectory() };

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
                        if(!await BuildStepAsync(
                            settings,
                            outputCloudFormationFilePathOption.Value() ?? Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                            skipAssemblyValidationOption.HasValue(),
                            skipFunctionBuildOption.HasValue() || (dryRun == DryRunLevel.CloudFormation),
                            gitShaOption.Value() ?? GetGitShaValue(settings.WorkingDirectory),
                            buildConfigurationOption.Value() ?? "Release",
                            selectorOption.Value(),
                            moduleSource
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

                // build options
                var compiledModulesArgument = cmd.Argument("<NAME>", "(optional) Path to assets folder or module file/folder (default: Module.yml)", multipleValues: true);
                var skipFunctionBuildOption = cmd.Option("--skip-function-build", "(optional) Do not build the function projects", CommandOptionType.NoValue);
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var buildConfigurationOption = cmd.Option("-c|--configuration <CONFIGURATION>", "(optional) Build configuration for function projects (default: \"Release\")", CommandOptionType.SingleValue);
                var gitShaOption = cmd.Option("--gitsha <VALUE>", "(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)", CommandOptionType.SingleValue);
                var outputDirectoryOption = cmd.Option("-o|--output <DIRECTORY>", "(optional) Path to output directory (default: bin)", CommandOptionType.SingleValue);
                var selectorOption = cmd.Option("--selector <NAME>", "(optional) Selector for resolving conditional compilation choices in module", CommandOptionType.SingleValue);

                // misc options
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputCloudFormationFilePathOption = cmd.Option("--cf-output <FILE>", "(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);
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
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, out value)) {

                            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by `TryParseEnumOption`
                            return;
                        }
                        dryRun = value;
                    }

                    // check if one or more arguments have been specified
                    var arguments = compiledModulesArgument.Values.Any()
                        ? compiledModulesArgument.Values
                        : new List<string> { Directory.GetCurrentDirectory() };

                    // run build & publish steps
                    foreach(var argument in arguments) {
                        string moduleSource = null;
                        if(Directory.Exists(argument)) {

                            // check if argument is pointing to a folder containing a module definition
                            if(File.Exists(Path.Combine(argument, "manifest.json"))) {
                                settings.WorkingDirectory = Path.GetFullPath(argument);
                                settings.OutputDirectory = settings.WorkingDirectory;
                            } else {
                                moduleSource = Path.Combine(Path.GetFullPath(argument), "Module.yml");
                            }
                        } else if(Path.GetFileName(argument) == "Module.yml") {
                            moduleSource = Path.GetFullPath(argument);
                        } else if(Path.GetFileName(argument) == "manifest.json") {
                            settings.WorkingDirectory = Path.GetDirectoryName(argument);
                            settings.OutputDirectory = settings.WorkingDirectory;
                        } else {
                            AddError($"unrecognized argument: {argument}");
                            break;
                        }
                        if(moduleSource != null) {
                            settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                            settings.OutputDirectory = outputDirectoryOption.HasValue()
                                ? Path.GetFullPath(outputDirectoryOption.Value())
                                : Path.Combine(settings.WorkingDirectory, "bin");
                            if(!await BuildStepAsync(
                                settings,
                                outputCloudFormationFilePathOption.Value() ?? Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                                skipAssemblyValidationOption.HasValue(),
                                skipFunctionBuildOption.HasValue() || (dryRun == DryRunLevel.CloudFormation),
                                gitShaOption.Value() ?? GetGitShaValue(settings.WorkingDirectory),
                                buildConfigurationOption.Value() ?? "Release",
                                selectorOption.Value(),
                                moduleSource
                            )) {
                                break;
                            }
                        }
                        if(await PublishStepAsync(settings, dryRun) == null) {
                            break;
                        }
                    }
                });
            });

            // add 'deploy' command
            app.Command("deploy", cmd => {
                cmd.HelpOption();
                cmd.Description = "Deploy LambdaSharp module";

                // deploy options
                var publishedModulesArgument = cmd.Argument("<NAME>", "(optional) Published module name, or path to assets folder, or module file/folder (default: Module.yml)", multipleValues: true);
                var altModuleNameOption = cmd.Option("--name", "(optional) Specify an alternate module name for the deployment (default: module name)", CommandOptionType.SingleOrNoValue);
                var inputsFileOption = cmd.Option("--inputs|-I <FILE>", "(optional) Specify module inputs (default: none)", CommandOptionType.SingleValue);
                var inputOption = cmd.Option("--input|-KV <KEY>=<VALUE>", "(optional) Specify module input (can be used multiple times)", CommandOptionType.MultipleValue);
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var tierOption = cmd.Option("--tier|-T <NAME>", "(optional) Name of deployment tier (default: LAMBDASHARP_TIER environment variable)", CommandOptionType.SingleValue);

                // build options
                var skipFunctionBuildOption = cmd.Option("--skip-function-build", "(optional) Do not build the function projects", CommandOptionType.NoValue);
                var skipAssemblyValidationOption = cmd.Option("--skip-assembly-validation", "(optional) Disable validating LambdaSharp assembly references in function project files", CommandOptionType.NoValue);
                var buildConfigurationOption = cmd.Option("-c|--configuration <CONFIGURATION>", "(optional) Build configuration for function projects (default: \"Release\")", CommandOptionType.SingleValue);
                var gitShaOption = cmd.Option("--gitsha <VALUE>", "(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)", CommandOptionType.SingleValue);
                var outputDirectoryOption = cmd.Option("-o|--output <DIRECTORY>", "(optional) Path to output directory (default: bin)", CommandOptionType.SingleValue);
                var selectorOption = cmd.Option("--selector <NAME>", "(optional) Selector for resolving conditional compilation choices in module", CommandOptionType.SingleValue);

                // misc options
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputCloudFormationFilePathOption = cmd.Option("--cf-output <FILE>", "(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)", CommandOptionType.SingleValue);
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
                    foreach(var inputKeyValue in inputOption.Values) {
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

                    // initialize deployment tier value
                    var tier = tierOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_TIER");
                    if(tier == null) {
                        AddError("missing deployment tier name");
                        return;
                    }
                    if(tier == "Default") {
                        AddError("deployment tier cannot be 'Default' because it is a reserved name");
                        return;
                    }

                    // check if one or more arguments have been specified
                    var arguments = publishedModulesArgument.Values.Any()
                        ? publishedModulesArgument.Values
                        : new List<string> { Directory.GetCurrentDirectory() };
                    Console.WriteLine($"Readying module for deployment tier '{tier}'");
                    foreach(var argument in arguments) {
                        string moduleKey = null;
                        string moduleSource = null;
                        if(Directory.Exists(argument)) {

                            // check if argument is pointing to a folder containing a module definition
                            if(File.Exists(Path.Combine(argument, "manifest.json"))) {
                                settings.WorkingDirectory = Path.GetFullPath(argument);
                                settings.OutputDirectory = settings.WorkingDirectory;
                            } else {
                                moduleSource = Path.Combine(Path.GetFullPath(argument), "Module.yml");
                            }
                        } else if(Path.GetFileName(argument) == "Module.yml") {
                            moduleSource = Path.GetFullPath(argument);
                        } else if(Path.GetFileName(argument) == "manifest.json") {
                            settings.WorkingDirectory = Path.GetDirectoryName(argument);
                            settings.OutputDirectory = settings.WorkingDirectory;
                        } else {
                            moduleKey = argument;
                        }
                        if(moduleSource != null) {
                            settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                            settings.OutputDirectory = outputDirectoryOption.HasValue()
                                ? Path.GetFullPath(outputDirectoryOption.Value())
                                : Path.Combine(settings.WorkingDirectory, "bin");
                            if(!await BuildStepAsync(
                                settings,
                                outputCloudFormationFilePathOption.Value() ?? Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                                skipAssemblyValidationOption.HasValue(),
                                skipFunctionBuildOption.HasValue() || (dryRun == DryRunLevel.CloudFormation),
                                gitShaOption.Value() ?? GetGitShaValue(settings.WorkingDirectory),
                                buildConfigurationOption.Value() ?? "Release",
                                selectorOption.Value(),
                                moduleSource
                            )) {
                                break;
                            }
                        }
                        if(moduleKey == null) {
                            moduleKey = await PublishStepAsync(settings, dryRun);
                            if(moduleKey == null) {
                                break;
                            }
                        }
                        if(!await DeployStepAsync(
                            settings,
                            dryRun,
                            moduleKey,
                            altModuleNameOption.Value(),
                            allowDataLossOption.HasValue(),
                            protectStackOption.HasValue(),
                            inputs,
                            tier
                        )) {
                            break;
                        }
                    }
                });
            });
        }

        public async Task<bool> BuildStepAsync(
            Settings settings,
            string outputCloudFormationFilePath,
            bool skipAssemblyValidation,
            bool skipFunctionBuild,
            string gitsha,
            string buildConfiguration,
            string selector,
            string moduleSource
        ) {
            try {
                if(!File.Exists(moduleSource)) {
                    AddError($"could not find '{moduleSource}'");
                    return false;
                }

                // read input file
                Console.WriteLine();
                Console.WriteLine($"Processing module: {moduleSource}");
                var source = await File.ReadAllTextAsync(moduleSource);

                // preprocess file
                var tokenStream = new ModelPreprocessor(settings, moduleSource).Preprocess(source, selector);
                if(HasErrors) {
                    return false;
                }

                // parse yaml module file
                var parsedModule = new ModelParser(settings, moduleSource).Parse(tokenStream);
                if(HasErrors) {
                    return false;
                }

                // validate module
                new ModelValidation(settings, moduleSource).Process(parsedModule);
                if(HasErrors) {
                    return false;
                }

                // TODO (2018-10-04, bjorg): refactor all model processing to use the strict model instead of the parsed model

                // package all functions
                new ModelFunctionPackager(settings, moduleSource).Process(
                    parsedModule,
                    settings.ToolVersion,
                    skipCompile: skipFunctionBuild,
                    skipAssemblyValidation: skipAssemblyValidation,
                    gitsha: gitsha,
                    buildConfiguration: buildConfiguration
                );

                // package all files
                new ModelFilesPackager(settings, moduleSource).Process(parsedModule);

                // compile module file
                var module = new ModelConverter(settings, moduleSource).Process(parsedModule);
                if(HasErrors) {
                    return false;
                }

                // resolve all parameter references
                new ModelReferenceResolver(settings, moduleSource).Resolve(module);
                if(HasErrors) {
                    return false;
                }

                // generate & save cloudformation template
                var template = new ModelGenerator(settings, moduleSource).Generate(module);
                if(HasErrors) {
                    return false;
                }
                var outputCloudFormationDirectory = Path.GetDirectoryName(outputCloudFormationFilePath);
                if(outputCloudFormationDirectory != "") {
                    Directory.CreateDirectory(outputCloudFormationDirectory);
                }
                File.WriteAllText(outputCloudFormationFilePath, template);

                // generate & save module manifest
                var functions = module.Functions
                    .Where(f => f.PackagePath != null)
                    .Select(f => Path.GetRelativePath(settings.OutputDirectory, f.PackagePath))
                    .ToList();
                var packages = module.Parameters.OfType<PackageParameter>()
                    .Select(p => Path.GetRelativePath(settings.OutputDirectory, p.PackagePath))
                    .ToList();
                var manifest = new ModuleManifest {
                    Name = module.Name,
                    Version = module.Version.ToString(),
                    Hash = template.ToMD5Hash(),
                    GitSha = gitsha,
                    Pragmas = module.Pragmas,
                    Template = Path.GetRelativePath(settings.OutputDirectory, outputCloudFormationFilePath),
                    FunctionAssets = functions,
                    PackageAssets = packages
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

        public async Task<string> PublishStepAsync(
            Settings settings,
            DryRunLevel? dryRun
        ) {
            var manifestFile = Path.Combine(settings.OutputDirectory, "manifest.json");
            if(!File.Exists(manifestFile)) {
                AddError("folder does not contain a module manifest for publishing");
                return null;
            }
            // load manifest file
            var manifestText = File.ReadAllText(manifestFile);
            var manifest = JsonConvert.DeserializeObject<ModuleManifest>(manifestText);
            await PopulateToolSettingsAsync(settings);

            // make sure there is a deployment bucket
            if(settings.DeploymentBucketName == null) {
                AddError("missing deployment bucket", new LambdaSharpToolConfigException(settings.ToolProfile));
                return null;
            }

            // publish module
            if((dryRun == null) || (dryRun == DryRunLevel.Everything)) {
                var templateFilePath = manifest.Template;
                var result = await new ModelPublisher(settings, manifestFile).PublishAsync(manifest);
                return result;
            }
            return "no-value";
        }

        public async Task<bool> DeployStepAsync(
            Settings settings,
            DryRunLevel? dryRun,
            string moduleKey,
            string instanceName,
            bool allowDataLoos,
            bool protectStack,
            Dictionary<string, string> inputs,
            string tier
        ) {
            await PopulateToolSettingsAsync(settings);
            await PopulateEnvironmentSettingsAsync(settings, tier);

            // module key formats
            // * MODULENAME:VERSION
            // * s3://bucket-name/path/cloudformation.json

            string marker;
            if(moduleKey.StartsWith("s3://", StringComparison.Ordinal)) {
                var uri = new Uri(moduleKey);
                if(uri.Host != settings.DeploymentBucketName) {
                    AddError("deploying from another S3 bucket than the deployment bucket is not supported");
                    return false;
                }

                // absolute path always starts with '/', which needs to be removed
                marker = uri.AbsolutePath.Substring(1);
            } else {
                VersionInfo version = null;
                string moduleName;

                // check if a version suffix is specified
                if(moduleKey.Contains(':')) {
                    var parts = moduleKey.Split(':', 2);
                    moduleName = parts[0];
                    version = VersionInfo.Parse(parts[1]);
                } else {
                    moduleName = moduleKey;
                    version = await FindNewestVersion(settings, moduleKey);
                    if(HasErrors) {
                        return false;
                    }
                }
                marker = await GetS3ObjectContents(settings, $"{settings.DeploymentBucketPath}{moduleName}/Versions/{version}");
                if(marker == null) {
                    AddError($"could not find module: {moduleName} (v{version})");
                    return false;
                }
            }

            // download manifest
            var manifest = JsonConvert.DeserializeObject<ModuleManifest>(
                await GetS3ObjectContents(settings, marker)
            );

            // bootstrap module doesn't expect an environment to exist
            if(!manifest.HasPragma("no-environment-check")) {
                if(settings.EnvironmentVersion == null) {

                    // check that LambdaSharp Environment & Tool versions match
                    AddError("could not determine the LambdaSharp Environment version", new LambdaSharpDeploymentTierSetupException(tier));
                } else {
                    if(settings.EnvironmentVersion != settings.ToolVersion) {
                        AddError($"LambdaSharp tool (v{settings.ToolVersion}) and environment (v{settings.EnvironmentVersion}) versions do not match", new LambdaSharpDeploymentTierSetupException(tier));
                    }
                }
            }
            if(HasErrors) {
                return false;
            }

            // deploy module
            if(dryRun == null) {
                try {
                    return await new ModelUpdater(settings, sourceFilename: null).DeployAsync(
                        manifest,
                        instanceName,
                        allowDataLoos,
                        protectStack,
                        inputs,
                        tier
                    );
                } catch(Exception e) {
                    AddError(e);
                    return false;
                }
            }
            return true;
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

        private async Task<VersionInfo> FindNewestVersion(Settings settings, string moduleName) {

            // enumerate versions in bucket
            var versions = new List<VersionInfo>();
            var request = new ListObjectsV2Request {
                BucketName = settings.DeploymentBucketName,
                Prefix = $"{settings.DeploymentBucketPath}{moduleName}/Versions/",
                Delimiter = "/",
                MaxKeys = 100
            };
            do {
                var response = await settings.S3Client.ListObjectsV2Async(request);
                versions.AddRange(response.S3Objects
                    .Select(found => VersionInfo.Parse(found.Key.Substring(request.Prefix.Length)))
                    .Where(v => !v.IsPreRelease)
                );
                request.ContinuationToken = response.NextContinuationToken;
            } while(request.ContinuationToken != null);

            // attempt to identify the newest version
            var newest = new List<VersionInfo>();
            foreach(var current in versions) {
                if(!newest.Any(v => v.CompareTo(current) == VersionInfoCompare.Newer)) {

                    // newest contains either only older or incomparable versions
                    // => keep only incomparable versions and add current version
                    newest = newest.Where(v => v.CompareTo(current) == VersionInfoCompare.Undefined)
                        .Append(current)
                        .ToList();
                }
            }

            // check if we found a single, newest version
            switch(newest.Count) {
            case 0:
                AddError("could not find a published version");
                return null;
            case 1:
                return newest.First();
            default:
                AddError($"found more than one possible version match: {string.Join(" ", newest)}");
                return null;;
            }
        }

        private async Task<string> GetS3ObjectContents(Settings settings, string key) {
            try {
                var response = await settings.S3Client.GetObjectAsync(new GetObjectRequest {
                    BucketName = settings.DeploymentBucketName,
                    Key = key
                });
                using(var stream = new MemoryStream()) {
                    await response.ResponseStream.CopyToAsync(stream);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            } catch {
                return null;
            }
        }
    }
}
