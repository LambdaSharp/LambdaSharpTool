/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace LambdaSharp.Tool.Internal {

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

        public static async Task<string> GetMostRecentStackEventIdAsync(this IAmazonCloudFormation cfClient, string stackName) {
            try {
                var response = await cfClient.DescribeStackEventsAsync(new DescribeStackEventsRequest {
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
            this IAmazonCloudFormation cfClient,
            string stackName,
            string mostRecentStackEventId,
            IDictionary<string, string> resourceNameMappings = null,
            IDictionary<string, string> typeNameMappings = null
        ) {
            var seenEventIds = new HashSet<string>();
            var foundMostRecentStackEvent = (mostRecentStackEventId == null);
            var request = new DescribeStackEventsRequest {
                StackName = stackName
            };

            // iterate as long as the stack is being created/updated
            var active = true;
            var success = false;
            while(active) {
                await Task.Delay(TimeSpan.FromSeconds(3));

                // fetch as many events as possible for the current stack
                var events = new List<StackEvent>();
                try {
                    var response = await cfClient.DescribeStackEventsAsync(request);
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
                    Console.WriteLine($"{evt.ResourceStatus,-35} {TranslateResourceTypeToFullName(evt.ResourceType),-55} {TranslateLogicalIdToFullName(evt.LogicalResourceId)}{(evt.ResourceStatusReason != null ? $" ({evt.ResourceStatusReason})" : "")}");
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
            }

            // describe stack and report any output values
            var description = await cfClient.DescribeStacksAsync(new DescribeStacksRequest {
                StackName = stackName
            });
            return (Stack: description.Stacks.FirstOrDefault(), Success: success);

            // local function
            string TranslateLogicalIdToFullName(string logicalId) {
                var fullName = logicalId;
                resourceNameMappings?.TryGetValue(logicalId, out fullName);
                return fullName ?? logicalId;
            }

            string TranslateResourceTypeToFullName(string awsType) {
                var fullName = awsType;
                typeNameMappings?.TryGetValue(awsType, out fullName);
                return fullName ?? awsType;
            }
        }

        public static bool IsFinalStackEvent(this StackEvent evt)
            => (evt.ResourceType == "AWS::CloudFormation::Stack") && _finalStates.Contains(evt.ResourceStatus);

        public static bool IsSuccessfulFinalStackEvent(this StackEvent evt)
            => (evt.ResourceType == "AWS::CloudFormation::Stack")
                && ((evt.ResourceStatus == "CREATE_COMPLETE") || (evt.ResourceStatus == "UPDATE_COMPLETE"));
   }
}