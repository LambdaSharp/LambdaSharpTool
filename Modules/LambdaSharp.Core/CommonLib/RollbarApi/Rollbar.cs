/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

using System.Text.Json.Serialization;

namespace LambdaSharp.Core.RollbarApi {

    public class Rollbar {

        //--- Properties ---
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("data")]
        public Data? Data { get; set; }
    }

    public class Data {

        //--- Properties ---
        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        [JsonPropertyName("body")]
        public DataBody? Body { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("code_version")]
        public string? CodeVersion { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("framework")]
        public string? Framework { get; set; }

        [JsonPropertyName("custom")]
        public object? Custom { get; set; }

        [JsonPropertyName("fingerprint")]
        public string? Fingerprint { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class DataBody {

        //--- Properties ---
        [JsonPropertyName("trace_chain")]
        public Trace[]? TraceChain { get; set; }

        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    public class Message {

        //--- Properties ---
        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }

    public class Trace {

        //--- Properties ---
        [JsonPropertyName("frames")]
        public Frame[]? Frames { get; set; }

        [JsonPropertyName("exception")]
        public ExceptionClass? Exception { get; set; }
    }

    public class ExceptionClass {

        //--- Properties ---
        [JsonPropertyName("class")]
        public string? Class { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class Frame {

        //--- Properties ---
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("lineno")]
        public int? Lineno { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }
    }
}
