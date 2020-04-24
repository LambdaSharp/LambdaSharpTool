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
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.Serialization.SystemTextJson.Converters;

namespace LambdaSharp.Serialization {

    /// <summary>
    /// Custom ILambdaSerializer implementation which uses System.Text.Json
    /// for serialization.
    ///
    /// <para>
    /// If the environment variable LAMBDA_NET_SERIALIZER_DEBUG is set to true the JSON coming
    /// in from Lambda and being sent back to Lambda will be logged.
    /// </para>
    /// </summary>
    public class LambdaJsonSerializer : DefaultLambdaJsonSerializer {

        //--- Fields ---

        // TODO: make instance field once 'Options` is exposed by the base class
        private static JsonSerializerOptions _staticOptions = new JsonSerializerOptions() {
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
            Converters = {
                new DateTimeConverter(),
                new MemoryStreamConverter(),
                new ConstantClassConverter()
            }
        };

        //--- Constructors ---

        /// <summary>
        /// Constructs instance of serializer.
        /// </summary>
        public LambdaJsonSerializer() : base(options => _staticOptions = options) { }

        /// <summary>
        /// The <see cref="Deserialize(Stream, Type)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// <param name="stream">Stream to serialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        public object Deserialize(Stream stream, Type type) {
            try {
                byte[] utf8Json;
                if(stream is MemoryStream ms) {
                    utf8Json = ms.ToArray();
                } else {
                    using(var copy = new MemoryStream()) {
                        stream.CopyTo(copy);
                        utf8Json = copy.ToArray();
                    }
                }
                return System.Text.Json.JsonSerializer.Deserialize(utf8Json, type, _staticOptions);
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