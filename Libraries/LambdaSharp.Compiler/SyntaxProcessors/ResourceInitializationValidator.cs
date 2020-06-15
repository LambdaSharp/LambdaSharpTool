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
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Compiler.TypeSystem;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ResourceInitializationValidator : ASyntaxProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly ErrorFunc ResourceUnknownProperty = parameter => new Error(0, $"unrecognized property '{parameter}'");
        private static readonly ErrorFunc ResourceUnknownType = parameter => new Error(0, $"unknown resource type '{parameter}'");
        private static readonly ErrorFunc ResourceMissingProperty = parameter => new Error(0, $"missing property '{parameter}'");
        private static readonly ErrorFunc ResourcePropertyExpectedMap = parameter => new Error(0, $"property type mismatch for '{parameter}', expected a map");
        private static readonly ErrorFunc ResourcePropertyExpectedList = parameter => new Error(0, $"property type mismatch for '{parameter}', expected a list");
        private static readonly ErrorFunc ResourcePropertyExpectedLiteral = parameter => new Error(0, $"property type mismatch for '{parameter}', expected a literal");
        private static readonly Warning ResourceContainsTransformAndCannotBeValidated = new Warning(0, "Fn::Transform prevents resource properties to be validated");
        #endregion

        //--- Constructors ---
        public ResourceInitializationValidator(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectType<IInitializedResourceDeclaration>(node => {

                // skip resources that are not being initialied or don't want to be validated
                if(!node.HasInitialization || !node.HasTypeValidation) {
                    return;
                }

                // validate resource initialization
                if(node.ResourceTypeName == null) {
                    throw new ShouldNeverHappenException();
                }
                if(Provider.TryGetResourceType(node.ResourceTypeName.Value, out var resourceType)) {

                    // validate resource properties for LambdaSharp custom resource type
                    ValidateProperties(resourceType, node.InitializationExpression ?? new ObjectExpression());
                } else {
                    Logger.Log(ResourceUnknownType(node.ResourceTypeName.Value), node.ResourceTypeName);
                }
            });
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
                    case ResourcePropertyCollectionType.NoCollection:

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
                    case ResourcePropertyCollectionType.List:

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
                    case ResourcePropertyCollectionType.Map:

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
            void Validate(IResourceProperty propertyType, AExpression expression, bool allowJson, Error error) {
                switch(propertyType.ItemType) {
                case ResourcePropertyItemType.Any:

                    // anything is valid; nothing to do
                    break;
                case ResourcePropertyItemType.ComplexType:

                    // validate experssion is an object matching the complex type
                    if(expression is ObjectExpression objectExpression) {
                        ValidateProperties(propertyType.ComplexType, objectExpression);
                    } else {
                        Logger.Log(error, expression);
                    }
                    break;
                case ResourcePropertyItemType.Boolean:
                case ResourcePropertyItemType.Double:
                case ResourcePropertyItemType.Integer:
                case ResourcePropertyItemType.Long:
                case ResourcePropertyItemType.String:
                case ResourcePropertyItemType.Timestamp:

                    // TODO: validate against primitive type
                    break;
                case ResourcePropertyItemType.Json:
                    if(allowJson) {

                        // TODO: validate against JSON type
                    } else {
                        throw new ShouldNeverHappenException($"unexpected map item type: {propertyType.ItemType}");
                    }
                    break;
                default:
                    throw new ShouldNeverHappenException($"unexpected map item type: {propertyType.ItemType}");
                }
            }
        }
    }
}