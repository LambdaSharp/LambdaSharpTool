/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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


using System.Collections.Generic;

namespace MindTouch.LambdaSharp.Tool.Model.AST {

    public class FunctionSourceNode {

        //--- Properties ---

        // API Gateway Source
        public string Api { get; set; }
        public string Integration { get; set; }

        // CloudWatch Schedule Event Source
        public string Schedule { get; set; }
        public string Name { get; set; }

        // S3 Bucket Source
        public string S3 { get; set; }
        public IList<string> Events { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }

        // Slack Command Source
        public string SlackCommand { get; set; }

        // SNS Topic Source
        public string Topic { get; set; }

        // SQS Source
        public string Sqs { get; set; }
        public int? BatchSize { get; set; }

        // Alexa Source
        public string Alexa { get; set; }
   }
}