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
using LambdaSharp.Logging.Metrics;

namespace Sample.Metric.MyFunction {

    public class FunctionRequest { }

    public class FunctionResponse { }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) { }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            LogMetric(new LambdaMetric[] {
                ("CompletedMessages.Latency", 100.0, LambdaMetricUnit.Milliseconds),
                ("CompletedMessages.Count", 1, LambdaMetricUnit.Count)
            });
            return new FunctionResponse();
        }
    }
}
