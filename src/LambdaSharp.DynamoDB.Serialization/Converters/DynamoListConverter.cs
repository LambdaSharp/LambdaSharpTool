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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public class DynamoListConverter : ADynamoAttributeConverter {

        //--- Class Fields ---
        public static readonly DynamoListConverter Instance = new DynamoListConverter();

        //--- Class Methods ---
        private static Type GetListItemType(Type type) {
            if(type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList<>))) {
                return type.GenericTypeArguments[0];
            }
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>)))
                .Select(i => i.GenericTypeArguments[0])
                .FirstOrDefault();
        }

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert)
            => typeof(IList).IsAssignableFrom(typeToConvert)
                || (typeToConvert.IsGenericType && (typeToConvert.GetGenericTypeDefinition() == typeof(IList<>)))
                || typeToConvert.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>)));

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
