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
using LambdaSharp.Compiler.Exceptions;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class Base64FunctionExpression : AFunctionExpression {

        //--- Fields ---
        private AExpression? _value;

        // !Base64 VALUE
        // NOTE: You can use any function that returns a string inside the Fn::Base64 function.

        //--- Properties ---
        public AExpression Value {
            get => _value ?? throw new InvalidOperationException();
            set => _value = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            Value = Value?.Visit(visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            Value.InspectNode(inspector);
        }

        public override ASyntaxNode CloneNode() => new Base64FunctionExpression {
            Value = Value.Clone()
        };
    }
}