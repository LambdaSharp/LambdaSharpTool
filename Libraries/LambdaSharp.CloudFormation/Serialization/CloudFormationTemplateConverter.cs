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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.CloudFormation.Serialization {

    public class CloudFormationTemplateConverter : JsonConverter<CloudFormationTemplate> {

        //--- Methods ---
        public override CloudFormationTemplate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, CloudFormationTemplate template, JsonSerializerOptions options) {

            // never emit null properties
            options.IgnoreNullValues = true;

            // emit template
            writer.WriteStartObject();
            WriteItem("AWSTemplateFormatVersion", template.AWSTemplateFormatVersion);
            WriteItem("Description", template.Description);
            WriteCollection("Transform", template.Transforms);
            WriteCollection("Parameters", template.Parameters);
            WriteCollection("Mappings", template.Mappings);
            WriteCollection("Conditions", template.Conditions);
            WriteCollection("Resources", template.Resources);
            WriteCollection("Outputs", template.Outputs);
            WriteCollection("Metadata", template.Metadata);
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
                    JsonSerializer.Serialize(writer, items, options);
                }
            }
        }
    }
}