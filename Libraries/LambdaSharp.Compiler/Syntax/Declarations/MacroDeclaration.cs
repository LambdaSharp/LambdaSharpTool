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

    [SyntaxDeclarationKeyword("Macro")]
    public sealed class MacroDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _handler;

        //--- Constructors ---
        public MacroDeclaration(LiteralExpression keywordValue) : base(keywordValue) { }

        //--- Properties ---

        [SyntaxRequired]
        public LiteralExpression? Handler {
            get => _handler;
            set => _handler = SetParent(value);
        }

        public string CloudFormationType => "AWS::CloudFormation::Macro";

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(ItemName, ItemName.Visit(visitor));
            Handler = Handler?.Visit(visitor);
            Declarations = Declarations.Visit(visitor);
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            ItemName.InspectNode(inspector);
            Handler?.InspectNode(inspector);
            Declarations.InspectNode(inspector);
        }
    }
}