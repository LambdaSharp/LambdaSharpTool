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

namespace LambdaSharp.Tool.Parser.Syntax {

    public abstract class AValueExpression : ASyntaxNode { }

    public class ObjectExpression : AValueExpression {

        //--- Properties ---
        public List<LiteralExpression> Keys { get; set; } = new List<LiteralExpression>();
        public Dictionary<string, AValueExpression> Values { get; set; } = new Dictionary<string, AValueExpression>();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);

            // TODO: introduce key-value structure so they can be visited together
            Keys?.Visit(this, visitor);
            Values?.Values.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ListExpression : AValueExpression {

        //--- Properties ---
        public List<AValueExpression> Values { get; set; } = new List<AValueExpression>();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Values?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class LiteralExpression : AValueExpression {

        //--- Properties ---
        public string Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            visitor.VisitEnd(parent, this);
        }
    }
}