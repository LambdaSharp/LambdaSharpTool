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
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.CloudFormation.ModuleManifest.TypeSystem {

    public class CloudFormationModuleManifestResourceProperty : IResourceProperty {

        //--- Fields ---
        private readonly ModuleManifest.CloudFormationModuleManifestResourceProperty _property;

        //--- Constructors ---
        public CloudFormationModuleManifestResourceProperty(ModuleManifest.CloudFormationModuleManifestResourceProperty property) => _property = property;

        //--- Properties ---
        public string Name => _property.Name ?? throw new InvalidOperationException();
        public bool Required => _property.Required;

        public ResourceCollectionType CollectionType =>
            (_property.Type == "List")
                ? ResourceCollectionType.List
                : ResourceCollectionType.NoCollection;

        public ResourceItemType ItemType => _property.Type switch {
            "Boolean" => ResourceItemType.Boolean,
            "Json" => ResourceItemType.Json,
            "List" => ResourceItemType.Any,
            "Number" => ResourceItemType.Double,
            "String" => ResourceItemType.String,
            _ => throw new InvalidOperationException($"unexpected type: {_property.Type ?? "<null>"} in {Name}")
        };

        public IResourceType ComplexType => throw new InvalidOperationException("property uses a primitive type");
    }
}