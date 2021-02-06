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

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.Build.Internal {

    internal class JsonToNativeConverter : JsonConverter<object?> {

        //--- Class Methods ---
        public static Dictionary<string, object?>? ParseObject(string json) {
            return JsonSerializer.Deserialize<object?>(json, new JsonSerializerOptions {
                Converters = {
                    new JsonToNativeConverter()
                }
            }) as Dictionary<string, object?>;
        }

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) {
            if(
                (typeToConvert == typeof(object))
                || (typeToConvert == typeof(string))
                || (typeToConvert == typeof(Dictionary<string, object>))
                || (typeToConvert == typeof(List<object>))
            ) {
                return true;
            }
            return false;
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return Read(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options) {
            switch(value) {
            case null:
                writer.WriteNullValue();
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case Dictionary<string, object> objectValue:
                WriteObject(writer, objectValue, options);
                break;
            case List<object> arrayValue:
                WriteArray(writer, arrayValue, options);
                break;
            default:
                throw new JsonException($"unexpected type to serialize: {value?.GetType().FullName}");
            }
        }

        private object? Read(ref Utf8JsonReader reader) {
            switch(reader.TokenType) {
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    if(reader.TryGetInt32(out var int32)) {
                        return int32;
                    }
                    if(reader.TryGetInt64(out var int64)) {
                        return int64;
                    }
                    if(reader.TryGetDouble(out var number)) {
                        return number;
                    }
                    throw new JsonException("unsupported number value");
                case JsonTokenType.StartObject:
                    return ReadObject(ref reader);
                case JsonTokenType.StartArray:
                    return ReadArray(ref reader);
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.True:
                    return true;
                default:
                    throw new JsonException("unsupported token");
            }
        }

        private Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader) {
            var result = new Dictionary<string, object?>();
            while(true) {
                reader.Read();
                if(reader.TokenType == JsonTokenType.EndObject) {
                    return result;
                }
                if(reader.TokenType != JsonTokenType.PropertyName) {
                    throw new JsonException();
                }

                // read property
                var key = reader.GetString();
                reader.Read();
                result[key] = Read(ref reader);
            }
        }

        private List<object?> ReadArray(ref Utf8JsonReader reader) {
            var result = new List<object?>();
            while(true) {
                reader.Read();
                if(reader.TokenType == JsonTokenType.EndArray) {
                    return result;
                }

                // read item
                result.Add(Read(ref reader));
            }
        }

        private void WriteObject(Utf8JsonWriter writer, Dictionary<string, object> mapValue, JsonSerializerOptions options) {
            writer.WriteStartObject();
            foreach(var kv in mapValue) {
                if(!options.IgnoreNullValues || (kv.Value != null)) {
                    writer.WritePropertyName(kv.Key);
                    Write(writer, kv.Value, options);
                }
            }
            writer.WriteEndObject();
        }

        private void WriteArray(Utf8JsonWriter writer, List<object> arrayValue, JsonSerializerOptions options) {
            writer.WriteStartArray();
            foreach(var item in arrayValue) {
                Write(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}