using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using LambdaSharp.App.EventBus.Records;
using LambdaSharp.App.EventBus.Actions;
using LambdaSharp.ApiGateway;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.App.EventBus.ListenerFunction {

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Types ---
        private class ConnectionHeader {

            //--- Properties ---
            public string Host { get; set; }
            public string ApiKey { get; set; }
            public string Id { get; set; }
        }

        //--- Class Methods ---
        private static string ComputeMD5Hash(string text) {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("X2")));
        }

        //--- Fields ---
        private IAmazonSimpleNotificationService _snsClient;
        private DataTable _dataTable;
        private string _eventTopicArn;
        private string _broadcastApiUrl;
        private string _httpApiToken;
        private string _clientApiKey;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            var dataTableName = config.ReadDynamoDBTableName("DataTable");
            _eventTopicArn = config.ReadText("EventTopic");
            _broadcastApiUrl = config.ReadText("EventBroadcastApiUrl");
            _httpApiToken = config.ReadText("HttpApiInvocationToken");
            _clientApiKey = config.ReadText("ClientApiKey");

            // initialize AWS clients
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _dataTable = new DataTable(dataTableName, new AmazonDynamoDBClient());
        }

        // [Route("$connect")]
        public async Task<APIGatewayProxyResponse> OpenConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Connected: {request.RequestContext.ConnectionId}");

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
                || (header.ApiKey != _clientApiKey)
                || !Guid.TryParse(header.Id, out _)
            ) {

                // reject connection request
                return new APIGatewayProxyResponse {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }

            // create new connection record
            await _dataTable.CreateConnectionAsync(new ConnectionRecord {
                ConnectionId = request.RequestContext.ConnectionId,
                ApplicationId = header.Id,
                Bearer = request.RequestContext.Authorizer?.Claims
            });
            return new APIGatewayProxyResponse {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // [Route("$disconnect")]
        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId}");

            // retrieve websocket connection record
            var connection = await _dataTable.GetConnectionAsync(request.RequestContext.ConnectionId);
            if(connection == null) {
                LogInfo("Connection was already removed");
                return;
            }

            // clean-up resources associated with websocket connection
            await Task.WhenAll(new Task[] {

                // unsubscribe from SNS topic notifications
                Task.Run(async () => {
                    if(connection.SubscriptionArn != null) {
                        await _snsClient.UnsubscribeAsync(new UnsubscribeRequest {
                            SubscriptionArn = connection.SubscriptionArn
                        });
                    }
                }),

                // delete all event rules for this websocket connection
                Task.Run(async () => {

                    // fetch all rules associated with websocket connection
                    var rules = await _dataTable.GetConnectionRulesAsync(request.RequestContext.ConnectionId);

                    // delete rules
                    await _dataTable.DeleteAllRulesAsync(rules);
                }),

                // delete websocket connection record
                _dataTable.DeleteConnectionAsync(connection.ConnectionId)
            });
        }

        // [Route("Hello")]
        public async Task HelloAsync(HelloAction action) {
            var connectionId = CurrentRequest.RequestContext.ConnectionId;
            LogInfo($"Hello: {connectionId}");

            // retrieve websocket connection record
            var connection = await _dataTable.GetConnectionAsync(connectionId);
            if(connection == null) {
                LogInfo("Connection was removed");
                return;
            }

            // subscribe websocket to SNS topic notifications
            connection.SubscriptionArn = (await _snsClient.SubscribeAsync(new SubscribeRequest {
                Protocol = "https",
                Endpoint = $"{_broadcastApiUrl}?ws={connectionId}&token={_httpApiToken}&rid={action.RequestId}",
                ReturnSubscriptionArn = true,
                TopicArn = _eventTopicArn
            })).SubscriptionArn;

            // update connection record
            await _dataTable.UpdateConnectionAsync(connection);
        }

        // [Route("Subscribe")]
        public async Task<AcknowledgeAction> SubscribeAsync(SubscribeAction action) {
            var connectionId = CurrentRequest.RequestContext.ConnectionId;
            LogInfo($"Subscribe request from: {connectionId}");

            // validate request
            if(string.IsNullOrEmpty(action.Rule)) {
                return new AcknowledgeAction {
                    RequestId = action.RequestId,
                    Status = "Error",
                    Message = "Missing or invalid rule name"
                };
            }

            // retrieve websocket connection record
            var connection = await _dataTable.GetConnectionAsync(connectionId);
            if(connection == null) {
                LogInfo("Connection was removed");
                return new AcknowledgeAction {
                    RequestId = action.RequestId,
                    Rule = action.Rule,
                    Status = "Error",
                    Message = "Connection gone"
                };
            }

            // validate pattern
            var validPattern = false;
            try {
                validPattern = EventPatternMatcher.IsValid(JObject.Parse(action.Pattern));
            } catch {

                // nothing to do
            }
            if(!validPattern) {
                return new AcknowledgeAction {
                    RequestId = action.RequestId,
                    Rule = action.Rule,
                    Status = "Error",
                    Message = "Invalid pattern"
                };
            }

            // create or update event rule
            await _dataTable.CreateOrUpdateRuleAsync(new RuleRecord {
                Rule = action.Rule,
                Pattern = action.Pattern,
                ConnectionId = connection.ConnectionId
            });
            return new AcknowledgeAction {
                RequestId = action.RequestId,
                Rule = action.Rule,
                Status = "Ok"
            };
        }

        // [Route("Unsubscribe")]
        public async Task<AcknowledgeAction> UnsubscribeAsync(UnsubscribeAction action) {
            var connectionId = CurrentRequest.RequestContext.ConnectionId;
            LogInfo($"Unsubscribe request from: {connectionId}");
            if(action.Rule != null) {

                // delete event rule
                await _dataTable.DeleteRuleAsync(connectionId, action.Rule);
            }
            return new AcknowledgeAction {
                RequestId = action.RequestId,
                Rule = action.Rule,
                Status = "Ok"
            };
        }
    }
}