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
using System.Reflection;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    /// <summary>
    /// The <see cref="DynamoObjectConverter"/> class is used to convert non-value types to/from a DynamoDB attribute value.
    /// </summary>
    /// <remarks>
    /// This converter should always be listed last as it has a broad set of types it matches.
    /// </remarks>
    public class DynamoObjectConverter : ADynamoAttributeConverter {

        //--- Class Fields ---

        /// <summary>
        /// The <see cref="Instance"/> class field exposes a reusable instance of the class.
        /// </summary>
        public static readonly DynamoObjectConverter Instance = new DynamoObjectConverter();

        //--- Methods ---

        /// <summary>
        /// The <see cref="CanConvert(Type)"/> method checks if this converter can handle the presented type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns><c>true</c> if the converter can handle the type; otherwise, <c>false</c></returns>
        public override bool CanConvert(Type typeToConvert) => !typeToConvert.IsValueType;

        /// <summary>
        /// The <see cref="ToAttributeValue(object,Type,DynamoSerializerOptions)"/> method converts an instance to a DynamoDB attribute value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The source value type.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A DynamoDB attribute value, or <c>null</c> if the instance state cannot be represented in DynamoDB.</returns>
        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var mapObject = new Dictionary<string, AttributeValue>();
            foreach(var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)) {
                var propertyAttributes = property.GetCustomAttributes();

                // check if this object property should be ignored
                if(!(propertyAttributes.OfType<DynamoPropertyIgnoreAttribute>().SingleOrDefault() is null)) {
                    continue;
                }

                var propertyValue = property.GetValue(value);
                if(!(propertyValue is null) || !options.IgnoreNullValues) {
                    var attributeValue = DynamoSerializer.Serialize(propertyValue, options);
                    if(!(attributeValue is null)) {

                        // check if this object property has a custom name
                        var name = propertyAttributes.OfType<DynamoPropertyNameAttribute>().SingleOrDefault()?.Name ?? property.Name;
                        mapObject.Add(name, attributeValue);
                    }
                }
            }
            return new AttributeValue {
                M = mapObject,
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

            // create instance and set properties on it
            var result = Activator.CreateInstance(targetType) ?? throw new ApplicationException("Activator.CreateInstance() returned null");
            foreach(var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)) {
                var propertyAttributes = property.GetCustomAttributes();

                // check if this object property should be ignored
                if(!(propertyAttributes.OfType<DynamoPropertyIgnoreAttribute>().SingleOrDefault() is null)) {
                    continue;
                }

                // check if this object property has a custom name
                var name = propertyAttributes.OfType<DynamoPropertyNameAttribute>().SingleOrDefault()?.Name ?? property.Name;
                if(value.TryGetValue(name, out var attribute)) {
                    property.SetValue(result, DynamoSerializer.Deserialize(attribute, property.PropertyType, options));
                } else {
                    property.SetValue(result, DynamoSerializer.Deserialize(attribute: null, property.PropertyType, options));
                }
            }
            return result;
        }
    }
}
