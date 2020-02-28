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

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AConditionExpression : AExpression { }

    public class ConditionExpression : AConditionExpression {

        // !Condition STRING

        //--- Fields ---
        private LiteralExpression? _referenceName;
        private ConditionDeclaration? _referencedDeclaration;

        //--- Properties ---
        public LiteralExpression ReferenceName {
            get => _referenceName ?? throw new InvalidOperationException();
            set => _referenceName = SetParent(value) ?? throw new ArgumentNullException();
        }

        public ConditionDeclaration? ReferencedDeclaration {
            get => _referencedDeclaration;
            set {
                if(_referencedDeclaration != null) {
                    _referencedDeclaration.UntrackDependency(this);
                }
                _referencedDeclaration = value;
                if(_referencedDeclaration != null) {
                    ParentItemDeclaration?.TrackDependency(_referencedDeclaration, this);
                }
            }
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName = ReferenceName.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new ConditionExpression {
            ReferenceName = ReferenceName.Clone(),
            ReferencedDeclaration = ReferencedDeclaration
        };
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
        public AExpression LeftValue {
            get => _leftValue ?? throw new InvalidOperationException();
            set => _leftValue = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression RightValue {
            get => _rightValue ?? throw new InvalidOperationException();
            set => _rightValue = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue = LeftValue.Visit(this, visitor) ?? throw new NullValueException();
            RightValue = RightValue.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new EqualsConditionExpression {
            LeftValue = LeftValue.Clone(),
            RightValue = RightValue.Clone()
        };
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
        public AExpression Value {
            get => _value ?? throw new InvalidOperationException();
            set => _value = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Value = Value.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new NotConditionExpression {
            Value = Value.Clone()
        };
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
        public AExpression LeftValue {
            get => _leftValue ?? throw new InvalidOperationException();
            set => _leftValue = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression RightValue {
            get => _rightValue ?? throw new InvalidOperationException();
            set => _rightValue = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue = LeftValue.Visit(this, visitor) ?? throw new NullValueException();
            RightValue = RightValue.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new AndConditionExpression {
            LeftValue = LeftValue.Clone(),
            RightValue = RightValue.Clone()
        };
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
        public AExpression LeftValue {
            get => _leftValue ?? throw new InvalidOperationException();
            set => _leftValue = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression RightValue {
            get => _rightValue ?? throw new InvalidOperationException();
            set => _rightValue = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            LeftValue = LeftValue.Visit(this, visitor) ?? throw new NullValueException();
            RightValue = RightValue.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new OrConditionExpression {
            LeftValue = LeftValue.Clone(),
            RightValue = RightValue.Clone()
        };
    }
}