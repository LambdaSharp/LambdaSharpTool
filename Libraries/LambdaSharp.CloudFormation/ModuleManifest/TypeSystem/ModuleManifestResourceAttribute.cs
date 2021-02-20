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

    public class ModuleManifestResourceAttribute : IResourceAttribute {

        //--- Fields ---
        private readonly CloudFormationModuleManifestResourceAttribute _attribute;

        //--- Constructors ---
        public ModuleManifestResourceAttribute(CloudFormationModuleManifestResourceAttribute attribute) => _attribute = attribute;

        //--- Properties ---
        public string Name => _attribute.Name ?? throw new InvalidOperationException();

        public ResourceCollectionType CollectionType => ResourceCollectionType.NoCollection;

        public ResourceItemType ItemType => _attribute.Type switch {
            "String" => ResourceItemType.String,
            "Number" => ResourceItemType.Double,
            _ => throw new InvalidOperationException($"unexpected type: {_attribute.Type ?? "<null>"} in {Name}")
        };

        public IResourceType ComplexType => throw new InvalidOperationException("attribute uses a primitive type");
    }
}