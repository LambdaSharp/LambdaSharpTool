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

namespace SqsSample.Producer;

using Amazon.SQS;
using LambdaSharp;

public sealed class Function : ALambdaFunction<int, string> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Fields ---
    private string? _sqsQueueUrl;
    private IAmazonSQS? _sqsClient;

    //--- Properties ---
    private string SqsQueueUrl => _sqsQueueUrl ?? throw new InvalidOperationException();
    private IAmazonSQS SqsClient => _sqsClient ?? throw new InvalidOperationException();

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {
        _sqsQueueUrl = config.ReadSqsQueueUrl("SqsQueue");
        _sqsClient = new AmazonSQSClient();
    }

    public override async Task<string> ProcessMessageAsync(int request) {
        LogInfo($"generating {request:N0} messages");
        await Task.WhenAll(Enumerable.Range(0, request).Select(i => SqsClient.SendMessageAsync(SqsQueueUrl, i.ToString())));
        return "OK";
    }
}
