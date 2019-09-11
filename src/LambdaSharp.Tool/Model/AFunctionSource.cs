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

namespace LambdaSharp.Tool.Model {

    public abstract class AFunctionSource {

        //--- Abstract Methods ---
        public abstract void Visit(AModuleItem item, ModuleVisitorDelegate visitor);
    }

    public class TopicSource : AFunctionSource {

       //--- Properties ---
        public object TopicName { get; set; }
        public IDictionary<string, object> Filters { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            if(TopicName != null) {
                TopicName = visitor(item, TopicName);
            }
        }
    }

    public class ScheduleSource : AFunctionSource {

       //--- Properties ---
        public object Expression { get; set; }
        public string Name { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            Expression = visitor(item, Expression);
        }
    }

    public enum ApiGatewaySourceIntegration {
        Unsupported,
        RequestResponse,
        SlackCommand
    }

    public class RestApiSource : AFunctionSource {

       //--- Properties ---
        public string HttpMethod { get; set; }
        public string[] Path { get; set; }
        public ApiGatewaySourceIntegration Integration { get; set; }
        public string OperationName { get; set; }
        public bool? ApiKeyRequired { get; set; }
        public string Invoke { get; set; }
        public string RequestContentType { get; set; }
        public object RequestSchema { get; set; }
        public string RequestSchemaName { get; set; }
        public Dictionary<string, bool> QueryStringParameters { get; set; }
        public string ResponseContentType { get; set; }
        public object ResponseSchema { get; set; }
        public string ResponseSchemaName { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) { }
    }

    public class WebSocketSource : AFunctionSource {

        //--- Properties ---
        public string RouteKey { get; set; }
        public string OperationName { get; set; }
        public bool? ApiKeyRequired { get; set; }
        public string Invoke { get; set; }
        public string RequestContentType { get; set; }
        public object RequestSchema { get; set; }
        public string RequestSchemaName { get; set; }
        public string ResponseContentType { get; set; }
        public object ResponseSchema { get; set; }
        public string ResponseSchemaName { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) { }
    }

    public class S3Source : AFunctionSource {

       //--- Properties ---
        public object Bucket { get; set; }
        public IList<string> Events { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            if(Bucket != null) {
                Bucket = visitor(item, Bucket);
            }
        }
    }

    public class SqsSource : AFunctionSource {

       //--- Properties ---
        public object Queue { get; set; }
        public object BatchSize { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            if(Queue != null) {
                Queue = visitor(item, Queue);
            }
            if(BatchSize != null) {
                BatchSize = visitor(item, BatchSize);
            }
        }
    }

    public class AlexaSource : AFunctionSource {

        //--- Properties ---
        public object EventSourceToken { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            if(EventSourceToken != null) {
                EventSourceToken = visitor(item, EventSourceToken);
            }
        }
    }

    public class DynamoDBSource : AFunctionSource {

       //--- Properties ---
        public object DynamoDB { get; set; }
        public object BatchSize { get; set; }
        public object StartingPosition { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            if(DynamoDB != null) {
                DynamoDB = visitor(item, DynamoDB);
            }
            if(BatchSize != null) {
                BatchSize = visitor(item, BatchSize);
            }
            if(StartingPosition != null) {
                StartingPosition = visitor(item, StartingPosition);
            }
        }
    }

    public class KinesisSource : AFunctionSource {

       //--- Properties ---
        public object Kinesis { get; set; }
        public object BatchSize { get; set; }
        public object StartingPosition { get; set; }

        //--- Methods ---
        public override void Visit(AModuleItem item, ModuleVisitorDelegate visitor) {
            if(Kinesis != null) {
                Kinesis = visitor(item, Kinesis);
            }
            if(BatchSize != null) {
                BatchSize = visitor(item, BatchSize);
            }
            if(StartingPosition != null) {
                StartingPosition = visitor(item, StartingPosition);
            }
       }
    }
}