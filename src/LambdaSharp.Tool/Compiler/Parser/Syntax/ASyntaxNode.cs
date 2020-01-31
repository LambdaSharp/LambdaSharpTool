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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class ASyntaxNode {

        //--- Class Methods ---
        public static void SetParent(ASyntaxNode? node, ASyntaxNode parent) {
            if(node != null) {

                // TODO: should we enforce this?
                // if(node.Parent != null) {
                //     throw new ApplicationException("node already has a parent");
                // }
                node.Parent = parent;
            }
        }

        //--- Fields ---
        private SourceLocation? _sourceLocation;

        //--- Properties ---
        public ASyntaxNode? Parent { get; private set; }

        public SourceLocation? SourceLocation {

            // TODO: consider return a default empty location when no source location is found
            get => _sourceLocation ?? Parent?.SourceLocation;
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

        public AItemDeclaration ParentItemDeclaration => Parents.OfType<AItemDeclaration>().First();
        public ModuleDeclaration ParentModuleDeclaration => Parents.OfType<ModuleDeclaration>().First();

        //--- Abstract Methods ---
        public abstract void Visit(ASyntaxNode parent, ISyntaxVisitor visitor);

        //--- Methods ---
        [return: NotNullIfNotNull("node") ]
        protected T? SetParent<T>(T? node) where T : ASyntaxNode {
            SetParent(node, this);
            return node;
        }

        [return: NotNullIfNotNull("list") ]
        protected SyntaxNodes<T>? SetParent<T>(SyntaxNodes<T>? list) where T : ASyntaxNode {
            if(list != null) {
                list.Parent = this;
            }
            return list;
        }
    }

    public class SyntaxNodes<T> : IEnumerable, IEnumerable<T> where T : ASyntaxNode {

        //--- Fields ---
        private ASyntaxNode? _parent;
        private readonly List<T> _nodes;

        //--- Constructors ---
        public SyntaxNodes() => _nodes = new List<T>();

        public SyntaxNodes(IEnumerable<T> nodes) => _nodes = new List<T>(nodes);

        //--- Properties ---
        public int Count => _nodes.Count;

        public ASyntaxNode Parent {
            get => _parent ?? throw new ArgumentNullException(nameof(Parent));
            set {
                _parent = value ?? throw new ArgumentNullException(nameof(Parent));
                foreach(var node in _nodes) {
                    SetParent(node);
                }
            }
        }

        public bool HasParent => _parent != null;

        //--- Operators ---
        public T this[int index] {
            get => _nodes[index];
            set => _nodes[index] = SetParent(value) ?? throw new ArgumentNullException(nameof(value));
        }

        //--- Methods ---
        public void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            foreach(var node in _nodes) {
                node?.Visit(Parent, visitor);
            }
        }

        public void Add(T expression) => _nodes.Add(SetParent(expression) ??  throw new ArgumentNullException(nameof(expression)));

        [return: NotNullIfNotNull("node")]
        private T SetParent(T node) {
            if(HasParent) {
                ASyntaxNode.SetParent(node, Parent);
            }
            return node;
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

        //--- IEnumerable<TS> Members ---
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _nodes.GetEnumerator();
    }

    public static class SyntaxNodesEx {

        //--- Extension Methods ---
        public static SyntaxNodes<T> ToSyntaxNodes<T>(this IEnumerable<T> enumerable) where T : ASyntaxNode => new SyntaxNodes<T>(enumerable);
    }
}