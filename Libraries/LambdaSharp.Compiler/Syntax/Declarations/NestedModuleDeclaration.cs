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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Nested")]
    public sealed class NestedModuleDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _module;
        private SyntaxNodeCollection<LiteralExpression> _dependsOn;
        private ObjectExpression? _parameters;
        private ListExpression _pragmas;

        //--- Constructors ---
        public NestedModuleDeclaration(LiteralExpression itemName) : base(itemName) {
            _dependsOn = Adopt(new SyntaxNodeCollection<LiteralExpression>());
            _pragmas = Adopt(new ListExpression());
        }

        //--- Properties ---

        [SyntaxRequired]
        public LiteralExpression? Module {
            get => _module;
            set => _module = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> DependsOn {
            get => _dependsOn;
            set => _dependsOn = Adopt(value);
        }

        [SyntaxOptional]
        public ObjectExpression? Parameters {
            get => _parameters;
            set => _parameters = Adopt(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = Adopt(value ?? throw new ArgumentNullException());
        }

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));

        //--- IResourceDeclaration Members ---
        LiteralExpression? IResourceDeclaration.ResourceTypeName => Fn.Literal("AWS::CloudFormation::Stack");
        bool IResourceDeclaration.HasInitialization => true;
        bool IResourceDeclaration.HasPropertiesValidation => false;
        ObjectExpression IResourceDeclaration.Properties => throw new InvalidOperationException();
        AExpression? IResourceDeclaration.Condition => null;
        LiteralExpression? IResourceDeclaration.DefaultAttribute => null;
    }
}