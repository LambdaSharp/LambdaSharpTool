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

    public abstract class ADynamoAttributeConverter : IDynamoAttributeConverter {

        //--- Abstract Methods ---
        public abstract bool CanConvert(Type typeToConvert);

        //--- Methods ---
        public virtual AttributeValue? ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options) => throw new NotImplementedException("conversion to attribute value is not implemented");

        public virtual object? GetDefaultValue(Type targetType, DynamoSerializerOptions options)
            => (targetType.IsValueType && (Nullable.GetUnderlyingType(targetType) == null))
                ? Activator.CreateInstance(targetType)
                : null;

        public virtual object? FromNull(Type targetType, DynamoSerializerOptions options)
            => (!targetType.IsValueType || (Nullable.GetUnderlyingType(targetType) != null))
                ? (object?)null
                : throw new DynamoSerializationException("conversion from NULL is not supported");

        public virtual object? FromBool(bool value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from BOOL is not supported");
        public virtual object? FromBinary(MemoryStream value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from B is not supported");
        public virtual object? FromNumber(string value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from N is not supported");
        public virtual object? FromString(string value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from S is not supported");
        public virtual object? FromList(List<AttributeValue> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from L is not supported");
        public virtual object? FromMap(Dictionary<string, AttributeValue> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from M is not supported");
        public virtual object? FromBinarySet(List<MemoryStream> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from BS is not supported");
        public virtual object? FromNumberSet(List<string> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from NS is not supported");
        public virtual object? FromStringSet(List<string> value, Type targetType, DynamoSerializerOptions options) => throw new DynamoSerializationException("conversion from SS is not supported");
    }
}
