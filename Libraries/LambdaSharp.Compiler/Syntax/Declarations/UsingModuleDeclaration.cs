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

    [SyntaxDeclarationKeyword("Module")]
    public class UsingModuleDeclaration : ADeclaration {

        //--- Fields ---
        private LiteralExpression? _description;

        //--- Constructors ---
        public UsingModuleDeclaration(LiteralExpression moduleName)
            => ModuleName = SetParent(moduleName ?? throw new ArgumentNullException(nameof(moduleName)));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Description {
            get => _description;
            set => _description = SetParent(value);
        }

        public LiteralExpression ModuleName { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(ModuleName, ModuleName.Visit(visitor));
            Description = Description?.Visit(visitor);
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            ModuleName.InspectNode(inspector);
            Description?.InspectNode(inspector);
        }
    }
}