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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.DynamoDB.Serialization.Converters;

namespace LambdaSharp.DynamoDB.Serialization {

    /// <summary>
    /// The <see cref="DynamoSerializerOptions"/> class provides properties to change the default (de)serialization behavior .
    /// </summary>
    public class DynamoSerializerOptions {

        //--- Class Fields ---
        internal static readonly IEnumerable<IDynamoAttributeConverter> DefaultConverters = new List<IDynamoAttributeConverter> {
            DynamoBoolConverter.Instance,
            DynamoIntConverter.Instance,
            DynamoLongConverter.Instance,
            DynamoDoubleConverter.Instance,
            DynamoFloatConverter.Instance,
            DynamoDateTimeOffsetConverter.Instance,
            DynamoDecimalConverter.Instance,
            DynamoStringConverter.Instance,
            DynamoEnumConverter.Instance,
            DynamoGuidConverter.Instance,
            DynamoByteArrayConverter.Instance,
            DynamoISetByteArrayConverter.Instance,
            DynamoISetStringConverter.Instance,
            DynamoISetIntConverter.Instance,
            DynamoISetLongConverter.Instance,
            DynamoISetDoubleConverter.Instance,
            DynamoISetDecimalConverter.Instance,
            DynamoIDictionaryConverter.Instance,
            DynamoListConverter.Instance,
            DynamoJsonElementConverter.Instance,
            DynamoObjectConverter.Instance
        };

        //--- Properties ---

        /// <summary>
        /// The <c>IgnoreNullValues</c> property controls if <c>null</c> values are serialized as DynamoDB NULL attribute values or skipped.
        /// </summary>
        public bool IgnoreNullValues { get; set; } = true;

        /// <summary>
        /// The <c>UseDefaultConverters</c> property controls if the default DynamoDB converters are enabled.
        /// </summary>
        public bool UseDefaultConverters { get; set; } = true;

        /// <summary>
        /// The <c>Converters</c> property lists additional custom converters to use when (de)serializing values.
        /// Custom converters take precedence over default converters. Default converters can be disabled entirely by
        /// setting the <c>UseDefaultConverters</c> property to <c>false</c>.
        /// </summary>
        public List<IDynamoAttributeConverter> Converters { get; set; } = new List<IDynamoAttributeConverter>();

        //--- Methods ---
        internal IEnumerable<IDynamoAttributeConverter> GetConverters()
            => UseDefaultConverters
                ? Converters.Concat(DefaultConverters)
                : Converters;
    }
}
