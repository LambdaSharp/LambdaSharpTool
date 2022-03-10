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

namespace WebSocketsSample.ConnectionFunction;

using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using LambdaSharp;
using LambdaSharp.ApiGateway;
using Demo.WebSocketsChat.Common;

public sealed class Function : ALambdaApiGatewayFunction {

    //--- Fields ---
    private ConnectionsTable? _connections;

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Properties ---
    private ConnectionsTable Connections => _connections ?? throw new InvalidOperationException();

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {
        _connections = new ConnectionsTable(
            config.ReadDynamoDBTableName("ConnectionsTable"),
            new AmazonDynamoDBClient()
        );
    }

    public async Task OpenConnectionAsync(APIGatewayProxyRequest request) {
        try {
            LogInfo($"Connected: {request.RequestContext.ConnectionId} [{request.RequestContext.RouteKey}]");
            await Connections.InsertRowAsync(request.RequestContext.ConnectionId);
        } catch(Exception e) {
            LogError(e);
            throw Abort(CreateResponse(500, $"Failure while attempting to connect: {e.Message}"));
        }
    }

    public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
        try {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId} [{request.RequestContext.RouteKey}]");
            await Connections.DeleteRowAsync(request.RequestContext.ConnectionId);
        } catch(Exception e) {
            LogError(e);
            throw Abort(CreateResponse(500, $"Failure while attempting to disconnect: {e.Message}"));
        }
    }
}
