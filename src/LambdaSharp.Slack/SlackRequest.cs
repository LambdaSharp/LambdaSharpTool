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

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.Slack {

    public class SlackRequest {

        //--- Properties ---

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("team_id")]
        public string TeamId { get; set; }

        [JsonPropertyName("team_domain")]
        public string TeamDomain { get; set; }

        [JsonPropertyName("enterprise_id")]
        public string EnterpriseId { get; set; }

        [JsonPropertyName("enterprise_name")]
        public string EnterpriseName { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("channel_name")]
        public string ChannelName { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("response_url")]
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
