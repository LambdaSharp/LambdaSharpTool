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

namespace LambdaSharp.Compiler.Syntax {

    public sealed class SyntaxNodeCollection<T> : ISyntaxNode, IEnumerable, IEnumerable<T> where T : ASyntaxNode {

        //--- Fields ---
        private ASyntaxNode? _parent;
        private List<T> _nodes;
        private SourceLocation? _sourceLocation;

        //--- Constructors ---
        public SyntaxNodeCollection() => _nodes = new List<T>();

        public SyntaxNodeCollection(IEnumerable<T> nodes) {
            if(nodes is null) {
                throw new ArgumentNullException(nameof(nodes));
            }
            _nodes = nodes.Select(node => SetItemParent(node)).ToList();
        }

        //--- Properties ---
        public int Count => _nodes.Count;

        public ASyntaxNode Parent {
            get => _parent ?? throw new ArgumentNullException(nameof(Parent));
            set {
                _parent = value ?? throw new ArgumentNullException(nameof(Parent));
                _nodes = _nodes.Select(node => SetItemParent(node)).ToList();
            }
        }

        [AllowNull]
        public SourceLocation SourceLocation {

            // TODO: consider return a default empty location when no source location is found
            // TODO: check Builder.Log implementation and compare
            // TODO: consider using the location of the first item when available
            get => _sourceLocation ?? Parent?.SourceLocation ?? SourceLocation.Empty;
            set => _sourceLocation = value;
        }

        //--- Operators ---
        public T this[int index] {
            get => _nodes[index];
            set => _nodes[index] = SetItemParent(value ?? throw new ArgumentNullException(nameof(value)));
        }

        //--- Methods ---
        public SyntaxNodeCollection<T> Visit(ISyntaxVisitor visitor) {
            var start = 0;
            do {
                var count = _nodes.Count;
                for(var i = start; i < count; ++i) {
                    _nodes[i] = _nodes[i].Visit(visitor) ?? throw new NullValueException();
                }
                start = count;
            } while(start < _nodes.Count);
            return this;
        }

        public void InspectNode(Action<ASyntaxNode> inspector) {
            foreach(var node in _nodes) {
                inspector(node);
            }
        }

        public void Substitute(Func<ASyntaxNode, ASyntaxNode> inspector) {
            for(var i = 0; i < _nodes.Count; ++i) {
                _nodes[i].Substitute(inspector);
                var newValue = inspector(_nodes[i]) ?? throw new NullValueException();
                _nodes[i] = (T)newValue;
            }
        }

        public void Add(T expression) => _nodes.Add(SetItemParent(expression ??  throw new ArgumentNullException(nameof(expression))));

        public void AddRange(IEnumerable<T> items) {
            foreach(var item in items) {
                Add(item);
            }
        }

        [return: NotNullIfNotNull("node")]
        private T? SetItemParent(T? node) {
            if((node != null) && (_parent != null)) {
                return (T)ASyntaxNode.SetParent(node, _parent);
            }
            return node;
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

        //--- IEnumerable<TS> Members ---
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _nodes.GetEnumerator();
    }
}