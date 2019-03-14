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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using LambdaSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LambdaSharp.Demo.WebSocketsChat.Common;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace WebSocketsSample.MessageFunction {

    public class Message {

        //--- Properties ---
        [JsonProperty("action")]
        public string Action { get; set; } = "send";

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private ConnectionsTable _connections;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _connections = new ConnectionsTable(
                config.ReadDynamoDBTableName("ConnectionsTable"),
                new AmazonDynamoDBClient()
            );
        }

        public async Task SendMessageAsync(Message request) {

            // enumerate open connections
            var connections = await _connections.GetAllRowsAsync();
            LogInfo($"Found {connections.Count()} open connection(s)");

            // attempt to send message on all open connections
            var messageBytes = Encoding.UTF8.GetBytes(SerializeJson(new Message {
                From = request.From,
                Text = request.Text
            }));
            await Task.WhenAll(connections
                .Select(async (connectionId, index) => {
                    LogInfo($"Post to connection {index}: {connectionId}");
                    try {
                        await WebSocketClient.PostToConnectionAsync(new PostToConnectionRequest {
                            ConnectionId = connectionId,
                            Data = new MemoryStream(messageBytes)
                        });
                        return true;
                    } catch(AmazonServiceException e) {

                        // API Gateway returns a status of 410 GONE when the connection is no
                        // longer available. If this happens, we need to delete the identifier
                        // from our DynamoDB table.
                        if(e.StatusCode == HttpStatusCode.Gone) {
                            LogInfo($"Deleting gone connection: {connectionId}");
                            await _connections.DeleteRowAsync(connectionId);
                        } else {
                            LogError(e, "PostToConnectionAsync() failed");
                        }
                        return false;
                    }
                })
            );
        }

        public APIGatewayProxyResponse UnrecognizedRequest(APIGatewayProxyRequest request) {
            try {
                var json = JObject.Parse(request.Body);
                var action = (string)json["action"];
                if(action != null) {
                    return CreateStatusResponse(404, $"Unrecognized action '{action}'");
                } else {
                    return CreateStatusResponse(404, $"Request is missing 'action' field");
                }
            } catch {
                return CreateStatusResponse(404, $"Request must be a JSON object");
            }
        }
    }
}
