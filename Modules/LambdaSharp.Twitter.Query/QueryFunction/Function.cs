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
using System.Linq;
using System.Threading.Tasks;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using LambdaSharp.Schedule;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Twitter.QueryFunction {
    using TwitterSearch = Tweetinvi.Search;

    public class Function : ALambdaScheduleFunction {

        //--- Fields ---
        private string _twitterSearchQuery;
        private HashSet<string> _twitterLanguageFilter;
        private string _twitterSentimentFilter;
        private IAmazonDynamoDB _dynamoClient;
        private Table _table;
        private IAmazonComprehend _comprehendClient;
        private IAmazonSimpleNotificationService _snsClient;
        private string _notificationTopic;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // initialize twitter client
            Auth.SetApplicationOnlyCredentials(
                config.ReadText("TwitterApiKey"),
                config.ReadText("TwitterApiSecretKey"),
                true
            );
            _twitterSearchQuery = config.ReadText("TwitterQuery");
            _twitterLanguageFilter = new HashSet<string>(config.ReadCommaDelimitedList("TwitterLanguageFilter"));
            _twitterSentimentFilter = config.ReadText("TwitterSentimentFilter");

            // initialize Comprehend client
            _comprehendClient = new AmazonComprehendClient();

            // initialize DynamoDB table
            _dynamoClient = new AmazonDynamoDBClient();
            _table = Table.LoadTable(_dynamoClient, config.ReadDynamoDBTableName("Table"));

            // initialize SNS client
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _notificationTopic = config.ReadText("TweetTopic");
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent request) {
            var lastId = 0L;

            // read last_id from table
            var document = await _table.GetItemAsync("last");
            if(
                (document != null)
                && document.TryGetValue("Query", out var queryEntry)
                && (queryEntry.AsString() == _twitterSearchQuery)
                && document.TryGetValue("LastId", out var lastIdEntry)
            ) {
                lastId = lastIdEntry.AsLong();
            }

            // query for tweets since last id
            LogInfo($"searching for tweets: query='{_twitterSearchQuery}', last_id={lastId}");
            var tweets = TwitterSearch.SearchTweets(new SearchTweetsParameters(_twitterSearchQuery) {
                Lang = null,
                TweetSearchType = TweetSearchType.OriginalTweetsOnly,
                SinceId = lastId
            }) ?? Enumerable.Empty<ITweet>();

            // check if any tweets were found
            LogInfo($"found {tweets.Count():N0} tweets");
            if(tweets.Any()) {
                var languages = string.Join(",", _twitterLanguageFilter.OrderBy(language => language));

                // send all tweets to topic
                var tasks = tweets

                    // convert tweet object back to JSON to extract the ISO language
                    .Select(tweet => JObject.Parse(tweet.ToJson()))
                    .Select(json => new {
                        Json = json,
                        IsoLanguage = (string)json["metadata"]["iso_language_code"]
                    })

                    // only keep tweets that match the ISO language filter if one is set
                    .Where(item => {
                        if(_twitterLanguageFilter.Any() && !_twitterLanguageFilter.Contains(item.IsoLanguage)) {
                            LogInfo($"tweet language '{item.IsoLanguage}' did not match '{languages}'");
                            return false;
                        }
                        return true;
                    })

                    // group tweets by language for sentiment analysis
                    .GroupBy(item => item.IsoLanguage, item => item.Json)
                    .Select(async group => {
                        if(_twitterSentimentFilter == "SKIP") {

                            // skip sentiment analysis
                            return group;
                        }

                        // loop over all tweets
                        var remaining = group.ToList();
                        while(remaining.Any()) {

                            // batch analyze tweets
                            var batch = remaining.Take(25).ToArray();
                            var response = await _comprehendClient.BatchDetectSentimentAsync(new BatchDetectSentimentRequest {
                                LanguageCode = group.Key,
                                TextList = batch.Select(item => (string)item["full_text"]).ToList()
                            });

                            // set result for successfully analyzed tweets
                            foreach(var result in response.ResultList) {
                                batch[result.Index]["aws_sentiment"] = result.Sentiment.Value;
                            }

                            // set 'N/A' for tweets that failed analysis
                            foreach(var result in response.ErrorList) {
                                batch[result.Index]["aws_sentiment"] = "N/A";
                            }

                            // skip analyzed tweets
                            remaining = remaining.Skip(25).ToList();
                        }
                        return group;
                    })
                    .SelectMany(group => group.Result)
                    .Where(jsonTweet => {

                        // check if message should be filtered based-on sentiment
                        switch(_twitterSentimentFilter) {
                        case "SKIP":
                        case "ALL":
                            return true;
                        default:
                            if(((string)jsonTweet["aws_sentiment"]) != _twitterSentimentFilter) {
                                LogInfo($"tweet sentiment '{jsonTweet["aws_sentiment"]}' did not match '{_twitterSentimentFilter}'");
                                return false;
                            }
                            return true;
                        }
                    })
                    .Select((jsonTweet, index) => {
                        LogInfo($"sending tweet #{index + 1}: {jsonTweet["full_text"]}\n{jsonTweet}");
                        return _snsClient.PublishAsync(new PublishRequest {
                            TopicArn = _notificationTopic,
                            Message = jsonTweet.ToString(Formatting.None)
                        });
                    })
                    .ToList();

                // store updated last_id
                await _table.PutItemAsync(new Document {
                    ["Id"] = "last",
                    ["LastId"] = tweets.Max(tweet => tweet.Id),
                    ["Query"] = _twitterSearchQuery
                });

                // wait for all tasks to finish before exiting
                LogInfo($"waiting for all tweets to be sent");
                await Task.WhenAll(tasks.ToArray());
                LogInfo($"all done");
            }
        }
    }
}
