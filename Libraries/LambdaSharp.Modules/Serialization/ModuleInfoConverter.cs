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

// NOTE (2020-08-02, bjorg): nullable is disabled, because the converter can return null
//  even though the type is non-nullable
#nullable disable

using System;
using Newtonsoft.Json;

namespace LambdaSharp.Modules.Serialization {

    public class ModuleInfoConverter : JsonConverter {

        //--- Methods ---
        public override bool CanConvert(Type objectType)
            => objectType == typeof(ModuleInfo);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => (reader.Value != null)
                ? ModuleInfo.Parse((string)reader.Value)
                : null;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => writer.WriteValue(((ModuleInfo)value).ToString());
    }
}