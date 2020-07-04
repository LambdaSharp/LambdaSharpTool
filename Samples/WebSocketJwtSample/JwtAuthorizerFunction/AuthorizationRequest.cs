/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sample.WebSocketsJwt.JwtAuthorizerFunction {

    public class AuthorizationRequest {

        //--- Properties ---

        [JsonProperty("methodArn")]
        public string MethodArn { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("queryStringParameters")]
        public Dictionary<string, string> QueryStringParameters { get; set; }

        [JsonProperty("pathParameters")]
        public Dictionary<string, string> PathParameters { get; set; }

        [JsonProperty("stageVariables")]
        public Dictionary<string, string> StageVariables { get; set; }
    }
}
