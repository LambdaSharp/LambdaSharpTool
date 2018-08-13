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

namespace MindTouch.LambdaSharp.Tool.Model {

    public abstract class AFunctionSource { }

    public class TopicSource : AFunctionSource {

       //--- Properties ---
        public string TopicName { get; set; }
    }

    public class ScheduleSource : AFunctionSource {

       //--- Properties ---
        public string Expression { get; set; }
        public string Name { get; set; }
    }

    public enum ApiGatewaySourceIntegration {
        Unsupported,
        RequestResponse,
        SlackCommand
    }

    public class ApiGatewaySource : AFunctionSource {

       //--- Properties ---
        public string Method { get; set; }
        public string[] Path { get; set; }
        public ApiGatewaySourceIntegration Integration { get; set; }
    }

    public class S3Source : AFunctionSource {

       //--- Properties ---
        public string Bucket { get; set; }
        public string BucketArn { get; set; }
        public IList<string> Events { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
    }

    public class SqsSource : AFunctionSource {

       //--- Properties ---
        public string Queue { get; set; }
        public int BatchSize { get; set; }
    }

    public class AlexaSource : AFunctionSource {

        //--- Properties ---
        public string EventSourceToken { get; set; }
    }
}