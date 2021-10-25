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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.App.OmniBus.Protocol.Serialization {

    public class JsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {

        //--- Methods ---

        /// <summary>
        /// Converts a number as Epoch seconds or a string as ISO 8601 date-time.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns></returns>
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if(reader.TokenType == JsonTokenType.Number) {
                return DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());
            } else if(reader.TokenType == JsonTokenType.String) {
                var value = reader.GetString();
                if(DateTimeOffset.TryParse(value, out var result)) {
                    return result;
                }
                if(long.TryParse(value, out var number)) {
                    return DateTimeOffset.FromUnixTimeSeconds(number);
                }
                throw new JsonException("string value must a number");
            } else {
                throw new JsonException($"value must either be a string or number, but was {reader.TokenType.ToString().ToLowerInvariant()}");
            }
        }

        /// <summary>
        /// Writes the DateTimeOffset value as Epoch seconds.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.ToUnixTimeSeconds());
    }
}
