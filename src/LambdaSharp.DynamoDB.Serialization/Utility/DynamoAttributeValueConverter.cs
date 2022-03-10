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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Utility {

    /// <summary>
    /// The <see cref="DynamoAttributeValueConverter"/> class converts a DynamoDB attribute value to JSON output.
    /// </summary>
    public class DynamoAttributeValueConverter : JsonConverter<AttributeValue> {

        //--- Methods ---

        /// <summary>
        /// This operation is NOT supported.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        public override AttributeValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("converter only supports write operations");

        /// <summary>
        /// Writes a specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, AttributeValue value, JsonSerializerOptions options) {
            if(value.IsBOOLSet) {
                writer.WriteStartObject();
                writer.WriteBoolean("BOOL", value.BOOL);
                writer.WriteEndObject();
            } else if(!(value.S is null)) {
                writer.WriteStartObject();
                writer.WriteString("S", value.S);
                writer.WriteEndObject();
            } else if(!(value.N is null)) {
                writer.WriteStartObject();
                writer.WriteString("N", value.N);
                writer.WriteEndObject();
            } else if(!(value.B is null)) {
                writer.WriteStartObject();
                writer.WriteString("B", Convert.ToBase64String(value.B.ToArray()));
                writer.WriteEndObject();
            } else if(value.IsLSet) {
                writer.WriteStartObject();
                writer.WritePropertyName("L");
                writer.WriteStartArray();
                foreach(var item in value.L) {
                    JsonSerializer.Serialize(writer, item, options);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            } else if(value.IsMSet) {
                writer.WriteStartObject();
                writer.WritePropertyName("M");
                JsonSerializer.Serialize(writer, value.M, options);
                writer.WriteEndObject();
            } else if(value.SS.Any()) {
                writer.WriteStartObject();
                writer.WritePropertyName("SS");
                writer.WriteStartArray();
                foreach(var item in value.SS) {
                    writer.WriteStringValue(item);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            } else if(value.NS.Any()) {
                writer.WriteStartObject();
                writer.WritePropertyName("NS");
                writer.WriteStartArray();
                foreach(var number in value.NS) {
                    writer.WriteStringValue(number);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            } else if(value.BS.Any()) {
                writer.WriteStartObject();
                writer.WritePropertyName("BS");
                writer.WriteStartArray();
                foreach(var item in value.BS) {
                    writer.WriteStringValue(Convert.ToBase64String(item.ToArray()));
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            } else {
                writer.WriteStartObject();
                writer.WriteBoolean("NULL", value.NULL);
                writer.WriteEndObject();
            }
        }
    }
}
