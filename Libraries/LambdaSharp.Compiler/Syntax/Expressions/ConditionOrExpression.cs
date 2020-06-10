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

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class ConditionOrExpression : AConditionExpression {

        // !Or [ EXPR, EXPR ]
        // NOTE: You can use the following functions in a Fn::Or function:
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

        //--- Fields ---
        private AExpression? _leftValue;
        private AExpression? _rightValue;

        //--- Properties ---
        public AExpression LeftValue {
            get => _leftValue ?? throw new InvalidOperationException();
            set => _leftValue = SetParent(value ?? throw new ArgumentNullException());
        }

        public AExpression RightValue {
            get => _rightValue ?? throw new InvalidOperationException();
            set => _rightValue = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new ConditionOrExpression {
            LeftValue = LeftValue.Clone(),
            RightValue = RightValue.Clone()
        };
    }
}