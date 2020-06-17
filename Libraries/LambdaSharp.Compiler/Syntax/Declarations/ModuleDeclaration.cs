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
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Module")]
    public class ModuleDeclaration : ADeclaration {

        //--- Types ---
        public class CloudFormationSpecExpression : ASyntaxNode {

            //--- Fields ---
            private LiteralExpression? _version;
            private LiteralExpression? _region;

            //--- Properties ---

            [SyntaxRequired]
            public LiteralExpression? Version {
                get => _version;
                set => _version = Adopt(value);
            }

            [SyntaxRequired]
            public LiteralExpression? Region {
                get => _region;
                set => _region = Adopt(value);
            }
        }

        //--- Fields ---
        private LiteralExpression _version;
        private LiteralExpression? _description;
        private ListExpression _pragmas;
        private SyntaxNodeCollection<LiteralExpression> _secrets;
        private SyntaxNodeCollection<UsingModuleDeclaration> _using;
        private SyntaxNodeCollection<AItemDeclaration> _items;

        // TODO: capture in release notes that modules can now require a minimum CloudFormation specification version
        private CloudFormationSpecExpression? _cloudformation;

        //--- Constructors ---
        public ModuleDeclaration(LiteralExpression moduleName) {
            ModuleName = Adopt(moduleName ?? throw new ArgumentNullException(nameof(moduleName)));
            _version = Adopt(Fn.Literal("1.0-DEV"));
            _pragmas = Adopt(new ListExpression());
            _secrets = Adopt(new SyntaxNodeCollection<LiteralExpression>());
            _using = Adopt(new SyntaxNodeCollection<UsingModuleDeclaration>());
            _items = Adopt(new SyntaxNodeCollection<AItemDeclaration>());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression Version {
            get => _version;
            set => _version = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public LiteralExpression? Description {
            get => _description;
            set => _description = Adopt(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Secrets {
            get => _secrets;
            set => _secrets = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<UsingModuleDeclaration> Using {
            get => _using;
            set => _using = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxRequired]
        public SyntaxNodeCollection<AItemDeclaration> Items {
            get => _items;
            set => _items = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public CloudFormationSpecExpression? CloudFormation {
            get => _cloudformation;
            set => _cloudformation = Adopt(value);
        }

        public LiteralExpression ModuleName { get; }
        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasLambdaSharpDependencies => !HasPragma("no-lambdasharp-dependencies");
        public bool HasModuleRegistration => !HasPragma("no-module-registration");
        public bool HasSamTransform => HasPragma("sam-transform");
    }
}