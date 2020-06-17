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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.Compiler.TypeSystem {

    public class EmptyTypeSystem : ITypeSystem {

        //--- Properties ---
        public IEnumerable<IResourceType> ResourceTypes => Enumerable.Empty<IResourceType>();

        //--- Methods ---
        public bool TryGetResourceType(string resourceTypeName, [NotNullWhen(true)] out IResourceType? resourceType) {
            resourceType = null;
            return false;
        }
    }
}