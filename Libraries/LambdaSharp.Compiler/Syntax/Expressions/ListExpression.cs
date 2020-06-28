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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Compiler.Exceptions;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class ListExpression : AValueExpression, IEnumerable, IEnumerable<AExpression> {

        //--- Fields ---
        private readonly List<AExpression> _items;

        //--- Constructors ---
        public ListExpression() => _items = new List<AExpression>();

        public ListExpression(IEnumerable<AExpression> items)
            => _items = items.Select(item => Adopt(item)).ToList();

        //--- Properties ---
        public int Count => _items.Count;

        //--- Operators ---
        public AExpression this[int index] {
            get => _items[index];
            set {
                if(!object.ReferenceEquals(_items[index], value)) {
                    Orphan(_items[index]);
                    _items[index]  = Adopt(value ?? throw new ArgumentNullException(nameof(value)));
                }
            }
        }

        //--- Methods ---
        public override void Inspect(Action<ASyntaxNode>? entryInspector, Action<ASyntaxNode>? exitInspector) {
            entryInspector?.Invoke(this);
            foreach(var item in _items) {
                item.Inspect(entryInspector, exitInspector);
            }
            exitInspector?.Invoke(this);
        }

        public override ISyntaxNode Substitute(Func<ISyntaxNode, ISyntaxNode> inspector) {
            for(var i = 0; i < Count; ++i) {
                var value = this[i];
                var newValue = value.Substitute(inspector) ?? throw new NullValueException();
                if(!object.ReferenceEquals(value, newValue)) {
                    this[i] = (AExpression)newValue;
                }
            }
            return inspector(this);
        }

        public override ASyntaxNode CloneNode() => new ListExpression(_items.Select(item => item.Clone()));

        public void Add(AExpression expression) => _items.Add(Adopt(expression));

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<AExpression> Members ---
        IEnumerator<AExpression> IEnumerable<AExpression>.GetEnumerator() => _items.GetEnumerator();
    }
}