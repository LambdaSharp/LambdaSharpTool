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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    /// <summary>
    /// The <see cref="DynamoIDictionaryConverter"/> class is used to convert <c>IDictionary&lt;string,T&gt;</c> value to/from a DynamoDB attribute value.
    /// </summary>
    public class DynamoIDictionaryConverter : ADynamoAttributeConverter {

        //--- Class Fields ---

        /// <summary>
        /// The <see cref="Instance"/> class field exposes a reusable instance of the class.
        /// </summary>
        public static readonly DynamoIDictionaryConverter Instance = new DynamoIDictionaryConverter();

        //--- Class Methods ---
        private static Type? GetDictionaryItemType(Type type) {
            if(
                type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                && (type.GenericTypeArguments[0] == typeof(string))
            ) {
                return type.GenericTypeArguments[1];
            }
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) && (i.GenericTypeArguments[0] == typeof(string)))
                .Select(i => i.GenericTypeArguments[1])
                .FirstOrDefault();
        }

        //--- Methods ---

        /// <summary>
        /// The <see cref="CanConvert(Type)"/> method checks if this converter can handle the presented type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns><c>true</c> if the converter can handle the type; otherwise, <c>false</c></returns>
        public override bool CanConvert(Type typeToConvert) => typeof(IDictionary).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// The <see cref="ToAttributeValue(object,Type,DynamoSerializerOptions)"/> method converts an instance to a DynamoDB attribute value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The source value type.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A DynamoDB attribute value, or <c>null</c> if the instance state cannot be represented in DynamoDB.</returns>
        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var mapDictionary = new Dictionary<string, AttributeValue>();
            foreach(DictionaryEntry? entry in (IDictionary)value) {
                if(entry is null) {
                    continue;
                }
                var entryValue = entry?.Value;
                if(!(entryValue is null) || !options.IgnoreNullValues) {
                    var entryKey = entry?.Key as string;
                    if(entryKey is null) {
                        throw new DynamoSerializationException("null key is not supported");
                    }
                    var attributeValue = DynamoSerializer.Serialize(entryValue, options);
                    if(attributeValue != null) {
                        mapDictionary.Add(entryKey, attributeValue);
                    }
                }
            }
            return new AttributeValue {
                M = mapDictionary,
                IsMSet = true
            };
        }

        /// <summary>
        /// The <see cref="FromMap(Dictionary{string,AttributeValue},Type,DynamoSerializerOptions)"/> method converts a DynamoDB M attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public override object? FromMap(Dictionary<string, AttributeValue> value, Type targetType, DynamoSerializerOptions options) {
            if(targetType.IsAssignableFrom(typeof(Dictionary<string, object>))) {
                return value.ToDictionary(kv => kv.Key, kv => DynamoSerializer.Deserialize(kv.Value, typeof(object), options));
            }

            // check if item type can be determined via `IDictionary<string, T>` inheritance
            var itemType = GetDictionaryItemType(targetType);

            // determine if a concrete type needs to be identified
            Type mapType = targetType;
            if(targetType.IsInterface) {
                if(!(itemType is null)) {
                    mapType = typeof(Dictionary<,>).MakeGenericType(new[] { typeof(string), itemType });
                } else {
                    mapType = typeof(Dictionary<string, object>);
                }
                if(targetType.IsAssignableFrom(mapType)) {
                    throw new DynamoSerializationException($"incompatible target type for M attribute value (given: {targetType.FullName})");
                }
            }

            // create dictionary instance and add items
            var result = (IDictionary)(Activator.CreateInstance(mapType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
            foreach(var (key, item) in value) {
                result.Add(key, DynamoSerializer.Deserialize(item, itemType, options));
            }
            return result;
        }
    }
}
