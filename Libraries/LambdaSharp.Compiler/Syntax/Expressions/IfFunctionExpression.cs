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
using LambdaSharp.Compiler.Exceptions;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class IfFunctionExpression : AFunctionExpression {

        // !If [ CONDITION, VALUE, VALUE ]
        // NOTE: AWS CloudFormation supports the Fn::If intrinsic function in the metadata attribute, update policy attribute, and property values in the Resources section and Outputs sections of a template.
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::Join
        //  - Fn::Select
        //  - Fn::Sub
        //  - Ref

        //--- Fields ---
        private AExpression? _condition;
        private AExpression? _ifTrue;
        private AExpression? _ifFalse;

        //--- Properties ---
        [SyntaxHidden]
        public AExpression Condition {
            get => _condition ?? throw new InvalidOperationException();
            set => _condition = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public AExpression IfTrue {
            get => _ifTrue ?? throw new InvalidOperationException();
            set => _ifTrue = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public AExpression IfFalse {
            get => _ifFalse ?? throw new InvalidOperationException();
            set => _ifFalse = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new IfFunctionExpression {
            Condition = Condition.Clone(),
            IfTrue = IfTrue.Clone(),
            IfFalse = IfFalse.Clone()
        };
    }
}