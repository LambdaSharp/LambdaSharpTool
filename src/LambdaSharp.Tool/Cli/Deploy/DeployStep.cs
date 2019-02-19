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

namespace LambdaSharp.Tool.Cli.Deploy {
    using CloudFormationStack = Amazon.CloudFormation.Model.Stack;
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class DeployStep : AModelProcessor {

        //--- Types ---
        private class DependencyRecord {

            //--- Properties ---
            public string Owner { get; set; }
            public ModuleManifest Manifest { get; set; }
            public ModuleLocation Location { get; set; }
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
            bool enableXRayTracing
        ) {
            Console.WriteLine($"Resolving module reference: {moduleReference}");

            // determine location of cloudformation template from module key
            var location = await _loader.LocateAsync(moduleReference);
            if(location == null) {
                AddError($"unable to resolve: {moduleReference}");
                return false;
            }

            // download module manifest
            var manifest = await _loader.LoadFromS3Async(location.ModuleBucketName, location.TemplatePath);
            if(manifest == null) {
                return false;
            }

            // check that the LambdaSharp Core & CLI versions match
            if(Settings.CoreVersion == null) {

                // core module doesn't expect a deployment tier to exist
                if(!forceDeploy && manifest.RuntimeCheck) {
                    AddError("could not determine the LambdaSharp Core version; use --force-deploy to proceed anyway", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                    return false;
                }
            } else if(!Settings.ToolVersion.IsCompatibleWith(Settings.CoreVersion)) {
                if(!forceDeploy) {
                    AddError($"LambdaSharp CLI (v{Settings.ToolVersion}) and Core (v{Settings.CoreVersion}) versions do not match; use --force-deploy to proceed anyway");
                    return false;
                }
            }

            // deploy module
            if(dryRun == null) {
                var stackName = ToStackName(manifest.GetFullName(), instanceName);

                // check version of previously deployed module
                CloudFormationStack existing = null;
                if(!forceDeploy) {
                    Console.WriteLine($"=> Validating module for deployment tier");
                    var updateValidation = await IsValidModuleUpdateAsync(stackName, manifest);
                    if(!updateValidation.Success) {
                        return false;
                    }
                    existing = updateValidation.ExistingStack;
                }

                // prompt for missing parameters
                var deployParameters = PromptModuleParameters(manifest, existing, parameters, promptAllParameters, promptsAsErrors);
                if(HasErrors) {
                    return false;
                }

                // check if module supports AWS X-Ray for tracing
                if(
                    enableXRayTracing
                    && manifest.GetAllParameters().Any(p => p.Name == "XRayTracing")
                    && !deployParameters.Any(p => p.ParameterKey == "XRayTracing")
                ) {
                    deployParameters.Add(new CloudFormationParameter {
                        ParameterKey = "XRayTracing",
                        ParameterValue = "Active"
                    });
                }

                // discover and deploy module dependencies
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
                foreach(var dependency in dependencies) {
                    if(!await new ModelUpdater(Settings, dependency.Location.ToModuleReference()).DeployChangeSetAsync(
                        dependency.Manifest,
                        dependency.Location,
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
                    location,
                    stackName,
                    allowDataLoos,
                    protectStack,
                    deployParameters
                );
            }
            return true;
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
                    AddError($"deployed module is not in a valid state; module deployment must be complete and successful (Status: {existing?.StackStatus})");
                    return (false, existing);
                }

                // validate existing module deployment
                var deployedOutputs = existing?.Outputs;
                var deployed = deployedOutputs?.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
                if(!deployed.TryParseModuleDescriptor(
                    out string deployedOwner,
                    out string deployedName,
                    out VersionInfo deployedVersion,
                    out string _
                )) {
                    AddError("unable to determine the name of the deployed module; use --force-deploy to proceed anyway");
                    return (false, existing);
                }
                var deployedFullName = $"{deployedOwner}.{deployedName}";
                if(deployedFullName != manifest.GetFullName()) {
                    AddError($"deployed module name ({deployedFullName}) does not match {manifest.GetFullName()}; use --force-deploy to proceed anyway");
                    return (false, existing);
                }
                if(deployedVersion > manifest.GetVersion()) {
                    AddError($"deployed module version (v{deployedVersion}) is newer than v{manifest.GetVersion()}; use --force-deploy to proceed anyway");
                    return (false, existing);
                }
                return (true, existing);
            } catch(AmazonCloudFormationException) {

                // stack doesn't exist
            }
            return (true, null);
        }

        private async Task<IEnumerable<(ModuleManifest Manifest, ModuleLocation Location)>> DiscoverDependenciesAsync(ModuleManifest manifest) {
            var deployments = new List<DependencyRecord>();
            var existing = new List<DependencyRecord>();
            var inProgress = new List<DependencyRecord>();

            // create a topological sort of dependencies
            await Recurse(manifest);
            return deployments.Select(tuple => (tuple.Manifest, tuple.Location)).ToList();

            // local functions
            async Task Recurse(ModuleManifest current) {
                foreach(var dependency in current.Dependencies) {

                    // check if we have already discovered this dependency
                    if(IsDependencyInList(current.GetFullName(), dependency, existing) || IsDependencyInList(current.GetFullName(), dependency, deployments))  {
                        continue;
                    }

                    // check if this dependency needs to be deployed
                    var deployed = await FindExistingDependencyAsync(dependency);
                    if(deployed != null) {
                        existing.Add(new DependencyRecord {
                            Location = deployed
                        });
                    } else if(inProgress.Any(d => d.Manifest.GetFullName() == dependency.ModuleFullName)) {

                        // circular dependency detected
                        AddError($"circular dependency detected: {string.Join(" -> ", inProgress.Select(d => d.Manifest.GetFullName()))}");
                        return;
                    } else {
                        dependency.ModuleFullName.TryParseModuleOwnerName(out string moduleOwner, out var moduleName);

                        // resolve dependencies for dependency module
                        var dependencyLocation = await _loader.LocateAsync(moduleOwner, moduleName, dependency.MinVersion, dependency.MaxVersion, dependency.BucketName);
                        if(dependencyLocation == null) {

                            // error has already been reported
                            continue;
                        }

                        // load manifest of dependency and add its dependencies
                        var dependencyManifest = await _loader.LoadFromS3Async(dependencyLocation.ModuleBucketName, dependencyLocation.TemplatePath);
                        if(dependencyManifest == null) {

                            // error has already been reported
                            continue;
                        }
                        var nestedDependency = new DependencyRecord {
                            Owner = current.Module,
                            Manifest = dependencyManifest,
                            Location = dependencyLocation
                        };

                        // keep marker for in-progress resolutions so that circular errors can be detected
                        inProgress.Add(nestedDependency);
                        await Recurse(dependencyManifest);
                        inProgress.Remove(nestedDependency);

                        // append dependency now that all nested dependencies have been resolved
                        Console.WriteLine($"=> Resolved dependency '{dependency.ModuleFullName}' to module reference: {dependencyLocation}");
                        deployments.Add(nestedDependency);
                    }
                }
            }
        }

        private bool IsDependencyInList(string fullName, ModuleManifestDependency dependency, IEnumerable<DependencyRecord> modules) {
            var deployed = modules.FirstOrDefault(module => module.Location.ModuleFullName == dependency.ModuleFullName);
            if(deployed == null) {
                return false;
            }
            var deployedOwner = (deployed.Owner == null)
                ? "existing module"
                : $"module '{deployed.Owner}'";

            // confirm that the dependency version is in a valid range
            var deployedVersion = deployed.Location.ModuleVersion;
            if((dependency.MaxVersion != null) && (deployedVersion > dependency.MaxVersion)) {
                AddError($"version conflict for module '{dependency.ModuleFullName}': module '{fullName}' requires max version v{dependency.MaxVersion}, but {deployedOwner} uses v{deployedVersion})");
            }
            if((dependency.MinVersion != null) && (deployedVersion < dependency.MinVersion)) {
                AddError($"version conflict for module '{dependency.ModuleFullName}': module '{fullName}' requires min version v{dependency.MinVersion}, but {deployedOwner} uses v{deployedVersion})");
            }
            return true;
        }

        private async Task<ModuleLocation> FindExistingDependencyAsync(ModuleManifestDependency dependency) {
            try {
                var describe = await Settings.CfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                    StackName = ToStackName(dependency.ModuleFullName)
                });
                var deployedOutputs = describe.Stacks.FirstOrDefault()?.Outputs;
                var deployedInfo = deployedOutputs?.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
                var success = deployedInfo.TryParseModuleDescriptor(
                    out string deployedOwner,
                    out string deployedName,
                    out VersionInfo deployedVersion,
                    out string deployedBucketName
                );
                var deployed = new ModuleLocation {
                    ModuleFullName = $"{deployedOwner}.{deployedName}",
                    ModuleVersion = deployedVersion,
                    ModuleBucketName = deployedBucketName,
                    TemplatePath = null
                };
                if(!success) {
                    AddError($"unable to retrieve information of the deployed dependent module");
                    return deployed;
                }

                // confirm that the module name matches
                if(deployed.ModuleFullName != dependency.ModuleFullName) {
                    AddError($"deployed dependent module name ({deployed.ModuleFullName}) does not match {dependency.ModuleFullName}");
                    return deployed;
                }

                // confirm that the module version is in a valid range
                if((dependency.MaxVersion != null) && (deployedVersion > dependency.MaxVersion)) {
                    AddError($"deployed dependent module version (v{deployedVersion}) is newer than max version constraint v{dependency.MaxVersion}");
                    return deployed;
                }
                if((dependency.MinVersion != null) && (deployedVersion < dependency.MinVersion)) {
                    AddError($"deployed dependent module version (v{deployedVersion}) is older than min version constraint v{dependency.MinVersion}");
                    return deployed;
                }
                return deployed;
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
                    Console.WriteLine($"*** {parameterGroup.Title.ToUpper()} ***");

                    // only prompt for required parameters
                    foreach(var parameter in parameterGroup.Parameters.Where(RequiresPrompt)) {
                        var enteredValue = PromptString(parameter, parameter.Default) ?? "";
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
                    AddError($"{manifest.GetFullName()} requires value for parameter '{parameter.Name}'");
                    return false;
                }
                return true;
            }

            string PromptString(ModuleManifestParameter parameter, string defaultValue = null) {
                var prompt = $"|=> {parameter.Name} [{parameter.Type}]:";
                if(parameter.Label != null) {
                    prompt += $" {parameter.Label}:";
                }
                if(!string.IsNullOrEmpty(defaultValue)) {
                    prompt = $"{prompt} [{defaultValue}]";
                }
                Console.Write(prompt);
                Console.Write(' ');
                SetCursorVisible(true);
                var resp = Console.ReadLine();
                SetCursorVisible(false);
                if(!string.IsNullOrEmpty(resp)) {
                    return resp;
                }
                return defaultValue;

                // local functions
                void SetCursorVisible(bool visible) {
                    try {
                        Console.CursorVisible = visible;
                    } catch { }
                }
            }
        }

        private string ToStackName(string moduleName, string instanceName = null)
            => $"{Settings.Tier}-{instanceName ?? moduleName.Replace(".", "-")}";
    }
}