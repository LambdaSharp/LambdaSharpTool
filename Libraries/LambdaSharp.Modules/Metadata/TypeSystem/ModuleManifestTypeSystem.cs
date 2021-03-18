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
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.Modules.Metadata.TypeSystem {

    public sealed class ModuleManifestTypeSystem : ITypeSystem {

        //--- Fields ---
        private readonly ModuleManifest _manifest;
        private readonly Dictionary<string, IResourceType> _resourceTypes;

        //--- Constructors ---
        public ModuleManifestTypeSystem(string source, ModuleManifest manifest) {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _manifest = manifest;
            _resourceTypes = new Dictionary<string, IResourceType>();
            foreach(var resourceType in manifest.ResourceTypes) {
                _resourceTypes[resourceType.Type ?? throw new ArgumentException("missing resource type name")] = new ModuleManifestResourceType(resourceType);
            }
        }

        //--- Properties ---
        public string Source { get; }
        public IEnumerable<IResourceType> ResourceTypes => _resourceTypes.Values;

        //--- Methods ---
        public bool TryGetResourceType(string resourceTypeName, [NotNullWhen(true)] out IResourceType? resourceType) => _resourceTypes.TryGetValue(resourceTypeName, out resourceType);
    }
}