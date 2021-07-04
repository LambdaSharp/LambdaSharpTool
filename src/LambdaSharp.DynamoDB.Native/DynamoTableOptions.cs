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
using LambdaSharp.DynamoDB.Serialization;

namespace LambdaSharp.DynamoDB.Native {

    public class DynamoTableOptions {

        //--- TYpes ---
        public class RecordType {

            //--- Fields ---
            private Type? _type;

            //--- Properties ---
            public Type Type {
                get => _type ?? throw new InvalidOperationException();
                set => _type = value ?? throw new ArgumentNullException();
            }
        }

        //--- Properties ---
        public DynamoSerializerOptions SerializerOptions { get; set; } = new DynamoSerializerOptions();
        public string? ExpectedTypeNamespace { get; set; }
        public List<RecordType> RecordTypes { get; set; } = new List<RecordType>();

        //--- Methods ---
        internal string RegisterTypeAndGetTypeName(Type type) {
            if(!RecordTypes.Any(recordType => recordType.Type == type)) {
                RecordTypes.Add(new RecordType {
                    Type = type
                });
            }
            return GetRecordTypeName(type);
        }

        internal string GetRecordTypeName(Type type) {
            var result = type.FullName ?? "";

            // check if the typename should be shortened
            if(
                !string.IsNullOrEmpty(ExpectedTypeNamespace)
                && result.StartsWith(ExpectedTypeNamespace, StringComparison.InvariantCulture)
            ) {
                result = result.Substring(ExpectedTypeNamespace.Length);
            }
            return result;
        }

        internal Type? GetRecordType(string typeName) {

            // check if typename was shortened
            if(typeName.StartsWith(".")) {
                typeName = $"{ExpectedTypeNamespace}{typeName}";
            }

            // find type that matches full typename
            return RecordTypes.FirstOrDefault(recordType => recordType.Type.FullName == typeName)?.Type;
        }
    }
}
