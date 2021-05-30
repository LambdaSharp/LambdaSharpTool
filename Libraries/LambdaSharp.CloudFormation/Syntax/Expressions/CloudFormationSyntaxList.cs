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
using System.Runtime.CompilerServices;

namespace LambdaSharp.CloudFormation.Syntax.Expressions {

    public class CloudFormationSyntaxList<TExpression> : ACloudFormationSyntaxExpression, IEnumerable, IEnumerable<TExpression>
        where TExpression : ACloudFormationSyntaxNode
    {

        //--- Fields ---
        private readonly List<TExpression> _items;

        //--- Constructors ---
        public CloudFormationSyntaxList([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
            => _items = new List<TExpression>();

        public CloudFormationSyntaxList(IEnumerable<TExpression> items, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
            => _items = items.Select(item => Adopt(item)).ToList();

        //--- Properties ---
        public override CloudFormationSyntaxValueType ExpressionValueType => CloudFormationSyntaxValueType.List;
        public int Count => _items.Count;

        //--- Operators ---
        public TExpression this[int index] {
            get => _items[index];
            set {
                if(!object.ReferenceEquals(_items[index], value)) {
                    Orphan(_items[index]);
                    _items[index]  = Adopt(value ?? throw new ArgumentNullException(nameof(value)));
                }
            }
        }

        //--- Methods ---
        public void Add(TExpression expression) => _items.Add(Adopt(expression));

        public override void Inspect(Action<ACloudFormationSyntaxNode>? entryInspector, Action<ACloudFormationSyntaxNode>? exitInspector) {
            entryInspector?.Invoke(this);
            foreach(var item in _items) {
                item.Inspect(entryInspector, exitInspector);
            }
            exitInspector?.Invoke(this);
        }

        public override ACloudFormationSyntaxNode Substitute(Func<ACloudFormationSyntaxNode, ACloudFormationSyntaxNode> inspector) {
            for(var i = 0; i < Count; ++i) {
                var value = this[i];
                var newValue = value.Substitute(inspector) ?? throw new NullValueException();
                if(!object.ReferenceEquals(value, newValue)) {
                    this[i] = (TExpression)newValue;
                }
            }
            return inspector(this);
        }

        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxList<TExpression>(this.Select(item => item.Clone())) {
            SourceLocation = SourceLocation
        };

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        //--- IEnumerable<TExpression> Members ---
        IEnumerator<TExpression> IEnumerable<TExpression>.GetEnumerator() => _items.GetEnumerator();
    }

    public class CloudFormationSyntaxList : CloudFormationSyntaxList<ACloudFormationSyntaxExpression> {

        //--- Constructors ---
        public CloudFormationSyntaxList( ) { }
        public CloudFormationSyntaxList(IEnumerable<ACloudFormationSyntaxExpression> items) : base(items) { }

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxList(this.Select(item => item.Clone())) {
            SourceLocation = SourceLocation
        };
   }
}