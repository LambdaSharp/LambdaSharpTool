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

    [SyntaxDeclarationKeyword("Kinesis", typeof(AExpression))]
    public sealed class KinesisEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private AExpression? _batchSize;
        private AExpression? _startingPosition;
        private AExpression? _maximumBatchingWindowInSeconds;

        //--- Constructors ---
        public KinesisEventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource ?? throw new ArgumentNullException(nameof(eventSource)));

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? BatchSize {
            get => _batchSize;
            set => _batchSize = SetParent(value);
        }

        [SyntaxOptional]
        public AExpression? StartingPosition {
            get => _startingPosition;
            set => _startingPosition = SetParent(value);
        }

        [SyntaxOptional]
        public AExpression? MaximumBatchingWindowInSeconds {
            get => _maximumBatchingWindowInSeconds;
            set => _maximumBatchingWindowInSeconds = SetParent(value);
        }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            BatchSize = BatchSize?.Visit(visitor);
            StartingPosition = StartingPosition?.Visit(visitor);
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            EventSource.InspectNode(inspector);
            BatchSize?.InspectNode(inspector);
            StartingPosition?.InspectNode(inspector);
        }
    }
}