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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LambdaSharp.CloudFormation.Builder.Expressions {

    public class CloudFormationBuilderList : ACloudFormationBuilderExpression, IEnumerable, IEnumerable<ACloudFormationBuilderExpression> {

        //--- Fields ---
        private readonly List<ACloudFormationBuilderExpression> _items;

        //--- Constructors ---
        public CloudFormationBuilderList( )
            => _items = new List<ACloudFormationBuilderExpression>();

        public CloudFormationBuilderList(IEnumerable<ACloudFormationBuilderExpression> items)
            => _items = items.Select(item => Adopt(item)).ToList();

        //--- Properties ---
        public override CloudFormationBuilderValueType ExpressionValueType => CloudFormationBuilderValueType.List;
        public int Count => _items.Count;

        //--- Operators ---
        public ACloudFormationBuilderExpression this[int index] {
            get => _items[index];
            set {
                if(!object.ReferenceEquals(_items[index], value)) {
                    Orphan(_items[index]);
                    _items[index]  = Adopt(value ?? throw new ArgumentNullException(nameof(value)));
                }
            }
        }

        //--- Methods ---
        public void Add(ACloudFormationBuilderExpression expression) => _items.Add(Adopt(expression));

        public override void Inspect(Action<ACloudFormationBuilderNode>? entryInspector, Action<ACloudFormationBuilderNode>? exitInspector) {
            entryInspector?.Invoke(this);
            foreach(var item in _items) {
                item.Inspect(entryInspector, exitInspector);
            }
            exitInspector?.Invoke(this);
        }

        public override ACloudFormationBuilderNode Substitute(Func<ACloudFormationBuilderNode, ACloudFormationBuilderNode> inspector) {
            for(var i = 0; i < Count; ++i) {
                var value = this[i];
                var newValue = value.Substitute(inspector) ?? throw new NullValueException();
                if(!object.ReferenceEquals(value, newValue)) {
                    this[i] = (ACloudFormationBuilderExpression)newValue;
                }
            }
            return inspector(this);
        }

        public override ACloudFormationBuilderNode CloneNode() => new CloudFormationBuilderList(_items.Select(item => item.Clone())) {
            SourceLocation = SourceLocation
        };

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<ACloudFormationBuilderExpression> Members ---
        IEnumerator<ACloudFormationBuilderExpression> IEnumerable<ACloudFormationBuilderExpression>.GetEnumerator() => _items.GetEnumerator();
    }
}