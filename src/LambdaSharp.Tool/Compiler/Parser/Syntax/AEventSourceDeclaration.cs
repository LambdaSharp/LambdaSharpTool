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

using System;
using System.Collections.Generic;

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AEventSourceDeclaration : ADeclaration { }

    [SyntaxDeclarationKeyword("Api")]
    public class ApiEventSourceDeclaration : AEventSourceDeclaration {

        //--- Types ---
        public enum IntegrationType {
            Unsupported,
            RequestResponse,
            SlackCommand
        }

        //--- Constructors ---
        public ApiEventSourceDeclaration(LiteralExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Integration { get; set; }

        [SyntaxOptional]
        public LiteralExpression? OperationName { get; set; }

        [SyntaxOptional]
        public LiteralExpression? ApiKeyRequired { get; set; }

        [SyntaxOptional]
        public LiteralExpression? AuthorizationType { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression>? AuthorizationScopes { get; set; }

        [SyntaxOptional]
        public AExpression? AuthorizerId { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Invoke { get; set; }

        public LiteralExpression EventSource { get; }
        public string? ApiMethod { get; set; }
        public string[]? ApiPath { get; set; }
        public IntegrationType ApiIntegrationType { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            Integration?.Visit(this, visitor);
            OperationName?.Visit(this, visitor);
            ApiKeyRequired?.Visit(this, visitor);
            AuthorizationType?.Visit(this, visitor);
            AuthorizationScopes?.Visit(this, visitor);
            AuthorizerId?.Visit(this, visitor);
            Invoke?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Schedule", typeof(AExpression))]
    public class SchedulEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public SchedulEventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Name { get; set; }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            Name?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("S3", typeof(AExpression))]
    public class S3EventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public S3EventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public List<LiteralExpression>? Events { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Prefix { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Suffix { get; set; }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            Events?.Visit(this, visitor);
            Prefix?.Visit(this, visitor);
            Suffix?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("SlackCommand")]
    public class SlackCommandEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public SlackCommandEventSourceDeclaration(LiteralExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---
        public string[]? SlackPath { get; set; }
        public LiteralExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Topic", typeof(AExpression))]
    public class TopicEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public TopicEventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        // TODO (2020-01-30, bjorg): add validation for topic filters
        [SyntaxOptional]
        public ObjectExpression? Filters { get; set; }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            Filters?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Sqs", typeof(AExpression))]
    public class SqsEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        // TODO: constructor must be able to take `AExpression`!
        public SqsEventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? BatchSize { get; set; }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            BatchSize?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Alexa", typeof(AExpression))]
    public class AlexaEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public AlexaEventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("DynamoDB", typeof(AExpression))]
    public class DynamoDBEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public DynamoDBEventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? BatchSize { get; set; }

        [SyntaxOptional]
        public AExpression? StartingPosition { get; set; }

        [SyntaxOptional]
        public AExpression? MaximumBatchingWindowInSeconds { get; set; }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            BatchSize?.Visit(this, visitor);
            StartingPosition?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Kinesis", typeof(AExpression))]
    public class KinesisEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public KinesisEventSourceDeclaration(AExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? BatchSize { get; set; }

        [SyntaxOptional]
        public AExpression? StartingPosition { get; set; }

        [SyntaxOptional]
        public AExpression? MaximumBatchingWindowInSeconds { get; set; }

        public AExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            BatchSize?.Visit(this, visitor);
            StartingPosition?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("WebSocket")]
    public class WebSocketEventSourceDeclaration : AEventSourceDeclaration {

        //--- Constructors ---
        public WebSocketEventSourceDeclaration(LiteralExpression eventSource) => EventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? OperationName { get; set; }

        [SyntaxOptional]
        public LiteralExpression? ApiKeyRequired { get; set; }

        [SyntaxOptional]
        public LiteralExpression? AuthorizationType { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression>? AuthorizationScopes { get; set; }

        [SyntaxOptional]
        public AExpression? AuthorizerId { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Invoke { get; set; }

        public LiteralExpression EventSource { get; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            EventSource.Visit(this, visitor);
            OperationName?.Visit(this, visitor);
            ApiKeyRequired?.Visit(this, visitor);
            AuthorizationType?.Visit(this, visitor);
            AuthorizationScopes?.Visit(this, visitor);
            AuthorizerId?.Visit(this, visitor);
            Invoke?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}