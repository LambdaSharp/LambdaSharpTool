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

        //--- Types ---
        private class LambdaTextWriterLogger : ILambdaLogger {

            //--- Fields ---
            private readonly DebugLambdaContext _context;

            //--- Constructors ---
            public LambdaTextWriterLogger(DebugLambdaContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

            //--- Methods ---
            public void Log(string message) => _context.Provider?.Log(message);
            public void LogLine(string message) => _context.Provider?.Log(message + Environment.NewLine);
        }

        //--- Constructors ---
        public DebugLambdaContext(ILambdaFunctionDependencyProvider provider) {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Logger = new LambdaTextWriterLogger(this);
        }

        //--- Properties ---
        public string AwsRequestId { get; set; }
        public IClientContext ClientContext { get; set; }
        public string FunctionName { get; set; }
        public string FunctionVersion { get; set; }
        public ICognitoIdentity Identity { get; set; }
        public string InvokedFunctionArn { get; set; }
        public ILambdaLogger Logger { get; }
        public string LogGroupName { get; set; }
        public string LogStreamName { get; set; }
        public int MemoryLimitInMB { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public ILambdaFunctionDependencyProvider Provider { get; }
    }
}
