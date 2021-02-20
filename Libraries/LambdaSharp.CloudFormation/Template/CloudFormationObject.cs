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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using LambdaSharp.CloudFormation.Template.Serialization;

namespace LambdaSharp.CloudFormation.Template {

    [JsonConverter(typeof(CloudFormationObjectConverter))]
    public class CloudFormationObject : ACloudFormationExpression, IEnumerable, IEnumerable<CloudFormationObject.KeyValuePair> {

        //--- Types ---
        public class KeyValuePair {

            //--- Constructors ---
            public KeyValuePair(string key, ACloudFormationExpression value) {
                Key = key;
                Value = value;
            }

            //--- Properties ---
            public string Key { get; }
            public ACloudFormationExpression Value { get; set; }
        }

        //--- Fields ---
        private readonly List<KeyValuePair> _pairs = new List<KeyValuePair>();

        //--- Constructors ---
        public CloudFormationObject() => _pairs = new List<KeyValuePair>();

        public CloudFormationObject(IEnumerable<KeyValuePair> pairs)
            => _pairs = pairs.Select(pair => new KeyValuePair(pair.Key, pair.Value)).ToList();

        //--- Operators ---
        public ACloudFormationExpression this[string key] {
            get {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                return _pairs.First(item => item.Key == key).Value;
            }
            set {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                if(value == null) {
                    throw new ArgumentNullException(nameof(value));

                }
                Remove(key);
                _pairs.Add(new KeyValuePair(key, value));
            }
        }

        //--- Properties ---
        public int Count => _pairs.Count;

        //--- Methods ---
        public bool TryGetValue(string key, [NotNullWhen(true)] out ACloudFormationExpression? value) {
            var found = _pairs.FirstOrDefault(item => item.Key == key);
            value = found?.Value;
            return value != null;
        }

        public bool Remove(string key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _pairs.RemoveAll(kv => kv.Key == key) > 0;
        }

        public bool ContainsKey(string key) => _pairs.Any(item => item.Key == key);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _pairs.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => _pairs.GetEnumerator();
    }
}