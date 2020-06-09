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

    public sealed class SelectFunctionExpression : AFunctionExpression {

        // !Select [ VALUE, VALUE ]
        // NOTE: For the Fn::Select index value, you can use the Ref and Fn::FindInMap functions.
        //  For the Fn::Select list of objects, you can use the following functions:
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::Split
        //  - Ref

        //--- Fields ---
        private AExpression? _index;
        private AExpression? _values;

        //--- Properties ---
        public AExpression Index {
            get => _index ?? throw new InvalidOperationException();
            set => _index = SetParent(value ?? throw new ArgumentNullException());
        }

        // TODO: use [DisallowNull] or make non-null?
        public AExpression Values {
            get => _values ?? throw new InvalidOperationException();
            set => _values = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new SelectFunctionExpression {
            Index = Index.Clone(),
            Values = Values.Clone()
        };
    }
}