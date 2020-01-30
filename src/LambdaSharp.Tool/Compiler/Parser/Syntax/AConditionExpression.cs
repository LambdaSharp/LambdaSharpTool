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

#nullable enable

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AConditionExpression : AExpression { }

    public class ConditionExpression : AConditionExpression {

        // !Condition STRING

        //--- Fields ---
        private LiteralExpression? _referenceName;
        private ConditionDeclaration? _referencedDeclaration;

        //--- Properties ---
        public LiteralExpression? ReferenceName {
            get => _referenceName;
            set => _referenceName = SetParent(value);
        }

        public ConditionDeclaration? ReferencedDeclaration {
            get => _referencedDeclaration;
            set => _referencedDeclaration = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class EqualsConditionExpression : AConditionExpression {

        // !Equals [ EXPR, EXPR ]
        // NOTE: You can use the following functions in a Fn::Equals function:
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

        //--- Fields ---
        private AExpression? _leftValue;
        private AExpression? _rightValue;

        //--- Properties ---
        public AExpression? LeftValue {
            get => _leftValue;
            set => _leftValue = SetParent(value);
        }

        public AExpression? RightValue {
            get => _rightValue;
            set => _rightValue = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue?.Visit(this, visitor);
            RightValue?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class NotConditionExpression : AConditionExpression {

        // parse !Not [ EXPR ]
        // NOTE: You can use the following functions in a Fn::Not function:
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

        //--- Fields ---
        private AExpression? _value;

        //--- Properties ---
        public AExpression? Value {
            get => _value;
            set => _value = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Value?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class AndConditionExpression : AConditionExpression {

        // !And [ EXPR, EXPR ]
        // NOTE: You can use the following functions in a Fn::And function:
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

        //--- Fields ---
        private AExpression? _leftValue;
        private AExpression? _rightValue;

        //--- Properties ---
        public AExpression? LeftValue {
            get => _leftValue;
            set => _leftValue = SetParent(value);
        }

        public AExpression? RightValue {
            get => _rightValue;
            set => _rightValue = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue?.Visit(this, visitor);
            RightValue?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class OrConditionExpression : AConditionExpression {

        // !Or [ EXPR, EXPR ]
        // NOTE: You can use the following functions in a Fn::Or function:
        //  - Fn::FindInMap
        //  - Ref
        //  - Condition
        //  - Other condition functions

        //--- Fields ---
        private AExpression? _leftValue;
        private AExpression? _rightValue;

        //--- Properties ---
        public AExpression? LeftValue {
            get => _leftValue;
            set => _leftValue = SetParent(value);
        }

        public AExpression? RightValue {
            get => _rightValue;
            set => _rightValue = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue?.Visit(this, visitor);
            RightValue?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}