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

using System;
using System.Threading.Tasks;
using LambdaSharp;

namespace BadModule.NearTimeout {

    public class FunctionRequest { }

    public class FunctionResponse { }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            await Task.Delay(CurrentContext.RemainingTime.Subtract(TimeSpan.FromSeconds(1)));
            return new FunctionResponse();
        }
    }
}
