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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Serialization.Utility;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    /// <summary>
    /// The <see cref="DynamoISetByteArrayConverter"/> class is used to convert <c>ISet&lt;byte[]&gt;</c> value to/from a DynamoDB attribute value.
    /// </summary>
    public class DynamoISetByteArrayConverter : ADynamoAttributeConverter {

        //--- Class Fields ---

        /// <summary>
        /// The <see cref="Instance"/> class field exposes a reusable instance of the class.
        /// </summary>
        public static readonly DynamoISetByteArrayConverter Instance = new DynamoISetByteArrayConverter();

        //--- Methods ---

        /// <summary>
        /// The <see cref="CanConvert(Type)"/> method checks if this converter can handle the presented type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns><c>true</c> if the converter can handle the type; otherwise, <c>false</c></returns>
        public override bool CanConvert(Type typeToConvert) => typeof(ISet<byte[]>).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// The <see cref="ToAttributeValue(object,Type,DynamoSerializerOptions)"/> method converts an instance to a DynamoDB attribute value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The source value type.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A DynamoDB attribute value, or <c>null</c> if the instance state cannot be represented in DynamoDB.</returns>
        public override AttributeValue? ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var binarySet = (ISet<byte[]>)value;

            // NOTE (2021-06-21, bjorg): DynamoDB does not allow storing empty sets!
            return binarySet.Any()
                ? new AttributeValue {
                    BS = binarySet.Select(bytes => new MemoryStream(bytes)).ToList()
                } : null;
        }

        /// <summary>
        /// The <see cref="GetDefaultValue(Type,DynamoSerializerOptions)"/> method instantiates a default value for a missing property during deserialization.
        /// </summary>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        public override object? GetDefaultValue(Type targetType, DynamoSerializerOptions options)
            => CreateInstance(targetType);

        /// <summary>
        /// The <see cref="FromBinarySet(List{MemoryStream},Type,DynamoSerializerOptions)"/> method converts a DynamoDB BS attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public override object? FromBinarySet(List<MemoryStream> value, Type targetType, DynamoSerializerOptions options) {
            var result = CreateInstance(targetType);
            foreach(var item in value) {
                result.Add(item.ToArray());
            }
            return result;
        }

        private ISet<byte[]> CreateInstance(Type targetType) {
            if(targetType.IsAssignableFrom(typeof(HashSet<byte[]>))) {

                // return HashSet<byte[]>
                return new HashSet<byte[]>(ByteArrayEqualityComparer.Instance);
            }
            if(!targetType.IsAbstract) {

                // create set instance and add items
                return (ISet<byte[]>)(Activator.CreateInstance(targetType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
            }
            throw new DynamoSerializationException($"incompatible target type for BS attribute value (given: {targetType.FullName})");
        }
    }
}
