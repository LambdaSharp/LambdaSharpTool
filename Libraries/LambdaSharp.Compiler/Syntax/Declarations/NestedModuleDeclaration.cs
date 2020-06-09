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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Nested")]
    public sealed class NestedModuleDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _module;
        private SyntaxNodeCollection<LiteralExpression> _dependsOn;
        private ObjectExpression? _parameters;

        //--- Constructors ---
        public NestedModuleDeclaration(LiteralExpression itemName) : base(itemName) {
            _dependsOn = SetParent(new SyntaxNodeCollection<LiteralExpression>());
        }

        //--- Properties ---

        [SyntaxRequired]
        public LiteralExpression? Module {
            get => _module;
            set => _module = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> DependsOn {
            get => _dependsOn;
            set => _dependsOn = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? Parameters {
            get => _parameters;
            set => _parameters = SetParent(value);
        }

        public string CloudFormationType => "AWS::CloudFormation::Stack";
    }
}