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
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.ApiGateway;
using Demo.WebSocketsChat.Common;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace WebSocketsSample.ConnectionFunction {

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

        public async Task OpenConnectionAsync(APIGatewayProxyRequest request) {
            try {
                LogInfo($"Connected: {request.RequestContext.ConnectionId} [{request.RequestContext.RouteKey}]");
                await _connections.InsertRowAsync(request.RequestContext.ConnectionId);
            } catch(Exception e) {
                LogError(e);
                throw Abort(CreateResponse(500, $"Failure while attempting to connect: {e.Message}"));
            }
        }

        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            try {
                LogInfo($"Disconnected: {request.RequestContext.ConnectionId} [{request.RequestContext.RouteKey}]");
                await _connections.DeleteRowAsync(request.RequestContext.ConnectionId);
            } catch(Exception e) {
                LogError(e);
                throw Abort(CreateResponse(500, $"Failure while attempting to disconnect: {e.Message}"));
            }
        }
    }
}
