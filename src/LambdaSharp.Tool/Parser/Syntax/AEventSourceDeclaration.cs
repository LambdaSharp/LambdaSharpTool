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
        public ListOf<LiteralExpression> AuthorizationScopes { get; set; }

        [SyntaxOptional]
        public AValueExpression AuthorizerId { get; set; }

        [SyntaxOptional]
        public LiteralExpression Invoke { get; set; }
    }

    public class SchedulEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Schedule { get; set; }

        [SyntaxOptional]
        public LiteralExpression Name { get; set; }
    }

    public class S3EventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression S3 { get; set; }

        [SyntaxOptional]
        public ListOf<LiteralExpression> Events { get; set; }

        [SyntaxOptional]
        public LiteralExpression Prefix { get; set; }

        [SyntaxOptional]
        public LiteralExpression Suffix { get; set; }
    }

    public class SlackCommandEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression SlackCommand { get; set; }
    }

    public class TopicEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Topic { get; set; }

        [SyntaxOptional]
        public ObjectExpression Filters { get; set; }
    }

    public class SqsEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Sqs { get; set; }

        [SyntaxOptional]
        public AValueExpression BatchSize { get; set; }
    }

    public class AlexaEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Alexa { get; set; }
    }

    public class DynamoDBEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression DynamoDB { get; set; }

        [SyntaxOptional]
        public AValueExpression BatchSize { get; set; }

        [SyntaxOptional]
        public AValueExpression StartingPosition { get; set; }
    }

    public class KinesisEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public AValueExpression Kinesis { get; set; }

        [SyntaxOptional]
        public AValueExpression BatchSize { get; set; }

        [SyntaxOptional]
        public AValueExpression StartingPosition { get; set; }
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
        public ListOf<LiteralExpression> AuthorizationScopes { get; set; }

        [SyntaxOptional]
        public AValueExpression AuthorizerId { get; set; }

        [SyntaxOptional]
        public LiteralExpression Invoke { get; set; }
    }
}