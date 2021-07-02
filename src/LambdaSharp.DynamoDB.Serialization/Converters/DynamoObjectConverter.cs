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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public class DynamoObjectConverter : ADynamoAttributeConverter {

        //--- Class Fields ---
        public static readonly DynamoObjectConverter Instance = new DynamoObjectConverter();

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) => !typeToConvert.IsValueType;

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
