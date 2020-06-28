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
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public sealed class FindInMapFunctionExpression : AFunctionExpression {

        // !FindInMap [ STRING, VALUE, VALUE ]
        // NOTE: You can use the following functions in a Fn::FindInMap function:
        //  - Fn::FindInMap
        //  - Ref

        //--- Fields ---
        private LiteralExpression? _mapName;
        private AExpression? _topLevelKey;
        private AExpression? _secondLevelKey;
        private MappingDeclaration? _referencedDeclaration;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        [SyntaxHidden]
        public LiteralExpression MapName {
            get => _mapName ?? throw new InvalidOperationException();
            set => _mapName = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public AExpression TopLevelKey {
            get => _topLevelKey ?? throw new InvalidOperationException();
            set => _topLevelKey = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxHidden]
        public AExpression SecondLevelKey {
            get => _secondLevelKey ?? throw new InvalidOperationException();
            set => _secondLevelKey = Adopt(value ?? throw new ArgumentNullException());
        }

        // TODO: remove ReferencedDeclaration property
        public MappingDeclaration? ReferencedDeclaration {
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
        public override ASyntaxNode CloneNode() => new FindInMapFunctionExpression {
            MapName = MapName.Clone(),
            TopLevelKey = TopLevelKey.Clone(),
            SecondLevelKey = SecondLevelKey.Clone(),
            ReferencedDeclaration = ReferencedDeclaration
        };
    }
}