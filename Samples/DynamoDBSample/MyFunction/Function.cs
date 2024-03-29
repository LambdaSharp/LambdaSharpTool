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

namespace DynamoDBSample.MyFunction;

using Amazon.Lambda.DynamoDBEvents;
using LambdaSharp;

public sealed class Function : ALambdaFunction<DynamoDBEvent, string> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<string> ProcessMessageAsync(DynamoDBEvent dynamoDbEvent) {
        LogInfo($"# Kinesis Records = {dynamoDbEvent.Records.Count}");
        for(var i = 0; i < dynamoDbEvent.Records.Count; ++i) {
            var record = dynamoDbEvent.Records[i];
            LogInfo($"Record #{i}");
            LogInfo($"AwsRegion = {record.AwsRegion}");
            LogInfo($"DynamoDB.ApproximateCreationDateTime = {record.Dynamodb.ApproximateCreationDateTime}");
            LogInfo($"DynamoDB.Keys.Count = {record.Dynamodb.Keys.Count}");
            LogInfo($"DynamoDB.SequenceNumber = {record.Dynamodb.SequenceNumber}");
            LogInfo($"DynamoDB.UserIdentity.PrincipalId = {record.UserIdentity?.PrincipalId}");
            LogInfo($"EventID = {record.EventID}");
            LogInfo($"EventName = {record.EventName}");
            LogInfo($"EventSource = {record.EventSource}");
            LogInfo($"EventSourceArn = {record.EventSourceArn}");
            LogInfo($"EventVersion = {record.EventVersion}");
        }
        return "Ok";
    }
}
