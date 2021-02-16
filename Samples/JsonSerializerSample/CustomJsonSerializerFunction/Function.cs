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

using System.Threading.Tasks;
using LambdaSharp;

namespace Sample.JsonSerializer.CustomJsonSerializerFunction {

    public class FunctionRequest {

        //--- Properties ---
        public string Bar { get; set; }
    }

    public class FunctionResponse {

        //--- Properties ---
        public string Bar { get; set; }
    }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Constructors ---

        // using a custom JSON serializer based on LitJson (https://litjson.net/)
        public Function() : base(new CustomJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) { }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            LogInfo("Deserialized using {1}: {0}", LambdaSerializer.Serialize(request), LambdaSerializer.GetType().FullName);
            return new FunctionResponse {
                Bar = request.Bar
            };
        }
    }
}
