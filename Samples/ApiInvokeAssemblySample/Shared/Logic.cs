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
using LambdaSharp;
using LambdaSharp.ApiGateway;
using LambdaSharp.Logger;
using Newtonsoft.Json;

namespace ApiInvokeSample.Shared {

    public class CaptureEventRequest {

        //--- Properties ---
        [JsonRequired]
        public string EventType { get; set; }

        [JsonRequired]
        public string Data { get; set; }
    }

    public class ComputeRequest {

        //--- Properties ---
        [JsonRequired]
        public double LeftOperand { get; set; }

        [JsonRequired]
        public double RightOperand { get; set; }
    }

    public class ComputeQuery {

        //--- Properties ---
        [JsonProperty("op", Required = Required.Always)]
        public string Operator { get; set; }
    }

    public class ComputeResponse {

        //--- Properties ---
        [JsonRequired]
        public double Value { get; set; }
    }

    public class Logic {

        //--- Fields ---
        private ILambdaLogLevelLogger _logger;

        //--- Constructors ---
        public Logic(ILambdaLogLevelLogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //--- Methods ---
        public async Task CaptureEvent(CaptureEventRequest request) {
            _logger.LogInfo($"received EventType: {request.EventType} = {request.Data}");

            // artificial delay to show that end-points without a response don't wait for the invocation to complete
            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        public ComputeResponse Compute(ComputeRequest computation, [FromUri] ComputeQuery query) {
            _logger.LogInfo($"request: {JsonConvert.SerializeObject(computation)}");
            _logger.LogInfo($"query: {JsonConvert.SerializeObject(query)}");
            switch(query.Operator) {
            case "add":
                return new ComputeResponse {
                    Value = computation.LeftOperand + computation.RightOperand
                };
            case "subtract":
                return new ComputeResponse {
                    Value = computation.LeftOperand - computation.RightOperand
                };
            case "multiply":
                return new ComputeResponse {
                    Value = computation.LeftOperand * computation.RightOperand
                };
            case "divide":
                return new ComputeResponse {
                    Value = computation.LeftOperand / computation.RightOperand
                };
            default:
                return new ComputeResponse {
                    Value = double.NaN
                };
            }
        }
    }
}
