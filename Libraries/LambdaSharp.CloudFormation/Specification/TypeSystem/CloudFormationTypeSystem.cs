/*
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.CloudFormation.Specification.TypeSystem {

    public sealed class CloudFormationTypeSystem : ITypeSystem {

        //--- Class Methods ---
        public static async Task<CloudFormationTypeSystem> LoadFromAsync(string source, Stream stream) {
            var specification = await JsonSerializer.DeserializeAsync<ExtendedCloudFormationSpecification>(stream)
                ?? throw new ArgumentException("stream deserialized to null");
            return new CloudFormationTypeSystem(source, specification);
        }

        //--- Fields ---
        private readonly ExtendedCloudFormationSpecification _specification;
        private readonly Dictionary<string, IResourceType> _resourceTypes = new Dictionary<string, IResourceType>();

        //--- Constructors ---
        public CloudFormationTypeSystem(string source, ExtendedCloudFormationSpecification specification) {
            if(source is null) {
                throw new ArgumentNullException(nameof(source));
            }
            _specification = specification;
            foreach(var resourceTypeEntry in _specification.ResourceTypes) {
                _resourceTypes[resourceTypeEntry.Key] = new CloudFormationResourceType(resourceTypeEntry.Key, resourceTypeEntry.Value, _specification);
            }
            Source = (_specification.ResourceSpecificationVersion != null)
                ? $"{source} (v{_specification.ResourceSpecificationVersion})"
                : source;
        }

        //--- Properties ---
        public string Source { get; }
        public string Version => _specification.ResourceSpecificationVersion ?? "<MISSING>";
        public IEnumerable<IResourceType> ResourceTypes => _resourceTypes.Values;

        //--- Methods ---
        public bool TryGetResourceType(string resourceTypeName, [NotNullWhen(true)] out IResourceType? resourceType) {

            // check for 'Custom::' type-name prefix
            if(resourceTypeName.StartsWith("Custom::", StringComparison.Ordinal)) {
                resourceType = AnyResourceType.Instance;
                return true;
            }

            // check CloudFormation specification for matching resource type
            return _resourceTypes.TryGetValue(resourceTypeName, out resourceType);
        }
    }
}