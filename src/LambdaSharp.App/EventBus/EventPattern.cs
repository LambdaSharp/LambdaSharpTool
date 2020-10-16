/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Text.Json.Serialization;

namespace LambdaSharp.App.EventBus {

    public sealed class EventPattern {

        //--- Class Methods ---
        public static object AnythingBut(object pattern)
            => new Dictionary<string, object> {
                ["anything-but"] = pattern ?? throw new ArgumentNullException(nameof(pattern))
            };

        public static object Prefix(string prefix)
            => new Dictionary<string, object> {
                ["prefix"] = prefix ?? throw new ArgumentNullException(nameof(prefix))
            };

        public static object Numeric(string operation, double value)
            => new Dictionary<string, object> {
                ["numeric"] = new object[] {
                    operation,
                    value
                }
            };

        public static object Numeric(string firstOperation, double firstValue, string secondOperation, double secondValue)
            => new Dictionary<string, object> {
                ["numeric"] = new object[] {
                    firstOperation,
                    firstValue,
                    secondOperation,
                    secondValue,
                }
            };

        public static object Cidr(string mask)
            => new Dictionary<string, object> {
                ["cidr"] = mask
            };

        public static object Exists(bool exists)
            => new Dictionary<string, object> {
                ["exists"] = exists
            };

        //--- Properties ---

        [JsonPropertyName("source")]
        public IEnumerable<object> Source { get; set; }

        [JsonPropertyName("detail-type")]
        public IEnumerable<object> DetailType { get; set; }

        [JsonPropertyName("resources")]
        public IEnumerable<object> Resources { get; set; }

        [JsonPropertyName("detail")]
        public object Detail { get; set; }
    }
}
