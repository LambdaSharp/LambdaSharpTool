/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Humidifier;
using MindTouch.LambdaSharp.Tool.Model;
using Newtonsoft.Json;

namespace MindTouch.LambdaSharp.Tool {
    using Stack = Amazon.CloudFormation.Model.Stack;

    public class ModelUpdater : AModelProcessor {

        //--- Class Fields ---
        private static HashSet<string> _finalStates = new HashSet<string> {
            "CREATE_COMPLETE",
            "CREATE_FAILED",
            "DELETE_COMPLETE",
            "DELETE_FAILED",
            "ROLLBACK_COMPLETE",
            "ROLLBACK_FAILED",
            "UPDATE_COMPLETE",
            "UPDATE_ROLLBACK_COMPLETE",
            "UPDATE_ROLLBACK_FAILED"
        };

        private static bool IsFinalStackEvent(StackEvent evt)
            => (evt.ResourceType == "AWS::CloudFormation::Stack") && _finalStates.Contains(evt.ResourceStatus);

        private static bool IsSuccessfulFinalStackEvent(StackEvent evt)
            => (evt.ResourceType == "AWS::CloudFormation::Stack")
                && ((evt.ResourceStatus == "CREATE_COMPLETE") || (evt.ResourceStatus == "UPDATE_COMPLETE"));

        //--- Constructors ---
        public ModelUpdater(Settings settings) : base(settings) { }

        //--- Methods ---
        public async Task<bool> Deploy(Module module, string templateFile, bool allowDataLoss, bool protectStack) {
            var stackName = $"{Settings.Tier}-{module.Name}";
            Console.WriteLine($"Deploying stack: {stackName}");

            // check if cloudformation stack already exists
            string mostRecentStackEventId = null;
            try {
                var response = await Settings.CfClient.DescribeStackEventsAsync(new DescribeStackEventsRequest {
                    StackName = stackName
                });
                var mostRecentStackEvent = response.StackEvents.First();

                // make sure the stack is not already in an update operation
                if(!IsFinalStackEvent(mostRecentStackEvent)) {
                    Settings.AddError("stack appears to be undergoing an update operation");
                    return false;
                }
                mostRecentStackEventId = mostRecentStackEvent.EventId;
            } catch(AmazonCloudFormationException) { }

            // set optional notification topics for cloudformation operations
            var notificationArns =  new List<string>();
            if(Settings.NotificationTopicArn != null) {
                notificationArns.Add(Settings.NotificationTopicArn);
            }

            // upload cloudformation template
            string templateUrl = null;
            if(Settings.DeploymentBucketName != null) {
                var templateSuffix = Settings.GitSha ?? ("UTC" + DateTime.UtcNow.ToString("yyyyMMddhhmmss"));
                var templateS3Key = $"{module.Name}/cloudformation-{templateSuffix}.json";
                templateUrl = $"https://s3.amazonaws.com/{Settings.DeploymentBucketName}/{templateS3Key}";
                Console.WriteLine($"=> Uploading CloudFormation template: s3://{Settings.DeploymentBucketName}/{templateS3Key}");
                var transferUtility = new TransferUtility(Settings.S3Client);
                await transferUtility.UploadAsync(templateFile, Settings.DeploymentBucketName, templateS3Key);
            }

            // default stack policy denies all updates
            var stackPolicyBody =
@"{
    ""Statement"": [{
        ""Effect"": ""Allow"",
        ""Action"": ""Update:*"",
        ""Principal"": ""*"",
        ""Resource"": ""*""
    }, {
        ""Effect"": ""Deny"",
        ""Action"": [
            ""Update:Replace"",
            ""Update:Delete""
        ],
        ""Principal"": ""*"",
        ""Resource"": ""*"",
        ""Condition"": {
            ""StringEquals"": {
                ""ResourceType"": [
                    ""AWS::ApiGateway::RestApi"",
                    ""AWS::AppSync::GraphQLApi"",
                    ""AWS::DynamoDB::Table"",
                    ""AWS::EC2::Instance"",
                    ""AWS::EMR::Cluster"",
                    ""AWS::Kinesis::Stream"",
                    ""AWS::KinesisFirehose::DeliveryStream"",
                    ""AWS::KMS::Key"",
                    ""AWS::Neptune::DBCluster"",
                    ""AWS::Neptune::DBInstance"",
                    ""AWS::RDS::DBInstance"",
                    ""AWS::Redshift::Cluster"",
                    ""AWS::S3::Bucket""
                ]
            }
        }
    }]
}";
            var stackDuringUpdatePolicyBody =
@"{
    ""Statement"": [{
        ""Effect"": ""Allow"",
        ""Action"": ""Update:*"",
        ""Principal"": ""*"",
        ""Resource"": ""*""
    }]
}";

            // create/update cloudformation stack
            var success = false;
            if(mostRecentStackEventId != null) {
                try {
                    Console.WriteLine($"=> Stack update initiated");
                    var request = new UpdateStackRequest {
                        StackName = stackName,
                        Capabilities = new List<string> {
                            "CAPABILITY_NAMED_IAM"
                        },
                        NotificationARNs = notificationArns,
                        StackPolicyBody = stackPolicyBody,
                        StackPolicyDuringUpdateBody = allowDataLoss ? stackDuringUpdatePolicyBody : null,
                        TemplateURL = templateUrl,
                        TemplateBody = (templateUrl == null) ? File.ReadAllText(templateFile) : null
                    };
                    var response = await Settings.CfClient.UpdateStackAsync(request);
                    var outcome = await TrackStackUpdate(module, response.StackId, mostRecentStackEventId);
                    if(outcome.Success) {
                        Console.WriteLine($"=> Stack update finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                        ShowStackResult(outcome.Stack);
                        success = true;
                    } else {
                        Console.WriteLine($"=> Stack update FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    }
                } catch(AmazonCloudFormationException e) when(e.Message == "No updates are to be performed.") {

                    // this error is thrown when no required updates where found
                    Console.WriteLine($"=> No stack update required (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    success = true;
                }
            } else {
                Console.WriteLine($"=> Stack creation initiated");
                var request = new CreateStackRequest {
                    StackName = stackName,
                    Capabilities = new List<string> {
                        "CAPABILITY_NAMED_IAM"
                    },
                    OnFailure = OnFailure.DELETE,
                    NotificationARNs = notificationArns,
                    StackPolicyBody = stackPolicyBody,
                    EnableTerminationProtection = protectStack,
                    TemplateURL = templateUrl,
                    TemplateBody = (templateUrl == null) ? File.ReadAllText(templateFile) : null
                };
                var response = await Settings.CfClient.CreateStackAsync(request);
                var outcome = await TrackStackUpdate(module, response.StackId, mostRecentStackEventId);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack creation finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    ShowStackResult(outcome.Stack);
                    success = true;
                } else {
                    Console.WriteLine($"=> Stack creation FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                }
            }
            return success;

            // local function
            void ShowStackResult(Stack stack) {
                var outputs = stack.Outputs;
                if(outputs.Any()) {
                    Console.WriteLine("Stack output values:");
                    foreach(var output in outputs) {
                        Console.WriteLine($"=> {output.Description}: {output.OutputValue}");
                    }
                }
            }
        }

        private async Task<(Stack Stack, bool Success)> TrackStackUpdate(Module module, string stackId, string mostRecentStackEventId) {
            var seenEventIds = new HashSet<string>();
            var foundMostRecentStackEvent = (mostRecentStackEventId == null);
            var request = new DescribeStackEventsRequest {
                StackName = stackId
            };

            // iterate as long as the stack is being created/updated
            var active = true;
            var success = false;
            while(active) {
                await Task.Delay(TimeSpan.FromSeconds(3));

                // fetch as many events as possible for the current stack
                var events = new List<StackEvent>();
                var response = await Settings.CfClient.DescribeStackEventsAsync(request);
                events.AddRange(response.StackEvents);
                events.Reverse();

                // skip any events that preceded the most recent event before the stack update operation
                while(!foundMostRecentStackEvent && events.Any()) {
                    var evt = events.First();
                    if(evt.EventId == mostRecentStackEventId) {
                        foundMostRecentStackEvent = true;
                    }
                    seenEventIds.Add(evt.EventId);
                    events.RemoveAt(0);
                }
                if(!foundMostRecentStackEvent) {
                    Settings.AddError("unable to find starting event");
                    return (Stack: null, Success: false);
                }

                // report only on new events
                foreach(var evt in events.Where(evt => !seenEventIds.Contains(evt.EventId))) {
                    Console.WriteLine($"{evt.ResourceStatus,-35} {evt.ResourceType,-55} {evt.LogicalResourceId}{(evt.ResourceStatusReason != null ? $" ({evt.ResourceStatusReason})" : "")}");
                    if(!seenEventIds.Add(evt.EventId)) {

                        // we found an event we already saw in the past, no point in looking at more events
                        break;
                    }
                    if(IsFinalStackEvent(evt)) {

                        // event signals stack creation/update completion; time to stop
                        active = false;
                        success = IsSuccessfulFinalStackEvent(evt);
                        break;
                    }
                }
            }

            // describe stack and report any output values
            var description = await Settings.CfClient.DescribeStacksAsync(new DescribeStacksRequest {
                StackName = stackId
            });
            return (Stack: description.Stacks.FirstOrDefault(), Success: success);
        }
    }
}