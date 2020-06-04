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

namespace LambdaSharp.Compiler.Syntax {

    public abstract class AEventSourceDeclaration : ADeclaration { }

    [SyntaxDeclarationKeyword("Api")]
    public class ApiEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _integration;
        private LiteralExpression? _operationName;
        private LiteralExpression? _apiKeyRequired;
        private LiteralExpression? _authorizationType;
        private SyntaxNodeCollection<LiteralExpression>? _authorizationScopes;
        private AExpression? _authorizerId;
        private LiteralExpression? _invoke;

        //--- Types ---
        public enum IntegrationType {
            Unsupported,
            RequestResponse,
            SlackCommand
        }

        //--- Constructors ---
        public ApiEventSourceDeclaration(LiteralExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Integration {
            get => _integration;
            set => _integration = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? OperationName {
            get => _operationName;
            set => _operationName = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? ApiKeyRequired {
            get => _apiKeyRequired;
            set => _apiKeyRequired = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? AuthorizationType {
            get => _authorizationType;
            set => _authorizationType = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? AuthorizationScopes {
            get => _authorizationScopes;
            set => _authorizationScopes = SetParent(value);
        }

        [SyntaxOptional]
        public AExpression? AuthorizerId {
            get => _authorizerId;
            set => _authorizerId = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Invoke {
            get => _invoke;
            set => _invoke = SetParent(value);
        }

        public LiteralExpression EventSource { get; }
        public string? ApiMethod { get; set; }
        public string[]? ApiPath { get; set; }
        public IntegrationType ApiIntegrationType { get; set; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            Integration = Integration?.Visit(visitor);
            OperationName = OperationName?.Visit(visitor);
            ApiKeyRequired = ApiKeyRequired?.Visit(visitor);
            AuthorizationType = AuthorizationType?.Visit(visitor);
            AuthorizationScopes = AuthorizationScopes?.Visit(visitor);
            AuthorizerId = AuthorizerId?.Visit(visitor);
            Invoke = Invoke?.Visit(visitor);
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("Schedule", typeof(AExpression))]
    public class SchedulEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _name;

        //--- Constructors ---
        public SchedulEventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Name {
            get => _name;
            set => _name = SetParent(value);
        }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            Name = Name?.Visit(visitor);
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("S3", typeof(AExpression))]
    public class S3EventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private SyntaxNodeCollection<LiteralExpression>? _events;
        private LiteralExpression? _prefix;
        private LiteralExpression? _suffix;

        //--- Constructors ---
        public S3EventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? Events {
            get => _events;
            set => _events = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Prefix {
            get => _prefix;
            set => _prefix = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Suffix {
            get => _suffix;
            set => _suffix = SetParent(value);
        }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            Events = Events?.Visit(visitor);
            Prefix = Prefix?.Visit(visitor);
            Suffix = Suffix?.Visit(visitor);
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("SlackCommand")]
    public class SlackCommandEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public SlackCommandEventSourceDeclaration(LiteralExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---
        public string[]? SlackPath { get; set; }
        public LiteralExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("Topic", typeof(AExpression))]
    public class TopicEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private ObjectExpression? _filters;

        //--- Constructors ---
        public TopicEventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public ObjectExpression? Filters {
            get => _filters;
            set => _filters = SetParent(value);
        }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            Filters = Filters?.Visit(visitor);
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("Sqs", typeof(AExpression))]
    public class SqsEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private AExpression? _batchSize;

        //--- Constructors ---
        public SqsEventSourceDeclaration(AExpression eventSource)
            => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? BatchSize {
            get => _batchSize;
            set => _batchSize = SetParent(value);
        }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            BatchSize = BatchSize?.Visit(visitor);
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("Alexa", typeof(AExpression))]
    public class AlexaEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public AlexaEventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        public AExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            return visitor.VisitEnd(this);
        }
    }

    [SyntaxDeclarationKeyword("DynamoDB", typeof(AExpression))]
    public class DynamoDBEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private AExpression? _batchSize;
        private AExpression? _startingPosition;
        private AExpression? _maximumBatchingWindowInSeconds;

        //--- Constructors ---
        public DynamoDBEventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

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
    }

    [SyntaxDeclarationKeyword("Kinesis", typeof(AExpression))]
    public class KinesisEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private AExpression? _batchSize;
        private AExpression? _startingPosition;
        private AExpression? _maximumBatchingWindowInSeconds;

        //--- Constructors ---
        public KinesisEventSourceDeclaration(AExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

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
    }

    [SyntaxDeclarationKeyword("WebSocket")]
    public class WebSocketEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _operationName;
        private LiteralExpression? _apiKeyRequired;
        private LiteralExpression? _authorizationType;
        private SyntaxNodeCollection<LiteralExpression>? _authorizationScopes;
        private AExpression? _authorizerId;
        private LiteralExpression? _invoke;

        //--- Constructors ---
        public WebSocketEventSourceDeclaration(LiteralExpression eventSource) => EventSource = SetParent(eventSource) ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? OperationName {
            get => _operationName;
            set => _operationName = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? ApiKeyRequired {
            get => _apiKeyRequired;
            set => _apiKeyRequired = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? AuthorizationType {
            get => _authorizationType;
            set => _authorizationType = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? AuthorizationScopes {
            get => _authorizationScopes;
            set => _authorizationScopes = SetParent(value);
        }

        [SyntaxOptional]
        public AExpression? AuthorizerId {
            get => _authorizerId;
            set => _authorizerId = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Invoke {
            get => _invoke;
            set => _invoke = SetParent(value);
        }

        public LiteralExpression EventSource { get; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(EventSource, EventSource.Visit(visitor));
            OperationName = OperationName?.Visit(visitor);
            ApiKeyRequired = ApiKeyRequired?.Visit(visitor);
            AuthorizationType = AuthorizationType?.Visit(visitor);
            AuthorizationScopes = AuthorizationScopes?.Visit(visitor);
            AuthorizerId = AuthorizerId?.Visit(visitor);
            Invoke = Invoke?.Visit(visitor);
            return visitor.VisitEnd(this);
        }
    }
}