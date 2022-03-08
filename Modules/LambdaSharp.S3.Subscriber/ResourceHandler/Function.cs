/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace LambdaSharpS3Subscriber.ResourceHandler;

using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp;
using LambdaSharp.CustomResource;

using LambdaSubscription = Amazon.S3.Model.LambdaFunctionConfiguration;
using LambdaFilter = Amazon.S3.Model.Filter;

public class S3SubscriptionProperties {

    //--- Properties ---
    public string? Bucket { get; set; }
    public string? Function { get; set; }
    public IList<Filter>? Filters { get; set; }

    public string? BucketName => (Bucket?.StartsWith("arn:") ?? false)
        ? AwsConverters.ConvertBucketArnToName(Bucket)
        : Bucket;
}

public class Filter {

    //--- Properties ---
    public IList<string>? Events { get; set; }
    public string? Prefix;
    public string? Suffix;
}

public class S3SubscriptionAttributes {

    //--- Properties ---
    public string? Result { get; set; }
}

public sealed class Function : ALambdaCustomResourceFunction<S3SubscriptionProperties, S3SubscriptionAttributes> {

    //--- Fields ---
    private IAmazonS3? _s3Client;

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Properties ---
    public IAmazonS3 S3Client => _s3Client ?? throw new InvalidOperationException();

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config) {
        _s3Client = new AmazonS3Client();
        return Task.CompletedTask;
    }

    public override async Task<Response<S3SubscriptionAttributes>> ProcessCreateResourceAsync(Request<S3SubscriptionProperties> request, CancellationToken cancellationToken) {
        if(request.ResourceProperties is null) {
            throw Abort("Missing ResourceProperties property");
        }
        var properties = request.ResourceProperties;

        // extract bucket name from arn (arn:aws:s3:::bucket_name)
        var bucketName = properties.BucketName;
        if(bucketName is null) {
            throw Abort("Missing ResourceProperties.Bucket property");
        }
        GetBucketNotificationResponse config;
        try {
            config = await S3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest {
                BucketName = bucketName
            });
            Add(config.LambdaFunctionConfigurations, properties);
        } catch(AmazonS3Exception e) {
            throw Abort(e.Message);
        }

        // attempt to update bucket notification configuration
        var attempts = 0;
        const int MAX_ATTEMPTS = 3;
        var backOff = TimeSpan.FromSeconds(5);
    again:
        try {
            await S3Client.PutBucketNotificationAsync(new PutBucketNotificationRequest {
                BucketName = bucketName,
                LambdaFunctionConfigurations = config.LambdaFunctionConfigurations,
                QueueConfigurations = config.QueueConfigurations,
                TopicConfigurations = config.TopicConfigurations
            });
        } catch(AmazonS3Exception e) when(
            (e.Message == "A conflicting conditional operation is currently in progress against this resource. Please try again.")
            && (attempts < MAX_ATTEMPTS)
        ) {

            // NOTE (2019-09-06, bjorg): encountered this error during a test run on a newly created bucket; it seems preventable with
            //  exponential back off, but it's not clear why it happened in the first place
            ++attempts;
            await Task.Delay(backOff);
            backOff = backOff * 2;
            goto again;
        }
        return new Response<S3SubscriptionAttributes> {
            PhysicalResourceId = $"s3subscription:{bucketName}:{properties.Function}",
            Attributes = new S3SubscriptionAttributes {
                Result = $"s3://{bucketName}/"
            }
        };
    }

    public override async Task<Response<S3SubscriptionAttributes>> ProcessDeleteResourceAsync(Request<S3SubscriptionProperties> request, CancellationToken cancellationToken) {

        // only attempt delete operation if a physical resource ID is present
        if(request.HasPhysicalResourceId()) {
            var properties = request.ResourceProperties;
            if(properties is null) {
                throw Abort("Missing ResourceProperties property");
            }

            // extract bucket name from arn (arn:aws:s3:::bucket_name)
            var bucketName = properties.BucketName;
            var config = await S3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest {
                BucketName = bucketName
            });
            Remove(config.LambdaFunctionConfigurations, properties);
            await S3Client.PutBucketNotificationAsync(new PutBucketNotificationRequest {
                BucketName = bucketName,
                LambdaFunctionConfigurations = config.LambdaFunctionConfigurations,
                QueueConfigurations = config.QueueConfigurations,
                TopicConfigurations = config.TopicConfigurations
            });
        } else {
            LogInfo("no physical ID provided; ignoring DELETE operation");
        }
        return new Response<S3SubscriptionAttributes>();
    }

    public override async Task<Response<S3SubscriptionAttributes>> ProcessUpdateResourceAsync(Request<S3SubscriptionProperties> request, CancellationToken cancellationToken) {
        if(request.ResourceProperties is null) {
            throw Abort("Missing ResourceProperties property");
        }
        if(request.OldResourceProperties is null) {
            throw Abort("Missing OldResourceProperties property");
        }
        var properties = request.ResourceProperties;

        // extract bucket name from arn (arn:aws:s3:::bucket_name)
        var bucketName = properties.BucketName;
        if(bucketName is null) {
            throw Abort("Missing ResourceProperties.Bucket property");
        }
        var config = await S3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest {
            BucketName = bucketName
        });
        Remove(config.LambdaFunctionConfigurations, request.OldResourceProperties);
        Add(config.LambdaFunctionConfigurations, properties);
        await S3Client.PutBucketNotificationAsync(new PutBucketNotificationRequest {
            BucketName = bucketName,
            LambdaFunctionConfigurations = config.LambdaFunctionConfigurations,
            QueueConfigurations = config.QueueConfigurations,
            TopicConfigurations = config.TopicConfigurations
        });
        return new Response<S3SubscriptionAttributes> {
            PhysicalResourceId = request.PhysicalResourceId,
            Attributes = new S3SubscriptionAttributes {
                Result = $"s3://{bucketName}/"
            }
        };
    }

    private void Add(List<LambdaSubscription> subscriptions, S3SubscriptionProperties properties) {
        if(properties.Filters is null) {
            throw Abort("Missing ResourceProperties.Filter property");
        }
        foreach(var (filter, index) in properties.Filters.Select((filter, index) => (filter, index))) {
            if(filter is null) {
                throw Abort($"Missing ResourceProperties.Filter[{index}] property");
            }
            if(filter.Events is null) {
                throw Abort($"Missing ResourceProperties.Filter[{index}].Events property");
            }
            var subscription = new LambdaSubscription {
                FunctionArn = properties.Function,
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

    private void Remove(List<LambdaSubscription> subscriptions, S3SubscriptionProperties properties) {
        subscriptions.RemoveAll(subscription => subscription.FunctionArn == properties.Function);
    }
}
