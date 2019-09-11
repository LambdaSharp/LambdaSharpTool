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
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using FluentAssertions;
using LambdaSharp.ApiGateway.Internal;
using Newtonsoft.Json;
using Xunit;

namespace Tests.LambdaSharp.ApiGateway {

    public class ApiGatewayInvocationTargetDirectoryTests {

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
        public void InvokeNoParametersWithSimpleResponseSync() {
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
        public void InvokeNoParametersWithAPIGatewayProxyResponseSync() {
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
        public void InvokeNoParametersWithSimpleResponseAsync() {
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
        public void InvokeNoParametersWithAPIGatewayProxyResponseAsync() {
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

        public SimpleResponse MethodStageParameter(string stage) {
            return CreateSimpleResponse(stage);
        }

        [Fact]
        public void InvokePathParameter() {
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
        public void InvokeQueryStringParameter() {
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
        public void InvokeRequestBody() {
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
        public void InvokeAPIGatewayProxyRequest() {
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
        public void InvokeKitchenSink() {
            Test(
                nameof(MethodKitchenSink),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(123, "text", "hello"))
            );
        }

        public SimpleResponse MethodKitchenSink(int id, string query, SimpleRequest request) {
            return CreateSimpleResponse(id, query, request.Text);
        }

        [Fact]
        public void InvokeDefaultValueType() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodDefaultValueType),
                    DefaultRequest,
                    CreateResponse(CreateSimpleResponse(0))
                );
            });
            exception.ParameterName.Should().Be("unknown");
            exception.Message.Should().Be("missing value");
        }

        public SimpleResponse MethodDefaultValueType(int unknown) {
            return CreateSimpleResponse(unknown);
        }

        [Fact]
        public void InvokeDefaultStringType() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodRequiredStringType),
                    DefaultRequest,
                    CreateResponse(CreateSimpleResponse(new object[] { null }))
                );
            });
            exception.ParameterName.Should().Be("unknown");
            exception.Message.Should().Be("missing value");
        }

        public SimpleResponse MethodRequiredStringType(string unknown) {
            return CreateSimpleResponse(unknown);
        }

        [Fact]
        public void InvokeOptionalValueType() {
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
        public void InvokeOptionalStringType() {
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
        public void InvokeNullableValueType() {
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
        public void InvokeNullableOptionalValueType() {
            Test(
                nameof(MethodNullableOptionalValueType),
                DefaultRequest,
                CreateResponse(CreateSimpleResponse(456))
            );
        }

        public SimpleResponse MethodNullableOptionalValueType(int? optional = 456) {
            return CreateSimpleResponse(optional);
        }

        [Fact]
        public void InvokeBadRequestBody() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodRequestBody),
                    new APIGatewayProxyRequest {
                        Body = "This is not json"
                    },
                    CreateResponse(CreateSimpleResponse(456))
                );
            });
            exception.ParameterName.Should().Be("request");
            exception.Message.Should().Be("invalid JSON document in request body");
        }

        [Fact]
        public void InvokeBadRequestBodyAsync() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodRequestBodyAsync),
                    new APIGatewayProxyRequest {
                        Body = "This is not json"
                    },
                    CreateResponse(CreateSimpleResponse(456))
                );
            });
            exception.ParameterName.Should().Be("request");
            exception.Message.Should().Be("invalid JSON document in request body");
        }

        public async Task<SimpleResponse> MethodRequestBodyAsync(SimpleRequest request) {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return CreateSimpleResponse(request.Text);
        }

        [Fact]
        public void InvokeNullRequestBody() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodRequestBody),
                    new APIGatewayProxyRequest(),
                    CreateResponse(CreateSimpleResponse(456))
                );
            });
            exception.ParameterName.Should().Be("request");
            exception.Message.Should().Be("invalid JSON document in request body");
        }

        [Fact]
        public void InvokeBadParameter() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodPathParameter),
                    new APIGatewayProxyRequest {
                        PathParameters = new Dictionary<string, string> {
                            ["id"] = "not-a-number"
                        },
                        Body = SerializeJson(new SimpleRequest {
                            Text = "hello"
                        })
                    },
                    CreateResponse(CreateSimpleResponse(456))
                );
            });
            exception.ParameterName.Should().Be("id");
            exception.Message.Should().Be("invalid parameter format");
        }

        [Fact]
        public void InvokeBadParameterAsync() {
            var exception = Assert.Throws<ApiGatewayInvocationTargetParameterException>(() => {
                Test(
                    nameof(MethodPathParameterAsync),
                    new APIGatewayProxyRequest {
                        PathParameters = new Dictionary<string, string> {
                            ["id"] = "not-a-number"
                        },
                        Body = SerializeJson(new SimpleRequest {
                            Text = "hello"
                        })
                    },
                    CreateResponse(CreateSimpleResponse(456))
                );
            });
            exception.ParameterName.Should().Be("id");
            exception.Message.Should().Be("invalid parameter format");
        }

        public async Task<SimpleResponse> MethodPathParameterAsync(int id) {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return CreateSimpleResponse(id);
        }

        private void Test(string methodName, APIGatewayProxyRequest request, APIGatewayProxyResponse expectedResponse) {

            // Arrange
            var invocationTargetDirectory = new ApiGatewayInvocationTargetDirectory(type => (type == GetType()) ? this : Activator.CreateInstance(type, new[] { this }));
            invocationTargetDirectory.Add("test", $"{GetType().Assembly.FullName}::{GetType().FullName}::{methodName}");

            // Act
            var found = invocationTargetDirectory.TryGetInvocationTarget("test", out var invocationTarget);

            // Assert
            found.Should().Be(true);

            // Act
            var response = invocationTarget(request).GetAwaiter().GetResult();

            // Assert
            response.Should().BeEquivalentTo(expectedResponse);
        }
    }
}
