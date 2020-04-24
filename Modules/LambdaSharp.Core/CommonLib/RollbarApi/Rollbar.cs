/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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

using Newtonsoft.Json;

namespace LambdaSharp.Core.RollbarApi {

    public class Rollbar {

        //--- Properties ---
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public class Data {

        //--- Properties ---
        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("body")]
        public DataBody Body { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("code_version")]
        public string CodeVersion { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("framework")]
        public string Framework { get; set; }

        [JsonProperty("custom")]
        public object Custom { get; set; }

        [JsonProperty("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class DataBody {

        //--- Properties ---
        [JsonProperty("trace_chain")]
        public Trace[] TraceChain { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }
    }

    public class Message {

        //--- Properties ---
        [JsonProperty("body")]
        public string Body { get; set; }
    }

    public class Trace {

        //--- Properties ---
        [JsonProperty("frames")]
        public Frame[] Frames { get; set; }

        [JsonProperty("exception")]
        public ExceptionClass Exception { get; set; }
    }

    public class ExceptionClass {

        //--- Properties ---
        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class Frame {

        //--- Properties ---
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("lineno")]
        public int? Lineno { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }
    }
}
