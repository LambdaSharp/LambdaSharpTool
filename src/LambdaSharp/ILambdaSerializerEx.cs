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
using LambdaSharp.Serialization;
using Newtonsoft.Json;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="ILambdaSerializerEx"/> class provides extension methods
    /// for the <see cref="Amazon.Lambda.Core.ILambdaSerializer"/> interface.
    /// </summary>
    public static class ILambdaSerializerEx {

        /// <summary>
        /// The <see cref="Serialize{T}(ILambdaSerializer, T)"/> extension method serializes
        /// n instance to a JSON <c>string</c>.
        /// </summary>
        /// <param name="serializer">The Lambda serializer.</param>
        /// <param name="value">The instance to serialize.</param>
        /// <returns>Serialized JSON <c>string</c>.</returns>
        public static string Serialize<T>(this ILambdaSerializer serializer, T value) {
            using(var stream = new MemoryStream()) {
                serializer.Serialize<T>(value, stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// The <see cref="Deserialize{T}(ILambdaSerializer, string)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// <param name="serializer">The Lambda serializer.</param>
        /// <param name="json">The <c>string</c> to deserialize.</param>
        /// <typeparam name="T">The deserialization target type.</typeparam>
        /// <returns>Deserialized instance.</returns>
        public static T Deserialize<T>(this ILambdaSerializer serializer, string json) => serializer.Deserialize<T>(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        /// <summary>
        /// The <see cref="Deserialize(ILambdaSerializer, string, Type)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// <param name="serializer">The Lambda serializer.</param>
        /// <param name="json">The <c>string</c> to deserialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        public static object Deserialize(this ILambdaSerializer serializer, string json, Type type) {
            if(serializer is LambdaJsonSerializer lambdaJsonSerializer) {
                return lambdaJsonSerializer.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(json)), type);
            } else {
                return JsonConvert.DeserializeObject(json, type);
            }
        }
    }
}
