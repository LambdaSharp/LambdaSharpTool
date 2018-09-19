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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MindTouch.LambdaSharp;
using MindTouch.LambdaSharp.CustomResource;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MindTouch.LambdaSharpS3Subscriber.ResourceHandler {
    using LambdaSubscription = Amazon.S3.Model.LambdaFunctionConfiguration;
    using LambdaFilter = Amazon.S3.Model.Filter;

    public class RequestProperties {

        //--- Properties ---
        public string BucketName { get; set; }
        public string FunctionArn { get; set; }
        public IList<Filter> Filters { get; set; }
    }

    public class Filter {

        //--- Properties ---
        public IList<string> Events { get; set; }
        public string Prefix;
        public string Suffix;
    }

    public class ResponseProperties {

        //--- Properties ---
        public string Result { get; set; }
    }

    public class Function : ALambdaCustomResourceFunction<RequestProperties, ResponseProperties> {

        //--- Fields ---
        private IAmazonS3 _s3Client;

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            _s3Client = new AmazonS3Client();
            return Task.CompletedTask;
        }

        protected override async Task<Response<ResponseProperties>> HandleCreateResourceAsync(Request<RequestProperties> request) {
            var properties = request.ResourceProperties;
            var config = await _s3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest {
                BucketName = properties.BucketName
            });
            Add(config.LambdaFunctionConfigurations, properties);
            await _s3Client.PutBucketNotificationAsync(new PutBucketNotificationRequest {
                BucketName = properties.BucketName,
                LambdaFunctionConfigurations = config.LambdaFunctionConfigurations,
                QueueConfigurations = config.QueueConfigurations,
                TopicConfigurations = config.TopicConfigurations
            });
            return new Response<ResponseProperties> {
                PhysicalResourceId = $"s3subscription:{properties.BucketName}:{properties.FunctionArn}",
                Properties = new ResponseProperties {
                    Result = $"s3://{properties.BucketName}/"
                }
            };
        }

        protected override async Task<Response<ResponseProperties>> HandleDeleteResourceAsync(Request<RequestProperties> request) {
            var properties = request.ResourceProperties;
            var config = await _s3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest {
                BucketName = properties.BucketName
            });
            Remove(config.LambdaFunctionConfigurations, properties);
            await _s3Client.PutBucketNotificationAsync(new PutBucketNotificationRequest {
                BucketName = properties.BucketName,
                LambdaFunctionConfigurations = config.LambdaFunctionConfigurations,
                QueueConfigurations = config.QueueConfigurations,
                TopicConfigurations = config.TopicConfigurations
            });
            return new Response<ResponseProperties>();
        }

        protected override async Task<Response<ResponseProperties>> HandleUpdateResourceAsync(Request<RequestProperties> request) {
            var properties = request.ResourceProperties;
            var config = await _s3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest {
                BucketName = properties.BucketName
            });
            Remove(config.LambdaFunctionConfigurations, request.OldResourceProperties);
            Add(config.LambdaFunctionConfigurations, properties);
            await _s3Client.PutBucketNotificationAsync(new PutBucketNotificationRequest {
                BucketName = properties.BucketName,
                LambdaFunctionConfigurations = config.LambdaFunctionConfigurations,
                QueueConfigurations = config.QueueConfigurations,
                TopicConfigurations = config.TopicConfigurations
            });
            return new Response<ResponseProperties> {
                PhysicalResourceId = request.PhysicalResourceId,
                Properties = new ResponseProperties {
                    Result = $"s3://{properties.BucketName}/"
                }
            };
        }

        private void Add(List<LambdaSubscription> subscriptions, RequestProperties properties) {
            foreach(var filter in properties.Filters) {
                var subscription = new LambdaSubscription {
                    FunctionArn = properties.FunctionArn,
                    Events = filter.Events.Select(e => EventType.FindValue(e)).ToList()
                };

                // check if prefix/suffix filter were provided
                if((filter.Prefix != null) || (filter.Suffix != null)) {
                    var rules = new List<FilterRule>();
                    if(filter.Prefix != null) {
                        rules.Add(new FilterRule {
                            Name = "prefix",
                            Value = filter.Prefix
                        });
                    }
                    if(filter.Suffix != null) {
                        rules.Add(new FilterRule {
                            Name = "suffix",
                            Value = filter.Suffix
                        });
                    }
                    subscription.Filter = new LambdaFilter {
                        S3KeyFilter = new S3KeyFilter {
                            FilterRules = rules
                        }
                    };
                }
                subscriptions.Add(subscription);
            }
        }

        private void Remove(List<LambdaSubscription> subscriptions, RequestProperties properties) {
            subscriptions.RemoveAll(subscription => subscription.FunctionArn == properties.FunctionArn);
        }
    }
}