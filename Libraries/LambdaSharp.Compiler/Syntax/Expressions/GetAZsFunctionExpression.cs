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

    public sealed class GetAZsFunctionExpression : AFunctionExpression {

        // !GetAZs VALUE
        // NOTE: You can use the Ref function in the Fn::GetAZs function.

        //--- Fields ---
        private AExpression? _region;

        //--- Properties ---
        public AExpression Region {
            get => _region ?? throw new InvalidOperationException();
            set => _region = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new GetAZsFunctionExpression {
            Region = Region.Clone()
        };
    }
}