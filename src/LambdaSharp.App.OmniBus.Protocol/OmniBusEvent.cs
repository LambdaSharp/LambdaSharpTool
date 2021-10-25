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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LambdaSharp.App.OmniBus.Protocol.Serialization;

namespace LambdaSharp.App.OmniBus.Protocol {

    public sealed class OmniBusEvent {

        //--- Properties ---

        [Required]
        public string Id { get; set; }

        [Required]
        public string Source { get; set; }

        [Required]
        [JsonConverter(typeof(JsonDateTimeOffsetConverter))]
        public DateTimeOffset Time { get; set; }

        [Required]
        public string AudienceScope { get; set; }

        [Required]
        public string ContentType { get; set; }

        [Required]
        public string Body { get; set; }
    }
}
