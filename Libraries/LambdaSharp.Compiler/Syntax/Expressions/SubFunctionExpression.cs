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

    public sealed class SubFunctionExpression : AFunctionExpression {

        // !Sub VALUE
        // !Sub [ VALUE, OBJECT ]
        // NOTE: For the String parameter, you cannot use any functions. You must specify a string value.
        // For the VarName and VarValue parameters, you can use the following functions:
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::ImportValue
        //  - Fn::Join
        //  - Fn::Select
        //  - Ref

        //--- Fields ---
        private LiteralExpression? _formatString;
        private ObjectExpression _parameters;

        //--- Constructors ---
        public SubFunctionExpression() {
            _parameters = SetParent(new ObjectExpression());
        }

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        [SyntaxHidden]
        public LiteralExpression FormatString {
            get => _formatString ?? throw new InvalidOperationException();
            set => _formatString = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public ObjectExpression Parameters {
            get => _parameters ?? throw new InvalidOperationException();
            set => _parameters = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new SubFunctionExpression {
            FormatString = FormatString.Clone(),
            Parameters = Parameters.Clone()
        };
    }
}