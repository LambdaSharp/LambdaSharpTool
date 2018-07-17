/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json;

namespace MindTouch.Rollbar {

    internal class NameValueCollectionConverter : JsonConverter {

        //--- Methods ---
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var collection = value as NameValueCollection;
            if(collection == null) {
                return;
            }
            writer.WriteStartObject();
            foreach(var key in collection.AllKeys) {
                collection.GetValues(key);
                writer.WritePropertyName(key);
                var values = collection.GetValues(key);
                if(values.Count() > 1) {
                    writer.WriteStartArray();
                    foreach(var item in values) {
                        writer.WriteValue(item);
                    }
                    writer.WriteEndArray();
                } else {
                    writer.WriteValue(collection.Get(key));
                }
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var nameValueCollection = new NameValueCollection();
            var key = string.Empty;
            while(reader.Read()) {
                if(reader.TokenType == JsonToken.StartObject) {
                    nameValueCollection = new NameValueCollection();
                }
                if(reader.TokenType == JsonToken.EndObject) {
                    return nameValueCollection;
                }
                if(reader.TokenType == JsonToken.PropertyName) {
                    key = reader.Value.ToString();
                }
                if(reader.TokenType == JsonToken.String) {
                    nameValueCollection.Add(key, reader.Value.ToString());
                }
                if(reader.TokenType == JsonToken.StartArray) {
                    while(reader.Read()) {
                        if(reader.TokenType == JsonToken.String) {
                            nameValueCollection.Add(key, reader.Value.ToString());
                        }
                        if(reader.TokenType == JsonToken.EndArray) {
                            break;
                        }
                    }
                }
            }
            return nameValueCollection;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(NameValueCollection);
        }
    }
}
