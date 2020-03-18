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
using Newtonsoft.Json;

namespace LambdaSharp.Tool.Compiler.CloudFormation {

    public static class CloudFormationSerializer {

        //--- Class Methods ---
        public static string Serialize(CloudFormationTemplate template) {
            return JsonConvert.SerializeObject(template, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = {
                    new CloudFormationTemplateConverter(),
                    new CloudFormationResourceConverter(),
                    new CloudFormationObjectExpressionConverter(),
                    new CloudFormationListExpressionConverter(),
                    new CloudFormationLiteralExpressionConverter()
                }
            });
        }
    }

    public class CloudFormationTemplateConverter : JsonConverter {

        //--- Methods ---
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanConvert(Type objectType) => objectType == typeof(CloudFormationTemplate);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if(value is CloudFormationTemplate template) {
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
            }

            // local functions
            void WriteItem(string key, object? item) {
                if(item != null) {
                    writer.WritePropertyName(key);
                    serializer.Serialize(writer, item);
                }
            }

            void WriteCollection<T>(string key, IEnumerable<T> items) {
                if(items?.Any() ?? false) {
                    writer.WritePropertyName(key);
                    serializer.Serialize(writer, items);
                }
            }
        }
    }

    public class CloudFormationResourceConverter : JsonConverter {

        //--- Methods ---
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanConvert(Type objectType) => objectType == typeof(CloudFormationResource);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if(value is CloudFormationResource resource) {
                writer.WriteStartObject();
                WriteItem("Type", resource.Type);
                WriteCollection("Properties", resource.Properties);
                WriteCollection("DependsOn", resource.DependsOn);
                WriteCollection("Metadata", resource.Metadata);
                WriteItem("Condition", resource.Condition);
                WriteItem("DeletionPolicy", resource.DeletionPolicy);
                writer.WriteEndObject();
            }

            // local functions
            void WriteItem(string key, object? item) {
                if(item != null) {
                    writer.WritePropertyName(key);
                    serializer.Serialize(writer, item);
                }
            }

            void WriteCollection<T>(string key, IEnumerable<T> items) {
                if(items?.Any() ?? false) {
                    writer.WritePropertyName(key);
                    serializer.Serialize(writer, items);
                }
            }
        }
    }

    public class CloudFormationObjectExpressionConverter : JsonConverter {

        //--- Methods ---
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanConvert(Type objectType) => objectType == typeof(CloudFormationObjectExpression);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if(value is CloudFormationObjectExpression map) {
                writer.WriteStartObject();
                foreach(var kv in map) {
                    writer.WritePropertyName(kv.Key);
                    serializer.Serialize(writer, kv.Value);
                }
                writer.WriteEndObject();
            }
        }
    }

    public class CloudFormationListExpressionConverter : JsonConverter {

        //--- Methods ---
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanConvert(Type objectType) => objectType == typeof(CloudFormationListExpression);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if(value is CloudFormationListExpression list) {
                writer.WriteStartArray();
                foreach(var item in list) {
                    serializer.Serialize(writer, item);
                }
                writer.WriteEndArray();
            }
        }
    }

    public class CloudFormationLiteralExpressionConverter : JsonConverter {

        //--- Methods ---
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanConvert(Type objectType) => objectType == typeof(CloudFormationLiteralExpression);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if(value is CloudFormationLiteralExpression literal) {
                writer.WriteValue(literal.Value);
            } else {
                writer.WriteNull();
            }
        }
    }
}