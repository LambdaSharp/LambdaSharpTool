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
using Amazon.Lambda.Serialization.SystemTextJson;

namespace LambdaSharp.Serialization {

    /// <summary>
    /// The <see cref="JsonEpochSecondsDateTimeOffsetConverter"/> converts <c>DateTimeOffset</c> to/from JSON number using epoch seconds.
    /// </summary>
    public class JsonEpochSecondsDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {

        //--- Methods ---

        /// <summary>
        /// Reads and converts JSON to the appropriate type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns></returns>
       public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if(reader.TokenType == JsonTokenType.String) {
                if(!long.TryParse(reader.GetString(), out var number)) {
                throw new JsonSerializerException("string value must a number");
                }
                return DateTimeOffset.FromUnixTimeSeconds(number);
            } else if(reader.TokenType == JsonTokenType.Number) {
                return DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());
            } else {
                throw new JsonSerializerException($"value must either be a string or number, but was {reader.TokenType.ToString().ToLowerInvariant()}");
            }
       }

        /// <summary>
        /// Writes a specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.ToUnixTimeSeconds());
    }
}