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
using LambdaSharp.Compiler.Exceptions;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class ObjectExpression : AValueExpression, IEnumerable, IEnumerable<ObjectExpression.KeyValuePair> {

        //--- Types ---
        public class KeyValuePair {

            //--- Constructors ---
            public KeyValuePair(LiteralExpression key, AExpression value) {
                Key = key;
                Value = value;
            }

            //--- Properties ---
            public LiteralExpression Key { get; }
            public AExpression Value { get; set; }
        }

        //--- Fields ---
        private readonly List<KeyValuePair> _pairs = new List<KeyValuePair>();

        //--- Constructors ---
        public ObjectExpression() => _pairs = new List<KeyValuePair>();

        public ObjectExpression(IEnumerable<KeyValuePair> pairs)
            => _pairs = pairs.Select(pair => new KeyValuePair(SetParent(pair.Key), SetParent(pair.Value))).ToList();

        //--- Operators ---
        public AExpression this[string key] {
            get => this[Fn.Literal(key)];
            set => this[Fn.Literal(key)] = value;
        }

        public AExpression this[LiteralExpression key] {
            get {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                return _pairs.First(item => item.Key.Value == key.Value).Value;
            }
            set {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                if(value == null) {
                    throw new ArgumentNullException(nameof(value));

                }
                Remove(key.Value);
                _pairs.Add(new KeyValuePair(SetParent(key), SetParent(value)));
            }
        }

        //--- Properties ---
        public int Count => _pairs.Count;

        //--- Methods ---
        public bool TryGetValue(string key, [NotNullWhen(true)] out AExpression? value) {
            var found = _pairs.FirstOrDefault(item => item.Key.Value == key);
            value = found?.Value;
            return value != null;
        }

        public bool Remove(string key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _pairs.RemoveAll(kv => kv.Key.Value == key) > 0;
        }

        public bool ContainsKey(string key) => _pairs.Any(item => item.Key.Value == key);

        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            for(var i = 0; i < _pairs.Count; ++i) {
                var kv = _pairs[i];
                _pairs[i] = new KeyValuePair(
                    kv.Key.Visit(visitor) ?? throw new NullValueException(),
                    kv.Value.Visit(visitor) ?? throw new NullValueException()
                );
            }
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            foreach(var pair in _pairs) {
                pair.Key.InspectNode(inspector);
                pair.Value.InspectNode(inspector);
            }
        }

        public T? GetOrCreate<T>(string key, Action<AExpression> error) where T : AExpression, new() {
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
                this[key] = SetParent(result);
                return result;
            }
        }

        public override ASyntaxNode CloneNode() => new ObjectExpression(_pairs.Select(item => new KeyValuePair(item.Key.Clone(), item.Value.Clone())));

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _pairs.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => _pairs.GetEnumerator();
    }
}