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

    public sealed class TransformFunctionExpression : AFunctionExpression {

        // !Transform { Name: STRING, Parameters: OBJECT }
        // NOTE: AWS CloudFormation passes any intrinsic function calls included in Fn::Transform to the specified macro as literal strings.

        //--- Fields ---
        private LiteralExpression? _macroName;
        private ObjectExpression? _parameters;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression MacroName {
            get => _macroName ?? throw new InvalidOperationException();
            set => _macroName = SetParent(value ?? throw new ArgumentNullException());
        }

        public ObjectExpression? Parameters {
            get => _parameters;
            set => _parameters = SetParent(value);
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new TransformFunctionExpression {
            MacroName = MacroName.Clone(),
            Parameters = Parameters?.Clone()
        };
    }
}