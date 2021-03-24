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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SNSEvents;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using LambdaSharp.App.EventBus.Actions;
using LambdaSharp.App.EventBus.Records;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.App.EventBus.BroadcastFunction {

    public sealed class Function : ALambdaFunction<APIGatewayHttpApiV2ProxyRequest, APIGatewayHttpApiV2ProxyResponse> {

        //--- Fields ---
        private IAmazonSimpleNotificationService _snsClient;
        private IAmazonApiGatewayManagementApi _amaClient;
        private DataTable _dataTable;
        private string _eventTopicArn;
        private string _keepAliveRuleArn;
        private string _httpApiToken;
        private XmlNamespaceManager _xmlNamespaces;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _xmlNamespaces = new XmlNamespaceManager(new NameTable());
            _xmlNamespaces.AddNamespace("sns", "http://sns.amazonaws.com/doc/2010-03-31/");

            // read configuration settings
            var dataTableName = config.ReadDynamoDBTableName("DataTable");
            var webSocketUrl = config.ReadText("Module::WebSocket::Url");
            _eventTopicArn = config.ReadText("EventTopic");
            _keepAliveRuleArn = config.ReadText("KeepAliveRuleArn");
            _httpApiToken = config.ReadText("HttpApiInvocationToken");

            // initialize clients
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _amaClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                ServiceURL = webSocketUrl
            });
            _dataTable = new DataTable(dataTableName, new AmazonDynamoDBClient());
        }

        public override async Task<APIGatewayHttpApiV2ProxyResponse> ProcessMessageAsync(APIGatewayHttpApiV2ProxyRequest request) {
            LogInfo($"Message received at {request.RequestContext.Http.Method}:{request.RawPath}?{request.RawQueryString}");

            // validate invocation method
            if(request.RequestContext.Http.Method != "POST") {
                LogInfo("Unsupported request method {0}", request.RequestContext.Http.Method);
                return BadRequest();
            }

            // validate request token
            if(
                !request.QueryStringParameters.TryGetValue("token", out var token)
                || (token != _httpApiToken)
            ) {
                LogInfo("Missing or invalid request token");
                return BadRequest();
            }

            // validate request websocket
            if(
                !request.QueryStringParameters.TryGetValue("ws", out var connectionId)
                || string.IsNullOrEmpty(connectionId)
            ) {
                LogInfo("Invalid websocket connection id");
                return BadRequest();
            }

            // validate request id
            if(
                !request.QueryStringParameters.TryGetValue("rid", out var requestId)
                || string.IsNullOrEmpty(requestId)
            ) {
                LogInfo("Invalid request id");
                return BadRequest();
            }

            // check if request is a subscription confirmation
            var topicSubscription = LambdaSerializer.Deserialize<TopicSubscriptionPayload>(request.Body);
            if(topicSubscription.Type == "SubscriptionConfirmation") {

                // confirm it's for the expected topic ARN
                if(topicSubscription.TopicArn != _eventTopicArn) {
                    LogWarn("Wrong Topic ARN for subscription confirmation (Expected: {0}, Received: {1})", _eventTopicArn, topicSubscription.TopicArn);
                    return BadRequest();
                }

                // retrieve connection record
                var connection = await _dataTable.GetConnectionRecordAsync(connectionId);
                if(connection == null) {
                    await SendMessageToConnection(new AcknowledgeAction {
                        RequestId = requestId,
                        Status = "Error",
                        Message = "connection gone"
                    }, connectionId);
                    return InternalServerError();
                }
                if(connection.State == ConnectionState.Failed) {
                    LogInfo("Connection is in failed state");
                    return Success("Failed state");
                }
                if(connection.State != ConnectionState.Pending) {
                    LogWarn("Connection is not in pending state (state: {0})", connection.State);
                    return BadRequest();
                }

                // confirm subscription
                string subscriptionArn;
                try {
                    using var response = await HttpClient.GetAsync(topicSubscription.SubscribeURL);
                    var xmlResponse = XDocument.Parse(await response.Content.ReadAsStringAsync());
                    subscriptionArn = xmlResponse.Document
                        .XPathSelectElement("sns:ConfirmSubscriptionResponse/sns:ConfirmSubscriptionResult/sns:SubscriptionArn", _xmlNamespaces)
                        ?.Value ?? throw new InvalidOperationException("missing subscription ARN");
                    LogInfo("Subscription confirmed: {0}", subscriptionArn);
                } catch(Exception e) {
                    LogError(e, "Unable to confirm subscription (topic: {0}, url: {1})", topicSubscription.TopicArn, topicSubscription.SubscribeURL);

                    // mark connection as failed and report error
                    await _dataTable.SetConnectionRecordStateAsync(connection, ConnectionState.Failed);
                    await SendMessageToConnection(new AcknowledgeAction {
                        RequestId = requestId,
                        Status = "Error",
                        Message = "internal error"
                    }, connectionId);
                    return InternalServerError();
                }
                if(!await _dataTable.UpdateConnectionRecordStateAndSubscriptionAsync(connection, ConnectionState.Open, subscriptionArn)) {

                    // unsubscribe since we couldn't save the subscription ARN
                    try {
                        await _snsClient.UnsubscribeAsync(subscriptionArn);
                    } catch(Exception e) {
                        LogErrorAsWarning(e, "failed to unsubscribe (subscription ARN: {0})", connection.SubscriptionArn);
                    }

                    // mark connection as failed and report error
                    await _dataTable.SetConnectionRecordStateAsync(connection, ConnectionState.Failed);
                    await SendMessageToConnection(new AcknowledgeAction {
                        RequestId = requestId,
                        Status = "Error",
                        Message = "internal error"
                    }, connectionId);
                    return InternalServerError();
                }

                // send `AcknowledgeAction` to websocket connection
                await SendMessageToConnection(new AcknowledgeAction {
                    RequestId = requestId,
                    Status = "Ok"
                }, connectionId);
                return Success("Confirmed");
            }

            // validate SNS message
            var snsMessage = LambdaSerializer.Deserialize<SNSEvent.SNSMessage>(request.Body);
            if(snsMessage.Message == null) {
                LogWarn("Invalid SNS message received: {0}", request.Body);
                return BadRequest();
            }

            // validate CloudWatch event
            var cloudWatchEvent = LambdaSerializer.Deserialize<CloudWatchEventPayload>(snsMessage.Message);
            if(
                (cloudWatchEvent.Source == null)
                || (cloudWatchEvent.DetailType == null)
                || (cloudWatchEvent.Resources == null)
            ) {
                LogInfo("Invalid CloudWatch event received: {0}", snsMessage.Message);
                return BadRequest();
            }

            // check if the keep-alive event was received
            if(
                (cloudWatchEvent.Source == "aws.events")
                && (cloudWatchEvent.DetailType == "Scheduled Event")
                && (cloudWatchEvent.Resources.Count == 1)
                && (cloudWatchEvent.Resources[0] == _keepAliveRuleArn)
            ) {

                // retrieve connection record
                var connection = await _dataTable.GetConnectionRecordAsync(connectionId);
                if(connection == null) {
                    return Success("Gone");
                }
                if(connection.State != ConnectionState.Open) {
                    return Success("Ignored");
                }

                // send keep-alive action to websocket connection
                LogInfo("KeepAlive tick");
                await SendMessageToConnection(new KeepAliveAction(), connectionId);
                return Success("Ok");
            }

            // determine what rules are matching
            JObject evt;
            try {
                evt = JObject.Parse(snsMessage.Message);
            } catch(Exception e) {
                LogError(e, "invalid message");
                return BadRequest();
            }
            var rules = await _dataTable.GetAllRuleRecordAsync(connectionId);
            var matchedRules = rules
                .Where(rule => {
                    try {
                        var pattern = JObject.Parse(rule.Pattern);
                        return EventPatternMatcher.IsMatch(evt, pattern);
                    } catch(Exception e) {
                        LogError(e, "invalid event pattern: {0}", rule.Pattern);
                        return false;
                    }
                }).Select(rule => rule.Rule)
                .ToList();
            if(matchedRules.Any()) {
                await SendMessageToConnection(
                    new EventAction {
                        Rules = matchedRules,
                        Source = cloudWatchEvent.Source,
                        Type = cloudWatchEvent.DetailType,
                        Event = snsMessage.Message
                    },
                    connectionId
                );
            }
            return Success("Ok");

            // local functions
            APIGatewayHttpApiV2ProxyResponse Success(string message)
                => new APIGatewayHttpApiV2ProxyResponse {
                    Body = message,
                    Headers = new Dictionary<string, string> {
                        ["Content-Type"] = "text/plain"
                    },
                    StatusCode = (int)HttpStatusCode.OK
                };

            APIGatewayHttpApiV2ProxyResponse BadRequest()
                => new APIGatewayHttpApiV2ProxyResponse {
                    Body = "Bad Request",
                    Headers = new Dictionary<string, string> {
                        ["Content-Type"] = "text/plain"
                    },
                    StatusCode = (int)HttpStatusCode.BadRequest
                };

            APIGatewayHttpApiV2ProxyResponse InternalServerError()
                => new APIGatewayHttpApiV2ProxyResponse {
                    Body = "Internal Error",
                    Headers = new Dictionary<string, string> {
                        ["Content-Type"] = "text/plain"
                    },
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
        }

        private async Task SendMessageToConnection(AAction action, string connectionId) {
            var json = LambdaSerializer.Serialize<object>(action);
            if(DebugLoggingEnabled) {
                LogDebug($"Post to connection: {connectionId}\n{{0}}", json);
            } else {
                LogInfo($"Post to connection: {connectionId}");
            }

            // attempt to send serialized message to connection
            var messageBytes = Encoding.UTF8.GetBytes(json);
            try {
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = connectionId,
                    Data = new MemoryStream(messageBytes)
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                // HTTP Gone status code indicates the connection has been closed; nothing to do
            } catch(Exception e) {
                LogErrorAsWarning(e, "PostToConnectionAsync() failed on connection {0}", connectionId);
            }
        }
    }
}
