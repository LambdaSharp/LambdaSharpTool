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

using System;
using System.Text.Json.Serialization;
using LambdaSharp.App.Bus.Protocol.Serialization;

namespace LambdaSharp.App.Bus.Events.Payloads {

    /// <summary>
    /// The <see cref="EventPayload"/> class describes a LambdaSharp App Bus event.
    /// </summary>
    public sealed class EventPayload {

        //--- Constants ---
        public const string ACTION = "Events/Match";
        public const string MIME_TYPE = "application/vnd.lambdasharp.bus.event2106+json";

        //--- Properties ---
        public string Id { get; set; }
        public string Source { get; set; }

        [JsonConverter(typeof(JsonDateTimeOffsetConverter))]
        public DateTimeOffset Time { get; set; }

        public string ContentType { get; set; }
        public string Body { get; set; }
    }
}