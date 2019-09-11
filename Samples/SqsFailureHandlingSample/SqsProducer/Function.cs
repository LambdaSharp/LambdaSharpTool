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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using LambdaSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsSample.Producer {

    public class Function : ALambdaFunction<int, string> {

        //--- Fields ---
        private string _sqsQueueUrl;
        private IAmazonSQS _sqsClient;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _sqsQueueUrl = config.ReadSqsQueueUrl("SqsQueue");
            _sqsClient = new AmazonSQSClient();
        }

        public override async Task<string> ProcessMessageAsync(int request) {
            LogInfo($"generating {request:N0} messages");
            await Task.WhenAll(Enumerable.Range(0, request).Select(i => _sqsClient.SendMessageAsync(_sqsQueueUrl, i.ToString())));
            return "OK";
        }
    }
}
