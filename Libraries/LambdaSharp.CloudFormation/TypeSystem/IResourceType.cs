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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LambdaSharp.CloudFormation.TypeSystem {

    public interface IResourceType {

        //--- Properties ---
        string Name { get; }
        bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IResourceProperty? property);
        bool TryGetAttribute(string attributeName, [NotNullWhen(true)] out IResourceAttribute? attribute);
        IEnumerable<IResourceProperty> RequiredProperties { get; }
    }
}