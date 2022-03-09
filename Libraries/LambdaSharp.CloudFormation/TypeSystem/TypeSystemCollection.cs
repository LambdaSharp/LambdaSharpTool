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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.CloudFormation.TypeSystem {

    public sealed class TypeSystemCollection : ITypeSystem, IEnumerable<ITypeSystem> {

        //--- Fields ---
        private readonly List<ITypeSystem> _collection = new List<ITypeSystem>();

        public TypeSystemCollection(string source) => Source = source ?? throw new ArgumentNullException(nameof(source));

        //--- Properties ---
        public string Source { get; }
        public IEnumerable<IResourceType> ResourceTypes => _collection.SelectMany(collection => collection.ResourceTypes);

        //--- Methods ---
        public void Add(ITypeSystem typeSystem)
            => _collection.Add(typeSystem ?? throw new ArgumentNullException(nameof(typeSystem)));

        public bool TryGetResourceType(string resourceTypeName, [NotNullWhen(true)] out IResourceType? resourceType) {

            // check if a type system defines this resource type; later definitions shadow earlier ones
            foreach(var typeSystem in Enumerable.Reverse(_collection)) {
                if(typeSystem.TryGetResourceType(resourceTypeName, out resourceType)) {
                    return true;
                }
            }

            // not matching resource type definition found
            resourceType = null;
            return false;
        }

        public IEnumerable<(string Source, IResourceType ResourceType)> GetAllMacthingResourceTypes(string resourceTypeName) {
            var result = new List<(string Source, IResourceType ResourceType)>();
            foreach(var typeSystem in Enumerable.Reverse(_collection)) {
                if(typeSystem.TryGetResourceType(resourceTypeName, out var resourceType)) {
                    result.Add((Source: typeSystem.Source, ResourceType: resourceType));
                }
            }
            return result;

        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

        //--- IEnumerable<ITypeSystem> Members ---
        IEnumerator<ITypeSystem> IEnumerable<ITypeSystem>.GetEnumerator() => _collection.GetEnumerator();
    }
}