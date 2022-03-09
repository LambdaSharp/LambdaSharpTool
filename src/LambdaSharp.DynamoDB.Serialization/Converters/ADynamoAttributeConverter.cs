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
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    /// <summary>
    /// The <see cref="ADynamoAttributeConverter"/> is the base class for DynamoDB attribute value converters.
    /// </summary>
    public abstract class ADynamoAttributeConverter : IDynamoAttributeConverter {

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="CanConvert(Type)"/> method checks if this converter can handle the presented type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns><c>true</c> if the converter can handle the type; otherwise, <c>false</c></returns>
        public abstract bool CanConvert(Type typeToConvert);

        //--- Methods ---

        /// <summary>
        /// The <see cref="ToAttributeValue(object,Type,DynamoSerializerOptions)"/> method converts an instance to a DynamoDB attribute value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The source value type.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A DynamoDB attribute value, or <c>null</c> if the instance state cannot be represented in DynamoDB.</returns>
        public virtual AttributeValue? ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) => throw new NotImplementedException("conversion to attribute value is not implemented");

        /// <summary>
        /// The <see cref="GetDefaultValue(Type,DynamoSerializerOptions)"/> method instantiates a default value for a missing property during deserialization.
        /// </summary>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        public virtual object? GetDefaultValue(Type targetType, DynamoSerializerOptions options)
            => (targetType.IsValueType && (Nullable.GetUnderlyingType(targetType) == null))
                ? Activator.CreateInstance(targetType)
                : null;

        /// <summary>
        /// The <see cref="FromNull(Type,DynamoSerializerOptions)"/> method converts a DynamoDB NULL attribute value to the type of the converter.
        /// </summary>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromNull(Type targetType, DynamoSerializerOptions options)
            => (!targetType.IsValueType || (Nullable.GetUnderlyingType(targetType) != null))
                ? (object?)null
                : throw new DynamoSerializationException("conversion from NULL is not supported");

        /// <summary>
        /// The <see cref="FromBool(bool,Type,DynamoSerializerOptions)"/> method converts a DynamoDB BOOL attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromBool(bool value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from BOOL is not supported");

        /// <summary>
        /// The <see cref="FromBinary(MemoryStream,Type,DynamoSerializerOptions)"/> method converts a DynamoDB B attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromBinary(MemoryStream value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from B is not supported");

        /// <summary>
        /// The <see cref="FromNumber(string,Type,DynamoSerializerOptions)"/> method converts a DynamoDB N attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromNumber(string value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from N is not supported");

        /// <summary>
        /// The <see cref="FromString(String,Type,DynamoSerializerOptions)"/> method converts a DynamoDB S attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromString(string value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from S is not supported");

        /// <summary>
        /// The <see cref="FromList(List{AttributeValue},Type,DynamoSerializerOptions)"/> method converts a DynamoDB L attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromList(List<AttributeValue> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from L is not supported");

        /// <summary>
        /// The <see cref="FromMap(Dictionary{string,AttributeValue},Type,DynamoSerializerOptions)"/> method converts a DynamoDB M attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromMap(Dictionary<string, AttributeValue> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from M is not supported");

        /// <summary>
        /// The <see cref="FromBinarySet(List{MemoryStream},Type,DynamoSerializerOptions)"/> method converts a DynamoDB BS attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromBinarySet(List<MemoryStream> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from BS is not supported");

        /// <summary>
        /// The <see cref="FromNumberSet(List{string},Type,DynamoSerializerOptions)"/> method converts a DynamoDB NS attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromNumberSet(List<string> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from NS is not supported");

        /// <summary>
        /// The <see cref="FromStringSet(List{string},Type,DynamoSerializerOptions)"/> method converts a DynamoDB SS attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public virtual object? FromStringSet(List<string> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from SS is not supported");
    }
}
