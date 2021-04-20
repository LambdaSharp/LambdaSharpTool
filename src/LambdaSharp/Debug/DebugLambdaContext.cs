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

using System;
using Amazon.Lambda.Core;

namespace LambdaSharp.Debug {

    public class DebugLambdaContext : ILambdaContext {

        //--- Properties ---
        public string AwsRequestId => throw new NotImplementedException();
        public IClientContext ClientContext => throw new NotImplementedException();
        public string FunctionName => throw new NotImplementedException();
        public string FunctionVersion => throw new NotImplementedException();
        public ICognitoIdentity Identity => throw new NotImplementedException();
        public string InvokedFunctionArn => throw new NotImplementedException();
        public ILambdaLogger Logger => throw new NotImplementedException();
        public string LogGroupName => throw new NotImplementedException();
        public string LogStreamName => throw new NotImplementedException();
        public int MemoryLimitInMB => throw new NotImplementedException();
        public TimeSpan RemainingTime => throw new NotImplementedException();
    }
}
