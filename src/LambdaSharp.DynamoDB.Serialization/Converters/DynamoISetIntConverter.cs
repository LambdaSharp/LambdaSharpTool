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
using System.Globalization;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public class DynamoISetIntConverter : ADynamoAttributeConverter {

        //--- Class Fields ---
        public static readonly DynamoISetIntConverter Instance = new DynamoISetIntConverter();

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) => typeof(ISet<int>).IsAssignableFrom(typeToConvert);

        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var numberSet = (ISet<int>)value;

            // NOTE (2021-06-21, bjorg): DynamoDB does not allow storing of empty sets!
            if(numberSet.Any()) {
                return new AttributeValue {
                    NS = numberSet.Select(item => item.ToString(CultureInfo.InvariantCulture)).ToList()
                };
            }
            throw new DynamoSerializationException("empty number set is not supported");
        }

        public override object? FromNumberSet(List<string> value, Type targetType, DynamoSerializerOptions options) {
            if(targetType.IsAssignableFrom(typeof(HashSet<int>))) {
                return value.Select(number => int.Parse(number)).ToHashSet();
            }
            if(!targetType.IsAbstract) {
                if(typeof(ISet<int>).IsAssignableFrom(targetType)) {

                    // create set instance and add items
                    var result = (ISet<int>)(Activator.CreateInstance(targetType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
                    foreach(var item in value) {
                        result.Add(int.Parse(item, CultureInfo.InvariantCulture));
                    }
                    return result;
                }
            }
            throw new DynamoSerializationException($"incompatible target type for NS attribute value (given: {targetType.FullName})");
        }
    }
}
