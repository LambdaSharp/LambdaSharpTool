﻿/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.CloudFormation.Specification.TypeSystem {

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

    internal class CloudFormationResourceProperty : IResourceProperty {

        // TODO (2021-02-25, bjorg): leverage UpdateType (from CloudFormation spec): _propertyType.UpdateType;
        // TODO (2021-02-25, bjorg): leverage ValueType (from Extended CloudFormation spec): _propertyType.Value.ValueType

        //--- Fields ---
        private readonly CloudFormationResourceType _resourceType;
        private readonly ResourcePropertyType _propertyType;
        private readonly ExtendedCloudFormationSpecification _specification;
        private readonly Lazy<IResourceType> _complexType;

        //--- Constructors ---
        public CloudFormationResourceProperty(string propertyName, CloudFormationResourceType resourceType, ResourcePropertyType propertyType, ExtendedCloudFormationSpecification specification) {
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

        public ResourceCollectionType CollectionType {
            get {
                if(_propertyType.Type == "List") {
                    return ResourceCollectionType.List;
                } else if(_propertyType.Type == "Map") {
                    return ResourceCollectionType.Map;
                }
                return ResourceCollectionType.NoCollection;
            }
        }

        public ResourceItemType ItemType {
            get {
                if(CollectionType == ResourceCollectionType.NoCollection) {
                    switch(_propertyType.PrimitiveType) {
                    case null when (_propertyType.Type != null):
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
                        throw new InvalidOperationException($"unexpected primitive type: {_propertyType.PrimitiveType ?? "<null>"} in {Name}");
                    }
                } else {
                    switch(_propertyType.PrimitiveItemType) {
                    case null when (_propertyType.ItemType != null):
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
                        throw new InvalidOperationException($"unexpected primitive item type: {_propertyType.PrimitiveItemType ?? "<null>"} in {Name}");
                    }
                }
            }
        }

        public IResourceType ComplexType => _complexType.Value;

        private IResourceType GetComplexType() {
            if(ItemType != ResourceItemType.ComplexType) {
                throw new InvalidOperationException("property uses a primitive type");
            }
            if(CollectionType == ResourceCollectionType.NoCollection) {
                return GetResourceType(_propertyType.Type ?? throw new InvalidOperationException("'Type' is null"));
            }
            return GetResourceType(_propertyType.ItemType ?? throw new InvalidOperationException("'ItemType' is null"));

            // local function
            CloudFormationResourceType GetResourceType(string complexTypeName) {

                // current resource type name could be a composite name, such as 'AWS::S3::Bucket.Transition'
                var dotIndex = _resourceType.Name.IndexOf('.');
                var resourceSpecificComplexTypeName =
                    (dotIndex >= 0)
                    ? $"{_resourceType.Name.Substring(0, dotIndex)}.{complexTypeName}"
                    : $"{_resourceType.Name}.{complexTypeName}";

                // check most specific name first, the check the complex type name by itself
                if(_specification.PropertyTypes.TryGetValue(resourceSpecificComplexTypeName, out var type)) {
                    return new CloudFormationResourceType(resourceSpecificComplexTypeName, type, _specification);
                } else if(_specification.PropertyTypes.TryGetValue(complexTypeName, out type)) {
                    return new CloudFormationResourceType(complexTypeName, type, _specification);
                } else {
                    throw new InvalidOperationException($"complex type name not found: {resourceSpecificComplexTypeName}");
                }
            }
        }
    }
}