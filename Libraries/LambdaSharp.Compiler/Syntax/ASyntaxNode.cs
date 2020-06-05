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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax {

    public interface ISyntaxNode {

        //--- Properties ---
        SourceLocation? SourceLocation { get; }
        ASyntaxNode? Parent { get; }
    }

    public abstract class ASyntaxNode : ISyntaxNode {

        //--- Class Methods ---
        [return: NotNullIfNotNull("node")]
        public static ASyntaxNode? SetParent(ASyntaxNode? node, ASyntaxNode parent) {
            if(node != null) {

                // declaration nodes must have another declaration node as their parent
                if((parent != null) && (node is ADeclaration) && !(parent is ADeclaration)) {
                    throw new ApplicationException("declarations must have another declaration as parent");
                }

                // check if parent has changed
                if(!object.ReferenceEquals(node.Parent, parent)) {

                    // check if node needs to be cloned
                    if((node is AExpression expression) && (node.Parent != null)) {
                        node = expression.Clone();
                    }

                    // update parent and invalidate any related information
                    node.Parent = parent;
                    node.ParentChanged();
                }
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

        public AItemDeclaration? ParentItemDeclaration => this.GetParents().OfType<AItemDeclaration>().FirstOrDefault();
        public ModuleDeclaration ParentModuleDeclaration => this.GetParents().OfType<ModuleDeclaration>().First();

        //--- Abstract Methods ---
        public abstract ASyntaxNode? VisitNode(ISyntaxVisitor visitor);
        public abstract void InspectNode(Action<ASyntaxNode> inspector);

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

        protected virtual void ParentChanged() { }
    }

    public static class ASyntaxNodeEx {

        //--- Extension Methods ---
        public static T? Visit<T>(this T node, ISyntaxVisitor visitor) where T : ASyntaxNode {
            var result = (T?)node.VisitNode(visitor);
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

        public static IEnumerable<ASyntaxNode> GetParents(this ISyntaxNode node) {
            while(node.Parent != null) {
                yield return node.Parent;
                node = node.Parent;
            }
        }
    }
}