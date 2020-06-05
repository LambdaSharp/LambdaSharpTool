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

    public abstract class AExpression : ASyntaxNode {

        //--- Methods ---
        public abstract ASyntaxNode CloneNode();
    }

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

    public class ListExpression : AValueExpression, IEnumerable, IEnumerable<AExpression> {

        //--- Fields ---
        private readonly List<AExpression> _items;

        //--- Constructors ---
        public ListExpression() => _items = new List<AExpression>();

        public ListExpression(IEnumerable<AExpression> items)
            => _items = items.Select(item => SetParent(item)).ToList();

        //--- Properties ---
        public int Count => _items.Count;

        //--- Operators ---
        public AExpression this[int index] {
            get => _items[index];
            set => _items[index] = SetParent(value ?? throw new ArgumentNullException(nameof(value)));
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            for(var i = 0; i < _items.Count; ++i) {
                _items[i] = _items[i].Visit(visitor) ?? throw new NullValueException();
            }
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            foreach(var item in _items) {
                item.InspectNode(inspector);
            }
        }

        public override ASyntaxNode CloneNode() => new ListExpression(_items.Select(item => item.Clone()));

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
        public LiteralExpression(string value, LiteralType type) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Type = type;
        }

        //--- Properties ---
        public string Value { get; }
        public LiteralType Type { get; }
        public bool IsString => Type == LiteralType.String;
        public bool IsInteger => Type == LiteralType.Integer;
        public bool IsFloat => Type == LiteralType.Float;
        public bool IsBool => Type == LiteralType.Bool;
        public bool IsTimestamp => Type == LiteralType.Timestamp;
        public bool IsNull => Type == LiteralType.Null;

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
        }

        public bool? AsBool() => (Type == LiteralType.Bool) ? bool.Parse(Value) : (bool?)null;
        public int? AsInt() => (Type == LiteralType.Integer) ? int.Parse(Value) : (int?)null;
        public override ASyntaxNode CloneNode() => new LiteralExpression(Value, Type);
    }
}