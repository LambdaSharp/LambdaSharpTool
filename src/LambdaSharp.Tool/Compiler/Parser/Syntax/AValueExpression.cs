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

 #nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
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
            public LiteralExpression Key {get; }
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
                _items.RemoveAll(kv => kv.Key.Value == key.Value);

                // don't add null entries
                if(value != null) {
                    _items.Add(new KeyValuePair(key, value));
                    key.Parent = this;
                    key.SourceLocation ??= SourceLocation;
                    value.Parent = this;
                    value.SourceLocation ??= SourceLocation;
                }
            }
        }

        //--- Methods ---

        // TODO: AExpression is only null if return value is false
        public bool TryGetValue(string key, out AExpression? value) {
            var found = _items.FirstOrDefault(item => item.Key.Value == key);
            value = found?.Value;
            return found != null;
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
                    Parent = this,
                    SourceLocation = SourceLocation
                };
                this[key] = result;
                return result;
            }
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => _items.GetEnumerator();
    }

    public class ListExpression : AValueExpression, IEnumerable, IEnumerable<AExpression> {

        //--- Properties ---
        public List<AExpression> Items { get; set; } = new List<AExpression>();
        public int Count => Items.Count;

        //--- Operators ---
        public AExpression this[int index] {
            get => Items[index];
            set {
                Items[index] = value ?? throw new ArgumentNullException(nameof(value));
                value.Parent = this;
                value.SourceLocation ??= SourceLocation;
            }
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Items?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }

        public void Add(AExpression expression) => Items.Add(expression);

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<AExpression> IEnumerable<AExpression>.GetEnumerator() => Items.GetEnumerator();
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