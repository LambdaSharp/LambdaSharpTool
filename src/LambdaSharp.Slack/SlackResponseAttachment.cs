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
using System.Text.Json.Serialization;

namespace LambdaSharp.Slack {

    public class SlackResponseAttachment {

        //--- Fields ---
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("pretext")]
        public string Pretext { get; set; }

        [JsonPropertyName("fallback")]
        public string Fallback { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("fields")]
        public IEnumerable<SlackResponseAttachmentFields> Fields { get; set; }

        [JsonPropertyName("footer")]
        public string Footer { get; set; }

        [JsonPropertyName("ts")]
        public string Timestamp { get; set; }

        [JsonPropertyName("mrkdwn_in")]
        public IEnumerable<string> MarkdownIn { get; set; }

        //--- Constructors ---
        public SlackResponseAttachment(string text) {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public SlackResponseAttachment(string text = null, string pretext = null, string fallback = null, string color = null, string title = null, IEnumerable<SlackResponseAttachmentFields> fields = null, string footer = null, string timestamp = null, IEnumerable<string> markdownIn = null) {
            Text = text;
            Pretext = pretext;
            Fallback = fallback;
            Color = color;
            Title = title;
            Fields = fields;
            Footer = footer;
            Timestamp = timestamp;
            MarkdownIn = markdownIn;
        }
    }

    public class SlackResponseAttachmentFields {

        //--- Fields ---
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("short")]
        public bool Short { get; set; }
    }
}
