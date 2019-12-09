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

using System.Collections.Generic;
using System.Linq;

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class ADeclaration : ASyntaxNode { }

    public class ModuleDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword()]
        public LiteralExpression Module { get; set; }

        [SyntaxOptional]
        public LiteralExpression Version { get; set; }

        [SyntaxOptional]
        public LiteralExpression Description { get; set; }

        [SyntaxOptional]
        public ListExpression Pragmas { get; set; } = new ListExpression();

        [SyntaxOptional]
        public List<LiteralExpression> Secrets { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public List<UsingDeclaration> Using { get; set; } = new List<UsingDeclaration>();

        [SyntaxRequired]
        public List<AItemDeclaration> Items { get; set; } = new List<AItemDeclaration>();

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Module?.Visit(this, visitor);
            Version?.Visit(this, visitor);
            Description?.Visit(this, visitor);
            Secrets?.Visit(this, visitor);
            Using?.Visit(this, visitor);
            Items?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class UsingDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Module { get; set; }

        [SyntaxOptional]
        public LiteralExpression Description { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Module?.Visit(this, visitor);
            Description?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}