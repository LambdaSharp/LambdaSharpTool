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
using LambdaSharp.CloudFormation.Specification;

namespace LambdaSharp.Compiler.TypeSystem.CloudFormation {

    // NOTE (2020-06-11, bjorg): list of all possible types

    // Primitive Types
    //  - String
    //  - Long
    //  - Integer
    //  - Double
    //  - Boolean
    //  - Timestamp
    //  - Json

    // Collection Types
    //  - List<String>
    //  - List<Long>
    //  - List<Integer>
    //  - List<Double>
    //  - List<Boolean>
    //  - List<Timestamp>
    //  - Map<String>
    //  - Map<Long>
    //  - Map<Integer>
    //  - Map<Double>
    //  - Map<Boolean>
    //  - Map<Timestamp>

    // Complex Types
    //  - T
    //  - List<T>
    //  - Map<T>

    internal class CloudFormationProperty : IProperty {

        // TODO: leverage UpdateType: _propertyType.UpdateType;
        // TODO: leverage ValueType: _propertyType.Value.ValueType

        //--- Fields ---
        private readonly CloudFormationResourceType _resourceType;
        private readonly ResourcePropertyType _propertyType;
        private readonly ExtendedCloudFormationSpecification _specification;
        private readonly Lazy<IResourceType> _complexType;

        //--- Constructors ---
        public CloudFormationProperty(string propertyName, CloudFormationResourceType resourceType, ResourcePropertyType propertyType, ExtendedCloudFormationSpecification specification) {
            Name = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            _propertyType = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _complexType = new Lazy<IResourceType>(GetComplexType);
        }

        //--- Properties ---
        public string Name { get; }
        public bool Required => _propertyType.Required;
        public bool DuplicatesAllowed => _propertyType.DuplicatesAllowed;

        public PropertyCollectionType CollectionType {
            get {
                if(_propertyType.Type == "List") {
                    return PropertyCollectionType.List;
                } else if(_propertyType.Type == "Map") {
                    return PropertyCollectionType.Map;
                }
                return PropertyCollectionType.NoCollection;
            }
        }

        public PropertyItemType ItemType {
            get {
                if(CollectionType == PropertyCollectionType.NoCollection) {
                    switch(_propertyType.PrimitiveType) {
                    case null when (_propertyType.Type != null):
                        return PropertyItemType.ComplexType;
                    case "String":
                        return PropertyItemType.String;
                    case "Long":
                        return PropertyItemType.Long;
                    case "Integer":
                        return PropertyItemType.Integer;
                    case "Double":
                        return PropertyItemType.Double;
                    case "Boolean":
                        return PropertyItemType.Boolean;
                    case "Timestamp":
                        return PropertyItemType.Timestamp;
                    case "Json":
                        return PropertyItemType.Json;
                    default:
                        throw new ShouldNeverHappenException($"unexpected primitive type: {_propertyType.PrimitiveType ?? "<null>"} in {Name}");
                    }
                } else {
                    switch(_propertyType.PrimitiveItemType) {
                    case null when (_propertyType.ItemType != null):
                        return PropertyItemType.ComplexType;
                    case "String":
                        return PropertyItemType.String;
                    case "Long":
                        return PropertyItemType.Long;
                    case "Integer":
                        return PropertyItemType.Integer;
                    case "Double":
                        return PropertyItemType.Double;
                    case "Boolean":
                        return PropertyItemType.Boolean;
                    case "Timestamp":
                        return PropertyItemType.Timestamp;
                    case "Json":
                        throw new ShouldNeverHappenException("'Json' is not supported for collections");
                    default:
                        throw new ShouldNeverHappenException($"unexpected primitive item type: {_propertyType.PrimitiveItemType ?? "<null>"} in {Name}");
                    }
                }
            }
        }

        public IResourceType ComplexType => _complexType.Value;

        private IResourceType GetComplexType() {
            if(ItemType != PropertyItemType.ComplexType) {
                throw new InvalidOperationException("property uses a primitive type");
            }
            if(CollectionType == PropertyCollectionType.NoCollection) {
                return GetResourceType(_propertyType.Type ?? throw new ShouldNeverHappenException("'Type' is null"));
            } else {
                return GetResourceType(_propertyType.ItemType ?? throw new ShouldNeverHappenException("'ItemType' is null"));
            }
            throw new ShouldNeverHappenException();

            // local function
            CloudFormationResourceType GetResourceType(string complexTypeName) {
                var resourceSpecificComplexTypeName = $"{_resourceType.Name}.{complexTypeName}";
                if(_specification.PropertyTypes.TryGetValue(resourceSpecificComplexTypeName, out var type)) {
                    return new CloudFormationResourceType(resourceSpecificComplexTypeName, type, _specification);
                } else if(_specification.PropertyTypes.TryGetValue(complexTypeName, out type)) {
                    return new CloudFormationResourceType(complexTypeName, type, _specification);
                } else {
                    throw new ShouldNeverHappenException($"complex type name not found: {resourceSpecificComplexTypeName}");
                }
            }
        }
    }
}