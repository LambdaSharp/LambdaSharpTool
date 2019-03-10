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

        private static SimpleResponse CreateSimpleResponse(params object[] values) => new SimpleResponse {
            Message = $"Value: ({string.Join(",", values)})"
        };

        //--- Methods ---
        [Fact]
        public void InvokeMethodNoParametersWithSimpleResponseSync() {
            Test(
                nameof(MethodNoParametersWithSimpleResponse),
                DefaultRequest,
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultSimpleResponse),
                    StatusCode = 200
                }
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
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultSimpleResponse),
                    StatusCode = 200
                }
            );
        }

        public APIGatewayProxyResponse MethodNoParametersWithAPIGatewayProxyResponse() {
            return new APIGatewayProxyResponse {
                Body = SerializeJson(DefaultSimpleResponse),
                StatusCode = 200
            };
        }

        [Fact]
        public void InvokeMethodNoParametersWithSimpleResponseAsync() {
            Test(
                nameof(MethodNoParametersWithSimpleResponseAsync),
                DefaultRequest,
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultSimpleResponse),
                    StatusCode = 200
                }
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
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultSimpleResponse),
                    StatusCode = 200
                }
            );
        }

        public async Task<APIGatewayProxyResponse> MethodNoParametersWithAPIGatewayProxyResponseAsync() {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return new APIGatewayProxyResponse {
                Body = SerializeJson(DefaultSimpleResponse),
                StatusCode = 200
            };
        }

        [Fact]
        public void InvokeMethodStageParameter() {
            Test(
                nameof(MethodStageParameter),
                DefaultRequest,
                new APIGatewayProxyResponse {
                    Body = SerializeJson(CreateSimpleResponse("test")),
                    StatusCode = 200
                }
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
                new APIGatewayProxyResponse {
                    Body = SerializeJson(CreateSimpleResponse(123)),
                    StatusCode = 200
                }
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
                new APIGatewayProxyResponse {
                    Body = SerializeJson(CreateSimpleResponse("text")),
                    StatusCode = 200
                }
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
                new APIGatewayProxyResponse {
                    Body = SerializeJson(CreateSimpleResponse("hello")),
                    StatusCode = 200
                }
            );
        }

        public SimpleResponse MethodRequestBody(SimpleRequest request) {
            return CreateSimpleResponse(request.Text);
        }

        [Fact]
        public void InvokeMethodKitchenSink() {
            Test(
                nameof(MethodKitchenSink),
                DefaultRequest,
                new APIGatewayProxyResponse {
                    Body = SerializeJson(CreateSimpleResponse("test", 123, "text", "hello")),
                    StatusCode = 200
                }
            );
        }

        public SimpleResponse MethodKitchenSink(string stage, int id, string query, SimpleRequest request) {
            return CreateSimpleResponse(stage, id, query, request.Text);
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
