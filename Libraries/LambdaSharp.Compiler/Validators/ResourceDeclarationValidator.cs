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
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Compiler.TypeSystem;

namespace LambdaSharp.Compiler.Validators {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ResourceDeclarationValidator : AValidator {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly ErrorFunc ResourceUnknownProperty = parameter => new Error(0, $"unrecognized property '{parameter}'");
        private static readonly Error IfAttributeRequiresCloudFormationType = new Error(0, "'If' attribute can only be used with a CloudFormation type");
        private static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error(0, "'Properties' attribute can only be used with a CloudFormation type");
        private static readonly ErrorFunc ResourceUnknownType = parameter => new Error(0, $"unknown resource type '{parameter}'");
        private static readonly Error TypeAttributeMissing = new Error(0, "'Type' attribute is required");
        private static readonly Error ResourceValueAttributeInvalid = new Error(0, "'Value' attribute must be a valid ARN or wildcard");
        private static readonly ErrorFunc ResourceMissingProperty = parameter => new Error(0, $"missing property '{parameter}'");
        private static readonly ErrorFunc ResourcePropertyExpectedMap = parameter => new Error(0, $"property type mismatch for '{parameter}', expected a map");
        private static readonly ErrorFunc ResourcePropertyExpectedList = parameter => new Error(0, $"property type mismatch for '{parameter}', expected a list");
        private static readonly ErrorFunc ResourcePropertyExpectedLiteral = parameter => new Error(0, $"property type mismatch for '{parameter}', expected a literal");
        private static readonly Warning ResourceContainsTransformAndCannotBeValidated = new Warning(0, "Fn::Transform prevents resource properties to be validated");
        #endregion

        //--- Constructors ---
        public ResourceDeclarationValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectType<ResourceDeclaration>(node => {

                // check if declaration is a resource reference
                if(node.Value != null) {

                    // referenced resource cannot be conditional
                    if(node.If != null) {
                        Logger.Log(IfAttributeRequiresCloudFormationType, node.If);
                    }

                    // referenced resource cannot have properties
                    if(node.Properties != null) {
                        Logger.Log(PropertiesAttributeRequiresCloudFormationType, node.Properties);
                    }

                    // validate Value attribute
                    if(node.Value is ListExpression listExpression) {
                        foreach(var arnValue in listExpression) {
                            ValidateARN(arnValue);
                        }

                        // default type to 'List'
                        if(node.Type == null) {

                            // TODO: what's the best type here?
                            node.Type = Fn.Literal("List");
                        }
                    } else {
                        ValidateARN(node.Value);

                        // default type to 'String'
                        if(node.Type == null) {
                            node.Type = Fn.Literal("String");
                        }
                    }
                } else if(node.Type != null) {

                    // ensure Properties property is set to an empty object expression when null
                    if(node.Properties == null) {
                        node.Properties = new ObjectExpression();
                    }

                    // check if type is AWS resource type or a LambdaSharp custom resource type
                    if(Provider.TryGetResourceType(node.Type.Value, out var resourceType)) {

                        // validate resource properties for LambdaSharp custom resource type
                        if(node.HasTypeValidation) {
                            ValidateProperties(resourceType, node.Properties);
                        }
                    } else {
                        Logger.Log(ResourceUnknownType(node.Type.Value), node.Type);
                    }
                } else {

                    // CloudFormation resource must have a type
                    Logger.Log(TypeAttributeMissing, node);
                }
            });

            // local functions
            void ValidateARN(AExpression arn) {
                if(
                    !(arn is LiteralExpression literalExpression)
                    || (
                        !literalExpression.Value.StartsWith("arn:", StringComparison.Ordinal)
                        && (literalExpression.Value != "*")
                    )
                ) {
                    Logger.Log(ResourceValueAttributeInvalid, arn);
                }
            }
        }

        private void ValidateProperties(IResourceType currentResource, ObjectExpression currentProperties) {

            // 'Fn::Transform' can add arbitrary properties at deployment time, so we can't validate the properties at compile time
            if(currentProperties.ContainsKey("Fn::Transform")) {
                Logger.Log(ResourceContainsTransformAndCannotBeValidated, currentProperties);
            } else {

                // check that all required properties are defined
                foreach(var property in currentResource.RequiredProperties) {
                    if(!currentProperties.ContainsKey(property.Name)) {
                        Logger.Log(ResourceMissingProperty(property.Name), currentProperties);
                    }
                }
            }

            // check that all referenced properties exist
            foreach(var currentProperty in currentProperties) {
                if(currentResource.TryGetProperty(currentProperty.Key.Value, out var propertyType)) {

                    // check if property represents a collection of items or a single item
                    switch(propertyType.CollectionType) {
                    case PropertyCollectionType.NoCollection:

                        // check the property expression type is a compatible list
                        switch(currentProperty.Value) {
                        case AFunctionExpression _:

                            // TODO: validate the return type of the function matches the item type
                            break;
                        case LiteralExpression _:
                        case ObjectExpression _:

                            // validate value against item type
                            Validate(propertyType, currentProperty.Value, allowJson: true, ResourcePropertyExpectedLiteral(currentProperty.Key.Value));
                            break;
                        default:
                            Logger.Log(ResourcePropertyExpectedLiteral(currentProperty.Key.Value), currentProperty.Value);
                            break;
                        }
                        break;
                    case PropertyCollectionType.List:

                        // check the property expression type is a compatible list
                        switch(currentProperty.Value) {
                        case AFunctionExpression _:

                            // TODO: validate the return type of the function is a list
                            break;
                        case ListExpression listExpression:

                            // validate all items in list are objects that match the nested resource type
                            for(var i = 0; i < listExpression.Count; ++i) {
                                var item = listExpression[i];
                                Validate(propertyType, item, allowJson: false, ResourcePropertyExpectedMap($"{currentProperty.Key.Value}[{i}]"));
                            }
                            break;
                        default:
                            Logger.Log(ResourcePropertyExpectedList(currentProperty.Key.Value), currentProperty.Value);
                            break;
                        }
                        break;
                    case PropertyCollectionType.Map:

                        // check the property expression type is a compatible map
                        switch(currentProperty.Value) {
                        case AFunctionExpression _:

                            // TODO: validate the return type of the function is a map
                            break;
                        case ObjectExpression objectExpression:

                            // validate all values in map are objects that match the nested resource type
                            foreach(var kv in objectExpression) {
                                var item = kv.Value;
                                Validate(propertyType, item, allowJson: false, ResourcePropertyExpectedMap($"{currentProperty.Key.Value}.{kv.Key.Value}"));
                            }
                            break;
                        default:
                            Logger.Log(ResourcePropertyExpectedMap(currentProperty.Key.Value), currentProperty.Value);
                            break;
                        }
                        break;
                    default:
                        throw new ShouldNeverHappenException();
                    }
                } else {
                    Logger.Log(ResourceUnknownProperty(currentProperty.Key.Value), currentProperty.Key);
                }
            }

            // local function
            void Validate(IProperty propertyType, AExpression expression, bool allowJson, Error error) {
                switch(propertyType.ItemType) {
                case PropertyItemType.Any:

                    // anything is valid; nothing to do
                    break;
                case PropertyItemType.ComplexType:

                    // validate experssion is an object matching the complex type
                    if(expression is ObjectExpression objectExpression) {
                        ValidateProperties(propertyType.ComplexType, objectExpression);
                    } else {
                        Logger.Log(error, expression);
                    }
                    break;
                case PropertyItemType.Boolean:
                case PropertyItemType.Double:
                case PropertyItemType.Integer:
                case PropertyItemType.Long:
                case PropertyItemType.String:
                case PropertyItemType.Timestamp:

                    // TODO: validate against primitive type
                    break;
                case PropertyItemType.Json:
                    if(allowJson) {

                        // TODO: validate against JSON type
                    } else {
                        throw new ShouldNeverHappenException($"unexpected map item type: {propertyType.ItemType}");
                    }
                    break;
                default:
                    throw new ShouldNeverHappenException($"unexpected map item type: {propertyType.ItemType}");
                }            }
        }
    }
}