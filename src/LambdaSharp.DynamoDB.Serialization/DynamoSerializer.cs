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
using LambdaSharp.DynamoDB.Serialization.Converters;

namespace LambdaSharp.DynamoDB.Serialization {

    /* DYNAMODB DATA TYPES
     * https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.NamingRulesDataTypes.html#HowItWorks.DataTypes
     *
     * Number (int, long, double, decimal, int?, long?, double?, decimal?)
     * String (string, enum)
     * Binary (byte[])
     * Boolean (bool, bool?)
     * Null (bool?, int?, long?, double?, decimal?, List<object>, List<T>, Dictionary<string, object>, Dictionary<string, T>, HashSet<string|byte[]|int|long|double|decimal>)
     * List (List<object>, List<T>)
     * Map (Dictionary<string, object>, Dictionary<string, T>, T)
     * Set of String values (ISet<string>)
     * Set of Number values (ISet<int>, ISet<long>, ISet<double>, ISet<decimal>)
     * Set of Binary values (ISet<byte[]>)
     */

    public static class DynamoSerializer {

        //--- Class Methods ---
        public static AttributeValue Serialize(object? value)
            => Serialize(value, new DynamoSerializerOptions());

        public static AttributeValue Serialize(object? value, DynamoSerializerOptions options) {

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

        public static TRecord? Deserialize<TRecord>(Dictionary<string, AttributeValue> attributes)
            where TRecord : class
            => Deserialize<TRecord>(attributes, new DynamoSerializerOptions());

        public static TRecord? Deserialize<TRecord>(Dictionary<string, AttributeValue> attributes, DynamoSerializerOptions options)
            where TRecord : class
            => (TRecord?)Deserialize(attributes, typeof(TRecord), options);

        public static object? Deserialize(Dictionary<string, AttributeValue> attributes, Type? targetType, DynamoSerializerOptions options) {
            var usedTargetType = ((targetType is null) || (targetType == typeof(object)))
                ? typeof(Dictionary<string, object>)
                : targetType;
            var converter = options.GetConverters().FirstOrDefault(converter => converter.CanConvert(usedTargetType));
            if(converter == null) {
                throw new DynamoSerializationException($"cannot convert attribute value {nameof(AttributeValue.M)} (given: {targetType?.FullName ?? "<null>"})");
            }
            return converter.FromMap(attributes, usedTargetType, options);
        }

        public static object? Deserialize(AttributeValue attribute, Type? targetType, DynamoSerializerOptions options) {

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
            object? FindConverter(Type defaultType, string attributeValueTypeName, Func<ADynamoAttributeConverter, Type, object?> convert) {
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

        private static Type GetListItemType(Type type) {
            if(type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList<>))) {
                return type.GenericTypeArguments[0];
            }
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>)))
                .Select(i => i.GenericTypeArguments[0])
                .FirstOrDefault();
        }

        private static Type GetDictionaryItemType(Type type) {
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
    }
}
