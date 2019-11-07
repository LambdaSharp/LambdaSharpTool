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
using System.Linq;

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AExpression : ASyntaxNode { }

    public class ObjectExpression : AExpression, IEnumerable, IEnumerable<ObjectExpression.KeyValuePair> {

        //--- Types ---
        public class KeyValuePair : ASyntaxNode {

            //--- Properties ---
            public LiteralExpression Key { get; set; }
            public AExpression Value { get; set; }

            //--- Methods ---
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                Value.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }

        //--- Properties ---
        public List<KeyValuePair> Items { get; set; } = new List<KeyValuePair>();

        //--- Operators ---
        public AExpression this[string key] {
            get => Items.First(item => item.Key.Value == key).Value;
            set => Items.Add(new KeyValuePair {
                Key = new LiteralExpression {
                    Value = key ?? throw new ArgumentNullException(nameof(key))
                },
                Value = value ?? throw new ArgumentNullException(nameof(value))
            });
        }

        public AExpression this[LiteralExpression key] {
            get => Items.First(item => item.Key.Value == key.Value).Value;
            set => Items.Add(new KeyValuePair {
                Key = key,
                Value = value ?? throw new ArgumentNullException(nameof(value))
            });
        }

        //--- Methods ---
        public bool TryGetValue(string key, out AExpression value) {
            var found = Items.FirstOrDefault(item => item.Key.Value == key);
            value = found?.Value;
            return found != null;
        }

        public bool ContainsKey(string key) => Items.Any(item => item.Key.Value == key);

        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Items.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<KeyValuePair> IEnumerable<KeyValuePair>.GetEnumerator() => Items.GetEnumerator();
    }

    public class ListExpression : AExpression, IEnumerable, IEnumerable<AExpression> {

        //--- Properties ---
        public List<AExpression> Items { get; set; } = new List<AExpression>();
        public int Count => Items.Count;

        //--- Operators ---
        public AExpression this[int index] {
            get => Items[index];
            set => Items[index] = value;
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

    public class LiteralExpression : AExpression {

        //--- Properties ---
        public string Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            visitor.VisitEnd(parent, this);
        }
    }
}