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
using Amazon.Lambda.SNSEvents;

namespace LambdaSharp.SimpleNotificationService.Extensions {

    internal static class SnsMessageEx {

        //--- Constants ---
        private const string SENT_TIME_ATTRIBUTE = "SentTimestamp";

        //--- Extension Methods ---
        public static DateTimeOffset GetLifespanTimestamp(this SNSEvent.SNSMessage message) {

            // a custom "SentTimestamp" message attribute takes precedence over the record "SentTimestamp" attribute
            if(
                message.MessageAttributes.TryGetValue(SENT_TIME_ATTRIBUTE, out var sentTimeMessageAttribute)
                && (sentTimeMessageAttribute.Type == "String")
                && long.TryParse(sentTimeMessageAttribute.Value, out var sentTimeEpoch)
            ) {
                return DateTimeOffset.FromUnixTimeMilliseconds(sentTimeEpoch);
            }
            return message.Timestamp.ToUniversalTime();
        }
    }
}
