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
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public class DynamoISetStringConverter : ADynamoAttributeConverter {

        //--- Class Fields ---
        public static readonly DynamoISetStringConverter Instance = new DynamoISetStringConverter();

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) => typeof(ISet<string>).IsAssignableFrom(typeToConvert);

        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var stringSet = (ISet<string>)value;

            // NOTE (2021-06-21, bjorg): DynamoDB does not allow storing of empty sets!
            if(stringSet.Any()) {
                return new AttributeValue {
                    SS = stringSet.ToList()
                };
            }
            throw new DynamoSerializationException("empty string set is not supported");
        }

        public override object? FromStringSet(List<string> value, Type targetType, DynamoSerializerOptions options) {
            if(targetType.IsAssignableFrom(typeof(HashSet<string>))) {

                // return HashSet<string>
                return value.ToHashSet();
            }
            if(!targetType.IsAbstract && typeof(ISet<string>).IsAssignableFrom(targetType)) {

                // create set instance and add items
                var result = (ISet<string>)(Activator.CreateInstance(targetType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
                foreach(var item in value) {
                    result.Add(item);
                }
                return result;
            }
            throw new DynamoSerializationException($"incompatible target type for SS attribute value (given: {targetType.FullName})");

        }
    }
}
