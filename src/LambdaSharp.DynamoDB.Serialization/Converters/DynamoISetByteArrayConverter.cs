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
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public class DynamoISetByteArrayConverter : ADynamoAttributeConverter {

        //--- Class Fields ---
        public static readonly DynamoISetByteArrayConverter Instance = new DynamoISetByteArrayConverter();

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) => typeof(ISet<byte[]>).IsAssignableFrom(typeToConvert);

        public override AttributeValue? ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var binarySet = (ISet<byte[]>)value;

            // NOTE (2021-06-21, bjorg): DynamoDB does not allow storing empty sets!
            return binarySet.Any()
                ? new AttributeValue {
                    BS = binarySet.Select(bytes => new MemoryStream(bytes)).ToList()
                } : null;
        }

        public override object? GetDefaultValue(Type targetType, DynamoSerializerOptions options)
            => CreateInstance(targetType);

        public override object? FromBinarySet(List<MemoryStream> value, Type targetType, DynamoSerializerOptions options) {
            var result = CreateInstance(targetType);
            foreach(var item in value) {
                result.Add(item.ToArray());
            }
            return result;
        }

        private ISet<byte[]> CreateInstance(Type targetType) {
            if(targetType.IsAssignableFrom(typeof(HashSet<byte[]>))) {

                // return HashSet<byte[]>
                return new HashSet<byte[]>(ByteArrayEqualityComparer.Instance);
            }
            if(!targetType.IsAbstract) {

                // create set instance and add items
                return (ISet<byte[]>)(Activator.CreateInstance(targetType) ?? throw new ApplicationException("Activator.CreateInstance() returned null"));
            }
            throw new DynamoSerializationException($"incompatible target type for BS attribute value (given: {targetType.FullName})");
        }
    }
}
