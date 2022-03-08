/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using LambdaSharp.DynamoDB.Serialization;
using LambdaSharp.DynamoDB.Serialization.Converters;

namespace Test.LambdaSharp.DynamoDB.Serialization.Internal {

    internal class DynamoTimeSpanConverter : ADynamoAttributeConverter {

        //--- Methods ---
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TimeSpan);

        public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options)
            => new AttributeValue {
                N = ((TimeSpan)value).TotalSeconds.ToString(CultureInfo.InvariantCulture)
            };

        public override object FromNumber(string value, Type targetType, DynamoSerializerOptions options) {
            if(!double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue)) {
                throw new DynamoSerializationException("invalid value for TimeSpan");
            }
            return TimeSpan.FromSeconds(parsedValue);
        }
    }
}
