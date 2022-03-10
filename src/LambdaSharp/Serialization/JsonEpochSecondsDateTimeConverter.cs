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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.Serialization {

    /// <summary>
    /// The <see cref="JsonEpochSecondsDateTimeConverter"/> converts <c>DateTime</c> to/from JSON number using epoch seconds.
    /// </summary>
    public class JsonEpochSecondsDateTimeConverter : JsonConverter<DateTime> {

        //--- Methods ---

        /// <summary>
        /// Reads and converts JSON to the appropriate type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
       public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTimeOffset.FromUnixTimeSeconds(long.Parse(reader.GetString() ?? throw new JsonException("expected JSON string"))).UtcDateTime;

        /// <summary>
        /// Writes a specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteNumberValue(((DateTimeOffset)value).ToUnixTimeSeconds());
    }
}