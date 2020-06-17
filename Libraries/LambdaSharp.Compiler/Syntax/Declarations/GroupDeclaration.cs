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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Group")]
    public sealed class GroupDeclaration : AItemDeclaration {

        //--- Fields ---
        private SyntaxNodeCollection<AItemDeclaration> _items;

        //--- Constructors ---
        public GroupDeclaration(LiteralExpression itemName) : base(itemName)
            => _items = Adopt(new SyntaxNodeCollection<AItemDeclaration>());

        //--- Properties ---

        [SyntaxRequired]
        public SyntaxNodeCollection<AItemDeclaration> Items {
            get => _items ?? throw new InvalidOperationException();
            set => _items = Adopt(value ?? throw new ArgumentNullException());
        }
    }
}