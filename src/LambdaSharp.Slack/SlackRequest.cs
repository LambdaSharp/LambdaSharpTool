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

using System.Text;
using Newtonsoft.Json;

namespace LambdaSharp.Slack {

    public class SlackRequest {

        //--- Properties ---
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("team_id")]
        public string TeamId { get; set; }

        [JsonProperty("team_domain")]
        public string TeamDomain { get; set; }

        [JsonProperty("enterprise_id")]
        public string EnterpriseId { get; set; }

        [JsonProperty("enterprise_name")]
        public string EnterpriseName { get; set; }

        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty("channel_name")]
        public string ChannelName { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("user_name")]
        public string UserName { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("response_url")]
        public string ResponseUrl { get; set; }

        //--- Methods ---
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine($"Token: ###");
            sb.AppendLine($"TeamId: {TeamId}");
            sb.AppendLine($"TeamDomain: {TeamDomain}");
            sb.AppendLine($"ChannelId: {ChannelId}");
            sb.AppendLine($"ChannelName: {ChannelName}");
            sb.AppendLine($"UserId: {UserId}");
            sb.AppendLine($"UserName: {UserName}");
            sb.AppendLine($"Command: {Command}");
            sb.AppendLine($"Text: {Text}");
            sb.AppendLine($"ResponseUrl: {ResponseUrl}");
            return sb.ToString();
        }
    }
}
