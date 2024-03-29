/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace Sample.Debug.MyFunction;

using LambdaSharp;

public class FunctionRequest { }

public class FunctionResponse { }

public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) { }

    public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

        // LogDebug() is only emitted to the logs when the DEBUG_LOGGING_ENABLED environment variable is set
        LogDebug("this will only show if DEBUG_LOGGING_ENABLED environment variable is set");

        // to avoid unnecessary overhead, check if debug logging is enabled before constructing debug output
        if(DebugLoggingEnabled) {
            LogDebug("more complex statements should be guarded using the DebugLoggingEnabled property");
        }
        return new FunctionResponse();
    }
}
