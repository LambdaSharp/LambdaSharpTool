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

namespace LambdaSharp.App.EventBus.BroadcastFunction;

using System.Net;
using System.Text;
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

public sealed class Function : ALambdaFunction<APIGatewayHttpApiV2ProxyRequest, APIGatewayHttpApiV2ProxyResponse> {

    //--- Fields ---
    private IAmazonSimpleNotificationService? _snsClient;
    private IAmazonApiGatewayManagementApi? _amaClient;
    private DataTable? _dataTable;
    private string? _eventTopicArn;
    private string? _keepAliveRuleArn;
    private string? _httpApiToken;
    private XmlNamespaceManager? _xmlNamespaces;

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Properties ---
    private IAmazonSimpleNotificationService SnsClient => _snsClient ?? throw new InvalidOperationException();
    private IAmazonApiGatewayManagementApi AmaClient => _amaClient ?? throw new InvalidOperationException();
    private DataTable DataTable => _dataTable ?? throw new InvalidOperationException();
    private string EventTopicArn => _eventTopicArn ?? throw new InvalidOperationException();
    private string KeepAliveRuleArn => _keepAliveRuleArn ?? throw new InvalidOperationException();
    private string HttpApiToken => _httpApiToken ?? throw new InvalidOperationException();
    private XmlNamespaceManager XmlNamespaces => _xmlNamespaces ?? throw new InvalidOperationException();

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
            LogWarn("Unsupported request method {0}", request.RequestContext.Http.Method);
            return BadRequestResponse();
        }

        // validate request token
        if(
            !request.QueryStringParameters.TryGetValue("token", out var token)
            || (token != HttpApiToken)
        ) {
            LogWarn("Missing or invalid request token");
            return BadRequestResponse();
        }

        // validate request websocket
        if(
            !request.QueryStringParameters.TryGetValue("ws", out var connectionId)
            || string.IsNullOrEmpty(connectionId)
        ) {
            LogWarn("Invalid websocket connection id");
            return BadRequestResponse();
        }

        // validate request id
        if(
            !request.QueryStringParameters.TryGetValue("rid", out var requestId)
            || string.IsNullOrEmpty(requestId)
        ) {
            LogWarn("Invalid request id");
            return BadRequestResponse();
        }

        // check if request is a subscription confirmation
        var topicSubscription = LambdaSerializer.Deserialize<TopicSubscriptionPayload>(request.Body);
        if(topicSubscription.Type == "SubscriptionConfirmation") {
            return await ConfirmSubscription(connectionId, requestId, topicSubscription);
        }

        // validate SNS message
        var snsMessage = LambdaSerializer.Deserialize<SNSEvent.SNSMessage>(request.Body);
        if(snsMessage.Message == null) {
            LogWarn("Invalid SNS message received: {0}", request.Body);
            return BadRequestResponse();
        }

        // validate EventBridge event
        var eventBridgeEvent = LambdaSerializer.Deserialize<EventBridgeventPayload>(snsMessage.Message);
        if(
            (eventBridgeEvent.Source == null)
            || (eventBridgeEvent.DetailType == null)
            || (eventBridgeEvent.Resources == null)
        ) {
            LogInfo("Invalid EventBridge event received: {0}", snsMessage.Message);
            return BadRequestResponse();
        }
        return await DispatchEvent(connectionId, snsMessage.Message);
    }

    private async Task SendActionToConnection(AAction action, string connectionId) {
        var json = LambdaSerializer.Serialize<object>(action);
        if(DebugLoggingEnabled) {
            LogDebug($"Post to connection: {connectionId}\n{{0}}", json);
        } else {
            LogInfo($"Post to connection: {connectionId}");
        }

        // attempt to send serialized message to connection
        var messageBytes = Encoding.UTF8.GetBytes(json);
        try {
            await AmaClient.PostToConnectionAsync(new PostToConnectionRequest {
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
            LogWarn("Wrong Topic ARN for subscription confirmation (Expected: {0}, Received: {1})", EventTopicArn, topicSubscription.TopicArn ?? "<null>");
            return BadRequestResponse();
        }

        // retrieve connection record
        var connection = await DataTable.GetConnectionRecordAsync(connectionId);
        if(connection == null) {
            await SendActionToConnection(new AcknowledgeAction {
                RequestId = requestId,
                Status = "Error",
                Message = "connection gone"
            }, connectionId);
            return InternalServerErrorResponse();
        }
        if(connection.State == ConnectionState.Failed) {
            LogInfo("Connection is in failed state");
            return SuccessResponse("Failed state");
        }
        if(connection.State != ConnectionState.Pending) {
            LogWarn("Connection is not in pending state (state: {0})", connection.State);
            return BadRequestResponse();
        }

        // confirm subscription
        string subscriptionArn;
        try {
            using var response = await HttpClient.GetAsync(topicSubscription.SubscribeURL);
            var xmlResponse = XDocument.Parse(await response.Content.ReadAsStringAsync());
            subscriptionArn = xmlResponse?.Document
                ?.XPathSelectElement("sns:ConfirmSubscriptionResponse/sns:ConfirmSubscriptionResult/sns:SubscriptionArn", XmlNamespaces)
                ?.Value ?? throw new InvalidOperationException("missing subscription ARN");
            LogInfo("Subscription confirmed: {0}", subscriptionArn);
        } catch(Exception e) {
            LogError(e, "Unable to confirm subscription (topic: {0}, url: {1})", topicSubscription.TopicArn ?? "<null>", topicSubscription.SubscribeURL ?? "<null>");

            // mark connection as failed and report error
            await DataTable.SetConnectionRecordStateAsync(connection, ConnectionState.Failed);
            await SendActionToConnection(new AcknowledgeAction {
                RequestId = requestId,
                Status = "Error",
                Message = "internal error"
            }, connectionId);
            return InternalServerErrorResponse();
        }
        if(!await DataTable.UpdateConnectionRecordStateAndSubscriptionAsync(connection, ConnectionState.Open, subscriptionArn)) {

            // unsubscribe since we couldn't save the subscription ARN
            try {
                await SnsClient.UnsubscribeAsync(subscriptionArn);
            } catch(Exception e) {
                LogErrorAsWarning(e, "failed to unsubscribe (subscription ARN: {0})", connection.SubscriptionArn ?? "<null>");
            }

            // mark connection as failed and report error
            await DataTable.SetConnectionRecordStateAsync(connection, ConnectionState.Failed);
            await SendActionToConnection(new AcknowledgeAction {
                RequestId = requestId,
                Status = "Error",
                Message = "internal error"
            }, connectionId);
            return InternalServerErrorResponse();
        }

        // send `AcknowledgeAction` to websocket connection
        await SendActionToConnection(new AcknowledgeAction {
            RequestId = requestId,
            Status = "Ok"
        }, connectionId);
        return SuccessResponse("Confirmed");
    }

    private async Task<APIGatewayHttpApiV2ProxyResponse> DispatchEvent(string connectionId, string message) {

        // validate EventBridge event
        var eventBridgeEvent = LambdaSerializer.Deserialize<EventBridgeventPayload>(message);
        if(
            (eventBridgeEvent.Source == null)
            || (eventBridgeEvent.DetailType == null)
            || (eventBridgeEvent.Resources == null)
        ) {
            LogInfo("Invalid EventBridge event received: {0}", message);
            return BadRequestResponse();
        }

        // check if the keep-alive event was received
        if(
            (eventBridgeEvent.Source == "aws.events")
            && (eventBridgeEvent.DetailType == "Scheduled Event")
            && (eventBridgeEvent.Resources?.Count == 1)
            && (eventBridgeEvent.Resources[0] == KeepAliveRuleArn)
        ) {

            // retrieve connection record
            var connection = await DataTable.GetConnectionRecordAsync(connectionId);
            if(connection == null) {
                return SuccessResponse("Gone");
            }
            if(connection.State != ConnectionState.Open) {
                return SuccessResponse("Ignored");
            }

            // send keep-alive action to websocket connection
            LogInfo("KeepAlive tick");
            await SendActionToConnection(new KeepAliveAction(), connectionId);
            return SuccessResponse("Ok");
        }

        // determine what rules are matching the event
        JObject evt;
        try {
            evt = JObject.Parse(message);
        } catch(Exception e) {
            LogError(e, "invalid message");
            return BadRequestResponse();
        }
        var rules = await DataTable.GetAllRuleRecordAsync(connectionId);
        var matchedRules = rules
            .Where(rule => {
                try {
                    var pattern = JObject.Parse(rule.Pattern!);
                    return EventPatternMatcher.IsMatch(evt, pattern);
                } catch(Exception e) {
                    LogError(e, "invalid event pattern: {0}", rule.Pattern ?? "<null>");
                    return false;
                }
            }).Select(rule => rule.Rule)
            .ToList();
        if(matchedRules.Any()) {
            LogInfo($"Event matched {matchedRules.Count():N0} rules: {string.Join(", ", matchedRules)}");
            await SendActionToConnection(
                new EventAction {
                    Rules = matchedRules,
                    Source = eventBridgeEvent.Source,
                    Type = eventBridgeEvent.DetailType,
                    Event = message
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

    private APIGatewayHttpApiV2ProxyResponse InternalServerErrorResponse()
        => new APIGatewayHttpApiV2ProxyResponse {
            Body = "Internal Error",
            Headers = new Dictionary<string, string> {
                ["Content-Type"] = "text/plain"
            },
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
}
