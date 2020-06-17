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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax {

    public abstract class ASyntaxNode : ISyntaxNode {

        //--- Class Methods ---
        [return: NotNullIfNotNull("node")]
        public static ASyntaxNode? SetParent(ASyntaxNode? node, ASyntaxNode? parent) {
            if(node != null) {

                // declaration nodes must have another declaration node as their parent
                if((parent != null) && (node is ADeclaration) && !(parent is ADeclaration)) {
                    throw new ApplicationException("declarations must have another declaration as parent");
                }

                // check if parent is changing
                if(!object.ReferenceEquals(node.Parent, parent)) {

                    // check if node needs to be cloned
                    if((parent != null) && (node is AExpression expression) && (node.Parent != null)) {
                        node = expression.Clone();
                    }

                    // update parent and invalidate any related information
                    node.Parent = parent;
                    node.ParentChanged();
                }
            }
            return node;
        }

        [return: NotNullIfNotNull("node")]
        public static ASyntaxNode? UnsetParent(ASyntaxNode? node) => SetParent(node, null);

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

        // TODO: can we make this non-nullable?
        public AItemDeclaration? ParentItemDeclaration => this.GetParents().OfType<AItemDeclaration>().FirstOrDefault();
        public ModuleDeclaration ParentModuleDeclaration => this.GetParents().OfType<ModuleDeclaration>().First();

        //--- Methods ---
        public virtual void Inspect(Action<ASyntaxNode>? entryInspector, Action<ASyntaxNode>? exitInspector) {
            var node = this as ASyntaxNode;
            if(node != null) {
                entryInspector?.Invoke(node);
            }
            foreach(var property in GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(property =>
                    (property.GetCustomAttribute<ASyntaxAttribute>() != null)
                    && typeof(ISyntaxNode).IsAssignableFrom(property.PropertyType)
                )
            ) {

                // skip null values
                var value = (ISyntaxNode?)property.GetValue(this);
                if(value == null) {
                    continue;
                }

                // recurse into property value
                value.Inspect(entryInspector, exitInspector);
            }
            if(node != null) {
                exitInspector?.Invoke(node);
            }
        }

        public void Inspect(Action<ASyntaxNode> inspector) => Inspect(inspector, exitInspector: null);

        public void InspectType<T>(Action<T> inspector)
            => Inspect(node => {
                if(node is T inspectableNode) {
                    inspector(inspectableNode);
                }
            });

        public virtual ISyntaxNode Substitute(Func<ISyntaxNode, ISyntaxNode> inspector) {
            foreach(var property in GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(property =>
                    (property.GetCustomAttribute<ASyntaxAttribute>() != null)
                    && typeof(ISyntaxNode).IsAssignableFrom(property.PropertyType)
                )
            ) {

                // only visit properties of type ISyntaxNode
                if(!typeof(ISyntaxNode).IsAssignableFrom(property.PropertyType)) {
                    continue;
                }

                // skip null values
                var value = (ISyntaxNode?)property.GetValue(this);
                if(value == null) {
                    continue;
                }

                // recurse into property value
                var newValue = value.Substitute(inspector) ?? throw new NullValueException();

                // update property if possible
                if(property.SetMethod != null) {
                    property.SetValue(this, newValue);
                } else if(!object.ReferenceEquals(newValue, value)) {

                    // TODO: better exception
                    throw new Exception("cannot update modified property");
                }
            }
            return inspector(this);
        }

        [return: NotNullIfNotNull("node") ]
        public T? Adopt<T>(T node) where T : ASyntaxNode => (T?)SetParent(node, this);

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
}