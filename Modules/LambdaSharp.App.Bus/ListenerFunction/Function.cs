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
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using LambdaSharp.App.Bus.Events;
using LambdaSharp.App.Bus.Events.Records;
using LambdaSharp.App.Bus.Events.Actions;
using LambdaSharp.App.Bus.Events.Payloads;
using LambdaSharp;
using LambdaSharp.ApiGateway;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using LambdaSharp.App.Bus.Protocol;
using System.Text.Json;

namespace LambdaSharp.App.Bus.ListenerFunction {

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Types ---
        private class ConnectionHeader {

            //--- Properties ---
            public string Host { get; set; }
            public string AppId { get; set; }
            public string Authorization { get; set; }
        }

        //--- Class Methods ---
        private static string ComputeMD5Hash(string text) {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("X2")));
        }

        //--- Fields ---
        private IAmazonSimpleNotificationService _snsClient;
        private IConnectionsDataAccess _connectionsClient;
        private IRulesDataAccess _rulesClient;
        private string _eventTopicArn;
        private string _broadcastApiUrl;
        private string _httpApiToken;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            var dataTableName = config.ReadDynamoDBTableName("DataTable");
            _eventTopicArn = config.ReadText("EventTopic");
            _broadcastApiUrl = config.ReadText("EventBroadcastApiUrl");
            _httpApiToken = config.ReadText("HttpApiInvocationToken");

            // initialize clients
            _snsClient = new AmazonSimpleNotificationServiceClient();
            var dataAccessClient = new DataAccessClient(dataTableName);
            _connectionsClient = dataAccessClient;
            _rulesClient = dataAccessClient;
        }

        // [Route("$connect")]
        public async Task<APIGatewayProxyResponse> OpenConnectionAsync(APIGatewayProxyRequest request) {
            var connectionId = request.RequestContext.ConnectionId;
            LogInfo($"Connection from '{connectionId}'");

            // verify client authorization
            string headerBase64Json = null;
            if(!(request.QueryStringParameters?.TryGetValue("header", out headerBase64Json) ?? false)) {

                // reject connection request
                return new APIGatewayProxyResponse {
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };
            }
            ConnectionHeader header;
            try {
                var headerJson = Encoding.UTF8.GetString(Convert.FromBase64String(headerBase64Json));
                header = LambdaSerializer.Deserialize<ConnectionHeader>(headerJson);
            } catch {

                // reject connection request
                return new APIGatewayProxyResponse {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
            if(
                (header.Host != request.RequestContext.DomainName)
                || !Guid.TryParse(header.AppId, out _)
            ) {

                // reject connection request
                return new APIGatewayProxyResponse {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }

            // create new connection record
            await _connectionsClient.CreateConnectionRecordAsync(new ConnectionRecord {
                ConnectionId = connectionId,
                AppId = header.AppId,
                Claims = request.RequestContext.Authorizer,
                Expiration = DateTimeOffset.UtcNow.AddHours(3)
            });
            return new APIGatewayProxyResponse {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // [Route("$disconnect")]
        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            var connectionId = request.RequestContext.ConnectionId;
            LogInfo($"Disconnection from '{connectionId}'");

            // retrieve connection record
            var connection = await _connectionsClient.DeleteConnectionRecordAsync(connectionId);
            if(connection == null) {
                LogInfo("Connection was already removed");
                return;
            }

            // clean-up rules associated with websocket connection
            RunTask(() => UnsubscribeAsync(connection.SubscriptionArn));
            RunTask(() => _rulesClient.DeleteAllRuleRecordAsync(connectionId));
        }

        // [Route("$default")]
        public async Task<BusAck> UnrecognizedActionAsync(BusAction action) {
            LogInfo($"Unrecognized action '{action.Action ?? "<null>"}' from '{CurrentRequest.RequestContext.ConnectionId}'");
            return action.AcknowledgeNotFound($"unrecognized action: {action.Action ?? "<null>"}");
        }

        // [Route("Events/Subscribe")]
        public async Task<BusAck> SubscribeAsync(BusAction action) {
            var connectionId = CurrentRequest.RequestContext.ConnectionId;
            LogInfo($"Action 'Subscribe' from '{connectionId}'");

            // validate request
            if(action.ContentType != SubscribePayload.MIME_TYPE) {
                return action.AcknowledgeBadRequest("missing or invalid content type");
            }
            if(action.Body is null) {
                return action.AcknowledgeBadRequest("missing body");
            }

            // validate request body
            SubscribePayload body;
            try {
                body = LambdaSerializer.Deserialize<SubscribePayload>(action.Body);
                if(string.IsNullOrEmpty(body.Rule)) {
                    return action.AcknowledgeBadRequest("missing or invalid rule name");
                }
            } catch(Exception e) {
                LogErrorAsInfo(e, "Failed to parse subscription body");
                return action.AcknowledgeBadRequest("invalid body");
            }

            // validate pattern
            var validPattern = false;
            try {
                validPattern = EventPatternMatcher.IsValid(JObject.Parse(body.Pattern));
            } catch(Exception e) {
                LogErrorAsInfo(e, "Failed to validate subscription pattern");
            }
            if(!validPattern) {
                return action.AcknowledgeBadRequest("invalid pattern");
            }

            // create rule
            var created = await _rulesClient.CreateRuleRecordAsync(new RuleRecord {
                Name = body.Rule,
                Pattern = body.Pattern,
                ConnectionId = connectionId,
                Expiration = DateTimeOffset.UtcNow.AddHours(3)
            });

            // the rule record already existed
            if(!created) {
                return action.AcknowledgeBadRequest("rule already exists");
            }

            // retrieve connection record and increase rules counter
            var connection = await _connectionsClient.GetConnectionRecordAndIncreaseRulesCounterAsync(connectionId);
            if(connection == null) {
                LogInfo("Connection was removed or the rules count was exceeded");
                try {
                    await _rulesClient.DeleteRuleRecordAsync(connectionId, body.Rule);
                } catch { }
                return action.AcknowledgeBadRequest("invalid operation for connection");
            }

            // for the first rule, we need to subscribe the broadcast function to the event topic for this websocket
            if(connection.RulesCounter == 1) {

                // subscribe websocket to SNS topic notifications
                try {

                    // retrieve audience claim from authorization context
                    if(!CurrentRequest.RequestContext.Authorizer.TryGetValue("omnibus:aud", out var audienceJson)) {
                        audienceJson = "{}";
                    }

                    // check audience is a valid JSON document
                    try {
                        JsonSerializer.Deserialize<object>(audienceJson as string);
                    } catch {
                        return action.AcknowledgeBadRequest("invalid audience claim");
                    }
                    var audience = Convert.ToBase64String(Encoding.UTF8.GetBytes(audienceJson as string));

                    // subscribe broadcast endpoint to SNS topic
                    await _snsClient.SubscribeAsync(new SubscribeRequest {
                        Protocol = "https",
                        Endpoint = $"{_broadcastApiUrl}?ws={connectionId}&token={_httpApiToken}&rid={action.RequestId}&aud={audience}",
                        TopicArn = _eventTopicArn
                    });
                } catch(Exception e) {
                    LogError(e, "failed to subscribe to SNS topic");
                    await _connectionsClient.GetConnectionRecordAndDecreaseRulesCounterAsync(connectionId);
                    throw Abort(action.AcknowledgeInternalError("internal error"));
                }
            }
            return action.AcknowledgeOk();
        }

        // [Route("Events/Unsubscribe")]
        public async Task<BusAck> UnsubscribeAsync(BusAction action) {
            var connectionId = CurrentRequest.RequestContext.ConnectionId;
            LogInfo($"Action 'Unsubscribe' from '{connectionId}'");

            // validate request
            if(action.ContentType != UnsubscribePayload.MIME_TYPE) {
                return action.AcknowledgeBadRequest("missing or invalid content type");
            }
            if(action.Body == null) {
                return action.AcknowledgeBadRequest("missing or invalid body");
            }

            // validate request body
            var body = LambdaSerializer.Deserialize<UnsubscribePayload>(action.Body);
            if(string.IsNullOrEmpty(body.Rule)) {
                return action.AcknowledgeBadRequest("missing or invalid rule name");
            }

            // confirm rule exists by deleting it
            if(!await _rulesClient.DeleteRuleRecordAsync(connectionId, body.Rule)) {
                return action.AcknowledgeNotFound("rule not found");
            }

            // retrieve connection record and decrease rules counter
            var connection = await _connectionsClient.GetConnectionRecordAndDecreaseRulesCounterAsync(connectionId);
            if(connection == null) {
                LogInfo("Connection was removed or the rules count is zero");
                return action.AcknowledgeBadRequest("invalid operation for connection");
            }

            // for the last rule, we need to unsubscribe the broadcast function
            if(connection.RulesCounter == 0) {
                RunTask(() => _connectionsClient.DeleteConnectionRecordSubscriptionAsync(connectionId));
                RunTask(() => UnsubscribeAsync(connection.SubscriptionArn));
            }
            return action.AcknowledgeOk();
        }

        // [Route("KeepAlive")]
        public async Task<BusAck> KeepAliveAsync(BusAction action) {
            LogInfo($"KeepAlive action '{action.Action ?? "<null>"}' from '{CurrentRequest.RequestContext.ConnectionId}'");
            return action.AcknowledgeOk();
        }

        private Exception Abort(BusAck acknowledgeAction)
            => Abort(new APIGatewayProxyResponse {
                StatusCode = 200,
                Body = LambdaSerializer.Serialize(acknowledgeAction),
                Headers = new Dictionary<string, string> {
                    ["ContentType"] = "application/json"
                }
            });

        private async Task UnsubscribeAsync(string subscriptionArn) {
            if(!(subscriptionArn is null)) {
                try {
                    await _snsClient.UnsubscribeAsync(subscriptionArn);
                } catch(Exception e) {
                    LogErrorAsWarning(e, "failed to unsubscribe (subscription ARN: {0})", subscriptionArn);
                }
            }
        }
    }
}