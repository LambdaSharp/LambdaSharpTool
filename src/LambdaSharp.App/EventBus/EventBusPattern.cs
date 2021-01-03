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
using System.Text.Json.Serialization;

namespace LambdaSharp.App.EventBus {

    /// <summary>
    /// The <see cref="EventBusPattern"/> describes an event pattern the LambdaSharp EventBus should forward to an app.
    /// </summary>
    public sealed class EventBusPattern {

        //--- Class Methods ---

        /// <summary>
        /// The <see cref="AnythingBut(object)"/> method creates an event pattern clause that matches everything that does not
        /// match the nested event pattern clause.
        /// </summary>
        /// <param name="pattern">A nested event pattern clause.</param>
        /// <returns>An event pattern clause.</returns>
        public static object AnythingBut(object pattern)
            => new Dictionary<string, object> {
                ["anything-but"] = pattern ?? throw new ArgumentNullException(nameof(pattern))
            };

        /// <summary>
        /// The <see cref="Prefix(string)"/> method creates an event pattern clause that matches the prefix of a literal string value.
        /// </summary>
        /// <param name="prefix">The string prefix to match.</param>
        /// <returns>An event pattern clause.</returns>
        public static object Prefix(string prefix)
            => new Dictionary<string, object> {
                ["prefix"] = prefix ?? throw new ArgumentNullException(nameof(prefix))
            };

        /// <summary>
        /// The <see cref="Numeric(string,double)"/> method creates an event pattern clauses that matches a numeric condition.
        /// </summary>
        /// <param name="operation">The numeric compare operation.</param>
        /// <param name="value">The numeric value to compare to.</param>
        /// <returns>An event pattern clause.</returns>
        public static object Numeric(string operation, double value)
            => new Dictionary<string, object> {
                ["numeric"] = new object[] {
                    operation,
                    value
                }
            };

        /// <summary>
        /// The <see cref="Numeric(string,double,string,double)"/> method creates an event pattern clauses that matches both numeric conditions.
        /// </summary>
        /// <param name="firstOperation">The first numeric compare operation.</param>
        /// <param name="firstValue">The first numeric value to compare to.</param>
        /// <param name="secondOperation">The second numeric compare operation.</param>
        /// <param name="secondValue">The second numeric value to compare to.</param>
        /// <returns>An event pattern clause.</returns>
        public static object Numeric(string firstOperation, double firstValue, string secondOperation, double secondValue)
            => new Dictionary<string, object> {
                ["numeric"] = new object[] {
                    firstOperation,
                    firstValue,
                    secondOperation,
                    secondValue,
                }
            };

        /// <summary>
        /// The <see cref="Cidr(string)"/> method creates an event pattern clause that checks if an IP address falls within the specified CIDR mask.
        /// </summary>
        /// <param name="mask">The CIDR mask to match against.</param>
        /// <returns>An event pattern clause.</returns>
        public static object Cidr(string mask)
            => new Dictionary<string, object> {
                ["cidr"] = mask
            };

        /// <summary>
        /// The <see cref="Exists(bool)"/> method creates an event pattern clauses that matches if a JSON property exists or not.
        /// </summary>
        /// <param name="exists">Boolean determining when the event pattern clause should match.</param>
        /// <returns>An event pattern clause.</returns>
        public static object Exists(bool exists)
            => new Dictionary<string, object> {
                ["exists"] = exists
            };

        //--- Properties ---

        /// <summary>
        /// The <see cref="Source"/> property sets the event pattern clauses for the 'source' event property.
        /// </summary>
        [JsonPropertyName("source")]
        public IEnumerable<object> Source { get; set; }

        /// <summary>
        /// The <see cref="DetailType"/> property sets the event pattern clauses for the 'detail-type' event property.
        /// </summary>
        [JsonPropertyName("detail-type")]
        public IEnumerable<object> DetailType { get; set; }

        /// <summary>
        /// The <see cref="Resources"/> property sets the event pattern clauses for the 'resources' event property.
        /// </summary>
        [JsonPropertyName("resources")]
        public IEnumerable<object> Resources { get; set; }

        /// <summary>
        /// The <see cref="Detail"/> property sets the event pattern clauses for the 'detail' event property.
        /// </summary>
        [JsonPropertyName("detail")]
        public object Detail { get; set; }
    }
}
