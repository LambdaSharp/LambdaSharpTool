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
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace LambdaSharp.Serialization {

    /// <summary>
    ///
    /// </summary>
    public class LambdaSourceGeneratorJsonSerializer : ILambdaJsonSerializer {

        //--- Fields ---
        private readonly JsonSerializerContext _jsonSerializerContext;
        private readonly JsonWriterOptions _writerOptions;

        //--- Constructors ---

        /// <summary>
        /// Constructs instance of serializer.
        /// </summary>
        /// <param name="serializerContext">A callback to customize the serializer settings.</param>
        /// <param name="jsonWriterCustomizer"></param>
        public LambdaSourceGeneratorJsonSerializer(JsonSerializerContext serializerContext, Action<JsonWriterOptions>? jsonWriterCustomizer = null) {
            _jsonSerializerContext = serializerContext ?? throw new ArgumentNullException(nameof(serializerContext));
            _writerOptions = new JsonWriterOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            jsonWriterCustomizer?.Invoke(_writerOptions);
        }

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
                return JsonSerializer.Deserialize(memoryStream.ToArray(), type, _jsonSerializerContext) ?? throw new JsonException("stream deserialized to null");
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

        /// <inheritdoc/>
        public T Deserialize<T>(Stream stream) => (T)Deserialize(stream, typeof(T));

        /// <inheritdoc/>
        public string Serialize<T>(T value) {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, _writerOptions);
            JsonSerializer.Serialize(writer, value, typeof(T), _jsonSerializerContext);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <inheritdoc/>
        public void Serialize<T>(T value, Stream stream) {
            using var writer = new Utf8JsonWriter(stream, _writerOptions);
            JsonSerializer.Serialize(writer, value, typeof(T), _jsonSerializerContext);
        }
    }
}