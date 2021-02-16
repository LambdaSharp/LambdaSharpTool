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
using System.Text;
using Amazon.Lambda.Core;

namespace LambdaSharp.Serialization {

    /// <summary>
    /// The <see cref="ILambdaJsonSerializer"/> interface extends <c>ILambdaSerializer</c> interface by adding
    /// the ability to deserialize a JSON document to an explicitly specified type instead of relying on a
    /// generic type parameter.
    /// </summary>
    public interface ILambdaJsonSerializer : ILambdaSerializer {

        //--- Methods ---

        /// <summary>
        /// The <see cref="Serialize{T}(T)"/> interface method serializes
        /// an instance to a JSON <c>string</c>.
        /// </summary>
        /// <param name="value">The instance to serialize.</param>
        /// <returns>Serialized JSON <c>string</c>.</returns>
        string Serialize<T>(T value) {
            using var stream = new MemoryStream();
            Serialize<T>(value, stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// The <see cref="Deserialize(Stream, Type)"/> method deserializes the JSON object from a <c>Stream</c>.
        /// </summary>
        /// <param name="stream">Stream to deserialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        object Deserialize(Stream stream, Type type);

        /// <summary>
        /// The <see cref="Deserialize{T}(string)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// /// <param name="json">The <c>string</c> to deserialize.</param>
        /// <typeparam name="T">The deserialization target type.</typeparam>
        /// <returns>Deserialized instance.</returns>
        T Deserialize<T>(string json) {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return Deserialize<T>(stream);
        }

        /// <summary>
        /// The <see cref="Deserialize(String, Type)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// <param name="json">String to deserialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        object Deserialize(string json, Type type) {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return Deserialize(stream, type);
        }
    }
}