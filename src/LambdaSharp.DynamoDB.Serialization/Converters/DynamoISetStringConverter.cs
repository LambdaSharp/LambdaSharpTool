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

        public override AttributeValue? ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var numberSet = (ISet<string>)value;

            // NOTE (2021-06-21, bjorg): DynamoDB does not allow storing empty sets!
            return numberSet.Any()
                ? new AttributeValue {
                    SS = numberSet.ToList()
                } : null;
        }

        public override object? GetDefaultValue(Type targetType, DynamoSerializerOptions options)
            => CreateInstance(targetType);

        public override object? FromStringSet(List<string> value, Type targetType, DynamoSerializerOptions options) {
            var result = CreateInstance(targetType);
            foreach(var item in value) {
                result.Add(item);
            }
            return result;
        }

        private ISet<string> CreateInstance(Type targetType) {
            if(targetType.IsAssignableFrom(typeof(HashSet<string>))) {

                // return HashSet<decimal>
                return new HashSet<string>();
            }
            if(!targetType.IsAbstract) {

                // create set instance and add items
                return (ISet<string>)(Activator.CreateInstance(targetType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
            }
            throw new DynamoSerializationException($"incompatible target type for SS attribute value (given: {targetType.FullName})");
        }
    }
}
