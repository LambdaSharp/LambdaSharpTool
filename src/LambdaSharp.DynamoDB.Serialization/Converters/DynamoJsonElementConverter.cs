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
using System.Text.Json;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public class DynamoJsonElementConverter : ADynamoAttributeConverter {

        //--- Class Fields ---
        public static readonly DynamoJsonElementConverter Instance = new DynamoJsonElementConverter();

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(JsonElement);

        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) {
            var json = (JsonElement)value;
            switch(json.ValueKind) {
            case JsonValueKind.Object:
                return WriteJsonObject(json, options);
            case JsonValueKind.Array:
                return WriteJsonArray(json, options);
            case JsonValueKind.String:
                return WriteJsonString(json, options);
            case JsonValueKind.Number:
                return WriteJsonNumber(json, options);
            case JsonValueKind.True:
            case JsonValueKind.False:
                return WriteJsonBool(json, options);
            case JsonValueKind.Null:
                return WriteJsonNull(options);
            default:
                throw new DynamoSerializationException($"unsupported JsonElement value kind: {json.ValueKind}");
            }
        }

        public override object? FromBool(bool value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from BOOL is not supported");
        public override object? FromBinary(MemoryStream value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from B is not supported");
        public override object? FromNumber(string value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from N is not supported");
        public override object? FromString(string value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from S is not supported");
        public override object? FromList(List<AttributeValue> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from L is not supported");
        public override object? FromMap(Dictionary<string, AttributeValue> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from M is not supported");
        public override object? FromBinarySet(List<MemoryStream> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from BS is not supported");
        public override object? FromNumberSet(List<string> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from NS is not supported");
        public override object? FromStringSet(List<string> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion to JsonElement from SS is not supported");

        private AttributeValue WriteJsonObject(JsonElement json, DynamoSerializerOptions options) => new AttributeValue {
            M = json.EnumerateObject().ToDictionary(property => property.Name, property => ToAttributeValue(property.Value, typeof(JsonElement), options)),
            IsMSet = true
        };

        private AttributeValue WriteJsonArray(JsonElement json, DynamoSerializerOptions options) => new AttributeValue {
            L = json.EnumerateArray().Select(item => ToAttributeValue(item, typeof(JsonElement), options)).ToList(),
            IsLSet = true
        };

        private AttributeValue WriteJsonString(JsonElement json, DynamoSerializerOptions options) => new AttributeValue {
            S = json.GetRawText()
        };

        private AttributeValue WriteJsonNumber(JsonElement json, DynamoSerializerOptions options) => new AttributeValue {
            N = json.GetRawText()
        };

        private AttributeValue WriteJsonBool(JsonElement json, DynamoSerializerOptions options) => new AttributeValue {
            BOOL = json.ValueKind == JsonValueKind.True
        };

        private AttributeValue WriteJsonNull(DynamoSerializerOptions options) => new AttributeValue {
            NULL = true
        };
    }
}
