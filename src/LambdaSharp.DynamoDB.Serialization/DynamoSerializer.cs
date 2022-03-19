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
using System.Linq;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Serialization.Converters;

namespace LambdaSharp.DynamoDB.Serialization {

    /// <summary>
    /// The <see cref="DynamoSerializer"/> static class provides methods for (de)serializing values to/from DynamoDB attribute values.
    /// The following list shows the default type mapping for DynamoDB attribute values:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Number</term>
    ///         <description>int, int?, long, long?, double, double?, decimal, decimal?, DateTimeOffset, DateTimeOffset?</description>
    ///     </item>
    ///     <item>
    ///         <term>String</term>
    ///         <description>string, enum</description>
    ///     </item>
    ///     <item>
    ///         <term>Binary</term>
    ///         <description>byte[]</description>
    ///     </item>
    ///     <item>
    ///         <term>Boolean</term>
    ///         <description>bool, bool?</description>
    ///     </item>
    ///     <item>
    ///         <term>Null</term>
    ///         <description>any nullable type</description>
    ///     </item>
    ///     <item>
    ///         <term>List</term>
    ///         <description>List&lt;T&gt;</description>
    ///     </item>
    ///     <item>
    ///         <term>Map</term>
    ///         <description>Dictionary&lt;string, object&gt;, Dictionary&lt;string, T&gt;, T</description>
    ///     </item>
    ///     <item>
    ///         <term>String Set</term>
    ///         <description>ISet&lt;string&gt;</description>
    ///     </item>
    ///     <item>
    ///         <term>Number Set</term>
    ///         <description>ISet&lt;int>, ISet&lt;long&gt;, ISet&lt;double&gt;, ISet&lt;decimal&gt;</description>
    ///     </item>
    ///     <item>
    ///         <term>Binary Set</term>
    ///         <description>ISet&lt;byte[]&gt;</description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <seealso href="https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.NamingRulesDataTypes.html#HowItWorks.DataTypes">DynamoDB Data Types</seealso>
    public static class DynamoSerializer {

        //--- Class Methods ---

        /// <summary>
        /// The <see cref="Serialize(object?)"/> method serializes an object to a DynamoDB attribute value using the default <see cref="DynamoSerializerOptions"/> instance.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A DynamoDB attribute value or <c>null</c> when the object state cannot be represented in DynamoDB.</returns>
        public static AttributeValue? Serialize(object? value)
            => Serialize(value, new DynamoSerializerOptions());

        /// <summary>
        /// The <see cref="Serialize(object?,DynamoSerializerOptions)"/> method serializes an object to a DynamoDB attribute value.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>A DynamoDB attribute value or <c>null</c> when the object state cannot be represented in DynamoDB.</returns>
        public static AttributeValue? Serialize(object? value, DynamoSerializerOptions options) {

            // check for types mapped to attribute type 'NULL'
            if(value is null) {
                return new AttributeValue {
                    NULL = true
                };
            }

            // find a suiteable converter for the provided type
            var typeToConvert = value.GetType();
            var converter = options.GetConverters().FirstOrDefault(converter => converter.CanConvert(typeToConvert));
            if(converter is null) {
                throw new DynamoSerializationException($"cannot convert value of type '{typeToConvert?.FullName ?? "<null>"}'");
            }
            return converter.ToAttributeValue(value, typeToConvert, options);
        }

        /// <summary>
        /// The <see cref="Deserialize(Dictionary{string, AttributeValue})"/> method deserializes a DynamoDB document into a <typeparamref name="TRecord"/> instance using the default <see cref="DynamoSerializerOptions"/> instance.
        /// </summary>
        /// <param name="document">The DynamoDB document to deserialize.</param>
        /// <typeparam name="TRecord">The type to deserialize into.</typeparam>
        /// <returns>An instance of <typeparamref name="TRecord"/> or <c>null</c> when the DynamoDB document is <c>null</c>.</returns>
        public static TRecord? Deserialize<TRecord>(Dictionary<string, AttributeValue> document)
            where TRecord : class
            => Deserialize<TRecord>(document, new DynamoSerializerOptions());

        /// <summary>
        /// The <see cref="Deserialize(Dictionary{string, AttributeValue},DynamoSerializerOptions)"/> method deserializes a DynamoDB document into a <typeparamref name="TRecord"/> instance.
        /// </summary>
        /// <param name="document">The DynamoDB document to deserialize.</param>
        /// <param name="options">The deserialization options to use.</param>
        /// <typeparam name="TRecord">The type to deserialize into.</typeparam>
        /// <returns>An instance of <typeparamref name="TRecord"/> or <c>null</c> when the DynamoDB document is <c>null</c>.</returns>
        public static TRecord? Deserialize<TRecord>(Dictionary<string, AttributeValue> document, DynamoSerializerOptions options)
            where TRecord : class
            => (TRecord?)Deserialize(document, typeof(TRecord), options);

        /// <summary>
        /// The <see cref="Deserialize(Dictionary{string, AttributeValue},Type)"/> method deserializes a DynamoDB document into a <paramref name="targetType"/> instance using the default <see cref="DynamoSerializerOptions"/> instance.
        /// </summary>
        /// <param name="document">The DynamoDB document to deserialize.</param>
        /// <param name="targetType">The type to deserialize into.</param>
        /// <returns>An instance of <paramref name="targetType"/> or <c>null</c> when the DynamoDB document is <c>null</c>.</returns>
        public static object? Deserialize(Dictionary<string, AttributeValue> document, Type? targetType)
            => Deserialize(document, targetType, new DynamoSerializerOptions());

        /// <summary>
        /// The <see cref="Deserialize(Dictionary{string, AttributeValue},Type,DynamoSerializerOptions)"/> method deserializes a DynamoDB document into a <paramref name="targetType"/> instance.
        /// </summary>
        /// <param name="document">The DynamoDB document to deserialize.</param>
        /// <param name="targetType">The type to deserialize into.</param>
        /// <param name="options">The deserialization options to use.</param>
        /// <returns>An instance of <paramref name="targetType"/> or <c>null</c> when the DynamoDB document is <c>null</c>.</returns>
        public static object? Deserialize(Dictionary<string, AttributeValue> document, Type? targetType, DynamoSerializerOptions options) {
            var usedTargetType = ((targetType is null) || (targetType == typeof(object)))
                ? typeof(Dictionary<string, object>)
                : targetType;
            var converter = options.GetConverters().FirstOrDefault(converter => converter.CanConvert(usedTargetType));
            if(converter == null) {
                throw new DynamoSerializationException($"cannot convert document {nameof(AttributeValue.M)} (given: {targetType?.FullName ?? "<null>"})");
            }
            return converter.FromMap(document, usedTargetType, options);
        }

        /// <summary>
        /// The <see cref="Deserialize(AttributeValue,Type)"/> method deserializes a DynamoDB attribute value into a <paramref name="targetType"/> instance using the default <see cref="DynamoSerializerOptions"/> instance.
        /// </summary>
        /// <param name="attribute">The DynamoDB attribute value to deserialize.</param>
        /// <param name="targetType">The type to deserialize into.</param>
        /// <returns>An instance of <paramref name="targetType"/> or <c>null</c> when the DynamoDB attribute value is <c>NULL</c>.</returns>
        public static object? Deserialize(AttributeValue? attribute, Type? targetType)
            => Deserialize(attribute, targetType, new DynamoSerializerOptions());

        /// <summary>
        /// The <see cref="Deserialize(AttributeValue,Type,DynamoSerializerOptions)"/> method deserializes a DynamoDB attribute value into a <paramref name="targetType"/> instance.
        /// </summary>
        /// <param name="attribute">The DynamoDB attribute value to deserialize.</param>
        /// <param name="targetType">The type to deserialize into.</param>
        /// <param name="options">The deserialization options to use.</param>
        /// <returns>An instance of <paramref name="targetType"/> or <c>null</c> when the DynamoDB attribute value is <c>NULL</c>.</returns>
        public static object? Deserialize(AttributeValue? attribute, Type? targetType, DynamoSerializerOptions options) {

            // handle missing value
            if(attribute == null) {
                return FindConverter(typeof(object), "<default>", (converter, usedTargetType) => converter.GetDefaultValue(usedTargetType, options));
            }

            // handle boolean value
            if(attribute.IsBOOLSet) {
                return FindConverter(typeof(bool), nameof(AttributeValue.BOOL), (converter, usedTargetType) => converter.FromBool(attribute.BOOL, usedTargetType, options));
            }

            // handle string value
            if(!(attribute.S is null)) {
                return FindConverter(typeof(string), nameof(AttributeValue.S), (converter, usedTargetType) => converter.FromString(attribute.S, usedTargetType, options));
            }

            // handle number value
            if(!(attribute.N is null)) {
                return FindConverter(typeof(double), nameof(AttributeValue.N), (converter, usedTargetType) => converter.FromNumber(attribute.N, usedTargetType, options));
            }

            // handle binary value
            if(!(attribute.B is null)) {
                return FindConverter(typeof(byte[]), nameof(AttributeValue.B), (converter, usedTargetType) => converter.FromBinary(attribute.B, usedTargetType, options));
            }

            // handle list value
            if(attribute.IsLSet) {
                return FindConverter(typeof(List<object>), nameof(AttributeValue.L), (converter, usedTargetType) => converter.FromList(attribute.L, usedTargetType, options));
            }

            // handle map value
            if(attribute.IsMSet) {
                return Deserialize(attribute.M, targetType, options);
            }

            // handle binary set value
            if(attribute.BS.Any()) {
                return FindConverter(typeof(HashSet<byte[]>), nameof(AttributeValue.BS), (converter, usedTargetType) => converter.FromBinarySet(attribute.BS, usedTargetType, options));
            }

            // handle string set value
            if(attribute.SS.Any()) {
                return FindConverter(typeof(HashSet<string>), nameof(AttributeValue.SS), (converter, usedTargetType) => converter.FromStringSet(attribute.SS, usedTargetType, options));
            }

            // handle number set value
            if(attribute.NS.Any()) {
                return FindConverter(typeof(HashSet<double>), nameof(AttributeValue.NS), (converter, usedTargetType) => converter.FromNumberSet(attribute.NS, usedTargetType, options));
            }

            // handle null value
            if(attribute.NULL) {
                return FindConverter(typeof(object), nameof(AttributeValue.NULL), (converter, usedTargetType) => converter.FromNull(usedTargetType, options));
            }
            throw new DynamoSerializationException($"invalid attribute value");

            // local functions
            object? FindConverter(Type defaultType, string attributeValueTypeName, Func<IDynamoAttributeConverter, Type, object?> convert) {
                var usedTargetType = ((targetType is null) || (targetType == typeof(object)))
                    ? defaultType
                    : targetType;
                var converter = options.GetConverters().FirstOrDefault(converter => converter.CanConvert(usedTargetType));
                if(converter == null) {
                    throw new DynamoSerializationException($"cannot convert attribute value {attributeValueTypeName} (given: {targetType?.FullName ?? "<null>"})");
                }
                return convert(converter, usedTargetType);
            }
        }
    }
}
