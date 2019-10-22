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

    public abstract class AEventSourceDeclaration : ADeclaration {

        //--- Properties ---
    }
    public class ApiEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Api { get; set; }


        [SyntaxOptional]
        public LiteralExpression Integration { get; set; }

        [SyntaxOptional]
        public LiteralExpression OperationName { get; set; }

        [SyntaxOptional]
        public LiteralExpression ApiKeyRequired { get; set; }

        [SyntaxOptional]
        public LiteralExpression AuthorizationType { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> AuthorizationScopes { get; set; }

        [SyntaxOptional]
        public AValueExpression AuthorizerId { get; set; }

        [SyntaxOptional]
        public LiteralExpression Invoke { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Api?.Visit(this, visitor);
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

    public class SchedulEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Schedule { get; set; }

        [SyntaxOptional]
        public LiteralExpression Name { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Schedule?.Visit(this, visitor);
            Name?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class S3EventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression S3 { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> Events { get; set; }

        [SyntaxOptional]
        public LiteralExpression Prefix { get; set; }

        [SyntaxOptional]
        public LiteralExpression Suffix { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            S3?.Visit(this, visitor);
            Events?.Visit(this, visitor);
            Prefix?.Visit(this, visitor);
            Suffix?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class SlackCommandEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression SlackCommand { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            SlackCommand?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class TopicEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Topic { get; set; }

        [SyntaxOptional]
        public ObjectExpression Filters { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Topic?.Visit(this, visitor);
            Filters?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class SqsEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Sqs { get; set; }

        [SyntaxOptional]
        public AValueExpression BatchSize { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Sqs?.Visit(this, visitor);
            BatchSize?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class AlexaEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Alexa { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Alexa?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class DynamoDBEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression DynamoDB { get; set; }

        [SyntaxOptional]
        public AValueExpression BatchSize { get; set; }

        [SyntaxOptional]
        public AValueExpression StartingPosition { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            DynamoDB?.Visit(this, visitor);
            BatchSize?.Visit(this, visitor);
            StartingPosition?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class KinesisEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Kinesis { get; set; }

        [SyntaxOptional]
        public AValueExpression BatchSize { get; set; }

        [SyntaxOptional]
        public AValueExpression StartingPosition { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Kinesis?.Visit(this, visitor);
            BatchSize?.Visit(this, visitor);
            StartingPosition?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class WebSocketEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression WebSocket { get; set; }

        [SyntaxOptional]
        public LiteralExpression OperationName { get; set; }

        [SyntaxOptional]
        public LiteralExpression ApiKeyRequired { get; set; }

        [SyntaxOptional]
        public LiteralExpression AuthorizationType { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> AuthorizationScopes { get; set; }

        [SyntaxOptional]
        public AValueExpression AuthorizerId { get; set; }

        [SyntaxOptional]
        public LiteralExpression Invoke { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            WebSocket?.Visit(this, visitor);
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