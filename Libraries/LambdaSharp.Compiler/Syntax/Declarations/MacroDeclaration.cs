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

    [SyntaxDeclarationKeyword("Macro")]
    public sealed class MacroDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Fields ---
        private AExpression? _handler;
        private ObjectExpression _properties;

        //--- Constructors ---
        public MacroDeclaration(LiteralExpression keywordValue) : base(keywordValue) {
            _properties = Adopt(new ObjectExpression());
        }

        //--- Properties ---

        [SyntaxRequired]
        public AExpression? Handler {
            get => _handler;
            set => _handler = Adopt(value);
        }

        //--- IResourceDeclaration Members ---
        LiteralExpression? IResourceDeclaration.ResourceTypeName => Fn.Literal("AWS::CloudFormation::Macro");
        bool IResourceDeclaration.HasInitialization => true;
        bool IResourceDeclaration.HasPropertiesValidation => false;
        ObjectExpression IResourceDeclaration.Properties => throw new InvalidOperationException();
        AExpression? IResourceDeclaration.Condition => null;
        AExpression IResourceDeclaration.ResourceReference => Fn.Ref(FullName);
    }
}