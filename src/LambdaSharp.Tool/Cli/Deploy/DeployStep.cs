/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Deploy {
    using CloudFormationStack = Amazon.CloudFormation.Model.Stack;
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class DeployStep : AModelProcessor {


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
            XRayTracingLevel xRayTracingLevel,
            bool deployOnlyIfExists
        ) {
            Console.WriteLine($"Resolving module reference: {moduleReference}");

            // determine location of cloudformation template from module key
            if(!ModuleInfo.TryParse(moduleReference, out var moduleInfo)) {
                LogError($"invalid module reference: {moduleReference}");
                return false;
            }
            var foundModuleLocation = await _loader.ResolveInfoToLocationAsync(moduleInfo, ModuleManifestDependencyType.Root, allowImport: false, showError: !deployOnlyIfExists);
            if(foundModuleLocation == null) {

                // nothing to do; loader already emitted an error
                return deployOnlyIfExists;
            }

            // download module manifest
            var manifest = await _loader.LoadManifestFromLocationAsync(foundModuleLocation);
            if(manifest == null) {
                return false;
            }

            // deploy module
            if(dryRun == null) {
                var stackName = Settings.GetStackName(manifest.GetFullName(), instanceName);

                // check version of previously deployed module
                if(!deployOnlyIfExists) {
                    Console.WriteLine("=> Validating module for deployment tier");
                }
                var updateValidation = await IsValidModuleUpdateAsync(stackName, manifest, showError: !forceDeploy && !deployOnlyIfExists);
                if(!forceDeploy && !updateValidation.Success) {
                    return false;
                }

                // check if a previous deployment was found
                if(deployOnlyIfExists && (updateValidation.ExistingStack == null)) {

                    // nothing to do
                    return true;
                }
                var existing = updateValidation.ExistingStack;

                // check if existing stack checksum matches template checksum
                if(!forceDeploy && !parameters.Any()) {
                    var existingChecksum = existing?.Outputs.FirstOrDefault(output => output.OutputKey == "ModuleChecksum");
                    if(existingChecksum?.OutputValue == manifest.TemplateChecksum) {
                        Settings.WriteAnsiLine("=> No changes found to deploy", AnsiTerminal.BrightBlack);
                        return true;
                    }
                }

                // prompt for missing parameters
                var deployParameters = PromptModuleParameters(manifest, existing, parameters, promptAllParameters);
                if(HasErrors) {
                    return false;
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

                // discover shard module dependencies and prompt for missing parameters
                var dependencies = (await _loader.DiscoverAllDependenciesAsync(manifest, checkExisting: true, allowImport: false))
                    .Where(dependency => dependency.Type == ModuleManifestDependencyType.Shared)
                    .ToList();
                if(HasErrors) {
                    return false;
                }
                var dependenciesParameters = dependencies
                    .Select(dependency => new {
                        ModuleFullName = dependency.Manifest.GetFullName(),
                        Parameters = PromptModuleParameters(
                            dependency.Manifest,
                            promptAll: promptAllParameters
                        )
                    })
                    .ToDictionary(t => t.ModuleFullName, t => t.Parameters);
                if(HasErrors) {
                    return false;
                }

                // deploy module dependencies
                foreach(var dependency in dependencies) {
                    var dependencyLocation = new ModuleLocation(Settings.DeploymentBucketName, dependency.ModuleLocation.ModuleInfo, dependency.ModuleLocation.Hash);
                    if(!await new ModelUpdater(Settings, SourceFilename).DeployChangeSetAsync(
                        dependency.Manifest,
                        await _loader.GetNameMappingsFromLocationAsync(dependencyLocation),
                        dependencyLocation,
                        Settings.GetStackName(dependency.Manifest.GetFullName()),
                        allowDataLoos,
                        protectStack,
                        dependenciesParameters[dependency.Manifest.GetFullName()]
                    )) {
                        return false;
                    }
                }

                // deploy module
                var moduleLocation = new ModuleLocation(Settings.DeploymentBucketName, manifest.ModuleInfo, manifest.TemplateChecksum);
                return await new ModelUpdater(Settings, moduleReference).DeployChangeSetAsync(
                    manifest,
                    await _loader.GetNameMappingsFromLocationAsync(moduleLocation),
                    moduleLocation,
                    stackName,
                    allowDataLoos,
                    protectStack,
                    deployParameters
                );
            }
            return true;
        }

        private async Task<(bool Success, CloudFormationStack ExistingStack)> IsValidModuleUpdateAsync(string stackName, ModuleManifest manifest, bool showError) {

            // check if the module was already deployed
            var existing = await Settings.CfnClient.GetStackAsync(stackName, LogError);
            if(existing.Stack == null) {
                return (existing.Success, existing.Stack);
            }

            // validate existing module deployment
            var deployed = existing.Stack?.GetModuleVersionText();
            if(!ModuleInfo.TryParse(deployed, out var deployedModuleInfo)) {
                if(showError) {
                    LogError("unable to determine the name of the deployed module; use --force-deploy to proceed anyway");
                }
                return (false, existing.Stack);
            }
            if(deployedModuleInfo.FullName != manifest.GetFullName()) {
                if(showError) {
                    LogError($"deployed module name ({deployedModuleInfo.FullName}) does not match {manifest.GetFullName()}; use --force-deploy to proceed anyway");
                }
                return (false, existing.Stack);
            }
            var versionComparison = deployedModuleInfo.Version.CompareToVersion(manifest.GetVersion());
            if(versionComparison > 0) {
                if(showError) {
                    LogError($"deployed module version (v{deployedModuleInfo.Version}) is newer than v{manifest.GetVersion()}; use --force-deploy to proceed anyway");
                }
                return (false, existing.Stack);
            } else if(versionComparison == null) {
                if(showError) {
                    LogError($"deployed module version (v{deployedModuleInfo.Version}) is not compatible with v{manifest.GetVersion()}; use --force-deploy to proceed anyway");
                }
                return (false, existing.Stack);
            }
            return (true, existing.Stack);
        }

        private List<CloudFormationParameter> PromptModuleParameters(
            ModuleManifest manifest,
            CloudFormationStack existing = null,
            Dictionary<string, string> parameters = null,
            bool promptAll = false
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
                    Settings.PromptLabel(parameterGroup.Title.ToUpper());

                    // only prompt for required parameters
                    foreach(var parameter in parameterGroup.Parameters.Where(RequiresPrompt)) {

                        // check if parameter is multiple choice
                        string enteredValue;
                        if(parameter.AllowedValues?.Any() ?? false) {
                            var message = parameter.Name;
                            if(parameter.Label != null) {
                                message += $": {parameter.Label}";
                            }
                            enteredValue = Settings.PromptChoice(message, parameter.AllowedValues);
                        } else {
                            var message = $"{parameter.Name} [{parameter.Type}]";
                            if(parameter.Label != null) {
                                message += $": {parameter.Label}";
                            }
                            enteredValue = Settings.PromptString(message, parameter.Default, parameter.AllowedPattern, parameter.ConstraintDescription) ?? "";
                        }
                        stackParameters[parameter.Name] = new CloudFormationParameter {
                            ParameterKey = parameter.Name,
                            ParameterValue = enteredValue
                        };
                    }
                }
            }

            // check if LambdaSharp.Core services should be enabled by default
            if(
                (Settings.CoreServices == CoreServices.Enabled)
                && manifest.GetAllParameters().Any(p => p.Name == "LambdaSharpCoreServices")
                && !stackParameters.Any(p => p.Value.ParameterKey == "LambdaSharpCoreServices")
            ) {
                stackParameters.Add("LambdaSharpCoreServices", new CloudFormationParameter {
                    ParameterKey = "LambdaSharpCoreServices",
                    ParameterValue = Settings.CoreServices.ToString()
                });
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
                if(Settings.PromptsAsErrors) {
                    LogError($"{manifest.GetFullName()} requires value for parameter '{parameter.Name}'");
                    return false;
                }
                return true;
            }
        }
   }
}