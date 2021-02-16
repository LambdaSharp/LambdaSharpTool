/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using Humidifier;
using Humidifier.Json;
using LambdaSharp.Modules;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelStackGenerator : AModelProcessor {

        //--- Fields ---
        private Module _module;
        private Stack _stack;

        //--- Constructors ---
        public ModelStackGenerator(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public string Generate(Module module, string gitSha, string gitBranch) {
            _module = module;

            // stack header
            _stack = new Stack {
                AWSTemplateFormatVersion = "2010-09-09",
                Description = (_module.Description != null)
                    ? _module.Description.TrimEnd() + $" (v{_module.Version})"
                    : null
            };

            // check if we need to add the SAM transform to the output template
            if(module.HasSamTransform) {
                _stack.Transform = "AWS::Serverless-2016-10-31";
            }

            // add items
            foreach(var item in _module.Items) {
                AddItem(item);
            }

            // add interface for presenting inputs
            var inputParameters = _module.Items.OfType<ParameterItem>();
            _stack.AddTemplateMetadata("AWS::CloudFormation::Interface", new Dictionary<string, object> {
                ["ParameterLabels"] = inputParameters.Where(input => (input.Label != null) || (input.Description != null)).ToDictionary(
                    input => input.LogicalId,
                    input => new Dictionary<string, object> {
                        ["default"] = $"{input.Label ?? input.Description} [{input.Type}]"
                    }
                ),
                ["ParameterGroups"] = inputParameters
                    .GroupBy(input => input.Section)
                    .Select(section => new Dictionary<string, object> {
                        ["Label"] = new Dictionary<string, string> {
                            ["default"] = section.Key
                        },
                        ["Parameters"] = section.Select(input => input.LogicalId).ToList()
                    }
                )
            });

            // add module manifest
            var manifest = new ModuleManifest {
                ModuleInfo = module.ModuleInfo,
                Description = module.Description,
                CoreServicesVersion = Settings.CoreServicesReferenceVersion,
                ParameterSections = inputParameters
                    .GroupBy(input => input.Section)
                    .Where(group => group.Key != "LambdaSharp Deployment Settings (DO NOT MODIFY)")
                    .Select(group => new ModuleManifestParameterSection {
                        Title = group.Key,
                        Parameters = group.Select(input => new ModuleManifestParameter {
                            Name = input.Name,
                            Type = input.Type,
                            Label = input.Label,
                            Default = input.Parameter.Default,
                            Import = input.Import,
                            AllowedValues = input.Parameter.AllowedValues,
                            AllowedPattern = input.Parameter.AllowedPattern,
                            ConstraintDescription = input.Parameter.ConstraintDescription,
                            MinValue = input.Parameter.MinValue,
                            MaxValue = input.Parameter.MaxValue,
                            MinLength = input.Parameter.MinLength,
                            MaxLength = input.Parameter.MaxLength
                        }).ToList()
                    }).ToList(),
                Artifacts = module.Artifacts.ToList(),
                Dependencies = module.Dependencies

                    // no need to store LambdaSharp.Core dependency since the manifest already has a CoreServicesVersion property
                    .Where(dependency => dependency.Value.ModuleLocation.ModuleInfo.FullName != "LambdaSharp.Core")
                    .Select(dependency => new ModuleManifestDependency {
                        ModuleInfo = dependency.Value.ModuleLocation.ModuleInfo,
                        Type = dependency.Value.Type
                    })
                    .OrderBy(dependency => dependency.ModuleInfo.ToString())
                    .ToList(),
                ResourceTypes = module.CustomResourceTypes.ToList(),
                Outputs = module.Items
                    .Where(item => item.Scope.Any(scope => scope == "public"))
                    .Select(item => new ModuleManifestOutput {
                        Name = item.FullName,
                        Description = item.Description,
                        Type = item.Type
                    })
                    .OrderBy(output => output.Name)
                    .ToList(),
            };
            _stack.AddTemplateMetadata("LambdaSharp::Manifest", manifest);

            // add resource name and type name mappings
            _stack.AddTemplateMetadata("LambdaSharp::NameMappings", new ModuleNameMappings {
                TypeNameMappings = module.ResourceTypeNameMappings
                    .Where(kv => _stack.Resources.Any(resource => resource.Value.AWSTypeName == kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                ResourceNameMappings = module.Items

                    // we only ned to worry about resource names
                    .Where(item => _stack.Resources.ContainsKey(item.LogicalId))

                    // we only care about items where the logical ID and full-name don't match
                    .Where(item => item.LogicalId != item.FullName)
                    .ToDictionary(item => item.LogicalId, item => item.FullName)
            });

            // update template with template hash
            var templateHash = GenerateCloudFormationTemplateChecksum();
            manifest.TemplateChecksum = templateHash;
            manifest.Date = Settings.UtcNow;
            if((gitSha != null) || (gitBranch != null)) {
                manifest.Git = new ModuleManifestGitInfo {
                    SHA = gitSha,
                    Branch = gitBranch
                };
            }
            _stack.Parameters["DeploymentChecksum"].Default = templateHash;

            // add outputs
            _stack.Add("ModuleInfo", new Humidifier.Output {
                Value = _module.ModuleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER).ToString()
            });
            _stack.Add("ModuleChecksum", new Humidifier.Output {
                Value = Fn.Ref("DeploymentChecksum")
            });
            _stack.Add("LambdaSharpTool", new Humidifier.Output {
                Value = Settings.ToolVersion.ToString()
            });
            _stack.Add("LambdaSharpTier", new Humidifier.Output {
                Value = Fn.Select("0", Fn.Split("-", Fn.Ref("DeploymentPrefix")))
            });

            // generate JSON template
            return new JsonStackSerializer().Serialize(_stack);
        }

        private void AddItem(AModuleItem item) {
            switch(item) {
            case VariableItem _:
            case PackageItem _:
                if(item.IsPublic) {
                    AddExport(item);
                }
                break;
            case ResourceItem resourceItem:
                var deletionPolicy = (resourceItem.DeletionPolicy != null)
                    ? (DeletionPolicy?)Enum.Parse<DeletionPolicy>(resourceItem.DeletionPolicy, ignoreCase: true)
                    : null;
                _stack.Add(
                    resourceItem.LogicalId,
                    resourceItem.Resource,
                    resourceItem.Condition,
                    dependsOn: resourceItem.DependsOn.ToArray(),
                    deletionPolicy: deletionPolicy
                );
                if(item.IsPublic) {
                    AddExport(item);
                }
                break;
            case ParameterItem parameterItem:
                _stack.Add(parameterItem.LogicalId, parameterItem.Parameter);
                if(item.IsPublic) {
                    AddExport(item);
                }
                break;
            case FunctionItem functionItem:
                _stack.Add(
                    functionItem.LogicalId,
                    functionItem.Function,
                    functionItem.Condition,
                    dependsOn: functionItem.DependsOn.ToArray()
                );
                if(item.IsPublic) {
                    AddExport(item);
                }
                break;
            case ConditionItem conditionItem:
                _stack.Add(conditionItem.LogicalId, new Condition(conditionItem.Reference));
                break;
            case MappingItem mappingItem: {
                    var mapping = new Mapping();
                    foreach(var level1Mapping in mappingItem.Mapping) {
                        mapping[level1Mapping.Key] = level1Mapping.Value.ToDictionary(
                            level2Mapping => level2Mapping.Key,
                            level2Mapping => level2Mapping.Value
                        );
                    }
                    _stack.Add(mappingItem.LogicalId, mapping);
                }
                break;
            case ResourceTypeItem resourceTypeItem:
                _stack.Add(resourceTypeItem.LogicalId, new Humidifier.Output {
                    Description = resourceTypeItem.Description,
                    Value = _module.Items.First(i => i.LogicalId == resourceTypeItem.Handler).GetExportReference(),
                    Export = new Dictionary<string, dynamic> {
                        ["Name"] = Fn.Sub($"${{DeploymentPrefix}}{resourceTypeItem.CustomResourceType}")
                    }
                });
                break;
            case AppItem _:

                // nothing to do
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item, "unknown parameter type");
            }

            // local functions
            void AddExport(AModuleItem exportItem) {
                var value = exportItem.GetExportReference();

                // TODO (2020-07-14, bjorg): add support for negated condition (requires us to generate an intermediate condition)
                //  !If [ Condition, !Ref AWS::NoValue, !Ref Value ]

                // check if this is a conditional public value
                if(
                    TryGetFnIf(value, out var condition, out var ifTrue, out var ifFalse)
                    && TryGetFnRef(ifFalse, out var key)
                    && (key == "AWS::NoValue")
                ) {
                    _stack.Add(exportItem.LogicalId, new Humidifier.Output {
                        Description = exportItem.Description,
                        Condition = condition,
                        Value = ifTrue,
                        Export = new Dictionary<string, dynamic> {
                            ["Name"] = Fn.Sub($"${{AWS::StackName}}::{exportItem.FullName}")
                        }
                    });
                } else {
                    _stack.Add(exportItem.LogicalId, new Humidifier.Output {
                        Description = exportItem.Description,
                        Value = value,
                        Export = new Dictionary<string, dynamic> {
                            ["Name"] = Fn.Sub($"${{AWS::StackName}}::{exportItem.FullName}")
                        }
                    });
                }
            }
        }

        private string GenerateCloudFormationTemplateChecksum() => StringEx.GetJsonChecksum(new JsonStackSerializer().Serialize(_stack));
    }
}