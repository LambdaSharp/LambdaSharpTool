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
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Serialization.Converters {

    public interface IDynamoAttributeConverter {

        //--- Methods ---
        bool CanConvert(Type typeToConvert);
        object? FromBinary(MemoryStream value, Type targetType, DynamoSerializerOptions options);
        object? FromBinarySet(List<MemoryStream> value, Type targetType, DynamoSerializerOptions options);
        object? FromBool(bool value, Type targetType, DynamoSerializerOptions options);
        object? FromList(List<AttributeValue> value, Type targetType, DynamoSerializerOptions options);
        object? FromMap(Dictionary<string, AttributeValue> value, Type targetType, DynamoSerializerOptions options);
        object? FromNull(Type targetType, DynamoSerializerOptions options);
        object? FromNumber(string value, Type targetType, DynamoSerializerOptions options);
        object? FromNumberSet(List<string> value, Type targetType, DynamoSerializerOptions options);
        object? FromString(string value, Type targetType, DynamoSerializerOptions options);
        object? FromStringSet(List<string> value, Type targetType, DynamoSerializerOptions options);
        object? GetDefaultValue(Type targetType, DynamoSerializerOptions options);
        AttributeValue? ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options);
    }
}
