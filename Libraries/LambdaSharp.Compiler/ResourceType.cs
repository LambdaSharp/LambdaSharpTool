/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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

namespace LambdaSharp.Compiler {

    public sealed class ResourceType {

        //--- Properties ---
        public string? Documentation { get; set; }
        public Dictionary<string, AttributeType>? Attributes { get; set; }
        public Dictionary<string, PropertyType>? Properties { get; set; }
    }

    public sealed class AttributeType {

        //--- Properties ---
        public string? ItemType { get; set; }
        public string? PrimitiveItemType { get; set; }
        public string? PrimitiveType { get; set; }
        public string? Type { get; set; }
    }

    public sealed class PropertyType {

        //--- Properties ---
        public bool DuplicatesAllowed { get; set; }
        public string? ItemType { get; set; }
        public string? PrimitiveItemType { get; set; }
        public string? PrimitiveType { get; set; }
        public bool Required { get; set; }
        public string? Type { get; set; }
    }
}