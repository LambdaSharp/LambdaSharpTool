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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.CloudFormation.Template.Serialization {

    public class CloudFormationResourceConverter : JsonConverter<CloudFormationResource> {

        //--- Methods ---
        public override CloudFormationResource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, CloudFormationResource resource, JsonSerializerOptions options) {
            writer.WriteStartObject();
            WriteItem("Type", resource.Type);
            WriteCollection("Properties", resource.Properties);
            WriteCollection("DependsOn", resource.DependsOn);
            WriteCollection("Metadata", resource.Metadata);
            WriteItem("Condition", resource.Condition);
            WriteItem("DeletionPolicy", resource.DeletionPolicy);
            writer.WriteEndObject();

            // local functions
            void WriteItem(string key, object? item) {
                if(item != null) {
                    writer.WritePropertyName(key);
                    JsonSerializer.Serialize(writer, item, options);
                }
            }

            void WriteCollection<T>(string key, IEnumerable<T> items) {
                if(items?.Any() ?? false) {
                    writer.WritePropertyName(key);
                    JsonSerializer.Serialize(writer, items, items.GetType(), options);
                }
            }
        }
    }
}