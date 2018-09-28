/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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

namespace MindTouch.LambdaSharp.Slack {

    public class SlackRequest {

        //--- Properties ---
        public string Token { get; set; }
        public string TeamId { get; set; }
        public string TeamDomain { get; set; }
        public string EnterpriseId { get; set; }
        public string EnterpriseName { get; set; }
        public string ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Command { get; set; }
        public string Text { get; set; }
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
