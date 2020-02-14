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

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AExpression : ASyntaxNode { }

    public abstract class AValueExpression : AExpression { }

    public class ObjectExpression : AValueExpression, IEnumerable, IEnumerable<ObjectExpression.KeyValuePair> {

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
        private readonly List<KeyValuePair> _items = new List<KeyValuePair>();

        //--- Operators ---
        public AExpression this[string key] {
            get => this[new LiteralExpression(key)];
            set => this[new LiteralExpression(key)] = value;
        }

        public AExpression this[LiteralExpression key] {
            get {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                return _items.First(item => item.Key.Value == key.Value).Value;
            }
            set {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                if(value == null) {
                    throw new ArgumentNullException(nameof(value));

                }
                Remove(key.Value);
                _items.Add(new KeyValuePair(SetParent(key), SetParent(value)));
            }
        }

        //--- Methods ---
        public bool TryGetValue(string key, [NotNullWhen(true)] out AExpression? value) {
            var found = _items.FirstOrDefault(item => item.Key.Value == key);
            value = found?.Value;
            return found != null;
        }

        public bool Remove(string key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _items.RemoveAll(kv => kv.Key.Value == key) > 0;
        }

        public bool ContainsKey(string key) => _items.Any(item => item.Key.Value == key);
        public int Count => _items.Count;

        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            foreach(var kv in _items) {
                kv.Key.Visit(this, visitor);
                kv.Value.Visit(this, visitor);
            }
            visitor.VisitEnd(parent, this);
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

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => _items.GetEnumerator();
    }

    public class ListExpression : AValueExpression, IEnumerable, IEnumerable<AExpression> {

        //--- Fields ---
        private readonly List<AExpression> _items = new List<AExpression>();

        //--- Constructors ---
        public ListExpression() { }

        public ListExpression(IEnumerable<AExpression> items) {
            _items.AddRange(items);
            foreach(var item in items) {
                SetParent(item);
            }
        }

        //--- Properties ---
        public int Count => _items.Count;

        //--- Operators ---
        public AExpression this[int index] {
            get => _items[index];
            set => _items[index] = SetParent(value) ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            foreach(var item in _items) {
                item?.Visit(this, visitor);
            }
            visitor.VisitEnd(parent, this);
        }

        public void Add(AExpression expression) => _items.Add(SetParent(expression));

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<AExpression> IEnumerable<AExpression>.GetEnumerator() => _items.GetEnumerator();
    }

    public enum LiteralType {
        String,
        Integer,
        Float,
        Bool,
        Timestamp,
        Null
    }

    public class LiteralExpression : AValueExpression {

        //--- Constructors ---
        public LiteralExpression(string value) : this(value, LiteralType.String) { }

        public LiteralExpression(string value, LiteralType type) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Type = type;
        }

        public LiteralExpression(int value) {
            Value = value.ToString();
            Type = LiteralType.Integer;
        }

        //--- Properties ---
        public string Value { get; }
        public LiteralType Type { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            visitor.VisitEnd(parent, this);
        }
    }
}