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
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using FluentAssertions;
using LambdaSharp.Internal;
using Newtonsoft.Json;
using Xunit;

namespace Tests.LambdaSharp.Internal {

    public class APIGatewayDispatchTableTests {

        //--- Types ---
        public class SimpleRequest {

            //--- Properties ---
            public string Text { get; set; }

        }

        public class SimpleResponse {

            //--- Properties ---
            public string Message { get; set; }
        }

        //--- Class Fields ---
        private static APIGatewayProxyRequest DefaultRequest = new APIGatewayProxyRequest {
            QueryStringParameters = new Dictionary<string, string> {
                ["query"] = "text"
            },
            PathParameters = new Dictionary<string, string> {
                ["id"] = "123"
            },
            StageVariables = new Dictionary<string, string> {
                ["stage"] = "test"
            },
            Body = SerializeJson(new SimpleRequest {
                Text = "hello"
            })
        };

        private static SimpleResponse DefaultSimpleResponse = new SimpleResponse {
            Message = "I was here!"
        };

        //--- Class Methods ---
        private static string SerializeJson(object value) => JsonConvert.SerializeObject(value);

        private static SimpleResponse CreateSimpleResponse(params object[] values)
            => new SimpleResponse {
                Message = $"Value: ({string.Join(",", values)})"
            };

        private static APIGatewayProxyResponse CreateResponse(object value)
            => new APIGatewayProxyResponse {
                Body = SerializeJson(value),
                Headers = new Dictionary<string, string> {
                    ["ContentType"] = "application/json"
                },
                StatusCode = 200
            };

        //--- Methods ---
        [Fact]
        public void InvokeMethodNoParametersWithSimpleResponseSync() {
            Test(
                nameof(MethodNoParametersWithSimpleResponse),
                DefaultRequest,
                CreateResponse(DefaultSimpleResponse)
            );
        }

        public SimpleResponse MethodNoParametersWithSimpleResponse() {
            return DefaultSimpleResponse;
        }

        [Fact]
        public void InvokeMethodNoParametersWithAPIGatewayProxyResponseSync() {
            Test(
                nameof(MethodNoParametersWithAPIGatewayProxyResponse),
                DefaultRequest,
                CreateResponse(DefaultSimpleResponse)
            );
        }

        public APIGatewayProxyResponse MethodNoParametersWithAPIGatewayProxyResponse() {
            return new APIGatewayProxyResponse {
                Body = SerializeJson(DefaultSimpleResponse),
                Headers = new Dictionary<string, string> {
                    ["ContentType"] = "application/json"
                },
                StatusCode = 200
            };
        }

        [Fact]
        public void InvokeMethodNoParametersWithSimpleResponseAsync() {
            Test(
                nameof(MethodNoParametersWithSimpleResponseAsync),
                DefaultRequest,
                CreateResponse(DefaultSimpleResponse)
            );
        }

        public async Task<SimpleResponse> MethodNoParametersWithSimpleResponseAsync() {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return DefaultSimpleResponse;
        }

        [Fact]
        public void InvokeMethodNoParametersWithAPIGatewayProxyResponseAsync() {
            Test(
                nameof(MethodNoParametersWithAPIGatewayProxyResponseAsync),
                DefaultRequest,
                CreateResponse(DefaultSimpleResponse)
            );
        }

        public async Task<APIGatewayProxyResponse> MethodNoParametersWithAPIGatewayProxyResponseAsync() {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return new APIGatewayProxyResponse {
                Body = SerializeJson(DefaultSimpleResponse),
                Headers = new Dictionary<string, string> {
                    ["ContentType"] = "application/json"
                },
                StatusCode = 200
            };
        }

        [Fact]
        public void InvokeMethodStageParameter() {
            Test(
                nameof(MethodStageParameter),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse("test"))
            );
        }

        public SimpleResponse MethodStageParameter(string stage) {
            return CreateSimpleResponse(stage);
        }

        [Fact]
        public void InvokeMethodPathParameter() {
            Test(
                nameof(MethodPathParameter),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(123))
            );
        }

        public SimpleResponse MethodPathParameter(int id) {
            return CreateSimpleResponse(id);
        }

        [Fact]
        public void InvokeMethodQueryStringParameter() {
            Test(
                nameof(MethodQueryStringParameter),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse("text"))
            );
        }

        public SimpleResponse MethodQueryStringParameter(string query) {
            return CreateSimpleResponse(query);
        }

        [Fact]
        public void InvokeMethodRequestBody() {
            Test(
                nameof(MethodRequestBody),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse("hello"))
            );
        }

        public SimpleResponse MethodRequestBody(SimpleRequest request) {
            return CreateSimpleResponse(request.Text);
        }

        [Fact]
        public void InvokeMethodAPIGatewayProxyRequest() {
            Test(
                nameof(MethodAPIGatewayProxyRequest),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(DefaultRequest.Body))
            );
        }

        public SimpleResponse MethodAPIGatewayProxyRequest(APIGatewayProxyRequest request) {
            return CreateSimpleResponse(request.Body);
        }

        [Fact]
        public void InvokeMethodKitchenSink() {
            Test(
                nameof(MethodKitchenSink),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse("test", 123, "text", "hello"))
            );
        }

        public SimpleResponse MethodKitchenSink(string stage, int id, string query, SimpleRequest request) {
            return CreateSimpleResponse(stage, id, query, request.Text);
        }

        [Fact]
        public void InvokeMethodDefaultValueType() {
            Test(
                nameof(MethodDefaultValueType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(0))
            );
        }

        public SimpleResponse MethodDefaultValueType(int unknown) {
            return CreateSimpleResponse(unknown);
        }

        [Fact]
        public void InvokeMethodDefaultStringType() {
            Test(
                nameof(MethodDefaultStringType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(new object[] { null }))
            );
        }

        public SimpleResponse MethodDefaultStringType(string unknown) {
            return CreateSimpleResponse(unknown);
        }

        [Fact]
        public void InvokeMethodOptionalValueType() {
            Test(
                nameof(MethodOptionalValueType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(123))
            );
        }

        public SimpleResponse MethodOptionalValueType(int optional = 123) {
            return CreateSimpleResponse(optional);
        }

        [Fact]
        public void InvokeMethodOptionalStringType() {
            Test(
                nameof(MethodOptionalStringType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse("default"))
            );
        }

        public SimpleResponse MethodOptionalStringType(string optional = "default") {
            return CreateSimpleResponse(optional);
        }

        [Fact]
        public void InvokeMethodNullableValueType() {
            Test(
                nameof(MethodNullableValueType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(new object[] { null }))
            );
        }

        public SimpleResponse MethodNullableValueType(int? optional) {
            return CreateSimpleResponse(optional);
        }

        [Fact]
        public void InvokeMethodNullableOptionalValueType() {
            Test(
                nameof(MethodNullableOptionalValueType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(456))
            );
        }

        public SimpleResponse MethodNullableOptionalValueType(int? optional = 456) {
            return CreateSimpleResponse(optional);
        }

        private void Test(string methodName, APIGatewayProxyRequest request, APIGatewayProxyResponse expectedResponse) {

            // Arrange
            var dispatchTable = new APIGatewayDispatchTable(GetType());
            dispatchTable.Add("test", methodName);

            // Act
            var found = dispatchTable.TryGetDispatcher("test", out var dispatcher);

            // Assert
            found.Should().Be(true);

            // Act
            var response = dispatcher(this, request).Result;

            // Assert
            response.Should().BeEquivalentTo(expectedResponse);
        }
    }
}
