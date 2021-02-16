/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace LambdaSharp.Serialization {

    /// <summary>
    /// Custom <see cref="ILambdaJsonSerializer"/> implementation which uses System.Text.Json.JsonSerializer
    /// for serialization.
    /// </summary>
    public class LambdaSystemTextJsonSerializer : DefaultLambdaJsonSerializer, ILambdaJsonSerializer {

        //--- Constructors ---

        /// <summary>
        /// Constructs instance of serializer.
        /// </summary>
        public LambdaSystemTextJsonSerializer() : this(customizer: null) { }

        /// <summary>
        /// Constructs instance of serializer.
        /// </summary>
        /// <param name="customizer">A callback to customize the serializer settings.</param>
        public LambdaSystemTextJsonSerializer(Action<JsonSerializerOptions>? customizer) : base(settings => {
            settings.IgnoreNullValues = true;
            settings.IncludeFields = true;
            settings.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            customizer?.Invoke(settings);
        }) { }

        /// <summary>
        /// The <see cref="Deserialize(Stream, Type)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// <param name="stream">Stream to serialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        public object Deserialize(Stream stream, Type type) {
            try {
                if(!(stream is MemoryStream memoryStream)) {
                    memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                }
                return JsonSerializer.Deserialize(memoryStream.ToArray(), type, SerializerOptions) ?? throw new JsonException("stream deserialized to null");
            } catch(Exception e) {
                string message;
                if(type == typeof(string)) {
                    message = $"Error converting the Lambda event JSON payload to a string. JSON strings must be quoted, for example \"Hello World\" in order to be converted to a string: {e.Message}";
                } else {
                    message = $"Error converting the Lambda event JSON payload to type {type.FullName}: {e.Message}";
                }
                throw new JsonSerializerException(message, e);
            }
        }
    }
}