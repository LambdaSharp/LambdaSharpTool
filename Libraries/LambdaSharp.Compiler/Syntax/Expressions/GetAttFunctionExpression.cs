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
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class GetAttFunctionExpression : AFunctionExpression {

        // !GetAtt [ STRING, VALUE ]
        // NOTE: For the Fn::GetAtt logical resource name, you cannot use functions. You must specify a string that is a resource's logical ID.
        // For the Fn::GetAtt attribute name, you can use the Ref function.

        //--- Fields ---
        private LiteralExpression? _referenceName;
        private AExpression? _attributeName;
        private AItemDeclaration? _referencedDeclaration;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression ReferenceName {
            get => _referenceName ?? throw new InvalidOperationException();
            set => _referenceName = SetParent(value ?? throw new ArgumentNullException());
        }

        public AExpression AttributeName {
            get => _attributeName ?? throw new InvalidOperationException();
            set => _attributeName = SetParent(value ?? throw new ArgumentNullException());
        }

        public AItemDeclaration? ReferencedDeclaration {
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
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            ReferenceName = ReferenceName.Visit(visitor) ?? throw new NullValueException();
            AttributeName = AttributeName.Visit(visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            ReferenceName.InspectNode(inspector);
            AttributeName.InspectNode(inspector);
        }

        public override ASyntaxNode CloneNode() => new GetAttFunctionExpression {
            ReferenceName = ReferenceName.Clone(),
            AttributeName = AttributeName.Clone(),
            ReferencedDeclaration = ReferencedDeclaration
        };
    }
}