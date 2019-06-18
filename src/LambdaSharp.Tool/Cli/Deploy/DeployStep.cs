/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using McMaster.Extensions.CommandLineUtils;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Amazon.S3.Model;

namespace LambdaSharp.Tool.Cli.Deploy {
    using CloudFormationStack = Amazon.CloudFormation.Model.Stack;
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class DeployStep : AModelProcessor {

        //--- Types ---
        private class DependencyRecord {

            //--- Properties ---
            public string DependencyOwner { get; set; }
            public ModuleManifest Manifest { get; set; }
            public ModuleInfo ModuleInfo { get; set; }
        }


        //--- Fields ---
        private ModelManifestLoader _loader;

        //--- Constructors ---
        public DeployStep(Settings settings, string sourceFilename) : base(settings, sourceFilename) {
            _loader = new ModelManifestLoader(Settings, sourceFilename);
        }

        //--- Methods ---
        public async Task<bool> DoAsync(
            DryRunLevel? dryRun,
            string moduleReference,
            string instanceName,
            bool allowDataLoos,
            bool protectStack,
            Dictionary<string, string> parameters,
            bool forceDeploy,
            bool promptAllParameters,
            bool promptsAsErrors,
            XRayTracingLevel xRayTracingLevel,
            bool deployOnlyIfExists
        ) {
            Console.WriteLine($"Resolving module reference: {moduleReference}");

            // determine location of cloudformation template from module key
            if(!ModuleInfo.TryParse(moduleReference, out var moduleInfo)) {
                LogError($"invalid module reference: {moduleReference}");
                return false;
            }
            var foundModuleLocation = await _loader.LocateAsync(moduleInfo);
            if(foundModuleLocation == null) {
                LogError($"unable to resolve: {moduleReference}");
                return false;
            }

            // download module manifest
            var manifest = await _loader.LoadFromS3Async(foundModuleLocation);
            if(manifest == null) {
                return false;
            }

            // check that the LambdaSharp Core & CLI versions match
            if(!forceDeploy && (manifest.GetFullName() == "LambdaSharp.Core")) {
                var toolToTierVersionComparison = Settings.ToolVersion.CompareToVersion(Settings.TierVersion);

                // core module has special rules for updates
                if(toolToTierVersionComparison == null) {

                    // tool version and tier version cannot be compared
                    LogError($"LambdaSharp tool is not compatible (tool: {Settings.ToolVersion}, tier: {Settings.TierVersion}); use --force-deploy to proceed anyway");
                    return false;
                } else if(toolToTierVersionComparison < 0) {

                    // TODO: this seems to be the opposite logic of `lash init`

                    // tier version is more recent; inform user to upgrade tool
                    LogError($"LambdaSharp tool is not compatible (tool: {Settings.ToolVersion}, tier: {Settings.TierVersion})", new LambdaSharpToolOutOfDateException(Settings.TierVersion));
                    return false;
                } else if(toolToTierVersionComparison > 0) {

                    // tool version is more recent; check if user wants to upgrade tier
                    Console.WriteLine($"LambdaSharp Tier is out of date");
                    var upgrade = Settings.UseAnsiConsole
                        ? Prompt.GetYesNo($"{AnsiTerminal.BrightBlue}|=> Do you want to upgrade LambdaSharp Tier '{Settings.TierName}' from v{Settings.TierVersion} to v{Settings.ToolVersion}?{AnsiTerminal.Reset}", false)
                        : Prompt.GetYesNo($"|=> Do you want to upgrade LambdaSharp Tier '{Settings.TierName}' from v{Settings.TierVersion} to v{Settings.ToolVersion}?", false);
                    if(!upgrade) {
                        return false;
                    }
                } else if(!Settings.ToolVersion.IsPreRelease && (Settings.CoreServices != CoreServices.Bootstrap)) {

                    // unless tool version is a pre-release or LambdaSharp.Core is bootstrapping; there is nothing to do
                    return true;
                }
            }

            // deploy module
            if(dryRun == null) {
                var stackName = ToStackName(manifest.GetFullName(), instanceName);

                // check version of previously deployed module
                CloudFormationStack existing = null;
                if(!forceDeploy) {
                    if(!deployOnlyIfExists) {
                        Console.WriteLine($"=> Validating module for deployment tier");
                    }
                    var updateValidation = await IsValidModuleUpdateAsync(stackName, manifest);
                    if(!updateValidation.Success) {
                        return false;
                    }

                    // check if a previous deployment was found
                    if(deployOnlyIfExists && (updateValidation.ExistingStack == null)) {

                        // nothing to do
                        return true;
                    }
                    existing = updateValidation.ExistingStack;
                }

                // prompt for missing parameters
                var deployParameters = PromptModuleParameters(manifest, existing, parameters, promptAllParameters, promptsAsErrors);
                if(HasErrors) {
                    return false;
                }

                // check if module should be run with core services
                if(
                    (Settings.CoreServices == CoreServices.Enabled)
                    && manifest.GetAllParameters().Any(p => p.Name == "LambdaSharpCoreServices")
                    && !deployParameters.Any(p => p.ParameterKey == "LambdaSharpCoreServices")
                ) {
                    deployParameters.Add(new CloudFormationParameter {
                        ParameterKey = "LambdaSharpCoreServices",
                        ParameterValue = Settings.CoreServices.ToString()
                    });
                }

                // check if module supports AWS X-Ray for tracing
                if(
                    manifest.GetAllParameters().Any(p => p.Name == "XRayTracing")
                    && !deployParameters.Any(p => p.ParameterKey == "XRayTracing")
                ) {
                    deployParameters.Add(new CloudFormationParameter {
                        ParameterKey = "XRayTracing",
                        ParameterValue = xRayTracingLevel.ToString()
                    });
                }

                // discover module dependencies and prompt for missing parameters
                var dependencies = await DiscoverDependenciesAsync(manifest);
                if(HasErrors) {
                    return false;
                }
                var dependenciesParameters = dependencies
                    .Select(dependency => new {
                        ModuleFullName = dependency.Manifest.GetFullName(),
                        Parameters = PromptModuleParameters(
                            dependency.Manifest,
                            promptAll: promptAllParameters,
                            promptsAsErrors: promptsAsErrors
                        )
                    })
                    .ToDictionary(t => t.ModuleFullName, t => t.Parameters);
                if(HasErrors) {
                    return false;
                }

                // TODO: this should be done at publishing as well!

                // copy all dependencies to deployment bucket that are missing or have a pre-release version
                foreach(var dependency in dependencies.Append((Manifest: manifest, ModuleInfo: moduleInfo)).Where(dependency => dependency.ModuleInfo.Origin != Settings.DeploymentBucketName)) {

                    // copy check-summed module assets (guaranteed immutable)
                    foreach(var asset in dependency.Manifest.Assets) {
                        await CopyS3Object(dependency.ModuleInfo.Origin, asset);
                    }
                    await CopyS3Object(dependency.ModuleInfo.Origin, dependency.Manifest.GetVersionedTemplatePath());

                    // copy cloudformation template
                    await CopyS3Object(dependency.ModuleInfo.Origin, dependency.ModuleInfo.TemplatePath, replace: dependency.ModuleInfo.Version.IsPreRelease);
                }

                // deploy module dependencies
                foreach(var dependency in dependencies) {
                    if(!await new ModelUpdater(Settings, dependency.ModuleInfo.ToModuleReference()).DeployChangeSetAsync(
                        dependency.Manifest,
                        dependency.ModuleInfo,
                        ToStackName(dependency.Manifest.GetFullName()),
                        allowDataLoos,
                        protectStack,
                        dependenciesParameters[dependency.Manifest.GetFullName()]
                    )) {
                        return false;
                    }
                }

                // deploy module
                return await new ModelUpdater(Settings, moduleReference).DeployChangeSetAsync(
                    manifest,
                    manifest.GetModuleInfo(),
                    stackName,
                    allowDataLoos,
                    protectStack,
                    deployParameters
                );
            }
            return true;

            // local functions
            async Task CopyS3Object(string sourceBucket, string key, bool replace = false) {

                // check if object must be copied, because it's a pre-release or is missing
                var found = false;
                try {
                    await Settings.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                        BucketName = Settings.DeploymentBucketName,
                        Key = key
                    });
                    found = true;
                } catch { }
                if(!found || replace) {
                    await Settings.S3Client.CopyObjectAsync(new CopyObjectRequest {
                        SourceBucket = sourceBucket,
                        SourceKey = key,
                        DestinationBucket = Settings.DeploymentBucketName,
                        DestinationKey = key
                    });
                }
            }
        }

        private async Task<(bool Success, CloudFormationStack ExistingStack)> IsValidModuleUpdateAsync(string stackName, ModuleManifest manifest) {
            try {

                // check if the module was already deployed
                var describe = await Settings.CfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                    StackName = stackName
                });

                // make sure the stack is in a stable state (not updating and not failed)
                var existing = describe.Stacks.FirstOrDefault();
                switch(existing?.StackStatus) {
                case null:
                case "CREATE_COMPLETE":
                case "ROLLBACK_COMPLETE":
                case "UPDATE_COMPLETE":
                case "UPDATE_ROLLBACK_COMPLETE":

                    // we're good to go
                    break;
                default:
                    LogError($"deployed module is not in a valid state; module deployment must be complete and successful (status: {existing?.StackStatus})");
                    return (false, existing);
                }

                // validate existing module deployment
                var deployedOutputs = existing?.Outputs;
                var deployed = deployedOutputs?.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
                if(!ModuleInfo.TryParse(deployed, out var deployedModuleInfo)) {
                    LogError("unable to determine the name of the deployed module; use --force-deploy to proceed anyway");
                    return (false, existing);
                }
                if(deployedModuleInfo.FullName != manifest.GetFullName()) {
                    LogError($"deployed module name ({deployedModuleInfo.FullName}) does not match {manifest.GetFullName()}; use --force-deploy to proceed anyway");
                    return (false, existing);
                }
                var versionComparison = deployedModuleInfo.Version.CompareToVersion(manifest.GetVersion());
                if(versionComparison > 0) {
                    LogError($"deployed module version (v{deployedModuleInfo.Version}) is newer than v{manifest.GetVersion()}; use --force-deploy to proceed anyway");
                    return (false, existing);
                } else if(versionComparison == null) {
                    LogError($"deployed module version (v{deployedModuleInfo.Version}) is not compatible with v{manifest.GetVersion()}; use --force-deploy to proceed anyway");
                    return (false, existing);
                }
                return (true, existing);
            } catch(AmazonCloudFormationException) {

                // stack doesn't exist
            }
            return (true, null);
        }

        private async Task<IEnumerable<(ModuleManifest Manifest, ModuleInfo ModuleInfo)>> DiscoverDependenciesAsync(ModuleManifest manifest) {
            var deployments = new List<DependencyRecord>();
            var existing = new List<DependencyRecord>();
            var inProgress = new List<DependencyRecord>();

            // create a topological sort of dependencies
            await Recurse(manifest);
            return deployments.Select(tuple => (tuple.Manifest, tuple.ModuleInfo)).ToList();

            // local functions
            async Task Recurse(ModuleManifest current) {
                foreach(var dependency in current.Dependencies) {

                    // check if we have already discovered this dependency
                    if(IsDependencyInList(current.GetFullName(), dependency, existing) || IsDependencyInList(current.GetFullName(), dependency, deployments))  {
                        continue;
                    }

                    // check if this dependency needs to be deployed
                    var deployedModuleInfo = await FindExistingDependencyAsync(dependency);
                    if(deployedModuleInfo != null) {
                        existing.Add(new DependencyRecord {
                            ModuleInfo = deployedModuleInfo
                        });
                    } else if(inProgress.Any(d => d.Manifest.GetModuleInfo().FullName == dependency.ModuleInfo.FullName)) {

                        // circular dependency detected
                        LogError($"circular dependency detected: {string.Join(" -> ", inProgress.Select(d => d.Manifest.GetFullName()))}");
                        return;
                    } else {

                        // resolve dependencies for dependency module
                        var dependencyModuleLocation = await _loader.LocateAsync(dependency.ModuleInfo.Owner, dependency.ModuleInfo.Name, dependency.MinVersion, dependency.MaxVersion, dependency.ModuleInfo.Origin);
                        if(dependencyModuleLocation == null) {

                            // error has already been reported
                            continue;
                        }

                        // load manifest of dependency and add its dependencies
                        var dependencyManifest = await _loader.LoadFromS3Async(dependencyModuleLocation);
                        if(dependencyManifest == null) {

                            // error has already been reported
                            continue;
                        }
                        var nestedDependency = new DependencyRecord {
                            DependencyOwner = current.Module,
                            Manifest = dependencyManifest,
                            ModuleInfo = dependencyModuleLocation.ModuleInfo
                        };

                        // keep marker for in-progress resolutions so that circular errors can be detected
                        inProgress.Add(nestedDependency);
                        await Recurse(dependencyManifest);
                        inProgress.Remove(nestedDependency);

                        // append dependency now that all nested dependencies have been resolved
                        Console.WriteLine($"=> Resolved dependency '{dependency.ModuleInfo.FullName}' to module reference: {dependencyModuleLocation}");
                        deployments.Add(nestedDependency);
                    }
                }
            }
        }

        private bool IsDependencyInList(string fullName, ModuleManifestDependency dependency, IEnumerable<DependencyRecord> deployedModules) {
            var deployedModule = deployedModules.FirstOrDefault(deployed => (deployed.ModuleInfo.Origin == dependency.ModuleInfo.Origin) && (deployed.ModuleInfo.FullName == dependency.ModuleInfo.FullName));
            if(deployedModule == null) {
                return false;
            }
            var deployedOwner = (deployedModule.DependencyOwner == null)
                ? "existing module"
                : $"module '{deployedModule.DependencyOwner}'";

            // confirm that the dependency version is in a valid range
            var deployedVersion = deployedModule.ModuleInfo.Version;
            if(!deployedModule.ModuleInfo.Version.MatchesConstraints(dependency.MinVersion, dependency.MaxVersion)) {
                LogError($"version conflict for module '{dependency.ModuleInfo.FullName}': module '{fullName}' requires v{dependency.MinVersion}..v{dependency.MaxVersion}, but {deployedOwner} uses v{deployedVersion})");
            }
            return true;
        }

        private async Task<ModuleInfo> FindExistingDependencyAsync(ModuleManifestDependency dependency) {
            try {
                var describe = await Settings.CfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                    StackName = ToStackName(dependency.ModuleInfo.FullName)
                });
                var deployedOutputs = describe.Stacks.FirstOrDefault()?.Outputs;
                var deployedModuleInfoText = deployedOutputs?.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
                var success = ModuleInfo.TryParse(deployedModuleInfoText, out var deployedModuleInfo);
                if(!success) {
                    LogWarn($"unable to retrieve information of the deployed dependent module");
                    return null;
                }

                // confirm that the module name matches
                if(deployedModuleInfo.FullName != dependency.ModuleInfo.FullName) {
                    LogError($"deployed dependent module name ({deployedModuleInfo.FullName}) does not match {dependency.ModuleInfo.FullName}");
                    return deployedModuleInfo;
                }

                // confirm that the module version is in a valid range
                if((dependency.MinVersion != null) && (dependency.MaxVersion != null)) {
                    if(!deployedModuleInfo.Version.MatchesConstraints(dependency.MinVersion, dependency.MaxVersion)) {
                        LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with v{dependency.MinVersion} to v{dependency.MaxVersion}");
                        return deployedModuleInfo;
                    }
                } else if(dependency.MaxVersion != null) {
                    var deployedToMinVersionComparison = deployedModuleInfo.Version.CompareToVersion(dependency.MaxVersion);
                    if(deployedToMinVersionComparison >= 0) {
                        LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is newer than max version constraint v{dependency.MaxVersion}");
                        return deployedModuleInfo;
                    } else if(deployedToMinVersionComparison == null) {
                        LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with max version constraint v{dependency.MaxVersion}");
                        return deployedModuleInfo;
                    }
                } else if(dependency.MinVersion != null) {
                    var deployedToMinVersionComparison = deployedModuleInfo.Version.CompareToVersion(dependency.MinVersion);
                    if(deployedToMinVersionComparison < 0) {
                        LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is older than min version constraint v{dependency.MinVersion}");
                        return deployedModuleInfo;
                    } else if(deployedToMinVersionComparison == null) {
                        LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with min version constraint v{dependency.MinVersion}");
                        return deployedModuleInfo;
                    }
                }
                return deployedModuleInfo;
            } catch(AmazonCloudFormationException) {

                // stack doesn't exist
                return null;
            }
        }

        private List<CloudFormationParameter> PromptModuleParameters(
            ModuleManifest manifest,
            CloudFormationStack existing = null,
            Dictionary<string, string> parameters = null,
            bool promptAll = false,
            bool promptsAsErrors = false
        ) {
            var stackParameters = new Dictionary<string, CloudFormationParameter>();

            // tentatively indicate to reuse previous parameter values
            if(existing != null) {
                foreach(var parameter in manifest.ParameterSections
                    .SelectMany(section => section.Parameters)
                    .Where(moduleParameter => existing.Parameters.Any(existingParameter => existingParameter.ParameterKey == moduleParameter.Name))
                ) {
                    stackParameters[parameter.Name] = new CloudFormationParameter {
                        ParameterKey = parameter.Name,
                        UsePreviousValue = true
                    };
                }
            }

            // add all provided parameters
            if(parameters != null) {
                foreach(var parameter in parameters) {
                    stackParameters[parameter.Key] = new CloudFormationParameter {
                        ParameterKey = parameter.Key,
                        ParameterValue = parameter.Value
                    };
                }
            }

            // check if module requires any prompts
            if(manifest.GetAllParameters().Any(RequiresPrompt)) {
                Console.WriteLine();
                Console.WriteLine($"Configuration for {manifest.GetFullName()} (v{manifest.GetVersion()})");

                // only list parameter sections that contain a parameter that requires a prompt
                foreach(var parameterGroup in manifest.ParameterSections.Where(group => group.Parameters.Any(RequiresPrompt))) {
                    Console.WriteLine();
                    ACliCommand.PromptText(parameterGroup.Title.ToUpper());

                    // only prompt for required parameters
                    foreach(var parameter in parameterGroup.Parameters.Where(RequiresPrompt)) {

                        // check if parameter is multiple choice
                        string enteredValue;
                        if(parameter.AllowedValues?.Any() ?? false) {
                            var message = parameter.Name;
                            if(parameter.Label != null) {
                                message += $": {parameter.Label}";
                            }
                            enteredValue = ACliCommand.PromptChoice(message, parameter.AllowedValues);
                        } else {
                            var message = $"{parameter.Name} [{parameter.Type}]";
                            if(parameter.Label != null) {
                                message += $": {parameter.Label}";
                            }
                            enteredValue = ACliCommand.PromptString(message, parameter.Default, parameter.AllowedPattern, parameter.ConstraintDescription) ?? "";
                        }
                        stackParameters[parameter.Name] = new CloudFormationParameter {
                            ParameterKey = parameter.Name,
                            ParameterValue = enteredValue
                        };
                    }
                }
            }
            return stackParameters.Values.ToList();

            // local functions
            bool RequiresPrompt(ModuleManifestParameter parameter) {
                if(parameters?.ContainsKey(parameter.Name) == true) {

                    // no prompt since parameter is provided explicitly
                    return false;
                }
                if(existing?.Parameters.Any(p => p.ParameterKey == parameter.Name) == true) {

                    // no prompt since we can reuse the previous parameter value
                    return false;
                }
                if(!promptAll && (parameter.Default != null)) {

                    // no prompt since parameter has a default value
                    return false;
                }
                if(promptsAsErrors) {
                    LogError($"{manifest.GetFullName()} requires value for parameter '{parameter.Name}'");
                    return false;
                }
                return true;
            }
        }

        private string ToStackName(string moduleName, string instanceName = null)
            => $"{Settings.TierPrefix}{instanceName ?? moduleName.Replace(".", "-")}";
    }
}