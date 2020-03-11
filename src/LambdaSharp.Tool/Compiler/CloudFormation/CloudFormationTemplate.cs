/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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

namespace LambdaSharp.Tool.Compiler.CloudFormation {

    public class CloudFormationTemplate {

        //--- Properties ---
        public string AWSTemplateFormatVersion => "2010-09-09";
        public string? Description { get; set; }
        public List<string> Transform { get; } = new List<string>();
        public Dictionary<string, CloudFormationParameter> Parameters { get; } = new Dictionary<string, CloudFormationParameter>();
        public Dictionary<string, Dictionary<string, string>> Mappings { get; } = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, CloudFormationObjectExpression> Conditions { get; } = new Dictionary<string, CloudFormationObjectExpression>();
        public Dictionary<string, CloudFormationResource> Resources { get; } = new Dictionary<string, CloudFormationResource>();
        public Dictionary<string, CloudFormationOutput> Outputs { get; } = new Dictionary<string, CloudFormationOutput>();
        public Dictionary<string, CloudFormationObjectExpression> Metadata { get; } = new Dictionary<string, CloudFormationObjectExpression>();
    }

    public class CloudFormationParameter {

        //--- Properties ---
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? AllowedPattern { get; set; }
        public List<string>? AllowedValues { get; set; }
        public string? ConstraintDescription { get; set; }
        public string? Default { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public bool? NoEcho { get; set; }
    }

    public class CloudFormationResource {

        //--- Properties ---
        public string? Type { get; set; }
        public CloudFormationObjectExpression Properties { get; set; } = new CloudFormationObjectExpression();
        public List<string> DependsOn { get; set; } = new List<string>();
        public Dictionary<string, CloudFormationObjectExpression> Metadata { get; } = new Dictionary<string, CloudFormationObjectExpression>();
        public string? Condition { get; set; }
        public string? DeletionPolicy { get; set; }
    }

    public class CloudFormationOutput {

        //--- Properties ---
        public ACloudFormationExpression? Value { get; set; }
        public ACloudFormationExpression? Export { get; set; }
        public string? Description { get; set; }
        public string? Condition { get; set; }
    }

    public class ACloudFormationExpression { }

    public class CloudFormationObjectExpression : ACloudFormationExpression, IEnumerable, IEnumerable<CloudFormationObjectExpression.KeyValuePair> {

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
        public CloudFormationObjectExpression() => _pairs = new List<KeyValuePair>();

        public CloudFormationObjectExpression(IEnumerable<KeyValuePair> pairs)
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
            return found != null;
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

    public class CloudFormationListExpression : ACloudFormationExpression, IEnumerable, IEnumerable<ACloudFormationExpression> {

        //--- Fields ---
        private readonly List<ACloudFormationExpression> _items;

        //--- Constructors ---
        public CloudFormationListExpression() => _items = new List<ACloudFormationExpression>();

        public CloudFormationListExpression(IEnumerable<ACloudFormationExpression> items)
            => _items = new List<ACloudFormationExpression>(items);

        //--- Properties ---
        public int Count => _items.Count;

        //--- Operators ---
        public ACloudFormationExpression this[int index] {
            get => _items[index];
            set => _items[index] = value ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Methods ---
        public void Add(ACloudFormationExpression expression) => _items.Add(expression);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<ACloudFormationExpression> IEnumerable<ACloudFormationExpression>.GetEnumerator() => _items.GetEnumerator();
    }

    public class CloudFormationLiteralExpression : ACloudFormationExpression {

        //--- Constructors ---
        public CloudFormationLiteralExpression(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public CloudFormationLiteralExpression(int value) => Value = value.ToString();

        //--- Properties ---
        public string Value { get; }
    }
}