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
using System.Runtime.CompilerServices;

namespace LambdaSharp.CloudFormation.Syntax.Expressions {

    public class CloudFormationSyntaxMap : ACloudFormationSyntaxExpression, IEnumerable, IEnumerable<CloudFormationSyntaxMap.KeyValuePair> {

        //--- Types ---
        public class KeyValuePair {

            //--- Constructors ---
            public KeyValuePair(CloudFormationSyntaxLiteral key, ACloudFormationSyntaxExpression value) {
                Key = key;
                Value = value;
            }

            public void Deconstruct(out CloudFormationSyntaxLiteral key, out ACloudFormationSyntaxExpression value) {
                key = Key;
                value = Value;
            }

            //--- Properties ---
            public CloudFormationSyntaxLiteral Key { get; }
            public ACloudFormationSyntaxExpression Value { get; private set; }

            //--- Methods ---
            public void SetValue(ACloudFormationSyntaxExpression value)
                => Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Fields ---
        private readonly List<KeyValuePair> _pairs = new List<KeyValuePair>();

        //--- Constructors ---
        public CloudFormationSyntaxMap([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
            => _pairs = new List<KeyValuePair>();

        public CloudFormationSyntaxMap(IEnumerable<KeyValuePair> pairs, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
            => _pairs = pairs.Select(pair => new KeyValuePair(Adopt(pair.Key), Adopt(pair.Value))).ToList();

        //--- Operators ---
        public ACloudFormationSyntaxExpression this[CloudFormationSyntaxLiteral key] {
            get {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                return _pairs.First(pair => pair.Key.Value == key.Value).Value;
            }
            set {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                if(value == null) {
                    throw new ArgumentNullException(nameof(value));

                }

                // replace value in existing key-value pair
                var index = _pairs.TakeWhile(pair => pair.Key.Value != key.Value).Count();
                if(index < _pairs.Count) {
                    var pair = _pairs[index];
                    if(!object.ReferenceEquals(pair.Value, value)) {
                        Orphan(pair.Value);
                        pair.SetValue(Adopt(value));
                    }
                } else {
                    _pairs.Add(new KeyValuePair(Adopt(key), Adopt(value)));
                }
            }
        }

        //--- Properties ---
        public override CloudFormationSyntaxValueType ExpressionValueType => CloudFormationSyntaxValueType.Map;
        public IEnumerable<CloudFormationSyntaxLiteral> Keys => _pairs.Select(pair => pair.Key);

        //--- Methods ---
         public bool TryGetValue(string key, [NotNullWhen(true)] out ACloudFormationSyntaxExpression? value) {
            var found = _pairs.FirstOrDefault(pair => pair.Key.Value == key);
            value = found?.Value;
            return value != null;
        }

        public bool Remove(string key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _pairs.RemoveAll(pair => pair.Key.Value == key) > 0;
        }

        public bool ContainsKey(string key) => _pairs.Any(pair => pair.Key.Value == key);

        public override void Inspect(Action<ACloudFormationSyntaxNode>? entryInspector, Action<ACloudFormationSyntaxNode>? exitInspector) {
            entryInspector?.Invoke(this);
            foreach(var pair in _pairs) {
                pair.Key.Inspect(entryInspector, exitInspector);
                pair.Value.Inspect(entryInspector, exitInspector);
            }
            exitInspector?.Invoke(this);
        }

        public override ACloudFormationSyntaxNode Substitute(Func<ACloudFormationSyntaxNode, ACloudFormationSyntaxNode> inspector) {
            foreach(var pair in new List<KeyValuePair>(_pairs)) {
                this[pair.Key] = (ACloudFormationSyntaxExpression)(pair.Value.Substitute(inspector) ?? throw new NullValueException());
            }
            return inspector(this);
        }

        public T? GetOrCreate<T>(string key, Action<ACloudFormationSyntaxExpression> error) where T : ACloudFormationSyntaxExpression, new() {
            if(error == null) {
                throw new ArgumentNullException(nameof(error));
            }
            if(TryGetValue(key, out var value)) {
                if(value is T inner) {
                    return inner;
                } else {
                    error(value!);
                    return default(T);
                }
            } else {
                var result = new T {
                    SourceLocation = SourceLocation
                };
                this[new CloudFormationSyntaxLiteral(key)] = Adopt(result);
                return result;
            }
        }

        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxMap(_pairs.Select(pair => new KeyValuePair(pair.Key.Clone(), pair.Value.Clone()))) {
            SourceLocation = SourceLocation
        };

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _pairs.GetEnumerator();

        //--- IEnumerable<ACloudFormationSyntaxExpression> Members ---
         IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => _pairs.GetEnumerator();
   }
}