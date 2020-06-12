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

using System.Text.Json.Serialization;

namespace LambdaSharp.Compiler.TypeSystem {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourcePropertyUpdateType {
        Unknown,
        Mutable,
        Immutable,
        Conditional
    }

    public sealed class ResourcePropertyType {

        //--- Properties ---
        public bool DuplicatesAllowed { get; set; }
        public string? ItemType { get; set; }
        public string? PrimitiveItemType { get; set; }
        public string? PrimitiveType { get; set; }
        public bool Required { get; set; }
        public string? Type { get; set; }
        public ResourcePropertyUpdateType UpdateType { get; set; }
        public ResourcePropertyValueType? Value { get; set; }
    }

    public sealed class ResourcePropertyValueType {

        //--- Properties ---
        public string? ValueType { get; set; }
    }
}
