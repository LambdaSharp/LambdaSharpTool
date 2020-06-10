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

    public sealed class CidrFunctionExpression : AFunctionExpression {

        // !Cidr [ VALUE, VALUE, VALUE ]
        // NOTE: You can use the following functions in a Fn::Cidr function:
        //  - !Select
        //  - !Ref

        //--- Fields ---
        private AExpression? _ipBlock;
        private AExpression? _count;
        private AExpression? _cidrBits;

        //--- Properties ---
        [SyntaxHidden]
        public AExpression IpBlock {
            get => _ipBlock ?? throw new InvalidOperationException();
            set => _ipBlock = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public AExpression Count {
            get => _count ?? throw new InvalidOperationException();
            set => _count = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public AExpression CidrBits {
            get => _cidrBits ?? throw new InvalidOperationException();
            set => _cidrBits = SetParent(value ?? throw new ArgumentNullException());
        }

        //--- Methods ---
        public override ASyntaxNode CloneNode() => new CidrFunctionExpression {
            IpBlock = IpBlock.Clone(),
            Count = Count.Clone(),
            CidrBits = CidrBits.Clone()
        };
    }
}