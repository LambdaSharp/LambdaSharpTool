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
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class ResourceDeclarationValidator : AValidator {

        //--- Constructors ---
        public ResourceDeclarationValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectNode(node => {
                switch(node) {
                case ResourceDeclaration resourceDeclaration:
                    ValidateResourceDeclaration(resourceDeclaration);
                    break;
                }
            });

            // local functions
            void ValidateResourceDeclaration(ResourceDeclaration node) {

                // check if declaration is a resource reference
                if(node.Value != null) {

                    // referenced resource cannot be conditional
                    if(node.If != null) {
                        Logger.Log(Error.IfAttributeRequiresCloudFormationType, node.If);
                    }

                    // referenced resource cannot have properties
                    if(node.Properties != null) {
                        Logger.Log(Error.PropertiesAttributeRequiresCloudFormationType, node.Properties);
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
                            ValidateProperties(node.Type.Value, resourceType, node.Properties);
                        }
                    } else {
                        Logger.Log(Error.ResourceUnknownType(node.Type.Value), node.Type);
                    }
                } else {

                    // CloudFormation resource must have a type
                    Logger.Log(Error.TypeAttributeMissing, node);
                }

                // local functions
                void ValidateARN(AExpression arn) {
                    if(
                        !(arn is LiteralExpression literalExpression)
                        || (
                            !literalExpression.Value.StartsWith("arn:", StringComparison.Ordinal)
                            && (literalExpression.Value != "*")
                        )
                    ) {
                        Logger.Log(Error.ResourceValueAttributeInvalid, arn);
                    }
                }
            }
        }

        private void ValidateProperties(string awsType, ResourceType currentResource, ObjectExpression currentProperties) {

            // 'Fn::Transform' can add arbitrary properties at deployment time, so we can't validate the properties at compile time
            if(currentProperties.ContainsKey("Fn::Transform")) {
                Logger.Log(Warning.ResourceContainsTransformAndCannotBeValidated, currentProperties);
            } else {

                // check that all required properties are defined
                foreach(var property in currentResource.Properties.Where(kv => kv.Value.Required)) {
                    if(!currentProperties.ContainsKey(property.Key)) {
                        Logger.Log(Error.ResourceMissingProperty(property.Key), currentProperties);
                    }
                }
            }

            // check that all defined properties exist
            foreach(var currentProperty in currentProperties) {
                if(currentResource.Properties.TryGetValue(currentProperty.Key.Value, out var propertyType)) {
                    switch(propertyType.Type) {
                    case "List": {
                            switch(currentProperty.Value) {
                            case AFunctionExpression _:

                                // TODO (2019-01-25, bjorg): validate the return type of the function is a list
                                break;
                            case ListExpression listExpression:
                                if(propertyType.ItemType != null) {
                                    if(!TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResourceType)) {
                                        throw new ShouldNeverHappenException($"unable to find property type for: {awsType}.{propertyType.ItemType}");
                                    }

                                    // validate all items in list are objects that match the nested resource type
                                    for(var i = 0; i < listExpression.Count; ++i) {
                                        var item = listExpression[i];
                                        if(item is ObjectExpression objectExpressionItem) {
                                            ValidateProperties(awsType, nestedResourceType, objectExpressionItem);
                                        } else {
                                            Logger.Log(Error.ResourcePropertyExpectedMap($"[{i}]"), item);
                                        }
                                    }
                                } else {

                                    // TODO (2018-12-06, bjorg): validate list items using the primitive type
                                }
                                break;
                            default:
                                Logger.Log(Error.ResourcePropertyExpectedList(currentProperty.Key.Value), currentProperty.Value);
                                break;
                            }
                        }
                        break;
                    case "Map": {
                            switch(currentProperty.Value) {
                            case AFunctionExpression _:

                                // TODO (2019-01-25, bjorg): validate the return type of the function is a map
                                break;
                            case ObjectExpression objectExpression:
                                if(propertyType.ItemType != null) {
                                    if(!TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResourceType)) {
                                        throw new ShouldNeverHappenException($"unable to find property type for: {awsType}.{propertyType.ItemType}");
                                    }

                                    // validate all values in map are objects that match the nested resource type
                                    foreach(var kv in objectExpression) {
                                        var item = kv.Value;
                                        if(item is ObjectExpression objectExpressionItem) {
                                            ValidateProperties(awsType, nestedResourceType, objectExpressionItem);
                                        } else {
                                            Logger.Log(Error.ResourcePropertyExpectedMap(kv.Key.Value), item);
                                        }
                                    }
                                } else {

                                    // TODO (2018-12-06, bjorg): validate map entries using the primitive type
                                }
                                break;
                            default:
                                Logger.Log(Error.ResourcePropertyExpectedMap(currentProperty.Key.Value), currentProperty.Value);
                                break;
                            }
                        }
                        break;
                    case null:

                        // TODO (2018-12-06, bjorg): validate property value with the primitive type
                        break;
                    default: {
                            switch(currentProperty.Value) {
                            case AFunctionExpression _:

                                // TODO (2019-01-25, bjorg): validate the return type of the function is a map
                                break;
                            case ObjectExpression objectExpression:
                                if(!TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResourceType)) {
                                    throw new ShouldNeverHappenException($"unable to find property type for: {awsType}.{propertyType.ItemType}");
                                }
                                ValidateProperties(awsType, nestedResourceType, objectExpression);
                                break;
                            default:
                                Logger.Log(Error.ResourcePropertyExpectedMap(currentProperty.Key.Value), currentProperty.Value);
                                break;
                            }
                        }
                        break;
                    }
                } else {
                    Logger.Log(Error.ResourceUnknownProperty(currentProperty.Key.Value, awsType), currentProperty.Key);
                }
            }
        }

        private bool TryGetPropertyItemType(string rootAwsType, string itemTypeName, out ResourceType type)
            // => PropertyTypes.TryGetValue(rootAwsType + "." + itemTypeName, out type)
            //     || PropertyTypes.TryGetValue(itemTypeName, out type);
            => throw new NotImplementedException();
    }
}