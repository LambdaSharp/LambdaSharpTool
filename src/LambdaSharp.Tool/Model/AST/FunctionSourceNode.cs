/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Collections.Generic;

namespace LambdaSharp.Tool.Model.AST {

    public class FunctionSourceNode {

        //--- Class Fields ---
        public static readonly Dictionary<string, IEnumerable<string>> FieldCombinations = new Dictionary<string, IEnumerable<string>> {
            ["Api"] = new[] {
                "Integration",
                "OperationName",
                "ApiKeyRequired"
            },
            ["Schedule"] = new[] {
                "Name"
            },
            ["S3"] = new[] {
                "Events",
                "Prefix",
                "Suffix"
            },
            ["SlackCommand"] = new string[0],
            ["Topic"] = new[] {
                "Filters"
            },
            ["Sqs"] = new[] {
                "BatchSize"
            },
            ["Alexa"] = new string[0],
            ["DynamoDB"] = new[] {
                "BatchSize",
                "StartingPosition"
            },
            ["Kinesis"] = new[] {
                "BatchSize",
                "StartingPosition"
            }
        };

        //--- Properties ---

        // API Gateway Source
        public string Api { get; set; }
        public string Integration { get; set; }
        public string OperationName { get; set; }
        public bool? ApiKeyRequired { get; set; }

        // CloudWatch Schedule Event Source
        public object Schedule { get; set; }
        public string Name { get; set; }

        // S3 Bucket Source
        public object S3 { get; set; }
        public IList<string> Events { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }

        // Slack Command Source
        public string SlackCommand { get; set; }

        // SNS Topic Source
        public object Topic { get; set; }
        public IDictionary<string, object> Filters { get; set; }

        // SQS Source
        public object Sqs { get; set; }
        public object BatchSize { get; set; }

        // Alexa Source
        public object Alexa { get; set; }

        // DynamoDB Source
        public object DynamoDB { get; set; }
        // object BatchSize { get; set; }
        public object StartingPosition { get; set; }

        // Kinesis Source
        public object Kinesis { get; set; }
        // object BatchSize { get; set; }
        // object StartingPosition { get; set; }
   }
}