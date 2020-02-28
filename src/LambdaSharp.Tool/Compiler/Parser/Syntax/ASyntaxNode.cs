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

    public abstract class ASyntaxNode {

        //--- Class Methods ---
        [return: NotNullIfNotNull("node")]
        public static ASyntaxNode? SetParent(ASyntaxNode? node, ASyntaxNode parent) {
            if(node != null) {

                // declaration nodes must have another declaration node as their parent
                if((parent != null) && (node is ADeclaration) && !(parent is ADeclaration)) {
                    throw new ApplicationException("declarations must have another declaration as parent");
                }

                // check if node needs to be cloned
                if((node is AExpression expression) && (node.Parent != null) && !object.ReferenceEquals(node.Parent, parent)) {
                    node = expression.Clone();
                }
                node.Parent = parent;
            }
            return node;
        }

        //--- Fields ---
        private SourceLocation? _sourceLocation;

        //--- Properties ---
        public ASyntaxNode? Parent { get; private set; }

        [AllowNull]
        public SourceLocation SourceLocation {

            // TODO: consider return a default empty location when no source location is found
            // TODO: check Builder.Log implementation and compare
            get => _sourceLocation ?? Parent?.SourceLocation ?? SourceLocation.Empty;
            set => _sourceLocation = value;
        }

        public IEnumerable<ASyntaxNode> Parents {
            get {
                var node = this;
                while(node.Parent != null) {
                    yield return node.Parent;
                    node = node.Parent;
                }
            }
        }

        public AItemDeclaration? ParentItemDeclaration => Parents.OfType<AItemDeclaration>().FirstOrDefault();
        public ModuleDeclaration ParentModuleDeclaration => Parents.OfType<ModuleDeclaration>().First();

        //--- Abstract Methods ---
        public abstract ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor);

        //--- Methods ---
        [return: NotNullIfNotNull("node") ]
        protected T? SetParent<T>(T? node) where T : ASyntaxNode => (T?)SetParent(node, this);

        [return: NotNullIfNotNull("list") ]
        protected SyntaxNodeCollection<T>? SetParent<T>(SyntaxNodeCollection<T>? list) where T : ASyntaxNode {
            if(list != null) {
                list.Parent = this;
            }
            return list;
        }

        [return: NotNullIfNotNull("list") ]
        protected SyntaxNodeCollection<AItemDeclaration>? SetParent(SyntaxNodeCollection<AItemDeclaration>? list) {
            if(list != null) {
                list.Parent = this;
            }
            return list;
        }

        protected void AssertIsSame(ASyntaxNode? originalValue, ASyntaxNode? newValue) {
            if(!object.ReferenceEquals(originalValue, newValue)) {
                throw new ApplicationException("attempt to change immutable value");
            }
        }
    }

    public static class ASyntaxNodeEx {

        //--- Extension Methods ---
        public static T? Visit<T>(this T node, ASyntaxNode? parent, ISyntaxVisitor visitor) where T : ASyntaxNode {
            var result = (T?)node.VisitNode(parent, visitor);
            if(result != null) {
                result.SourceLocation = node.SourceLocation;
            }
            return result;
        }

        // TODO: generalize to ASyntaxNode
        public static T Clone<T>(this T node) where T : AExpression {
            var result = (T)node.CloneNode();
            result.SourceLocation = node.SourceLocation;
            return result;
        }
    }

    public sealed class SyntaxNodeCollection<T> : IEnumerable, IEnumerable<T> where T : ASyntaxNode {

        //--- Fields ---
        private ASyntaxNode? _parent;
        private List<T> _nodes;

        //--- Constructors ---
        public SyntaxNodeCollection() => _nodes = new List<T>();

        public SyntaxNodeCollection(IEnumerable<T> nodes) {
            if(nodes is null) {
                throw new ArgumentNullException(nameof(nodes));
            }
            _nodes = nodes.Select(node => SetParent(node)).ToList();
        }

        //--- Properties ---
        public int Count => _nodes.Count;

        public ASyntaxNode Parent {
            get => _parent ?? throw new ArgumentNullException(nameof(Parent));
            set {
                _parent = value ?? throw new ArgumentNullException(nameof(Parent));
                _nodes = _nodes.Select(node => SetParent(node)).ToList();
            }
        }

        //--- Operators ---
        public T this[int index] {
            get => _nodes[index];
            set => _nodes[index] = SetParent(value) ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Methods ---
        public SyntaxNodeCollection<T> Visit(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            var start = 0;
            do {
                var count = _nodes.Count;
                for(var i = start; i < count; ++i) {
                    _nodes[i] = _nodes[i].Visit(Parent, visitor) ?? throw new NullValueException();
                }
                start = count;
            } while(start < _nodes.Count);
            return this;
        }

        public void Add(T expression) => _nodes.Add(SetParent(expression) ??  throw new ArgumentNullException(nameof(expression)));

        [return: NotNullIfNotNull("node")]
        private T? SetParent(T? node) {
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

    public static class SyntaxNodeCollectionEx {

        //--- Extension Methods ---
        public static SyntaxNodeCollection<T> ToSyntaxNodes<T>(this IEnumerable<T> enumerable) where T : ASyntaxNode
            => new SyntaxNodeCollection<T>(enumerable);
    }
}