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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.CloudFormation.Specification.TypeSystem {

    internal sealed class CloudFormationResourceType : IResourceType {

        //--- Types ---
        private class StringResourceAttribute : IResourceAttribute {

            //--- Constructors ---
            public StringResourceAttribute(string attributeName) => Name = attributeName ?? throw new ArgumentNullException(nameof(attributeName));

            //--- Properties ---
            public string Name { get; }
            public ResourceCollectionType CollectionType => ResourceCollectionType.NoCollection;
            public ResourceItemType ItemType => ResourceItemType.String;
            public IResourceType ComplexType => throw new InvalidOperationException();
        }

        //--- Fields ---
        private readonly ResourceType _resourceType;
        private readonly ExtendedCloudFormationSpecification _specification;
        private readonly Lazy<IEnumerable<IResourceProperty>> _properties;
        private readonly Lazy<IEnumerable<IResourceAttribute>> _attributes;

        //--- Constructors ---
        public CloudFormationResourceType(string resourceName, ResourceType resourceType, ExtendedCloudFormationSpecification specification) {
            Name = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _properties = new Lazy<IEnumerable<IResourceProperty>>(() => _resourceType.Properties
                .Select(kv => new CloudFormationResourceProperty(kv.Key, this, kv.Value, _specification))
                .ToArray()
            );
            _attributes = new Lazy<IEnumerable<IResourceAttribute>>(() => _resourceType.Attributes
                .Select(kv => new CloudFormationResourceAttribute(kv.Key, this, kv.Value, _specification))
                .ToArray()
            );
        }

        //--- Properties ---
        public string Name { get; }
        public string? Documentation => _resourceType.Documentation;
        public IEnumerable<IResourceProperty> RequiredProperties => Properties.Where(property => property.Required);
        public IEnumerable<IResourceProperty> Properties => _properties.Value;
        public IEnumerable<IResourceAttribute> Attributes => _attributes.Value;

        //--- Methods ---
        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IResourceProperty? property) {
            if(_resourceType.Properties.TryGetValue(propertyName, out var type)) {
                property = new CloudFormationResourceProperty(propertyName, this, type, _specification);
                return true;
            }
            property = null;
            return false;
        }

        public bool TryGetAttribute(string attributeName, [NotNullWhen(true)] out IResourceAttribute? attribute) {

            // special case for nested stacks, which have an arbitrary number of output attributes
            if(
                Name.Equals("AWS::CloudFormation::Stack", StringComparison.Ordinal)
                && attributeName.StartsWith("Outputs.", StringComparison.Ordinal)
            ) {

                // TODO: we need more meta-data to determine the output attribute type from the stack
                attribute = new StringResourceAttribute(attributeName);
                return true;
            }

            // check if attribute exists in type specification
            if(_resourceType.Attributes.TryGetValue(attributeName, out var type)) {
                attribute = new CloudFormationResourceAttribute(attributeName, this, type, _specification);
                return true;
            }
            attribute = null;
            return false;
        }
    }
}