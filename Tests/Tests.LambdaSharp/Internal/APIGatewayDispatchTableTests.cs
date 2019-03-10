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
        private static SimpleResponse DefaultResponse = new SimpleResponse {
            Message = "I was here!"
        };

        //--- Class Methods ---
        private static string SerializeJson(object value) => JsonConvert.SerializeObject(value);

        private static SimpleResponse CreateResponse(object value) => new SimpleResponse {
            Message = $"Value: {value}"
        };

        //--- Methods ---
        [Fact]
        public void InvokeMethodNoParametersWithSimpleResponseSync() {
            Test(
                nameof(MethodNoParametersWithSimpleResponseSync),
                new APIGatewayProxyRequest(),
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultResponse),
                    StatusCode = 200
                }
            ).Wait();
        }

        [Fact]
        public void InvokeMethodNoParametersWithAPIGatewayProxyResponseSync() {
            Test(
                nameof(MethodNoParametersWithAPIGatewayProxyResponseSync),
                new APIGatewayProxyRequest(),
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultResponse),
                    StatusCode = 200
                }
            ).Wait();
        }

        [Fact]
        public void InvokeMethodNoParametersWithSimpleResponseAsync() {
            Test(
                nameof(MethodNoParametersWithSimpleResponseAsync),
                new APIGatewayProxyRequest(),
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultResponse),
                    StatusCode = 200
                }
            ).Wait();
        }

        [Fact]
        public void InvokeMethodNoParametersWithAPIGatewayProxyResponseAsync() {
            Test(
                nameof(MethodNoParametersWithAPIGatewayProxyResponseAsync),
                new APIGatewayProxyRequest(),
                new APIGatewayProxyResponse {
                    Body = SerializeJson(DefaultResponse),
                    StatusCode = 200
                }
            ).Wait();
        }

        public SimpleResponse MethodNoParametersWithSimpleResponseSync() => DefaultResponse;

        public APIGatewayProxyResponse MethodNoParametersWithAPIGatewayProxyResponseSync()
            => new APIGatewayProxyResponse {
                Body = SerializeJson(DefaultResponse),
                StatusCode = 200
            };

        public async Task<SimpleResponse> MethodNoParametersWithSimpleResponseAsync() {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return DefaultResponse;
        }

        public async Task<APIGatewayProxyResponse> MethodNoParametersWithAPIGatewayProxyResponseAsync() {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return new APIGatewayProxyResponse {
                Body = SerializeJson(DefaultResponse),
                StatusCode = 200
            };
        }

        private async Task Test(string methodName, APIGatewayProxyRequest request, APIGatewayProxyResponse expectedResponse) {

            // Arrange
            var dispatchTable = new APIGatewayDispatchTable(GetType());
            dispatchTable.Add("test", methodName);

            // Act
            var found = dispatchTable.TryGetDispatcher("test", out var dispatcher);

            // Assert
            found.Should().Be(true);

            // Act
            var response = await dispatcher(this, request);

            // Assert
            response.Should().BeEquivalentTo(expectedResponse);
        }
    }
}
