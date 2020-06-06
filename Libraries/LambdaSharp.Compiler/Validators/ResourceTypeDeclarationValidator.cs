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
using System.Linq;
using LambdaSharp.Compiler.Model;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class ResourceTypeDeclarationValidator : AValidator {

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

        //--- Constructors ---
        public ResourceTypeDeclarationValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public IEnumerable<ModuleManifestResourceType> FindResourceTypes(ModuleDeclaration moduleDeclaration) {
            var result = new List<ModuleManifestResourceType>();
            moduleDeclaration.InspectNode(node => {
                switch(node) {
                case ResourceTypeDeclaration resourceTypeDeclaration:
                    result.Add(ValidateResourceTypeDeclaration(resourceTypeDeclaration));
                    break;
                }
            });
            return result;
        }

        private ModuleManifestResourceType ValidateResourceTypeDeclaration(ResourceTypeDeclaration node) {

            // TODO: better rules for parsing CloudFormation types
            //  - https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html

            // validate resource type name
            var resourceTypeNameParts = node.ItemName.Value.Split("::", 2);
            if(resourceTypeNameParts.Length == 1) {
                Logger.Log(Error.ResourceTypeNameInvalidFormat, node.ItemName);
            }
            if(_reservedResourceTypePrefixes.Contains(resourceTypeNameParts[0])) {
                Logger.Log(Error.ResourceTypeNameReservedPrefix(resourceTypeNameParts[0]), node.ItemName);
            }

            // ensure unique property names
            var names = new HashSet<string>();
            var properties = new List<ModuleManifestResourceProperty>();
            if(node.Properties.Any()) {
                foreach(var property in node.Properties) {
                    if(!CloudFormationValidationRules.IsValidCloudFormationName(property.Name.Value)) {
                        Logger.Log(Error.ResourceTypePropertyNameMustBeAlphanumeric, property);
                    }
                    if(property.Type == null) {

                        // default Type is String when omitted
                        property.Type = Fn.Literal("String");
                    } else if(!IsValidCloudFormationType(property.Type.Value)) {
                        Logger.Log(Error.ResourceTypePropertyTypeIsInvalid, property.Type);
                    }
                    if((property.Required != null) && !property.Required.IsBool) {
                        Logger.Log(Error.ResourceTypePropertyRequiredMustBeBool, property.Required);
                    }
                    if(names.Add(property.Name.Value)) {
                        properties.Add(new ModuleManifestResourceProperty {
                            Name = property.Name.Value,
                            Description = property.Description?.Value,
                            Type = property.Type?.Value ?? "String",
                            Required = property.Required?.AsBool() ?? false
                        });
                    } else {
                        Logger.Log(Error.ResourceTypePropertyDuplicateName(property.Name.Value), property.Name);
                    }
                }
            } else {
                Logger.Log(Error.ResourceTypePropertiesAttributeIsInvalid, node);
            }

            // ensure unique attribute names
            names.Clear();
            var attributes = new List<ModuleManifestResourceAttribute>();
            if(node.Attributes.Any()) {
                foreach(var attribute in node.Attributes) {
                    if(!CloudFormationValidationRules.IsValidCloudFormationName(attribute.Name.Value)) {
                        Logger.Log(Error.ResourceTypeAttributeNameMustBeAlphanumeric, attribute);
                    }
                    if(attribute.Type == null) {

                        // default Type is String when omitted
                        attribute.Type = Fn.Literal("String");
                    } else if(!IsValidCloudFormationType(attribute.Type.Value)) {
                        Logger.Log(Error.ResourceTypeAttributeTypeIsInvalid, attribute.Type);
                    }
                    if(names.Add(attribute.Name.Value)) {
                        attributes.Add(new ModuleManifestResourceAttribute {
                            Name = attribute.Name.Value,
                            Description = attribute.Description?.Value,
                            Type = attribute.Type?.Value ?? "String"
                        });
                    } else {
                        Logger.Log(Error.ResourceTypeAttributeDuplicateName(attribute.Name.Value), attribute.Name);
                    }
                }
            } else {
                Logger.Log(Error.ResourceTypeAttributesAttributeIsInvalid, node);
            }
            return new ModuleManifestResourceType {
                Type = node.ItemName.Value,
                Description = node.Description?.Value,
                Properties = properties,
                Attributes = attributes
            };
        }
    }
}