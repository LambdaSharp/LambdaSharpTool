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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Import")]
    public sealed class ImportDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private LiteralExpression? _type;
        private SyntaxNodeCollection<LiteralExpression> _scope;
        private SyntaxNodeCollection<LiteralExpression>? _allow;
        private LiteralExpression? _module;
        private ObjectExpression? _encryptionContext;

        //--- Constructors ---
        public ImportDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = SetParent(new SyntaxNodeCollection<LiteralExpression>());
            DiscardIfNotReachable = true;
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Type {
            get => _type;
            set => _type = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope;
            set => _scope = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? Allow {
            get => _allow;
            set => _allow = SetParent(value);
        }

        [SyntaxRequired]
        public LiteralExpression? Module {
            get => _module;
            set => _module = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext {
            get => _encryptionContext;
            set => _encryptionContext = SetParent(value);
        }

        public bool HasSecretType => Type!.Value == "Secret";

        //--- Methods ---
        public void GetModuleAndExportName(
            out string moduleReference,
            out string exportName
        ) {
            if(Module == null) {
                throw new InvalidOperationException();
            }
            var split = Module.Value.Split("::", 2);
            if(split.Length == 2) {
                moduleReference = split[0];
                exportName = split[1];
            } else {

                // assume the item name matches the export name
                moduleReference = split[0];
                exportName = ItemName.Value;
            }
        }
    }
}