/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LambdaSharp.CloudFormation.Builder.Declarations;
using LambdaSharp.CloudFormation.Builder.Expressions;

namespace LambdaSharp.CloudFormation.Builder {

    // TODO: !Transform is probably NOT a function since it modifies it surroundings as well

    public abstract class ACloudFormationBuilderNode {

        //--- Class Methods ---
        [return: NotNullIfNotNull("node")]
        public static ACloudFormationBuilderNode? SetParent(ACloudFormationBuilderNode? child, ACloudFormationBuilderNode? parent) {
            if(child != null) {

                // declaration nodes must have another declaration node as their parent
                if((parent != null) && (child is ACloudFormationBuilderDeclaration) && !(parent is ACloudFormationBuilderDeclaration)) {
                    throw new ApplicationException("declarations must have another declaration as parent");
                }

                // check if parent is changing
                if(!object.ReferenceEquals(child.Parent, parent)) {

                    // check if node needs to be cloned
                    if((parent != null) && (child is ACloudFormationBuilderExpression expression) && (child.Parent != null)) {
                        child = expression.Clone();
                    }

                    // update parent and invalidate any related information
                    child.Parent = parent;
                    child.ParentChanged();
                }
            }
            return child;
        }

        [return: NotNullIfNotNull("node")]
        public static ACloudFormationBuilderNode? Orphan(ACloudFormationBuilderNode? node) => SetParent(node, parent: null);

        //--- Properties ---
        public ACloudFormationBuilderNode? Parent { get; private set; }
        public SourceLocation SourceLocation { get; set; } = SourceLocation.Empty;

        //--- Methods ---
        public void Inspect(Action<ACloudFormationBuilderNode> inspector) => Inspect(inspector, exitInspector: null);

        public virtual void Inspect(Action<ACloudFormationBuilderNode>? entryInspector, Action<ACloudFormationBuilderNode>? exitInspector) {
            var node = this as ACloudFormationBuilderNode;
            if(node != null) {
                entryInspector?.Invoke(node);
            }
            foreach(var property in GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(property =>
                    (property.GetCustomAttribute<InspectAttribute>() != null)
                    && typeof(ACloudFormationBuilderNode).IsAssignableFrom(property.PropertyType)
                )
            ) {

                // skip null values
                var value = (ACloudFormationBuilderNode?)property.GetValue(this);
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

        public void InspectType<T>(Action<T> inspector)
            => Inspect(node => {
                if(node is T inspectableNode) {
                    inspector(inspectableNode);
                }
            });

        public virtual ACloudFormationBuilderNode Substitute(Func<ACloudFormationBuilderNode, ACloudFormationBuilderNode> inspector) {
            foreach(var property in GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(property =>
                    (property.GetCustomAttribute<InspectAttribute>() != null)
                    && typeof(ACloudFormationBuilderNode).IsAssignableFrom(property.PropertyType)
                )
            ) {

                // only visit properties of type ISyntaxNode
                if(!typeof(ACloudFormationBuilderNode).IsAssignableFrom(property.PropertyType)) {
                    continue;
                }

                // skip null values
                var value = (ACloudFormationBuilderNode?)property.GetValue(this);
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
        public T? Adopt<T>(T? node) where T : ACloudFormationBuilderNode => (T?)SetParent(node, this);

        protected virtual void ParentChanged() { }
    }
}