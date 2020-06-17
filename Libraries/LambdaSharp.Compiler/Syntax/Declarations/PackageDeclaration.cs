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
using System.Collections.Generic;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Package")]
    public sealed class PackageDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private SyntaxNodeCollection<LiteralExpression> _scope;
        private LiteralExpression? _files;

        //--- Constructors ---
        public PackageDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = Adopt(new SyntaxNodeCollection<LiteralExpression>());
        }

        //--- Properties --

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope;
            set => _scope = Adopt(value ?? throw new ArgumentNullException());
        }

        // TODO: shouldn't this be List<LiteralExpression>?
        [SyntaxRequired]
        public LiteralExpression? Files {
            get => _files;
            set => _files = Adopt(value);
        }

        public List<KeyValuePair<string, string>> ResolvedFiles { get; set; } = new List<KeyValuePair<string, string>>();
        public bool HasSecretType => false;
        public LiteralExpression? Type => Fn.Literal("String");
    }
}