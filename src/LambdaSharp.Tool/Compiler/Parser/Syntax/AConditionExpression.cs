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

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public class ConditionExpression : AExpression {

        // TODO: parse !Condition STRING

        //--- Properties ---
        public LiteralExpression ReferenceName { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class EqualsConditionExpression : AExpression {

        // TODO: parse !Equals [ EXPR, EXPR ]
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

         //--- Properties ---
         public AExpression LeftValue { get; set; }
         public AExpression RightValue { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue?.Visit(this, visitor);
            RightValue?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class NotConditionExpression : AExpression {

        // TODO: parse !Not [ EXPR ]
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

         //--- Properties ---
         public AExpression Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Value?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class AndConditionExpression : AExpression {

        // TODO: parse !And [ EXPR, EXPR ]
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

         //--- Properties ---
         public AExpression LeftValue { get; set; }
         public AExpression RightValue { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue?.Visit(this, visitor);
            RightValue?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class OrConditionExpression : AExpression {

        // TODO: parse !Or [ EXPR, EXPR ]
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

         //--- Properties ---
         public AExpression LeftValue { get; set; }
         public AExpression RightValue { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue?.Visit(this, visitor);
            RightValue?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}