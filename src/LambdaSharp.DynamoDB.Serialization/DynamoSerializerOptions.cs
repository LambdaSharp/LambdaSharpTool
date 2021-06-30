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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.DynamoDB.Serialization.Converters;

namespace LambdaSharp.DynamoDB.Serialization {

    public class DynamoSerializerOptions {

        //--- Class Fields ---
        public static readonly IEnumerable<ADynamoAttributeConverter> DefaultConverters = new List<ADynamoAttributeConverter> {
            DynamoBoolConverter.Instance,
            DynamoIntConverter.Instance,
            DynamoLongConverter.Instance,
            DynamoDoubleConverter.Instance,
            DynamoDateTimeOffsetConverter.Instance,
            DynamoDecimalConverter.Instance,
            DynamoStringConverter.Instance,
            DynamoEnumConverter.Instance,
            DynamoByteArrayConverter.Instance,
            DynamoISetByteArrayConverter.Instance,
            DynamoISetStringConverter.Instance,
            DynamoISetIntConverter.Instance,
            DynamoISetLongConverter.Instance,
            DynamoISetDoubleConverter.Instance,
            DynamoISetDecimalConverter.Instance,
            DynamoIDictionarySetConverter.Instance,
            DynamoListConverter.Instance,
            DynamoJsonElementConverter.Instance,
            DynamoObjectConverter.Instance
        };

        //--- Properties ---
        public bool IgnoreNullValues { get; set; } = true;
        public bool UseDefaultConverters { get; set; } = true;
        public List<ADynamoAttributeConverter> Converters { get; set; } = new List<ADynamoAttributeConverter>();

        //--- Methods ---
        internal IEnumerable<ADynamoAttributeConverter> GetConverters()
            => UseDefaultConverters
                ? Converters.Concat(DefaultConverters)
                : Converters;
    }
}
