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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Compiler.Parser.Syntax;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class DiscoverDependenciesAnalyzer : ASyntaxAnalyzer {

        //--- Class Fields ---
        private static readonly HashSet<string> _reservedResourceTypePrefixes = new HashSet<string> {
            "Alexa",
            "AMZN",
            "Amazon",
            "ASK",
            "AWS",
            "Custom",
            "Dev"
        };

        //--- Class Methods ---
        private static bool IsValidCloudFormationType(string type) {
            switch(type) {

            // CloudFormation primitive types
            case "String":
            case "Long":
            case "Integer":
            case "Double":
            case "Boolean":
            case "Timestamp":
                return true;

            // LambdaSharp primitive types
            case "Secret":
                return true;
            default:
                return false;
            }
        }

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public DiscoverDependenciesAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override bool VisitStart(ModuleDeclaration node) {
            if(node.HasModuleRegistration) {

                // add module reference as a shared dependency
                _builder.AddDependencyAsync(
                    new ModuleInfo("LambdaSharp", "Core", _builder.CoreServicesReferenceVersion, "lambdasharp"),
                    ModuleManifestDependencyType.Shared,
                    node: null
                ).Wait();
            }

            // load CloudFormation resource specification
            _builder.LoadCloudFormationSpecAsync(node.CloudFormation).Wait();
            return true;
        }

        public override bool VisitStart(UsingModuleDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.ModuleName.Value, out var moduleInfo)) {
                _builder.Log(Error.ModuleAttributeInvalid, node.ModuleName);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // add module reference as a shared dependency
                _builder.AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Shared, node.ModuleName).Wait();
            }
            return true;
        }

        public override bool VisitStart(NestedModuleDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module?.Value, out var moduleInfo)) {
                _builder.Log(Error.ModuleAttributeInvalid, node.Module);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // add module reference as a nested dependency
                _builder.AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Nested, node.Module).Wait();
            }
            return true;
        }

        public override bool VisitStart(ResourceTypeDeclaration node) {

            // validate resource type name
            var resourceTypeNameParts = node.ItemName.Value.Split("::", 2);
            if(resourceTypeNameParts.Length == 1) {
                _builder.Log(Error.ResourceTypeNameInvalidFormat, node.ItemName);
            }
            if(_reservedResourceTypePrefixes.Contains(resourceTypeNameParts[0])) {
                _builder.Log(Error.ResourceTypeNameReservedPrefix(resourceTypeNameParts[0]), node.ItemName);
            }

            // NOTE (2019-11-05, bjorg): additional processing happens in VisitEnd() after the property and attribute nodes have been processed
            return true;
        }

        public override ASyntaxNode? VisitEnd(ResourceTypeDeclaration node) {

            // TODO: better rules for parsing CloudFormation types
            //  - https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html

            // ensure unique property names
            var names = new HashSet<string>();
            var properties = new List<ModuleManifestResourceProperty>();
            if(node.Properties.Any()) {
                foreach(var property in node.Properties) {
                    if(names.Add(property.Name.Value)) {
                        properties.Add(new ModuleManifestResourceProperty {
                            Name = property.Name.Value,
                            Description = property.Description?.Value,
                            Type = property.Type?.Value ?? "String",
                            Required = property.Required?.AsBool() ?? false
                        });
                    } else {
                        _builder.Log(Error.ResourceTypePropertyDuplicateName(property.Name.Value), property.Name);
                    }
                }
            } else {
                _builder.Log(Error.ResourceTypePropertiesAttributeIsInvalid, node);
            }

            // ensure unique attribute names
            names.Clear();
            var attributes = new List<ModuleManifestResourceAttribute>();
            if(node.Attributes.Any()) {
                foreach(var attribute in node.Attributes) {
                    if(names.Add(attribute.Name.Value)) {
                        attributes.Add(new ModuleManifestResourceAttribute {
                            Name = attribute.Name.Value,
                            Description = attribute.Description?.Value,
                            Type = attribute.Type?.Value ?? "String"
                        });
                    } else {
                        _builder.Log(Error.ResourceTypeAttributeDuplicateName(attribute.Name.Value), attribute.Name);
                    }
                }
            } else {
                _builder.Log(Error.ResourceTypeAttributesAttributeIsInvalid, node);
            }

            // register custom resource type
            var resourceType = new ModuleManifestResourceType {
                Type = node.ItemName.Value,
                Description = node.Description?.Value,
                Properties = properties,
                Attributes = attributes
            };
            if(!_builder.LocalResourceTypes.TryAdd(resourceType.Type, resourceType)) {
                _builder.Log(Error.ResourceTypeDuplicateName(resourceType.Type), node);
            }
            return node;
        }

        public override bool VisitStart(ResourceTypeDeclaration.PropertyTypeExpression node) {
            if(!_builder.IsValidCloudFormationName(node.Name.Value)) {
                _builder.Log(Error.ResourceTypePropertyNameMustBeAlphanumeric, node);
            }
            if(node.Type == null) {

                // default Type is String when omitted
                node.Type = Literal("String");
            } else if(!IsValidCloudFormationType(node.Type.Value)) {
                _builder.Log(Error.ResourceTypePropertyTypeIsInvalid, node.Type);
            }
            if((node.Required != null) && !node.Required.IsBool) {
                _builder.Log(Error.ResourceTypePropertyRequiredMustBeBool, node.Required);
            }
            return true;
        }

        public override bool VisitStart(ResourceTypeDeclaration.AttributeTypeExpression node) {
            if(!_builder.IsValidCloudFormationName(node.Name.Value)) {
                _builder.Log(Error.ResourceTypeAttributeNameMustBeAlphanumeric, node);
            }
            if(node.Type == null) {

                // default Type is String when omitted
                node.Type = Literal("String");
            } else if(!IsValidCloudFormationType(node.Type.Value)) {
                _builder.Log(Error.ResourceTypeAttributeTypeIsInvalid, node.Type);
            }
            return true;
        }
    }
}
