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

using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.SimpleQueueService;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsSample.MyFunction {

    public class MyMessage {

        //--- Properties ---
        public string Text { get; set; }
    }

    public class Function : ALambdaQueueFunction<MyMessage> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task ProcessMessageAsync(MyMessage message) {
            LogInfo($"Message.Text = {message.Text}");
            foreach(var attribute in CurrentRecord.Attributes) {
                LogInfo($"CurrentRecord.Attributes.{attribute.Key} = {attribute.Value}");
            }
            LogInfo($"CurrentRecord.Body = {CurrentRecord.Body}");
            LogInfo($"CurrentRecord.EventSource = {CurrentRecord.EventSource}");
            LogInfo($"CurrentRecord.EventSourceArn = {CurrentRecord.EventSourceArn}");
            LogInfo($"CurrentRecord.Md5OfBody = {CurrentRecord.Md5OfBody}");
            LogInfo($"CurrentRecord.Md5OfMessageAttributes = {CurrentRecord.Md5OfMessageAttributes}");
            foreach(var attribute in CurrentRecord.MessageAttributes) {
                LogInfo($"CurrentRecord.MessageAttributes.{attribute.Key} = {attribute.Value}");
            }
            LogInfo($"CurrentRecord.MessageId = {CurrentRecord.MessageId}");
            LogInfo($"CurrentRecord.ReceiptHandle = {CurrentRecord.ReceiptHandle}");
        }
    }
}