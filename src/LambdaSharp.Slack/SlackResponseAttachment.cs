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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LambdaSharp.Slack {

    public class SlackResponseAttachment {

        //--- Fields ---
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text;

        [JsonProperty("pretext", NullValueHandling = NullValueHandling.Ignore)]
        public string Pretext;

        [JsonProperty("fallback", NullValueHandling = NullValueHandling.Ignore)]
        public string Fallback;

        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        public string Color;

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title;

        [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<SlackResponseAttachmentFields> Fields;

        [JsonProperty("footer", NullValueHandling = NullValueHandling.Ignore)]
        public string Footer;

        [JsonProperty("ts", NullValueHandling = NullValueHandling.Ignore)]
        public string Timestamp;

        [JsonProperty("mrkdwn_in", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> MarkdownIn;

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
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title;

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value;

        [JsonProperty("short", NullValueHandling = NullValueHandling.Ignore)]
        public bool Short;
    }
}
