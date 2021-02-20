/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Variable")]
    public sealed class VariableDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private LiteralExpression? type;
        private SyntaxNodeCollection<LiteralExpression> _scope;
        private AExpression? _value;
        private ObjectExpression? _encryptionContext;

        //--- Constructors ---
        public VariableDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = Adopt(new SyntaxNodeCollection<LiteralExpression>());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Type {
            get => type;
            set => type = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope;
            set => _scope = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxRequired]
        public AExpression? Value {
            get => _value;
            set => _value = Adopt(value);
        }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext {
            get => _encryptionContext;
            set => _encryptionContext = Adopt(value);
        }

        public bool HasSecretType => Type!.Value == "Secret";
    }
}