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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LambdaSharp.App.Logging.Models {

    internal sealed class PutEventsRequest {

        //--- Properties ---
        public List<PutEventsRequestEntry> Entries { get; set; } = new List<PutEventsRequestEntry>();
    }

    internal sealed class PutEventsRequestEntry {

        //--- Properties ---
        public string Detail { get; set; }
        public string DetailType { get; set; }
        public List<string> Resources { get; set; }
        public string Source { get; set; }
    }

    internal sealed class PutEventsResponse {

        //--- Properties ---

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
}
