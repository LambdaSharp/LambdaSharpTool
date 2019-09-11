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
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using LambdaSharp;
using LambdaSharp.Exceptions;
using LambdaSharp.SimpleQueueService;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsSample.Consumer {

    public class Function : ALambdaQueueFunction<int> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task ProcessMessageAsync(int message) {
            LogInfo($"received: {message}");
            if(message % 10 == 0) {
                LogWarn("Retriable Error");
                throw new LambdaRetriableException("Retriable Error!");
            }
            if(message % 5 == 0) {
                LogWarn("Non Retriable Error");
                throw new Exception("Non Retriable Error");
            }
        }
    }
}
