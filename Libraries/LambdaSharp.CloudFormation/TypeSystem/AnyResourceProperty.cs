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

namespace LambdaSharp.CloudFormation.TypeSystem {

    public class AnyResourceProperty : IResourceProperty {

        //--- Constructors ---
        public AnyResourceProperty(string propertyName) => Name = propertyName ?? throw new ArgumentNullException(nameof(propertyName));

        //--- Properties ---
        public string Name { get; }
        public bool Required => false;
        public ResourceCollectionType CollectionType => ResourceCollectionType.NoCollection;
        public ResourceItemType ItemType => ResourceItemType.Any;
        public IResourceType ComplexType => AnyResourceType.Instance;
    }
}