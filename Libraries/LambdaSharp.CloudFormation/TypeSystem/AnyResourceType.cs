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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.CloudFormation.TypeSystem {

    public class AnyResourceType : IResourceType {

        //--- Class Fields ---
        public static readonly IResourceType Instance = new AnyResourceType();

        //--- Constructors ---
        private AnyResourceType() { }

        //--- Properties ---
        public string Name => "*";
        public string? Documentation => null;
        public IEnumerable<IResourceProperty> RequiredProperties => Enumerable.Empty<IResourceProperty>();
        public IEnumerable<IResourceProperty> Properties => Enumerable.Empty<IResourceProperty>();
        public IEnumerable<IResourceAttribute> Attributes => Enumerable.Empty<IResourceAttribute>();

        //--- Methods ---
        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IResourceProperty? property) {
            property = new AnyResourceProperty(propertyName);
            return true;
        }

        public bool TryGetAttribute(string attributeName, [NotNullWhen(true)] out IResourceAttribute? attribute) {
            attribute = new AnyResourceAttribute(attributeName);
            return true;
        }
    }
}