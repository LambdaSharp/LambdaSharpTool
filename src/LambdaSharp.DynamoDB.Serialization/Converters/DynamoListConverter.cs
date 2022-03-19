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
    /// The <see cref="DynamoListConverter"/> class is used to convert <c>ILIst</c> or <c>List&lt;T&gt;</c> value to/from a DynamoDB attribute value.
    /// </summary>
    public class DynamoListConverter : ADynamoAttributeConverter {

        //--- Class Fields ---

        /// <summary>
        /// The <see cref="Instance"/> class field exposes a reusable instance of the class.
        /// </summary>
        public static readonly DynamoListConverter Instance = new DynamoListConverter();

        //--- Class Methods ---
        private static Type? GetListItemType(Type type) {
            if(type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList<>))) {
                return type.GenericTypeArguments[0];
            }
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>)))
                .Select(i => i.GenericTypeArguments[0])
                .FirstOrDefault();
        }

        //--- Methods ---

        /// <summary>
        /// The <see cref="CanConvert(Type)"/> method checks if this converter can handle the presented type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns><c>true</c> if the converter can handle the type; otherwise, <c>false</c></returns>
        public override bool CanConvert(Type typeToConvert)
            => typeof(IList).IsAssignableFrom(typeToConvert)
                || (typeToConvert.IsGenericType && (typeToConvert.GetGenericTypeDefinition() == typeof(IList<>)))
                || typeToConvert.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>)));

        /// <summary>
        /// The <see cref="ToAttributeValue(object,Type,DynamoSerializerOptions)"/> method converts an instance to a DynamoDB attribute value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The source value type.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A DynamoDB attribute value, or <c>null</c> if the instance state cannot be represented in DynamoDB.</returns>
        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var list = new List<AttributeValue>();
            foreach(var item in (IEnumerable)value) {
                var attributeValue = DynamoSerializer.Serialize(item, options);
                list.Add(attributeValue ?? new AttributeValue {
                    NULL = true
                });
            }
            return new AttributeValue {
                L = list,
                IsLSet = true
            };
        }

        /// <summary>
        /// The <see cref="FromList(List{AttributeValue},Type,DynamoSerializerOptions)"/> method converts a DynamoDB L attribute value to the type of the converter.
        /// </summary>
        /// <param name="value">The DynamoDB attribute value to convert.</param>
        /// <param name="targetType">The expected return type.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>An instance of type <paramref name="targetType"/>.</returns>
        public override object? FromList(List<AttributeValue> value, Type targetType, DynamoSerializerOptions options) {
            if(targetType.IsAssignableFrom(typeof(List<object>))) {

                // NOTE (2021-06-23, bjorg): this covers the case where targetype is `IList`

                // return List<object>
                return value.Select(item => DynamoSerializer.Deserialize(item, targetType: null, options)).ToList();
            }

            // check if item type can be determined via `IList<T>` inheritance
            var itemType = GetListItemType(targetType);

            // determine if a concrete type needs to be identified
            Type listType = targetType;
            if(targetType.IsInterface) {
                if(itemType != null) {

                    // create `List<T>`
                    listType = typeof(List<>).MakeGenericType(new[] { itemType });
                } else {

                    // create `ArrayList`
                    listType = typeof(ArrayList);
                }
                if(!targetType.IsAssignableFrom(listType)) {
                    throw new DynamoSerializationException($"incompatible target type for L attribute value (given: {targetType.FullName})");
                }
            }

            // check if we can use the `IList` interface to add items to the list instance
            if(typeof(IList).IsAssignableFrom(listType)) {
                var result = (IList)(Activator.CreateInstance(listType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
                foreach(var item in value) {
                    result.Add(DynamoSerializer.Deserialize(item, itemType, options));
                }
                return result;
            } else {

                // use `dynamic` to invoke the appropriate typed `Add()` method
                dynamic result = Activator.CreateInstance(listType) ?? throw new ApplicationException("Activator.CreateInstance() returned null");
                foreach(var item in value) {
                    result.Add(DynamoSerializer.Deserialize(item, itemType, options));
                }
                return result;
            }
        }
    }
}
