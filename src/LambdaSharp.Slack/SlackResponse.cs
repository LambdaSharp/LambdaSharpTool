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
using System.Text;
using Newtonsoft.Json;

namespace LambdaSharp.Slack {

    public class SlackResponse {

        //--- Types ---

        //--- Class Methods ---
        public static SlackResponse InChannel(string text, params SlackResponseAttachment[] attachments) {
            return new SlackResponse("in_channel", text, attachments);
        }

        public static SlackResponse Ephemeral(string text, params SlackResponseAttachment[] attachments) {
            return new SlackResponse("ephemeral", text, attachments);
        }

        //--- Fields ---
        [JsonProperty("response_type")]
        public readonly string ResponseType;

        [JsonProperty("text")]
        public readonly string Text;

        [JsonProperty("attachments")]
        public readonly SlackResponseAttachment[] Attachments;

        [JsonProperty("channel", NullValueHandling = NullValueHandling.Ignore)]
        public string Channel;

        //--- Constructors ---
        private SlackResponse(string responseType, string text, SlackResponseAttachment[] attachments) {
            if(string.IsNullOrWhiteSpace(responseType)) {
                throw new ArgumentException("Argument is null or whitespace", nameof(responseType));
            }
            this.ResponseType = responseType;
            this.Text = text ?? "";
            this.Attachments = attachments;
        }

        //--- Methods ---
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine(Text);
            if(Attachments != null) {
                foreach(var attachment in Attachments) {
                    sb.AppendLine(attachment.Text);
                }
            }
            return sb.ToString();
        }
    }
}
