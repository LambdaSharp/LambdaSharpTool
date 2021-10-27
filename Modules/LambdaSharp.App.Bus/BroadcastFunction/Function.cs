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
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SNSEvents;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using LambdaSharp.App.Bus.Events;
using LambdaSharp.App.Bus.Events.Payloads;
using Newtonsoft.Json.Linq;
using LambdaSharp.App.Bus.Protocol;

namespace LambdaSharp.App.Bus.BroadcastFunction {

    public sealed class Function : ALambdaFunction<APIGatewayHttpApiV2ProxyRequest, APIGatewayHttpApiV2ProxyResponse> {

        //--- Constants ---
        private const string QUERY_PARAMETER_INVOCATION_TOKEN = "token";
        private const string QUERY_PARAMETER_WEBSOCKET_CONNECTION_ID = "ws";
        private const string QUERY_PARAMETER_REQUEST_ID = "rid";
        private const string QUERY_PARAMETER_AUDIENCE = "aud";

        //--- Fields ---
        private IAmazonSimpleNotificationService _snsClient;
        private IAmazonApiGatewayManagementApi _amaClient;
        private IConnectionsDataAccess _connectionsClient;
        private IRulesDataAccess _rulesClient;
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
            var dataAccessClient = new DataAccessClient(dataTableName);
            _connectionsClient = dataAccessClient;
            _rulesClient = dataAccessClient;
        }

        public override async Task<APIGatewayHttpApiV2ProxyResponse> ProcessMessageAsync(APIGatewayHttpApiV2ProxyRequest request) {
            LogInfo($"Message received at {request.RequestContext.Http.Method}:{request.RawPath}?{request.RawQueryString}");

            // validate invocation method
            if(request.RequestContext.Http.Method != "POST") {
                LogWarn("Unsupported request method {0}", request.RequestContext.Http.Method);
                return BadRequestResponse();
            }

            // validate request token
            if(
                !request.QueryStringParameters.TryGetValue(QUERY_PARAMETER_INVOCATION_TOKEN, out var token)
                || (token != _httpApiToken)
            ) {
                LogWarn("Missing or invalid request token");
                return BadRequestResponse();
            }

            // validate request websocket
            if(
                !request.QueryStringParameters.TryGetValue(QUERY_PARAMETER_WEBSOCKET_CONNECTION_ID, out var connectionId)
                || string.IsNullOrEmpty(connectionId)
            ) {
                LogWarn("Missing or invalid websocket connection id");
                return BadRequestResponse();
            }

            // validate request id
            if(
                !request.QueryStringParameters.TryGetValue(QUERY_PARAMETER_REQUEST_ID, out var requestId)
                || string.IsNullOrEmpty(requestId)
            ) {
                LogWarn("Missing or invalid request id");
                return BadRequestResponse();
            }

            // validate claims
            if(!request.QueryStringParameters.TryGetValue(QUERY_PARAMETER_AUDIENCE, out var audienceParameter)) {
                LogWarn("Missing audience");
                return BadRequestResponse();
            }
            JObject audience;
            try {
                audience = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(audienceParameter)));
            } catch(Exception e) {
                LogErrorAsWarning(e, "Invalid audience value");
                return BadRequestResponse();
            }

            // check if request is a subscription confirmation
            var topicSubscription = LambdaSerializer.Deserialize<TopicSubscriptionPayload>(request.Body);
            if(topicSubscription.Type == "SubscriptionConfirmation") {
                return await ConfirmSubscription(connectionId, requestId, topicSubscription);
            }

            // validate SNS message
            SNSEvent.SNSMessage snsMessage = null;
            try {
                snsMessage = LambdaSerializer.Deserialize<SNSEvent.SNSMessage>(request.Body);
            } catch {  }
            if(snsMessage == null) {
                LogWarn("Invalid SNS message received: {0}", request.Body);
                return BadRequestResponse();
            }

            // validate EventBridge event
            BusEvent busEvent = null;
            try {
                busEvent = LambdaSerializer.Deserialize<BusEvent>(snsMessage.Message);
            } catch { }
            if(
                (busEvent == null)
                || (busEvent.Id == null)
                || (busEvent.Source == null)
                || (busEvent.Time == default)
                || (busEvent.AudienceScope == null)
            ) {
                LogInfo("Invalid EventBridge event received: {0}", snsMessage.Message);
                return BadRequestResponse();
            }

            // validate the audience matches the audience scope
            try {
                LogInfo("Checking audience scope: {0}", busEvent.AudienceScope);
                var audienceScope = JObject.Parse(busEvent.AudienceScope);
                if(!EventPatternMatcher.IsMatch(audience, audienceScope)) {
                    return SuccessResponse("AudienceScope mismatch");
                }
            } catch(Exception e) {
                LogError(e, "Invalid AudienceScope in EventBridge event: {0}", snsMessage.Message);
                return BadRequestResponse();
            }
            return await DispatchEvent(connectionId, busEvent, snsMessage.Message);
        }

        private async Task SendActionToConnection(BusAction action, string connectionId) {
            var json = LambdaSerializer.Serialize<object>(action);
            if(DebugLoggingEnabled) {
                LogDebug($"Post to connection: {connectionId}: {{0}}", json);
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

        private async Task<APIGatewayHttpApiV2ProxyResponse> ConfirmSubscription(string connectionId, string requestId, TopicSubscriptionPayload topicSubscription) {

            // confirm it's for the expected topic ARN
            if(topicSubscription.TopicArn != _eventTopicArn) {
                LogWarn("Wrong Topic ARN for subscription confirmation (Expected: {0}, Received: {1})", _eventTopicArn, topicSubscription.TopicArn);
                return BadRequestResponse();
            }

            // retrieve connection record
            var connection = await _connectionsClient.GetConnectionRecordAsync(connectionId);
            if(connection == null) {
                return GoneResponse("connection is gone");
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
                return InternalServerErrorResponse();
            }
            if(!await _connectionsClient.UpdateConnectionRecordSubscriptionAsync(connection, subscriptionArn)) {

                // unsubscribe since we couldn't save the subscription ARN
                try {
                    await _snsClient.UnsubscribeAsync(subscriptionArn);
                } catch(Exception e) {
                    LogErrorAsWarning(e, "failed to unsubscribe (subscription ARN: {0})", connection.SubscriptionArn);
                }

                // mark connection as failed and report error
                return InternalServerErrorResponse();
            }
            return SuccessResponse("Confirmed");
        }

        private async Task<APIGatewayHttpApiV2ProxyResponse> DispatchEvent(string connectionId, BusEvent busEvent, string originalbusEvent) {

            // check if the keep-alive event was received
            if(busEvent.Source == "LambdaSharp.App.Bus::KeepAlive") {

                // send keep-alive action to websocket connection
                LogInfo("KeepAlive tick");
                await SendActionToConnection(new BusAction {
                    Action = BusKeepAlivePayload.ACTION
                }, connectionId);
                return SuccessResponse("Ok");
            }

            // determine what rules are matching the event
            JObject busEventObject;
            LogInfo("matching event: {0}", originalbusEvent);
            try {
                busEventObject = JObject.Parse(originalbusEvent);
            } catch(Exception e) {
                LogError(e, "invalid event");
                return BadRequestResponse();
            }
            var rules = await _rulesClient.GetAllRuleRecordAsync(connectionId);
            var matchedRules = rules
                .Where(rule => {
                    try {
                        LogInfo("matching rule '{0}': {1}", rule.Name, rule.Pattern);
                        var pattern = JObject.Parse(rule.Pattern);
                        var result = EventPatternMatcher.IsMatch(busEventObject, pattern);
                        LogInfo("match: {0}", result);
                        return result;
                    } catch(Exception e) {
                        LogError(e, "invalid event pattern: {0}", rule.Pattern);
                        return false;
                    }
                }).Select(rule => rule.Name)
                .ToList();
            if(matchedRules.Any()) {
                LogInfo($"Event matched {matchedRules.Count():N0} rules: {string.Join(", ", matchedRules)}");
                await SendActionToConnection(
                    new BusAction {
                        Action = EventPayload.ACTION,
                        ContentType = busEvent.ContentType,
                        RequestId = busEvent.Id,
                        Body = LambdaSerializer.Serialize(new EventPayload {
                            Id = busEvent.Id,
                            Source = busEvent.Source,
                            Time = busEvent.Time,
                            ContentType = busEvent.ContentType,
                            Body = busEvent.Body
                        }),
                        Headers = new Dictionary<string, string> {
                            ["MatchedRules"] = string.Join(", ", matchedRules),
                        }
                    },
                    connectionId
                );
            } else {
                LogInfo("Event matched no rules");
            }
            return SuccessResponse("Ok");
        }

        private APIGatewayHttpApiV2ProxyResponse SuccessResponse(string message)
            => new APIGatewayHttpApiV2ProxyResponse {
                Body = message,
                Headers = new Dictionary<string, string> {
                    ["Content-Type"] = "text/plain"
                },
                StatusCode = (int)HttpStatusCode.OK
            };

        private APIGatewayHttpApiV2ProxyResponse BadRequestResponse()
            => new APIGatewayHttpApiV2ProxyResponse {
                Body = "Bad Request",
                Headers = new Dictionary<string, string> {
                    ["Content-Type"] = "text/plain"
                },
                StatusCode = (int)HttpStatusCode.BadRequest
            };

        private APIGatewayHttpApiV2ProxyResponse GoneResponse(string message)
            => new APIGatewayHttpApiV2ProxyResponse {
                Body = message,
                Headers = new Dictionary<string, string> {
                    ["Content-Type"] = "text/plain"
                },
                StatusCode = (int)HttpStatusCode.Gone
            };

        private APIGatewayHttpApiV2ProxyResponse InternalServerErrorResponse()
            => new APIGatewayHttpApiV2ProxyResponse {
                Body = "Internal Error",
                Headers = new Dictionary<string, string> {
                    ["Content-Type"] = "text/plain"
                },
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
    }
}
