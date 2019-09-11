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
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using LambdaSharp.SimpleNotificationService;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Demo.TwitterNotifier.NotifyFunction {

    public class Tweet {

        //--- Properties ---
        public string id_str { get; set; }
        public string full_text { get; set; }
        public TweetUser user { get; set; }
        public string aws_sentiment { get; set; }
    }

    public class TweetUser {

        //--- Properties ---
        public string name { get; set; }
        public string screen_name { get; set; }
    }

    public class Function : ALambdaTopicFunction<Tweet> {

        //--- Fields ---
        private string _twitterQuery;
        private IAmazonSimpleNotificationService _snsClient;
        private string _notificationTopic;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _twitterQuery = config.ReadText("TwitterQuery");

            // initialize SNS client
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _notificationTopic = config.ReadText("FoundTopic");
        }

        public override async Task ProcessMessageAsync(Tweet tweet) {
            var subject = $"@{tweet.user.screen_name} tweeted";
            if(tweet.aws_sentiment != null) {
                subject += $" [{tweet.aws_sentiment}]";
            }
            await _snsClient.PublishAsync(new PublishRequest {
                TopicArn = _notificationTopic,
                Message = FormatMessage(tweet),
                Subject = subject
            });
        }

        private string FormatMessage(Tweet tweet)
            =>
                $"---\n" +
                $"{tweet.full_text}\n" +
                $"---\n" +
                $"\n" +
                $"Link: https://twitter.com/{tweet.user.screen_name}/status/{tweet.id_str}\n" +
                $"\n" +
                $"From: https://twitter.com/{tweet.user.screen_name}\n";
    }
}
