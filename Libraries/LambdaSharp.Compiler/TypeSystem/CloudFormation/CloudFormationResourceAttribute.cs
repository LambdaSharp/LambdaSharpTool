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

    internal class CloudFormationResourceAttribute : IResourceAttribute {

        //--- Fields ---
        private readonly CloudFormationResourceType _resourceType;
        private readonly ResourceAttributeType _attributeType;
        private readonly ExtendedCloudFormationSpecification _specification;
        private readonly Lazy<IResourceType> _complexType;

        //--- Constructors ---
        public CloudFormationResourceAttribute(string propertyName, CloudFormationResourceType resourceType, ResourceAttributeType attributeType, ExtendedCloudFormationSpecification specification) {
            Name = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            _attributeType = attributeType ?? throw new ArgumentNullException(nameof(attributeType));
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _complexType = new Lazy<IResourceType>(GetComplexType);
        }

        //--- Properties ---
        public string Name { get; }

        public ResourceCollectionType CollectionType {
            get {
                if(_attributeType.Type == "List") {
                    return ResourceCollectionType.List;
                } else if(_attributeType.Type == "Map") {
                    return ResourceCollectionType.Map;
                }
                return ResourceCollectionType.NoCollection;
            }
        }

        public ResourceItemType ItemType {
            get {
                if(CollectionType == ResourceCollectionType.NoCollection) {
                    switch(_attributeType.PrimitiveType) {
                    case null when (_attributeType.Type != null):
                        return ResourceItemType.ComplexType;
                    case "String":
                        return ResourceItemType.String;
                    case "Long":
                        return ResourceItemType.Long;
                    case "Integer":
                        return ResourceItemType.Integer;
                    case "Double":
                        return ResourceItemType.Double;
                    case "Boolean":
                        return ResourceItemType.Boolean;
                    case "Timestamp":
                        return ResourceItemType.Timestamp;
                    case "Json":
                        return ResourceItemType.Json;
                    default:
                        throw new ShouldNeverHappenException($"unexpected primitive type: {_attributeType.PrimitiveType ?? "<null>"} in {Name}");
                    }
                } else {
                    switch(_attributeType.PrimitiveItemType) {
                    case null when (_attributeType.ItemType != null):
                        return ResourceItemType.ComplexType;
                    case "String":
                        return ResourceItemType.String;
                    case "Long":
                        return ResourceItemType.Long;
                    case "Integer":
                        return ResourceItemType.Integer;
                    case "Double":
                        return ResourceItemType.Double;
                    case "Boolean":
                        return ResourceItemType.Boolean;
                    case "Timestamp":
                        return ResourceItemType.Timestamp;
                    case "Json":
                        return ResourceItemType.Json;
                    default:
                        throw new ShouldNeverHappenException($"unexpected primitive item type: {_attributeType.PrimitiveItemType ?? "<null>"} in {Name}");
                    }
                }
            }
        }

        public IResourceType ComplexType => _complexType.Value;

        private IResourceType GetComplexType() {
            if(ItemType != ResourceItemType.ComplexType) {
                throw new InvalidOperationException("attribute uses a primitive type");
            }
            if(CollectionType == ResourceCollectionType.NoCollection) {
                return GetResourceType(_attributeType.Type ?? throw new ShouldNeverHappenException("'Type' is null"));
            } else {
                return GetResourceType(_attributeType.ItemType ?? throw new ShouldNeverHappenException("'ItemType' is null"));
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