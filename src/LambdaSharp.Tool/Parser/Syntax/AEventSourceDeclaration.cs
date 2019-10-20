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
        public override string Keyword => "Api";
        public StringLiteral Api { get; set; }
        public StringLiteral Integration { get; set; }
        public StringLiteral OperationName { get; set; }
        public BoolLiteral ApiKeyRequired { get; set; }
        public StringLiteral AuthorizationType { get; set; }
        public DeclarationList<StringLiteral> AuthorizationScopes { get; set; }
        public AValueExpression AuthorizerId { get; set; }
        public StringLiteral Invoke { get; set; }
    }

    public class SchedulEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "Schedule";
        public AValueExpression Schedule { get; set; }
        public StringLiteral Name { get; set; }
    }

    public class S3EventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "S3";
        public AValueExpression S3 { get; set; }
        public DeclarationList<StringLiteral> Events { get; set; }
        public StringLiteral Prefix { get; set; }
        public StringLiteral Suffix { get; set; }
    }

    public class SlackCommandEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "SlackCommand";
        public StringLiteral SlackCommand { get; set; }
    }

    public class TopicEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "Topic";
        public AValueExpression Topic { get; set; }
        public ObjectExpression Filters { get; set; }
    }

    public class SqsEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "Sqs";
        public AValueExpression Sqs { get; set; }
        public AValueExpression BatchSize { get; set; }
    }

    public class AlexaEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "Alexa";
        public AValueExpression Alexa { get; set; }
    }

    public class DynamoDBEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "DynamoDB";
        public AValueExpression DynamoDB { get; set; }
        public AValueExpression BatchSize { get; set; }
        public AValueExpression StartingPosition { get; set; }
    }

    public class KinesisEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "Kinesis";
        public AValueExpression Kinesis { get; set; }
        public AValueExpression BatchSize { get; set; }
        public AValueExpression StartingPosition { get; set; }
    }

    public class WebSocketEventSourceDeclaration : AEventSourceDeclaration {

        //--- Properties ---
        public override string Keyword => "WebSocket";
        public StringLiteral WebSocket { get; set; }
        public StringLiteral OperationName { get; set; }
        public BoolLiteral ApiKeyRequired { get; set; }
        public StringLiteral AuthorizationType { get; set; }
        public DeclarationList<StringLiteral> AuthorizationScopes { get; set; }
        public AValueExpression AuthorizerId { get; set; }
        public StringLiteral Invoke { get; set; }
    }
}