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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Internal {

    public delegate void LogErrorDelegate(string messages, Exception exception);
    public delegate void LogWarnDelegate(string messages);

    internal static class AwsEx {

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

        private static Dictionary<string, string> _ansiStatusColorCodes = new Dictionary<string, string> {
            ["CREATE_IN_PROGRESS"] = AnsiTerminal.BrightYellow,
            ["CREATE_FAILED"] = AnsiTerminal.Red,
            ["CREATE_COMPLETE"] = AnsiTerminal.Green,

            ["ROLLBACK_IN_PROGRESS"] = AnsiTerminal.BackgroundRed + AnsiTerminal.White,
            ["ROLLBACK_FAILED"] = AnsiTerminal.BackgroundBrightRed + AnsiTerminal.BrightWhite,
            ["ROLLBACK_COMPLETE"] = AnsiTerminal.BackgroundRed + AnsiTerminal.Black,

            ["DELETE_IN_PROGRESS"] = AnsiTerminal.BrightYellow,
            ["DELETE_FAILED"] = AnsiTerminal.BackgroundBrightRed + AnsiTerminal.BrightWhite,
            ["DELETE_COMPLETE"] = AnsiTerminal.Green,

            ["UPDATE_IN_PROGRESS"] = AnsiTerminal.BrightYellow,
            ["UPDATE_FAILED"] = AnsiTerminal.BackgroundBrightRed + AnsiTerminal.BrightWhite,
            ["UPDATE_COMPLETE_CLEANUP_IN_PROGRESS"] = AnsiTerminal.BrightYellow,
            ["UPDATE_COMPLETE"] = AnsiTerminal.Green,

            ["UPDATE_ROLLBACK_IN_PROGRESS"] = AnsiTerminal.BackgroundRed + AnsiTerminal.White,
            ["UPDATE_ROLLBACK_FAILED"] = AnsiTerminal.BackgroundBrightRed + AnsiTerminal.BrightWhite,
            ["UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS"] = AnsiTerminal.BackgroundRed + AnsiTerminal.White,
            ["UPDATE_ROLLBACK_COMPLETE"] = AnsiTerminal.BackgroundRed + AnsiTerminal.Black,

            ["REVIEW_IN_PROGRESS"] = ""
        };

        //--- Extension Methods ---
        public async static Task<Dictionary<string, KeyValuePair<string, string>>> GetAllParametersByPathAsync(this IAmazonSimpleSystemsManagement client, string path) {
            var parametersRequest = new GetParametersByPathRequest {
                MaxResults = 10,
                Recursive = true,
                Path = path
            };
            var result = new Dictionary<string, KeyValuePair<string, string>>();
            do {
                var response = await client.GetParametersByPathAsync(parametersRequest);
                foreach(var parameter in response.Parameters) {
                    result[parameter.Name] = new KeyValuePair<string, string>(parameter.Type, parameter.Value);
                }
                parametersRequest.NextToken = response.NextToken;
            } while(parametersRequest.NextToken != null);
            return result;
        }

        public static async Task<string> GetMostRecentStackEventIdAsync(this IAmazonCloudFormation cfnClient, string stackName) {
            try {
                var response = await cfnClient.DescribeStackEventsAsync(new DescribeStackEventsRequest {
                    StackName = stackName
                });
                var mostRecentStackEvent = response.StackEvents.First();

                // make sure the stack is not already in an update operation
                if(!mostRecentStackEvent.IsFinalStackEvent()) {
                    throw new System.InvalidOperationException("stack appears to be undergoing an update operation");
                }
                return mostRecentStackEvent.EventId;
            } catch(AmazonCloudFormationException) {

                // NOTE (2018-12-11, bjorg): exception is thrown when stack doesn't exist; ignore it
            }
            return null;
        }

        public static async Task<(Stack Stack, bool Success)> TrackStackUpdateAsync(
            this IAmazonCloudFormation cfnClient,
            string stackName,
            string stackId,
            string mostRecentStackEventId,
            ModuleNameMappings nameMappings = null,
            LogErrorDelegate logError = null
        ) {
            var seenEventIds = new HashSet<string>();
            var foundMostRecentStackEvent = (mostRecentStackEventId == null);
            var request = new DescribeStackEventsRequest {
                StackName = stackId ?? stackName
            };
            var eventList = new List<StackEvent>();
            var ansiLinesPrinted = 0;

            // iterate as long as the stack is being created/updated
            var active = true;
            var success = false;
            while(active) {
                await Task.Delay(TimeSpan.FromSeconds(3));

                // fetch as many events as possible for the current stack
                var events = new List<StackEvent>();
                try {
                    var response = await cfnClient.DescribeStackEventsAsync(request);
                    events.AddRange(response.StackEvents);
                } catch(System.Net.Http.HttpRequestException e) when((e.InnerException is System.Net.Sockets.SocketException) && (e.InnerException.Message == "No such host is known")) {

                    // ignore network issues and just try again
                    continue;
                }
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
                    throw new ApplicationException($"unable to find starting event for stack: {stackName}");
                }

                // report only on new events
                foreach(var evt in events.Where(evt => !seenEventIds.Contains(evt.EventId))) {
                    UpdateEvent(evt);
                    if(!seenEventIds.Add(evt.EventId)) {

                        // we found an event we already saw in the past, no point in looking at more events
                        break;
                    }
                    if(IsFinalStackEvent(evt) && (evt.LogicalResourceId == stackName)) {

                        // event signals stack creation/update completion; time to stop
                        active = false;
                        success = IsSuccessfulFinalStackEvent(evt);
                        break;
                    }
                }
                RenderEvents();
            }
            if(!success) {
                return (Stack: null, Success: false);
            }

            // describe stack and report any output values
            var description = await cfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                StackName = stackName
            });
            return (Stack: description.Stacks.FirstOrDefault(), Success: success);

            // local function
            string TranslateLogicalIdToFullName(string logicalId) {
                var fullName = logicalId;
                nameMappings?.ResourceNameMappings?.TryGetValue(logicalId, out fullName);
                return fullName ?? logicalId;
            }

            string TranslateResourceTypeToFullName(string awsType) {
                var fullName = awsType;
                nameMappings?.TypeNameMappings?.TryGetValue(awsType, out fullName);
                return fullName ?? awsType;
            }

            void RenderEvents() {
                if(Settings.UseAnsiConsole) {
                    if(ansiLinesPrinted > 0) {
                        Console.Write(AnsiTerminal.MoveLineUp(ansiLinesPrinted));
                    }
                    var maxResourceStatusLength = eventList.Any() ? eventList.Max(evt => evt.ResourceStatus.ToString().Length) : 0;
                    var maxResourceTypeNameLength = eventList.Any() ? eventList.Max(evt => TranslateResourceTypeToFullName(evt.ResourceType).Length) : 0;
                    foreach(var evt in eventList) {
                        var resourceStatus = evt.ResourceStatus.ToString();
                        var resourceType = TranslateResourceTypeToFullName(evt.ResourceType);
                        if(_ansiStatusColorCodes.TryGetValue(evt.ResourceStatus, out var ansiColor)) {

                            // print resource status
                            Console.Write(ansiColor);
                            Console.Write(resourceStatus);
                            Console.Write(AnsiTerminal.Reset);
                            Console.Write("".PadRight(maxResourceStatusLength - resourceStatus.Length + 4));

                            // print resource type
                            Console.Write(resourceType);
                            Console.Write("".PadRight(maxResourceTypeNameLength - resourceType.Length + 4));

                            // print resource name
                            Console.Write(TranslateLogicalIdToFullName(evt.LogicalResourceId));

                            // print status reason
                            if((logError == null) && (evt.ResourceStatusReason != null)) {
                                Console.Write($" ({evt.ResourceStatusReason})");
                            }
                        } else {
                            Console.Write($"{resourceStatus}    {resourceType}    {TranslateLogicalIdToFullName(evt.LogicalResourceId)}{(evt.ResourceStatusReason != null ? $" ({evt.ResourceStatusReason})" : "")}");
                        }
                        Console.Write(AnsiTerminal.ClearEndOfLine);
                        Console.WriteLine();
                    }
                    ansiLinesPrinted = eventList.Count;
                }
            }

            void UpdateEvent(StackEvent evt) {
                if(Settings.UseAnsiConsole) {
                    var index = eventList.FindIndex(e => e.LogicalResourceId == evt.LogicalResourceId);
                    if(index < 0) {
                        eventList.Add(evt);
                    } else {
                        eventList[index] = evt;
                    }
                } else {
                    Console.WriteLine($"{evt.ResourceStatus,-35} {TranslateResourceTypeToFullName(evt.ResourceType),-55} {TranslateLogicalIdToFullName(evt.LogicalResourceId)}{(evt.ResourceStatusReason != null ? $" ({evt.ResourceStatusReason})" : "")}");
                }

                // capture failed operation as an error
                switch(evt.ResourceStatus) {
                case "CREATE_FAILED":
                case "ROLLBACK_FAILED":
                case "UPDATE_FAILED":
                case "DELETE_FAILED":
                case "UPDATE_ROLLBACK_FAILED":
                case "UPDATE_ROLLBACK_IN_PROGRESS":
                    if(evt.ResourceStatusReason != "Resource creation cancelled") {
                        logError?.Invoke($"{evt.ResourceStatus} {TranslateLogicalIdToFullName(evt.LogicalResourceId)} [{TranslateResourceTypeToFullName(evt.ResourceType)}]: {evt.ResourceStatusReason}", /*Exception*/ null);
                    }
                    break;
                }
            }
        }

        public static bool IsFinalStackEvent(this StackEvent evt)
            => (evt.ResourceType == "AWS::CloudFormation::Stack") && _finalStates.Contains(evt.ResourceStatus);

        public static bool IsSuccessfulFinalStackEvent(this StackEvent evt)
            => (evt.ResourceType == "AWS::CloudFormation::Stack")
                && ((evt.ResourceStatus == "CREATE_COMPLETE") || (evt.ResourceStatus == "UPDATE_COMPLETE"));

        public static async Task<(bool Success, Stack Stack)> GetStackAsync(this IAmazonCloudFormation cfnClient, string stackName, LogErrorDelegate logError) {
            Stack stack = null;
            try {
                var describe = await cfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                    StackName = stackName
                });

                // make sure the stack is in a stable state (not updating and not failed)
                stack = describe.Stacks.FirstOrDefault();
                switch(stack?.StackStatus) {
                case null:
                case "CREATE_COMPLETE":
                case "ROLLBACK_COMPLETE":
                case "UPDATE_COMPLETE":
                case "UPDATE_ROLLBACK_COMPLETE":

                    // we're good to go
                    break;
                default:
                    logError?.Invoke($"{stackName} is not in a valid state; module deployment must be complete and successful (status: {stack?.StackStatus})", null);
                    return (false, null);
                }
            } catch(AmazonCloudFormationException) {

                // stack not found; nothing to do
            }
            return (true, stack);
        }

        public static async Task<string> GetS3ObjectContents(this IAmazonS3 s3Client, string bucketName, string key) {
            try {
                var response = await s3Client.GetObjectAsync(new GetObjectRequest {
                    BucketName = bucketName,
                    Key = key,
                    RequestPayer = RequestPayer.Requester
                });
                using(var stream = new MemoryStream()) {
                    await response.ResponseStream.CopyToAsync(stream);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            } catch(AmazonS3Exception) {
                return null;
            }
        }

        public static async Task<bool> DoesS3ObjectExistAsync(this IAmazonS3 s3Client, string bucketName, string key) {
            try {
                await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                    BucketName = bucketName,
                    Key = key,
                    RequestPayer = RequestPayer.Requester
                });
            } catch {
                return false;
            }
            return true;
        }

        public static string GetModuleVersionText(this Stack stack) => stack.Outputs?.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
        public static string GetModuleManifestChecksum(this Stack stack) => stack.Outputs?.FirstOrDefault(output => output.OutputKey == "ModuleChecksum")?.OutputValue;
    }
}