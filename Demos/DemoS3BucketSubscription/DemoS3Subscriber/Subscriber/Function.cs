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

using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using LambdaSharp;

namespace DemoS3Subscriber.Subscriber {

    public sealed class Function : ALambdaFunction<S3Event, string> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<string> ProcessMessageAsync(S3Event s3Event) {
            LogInfo($"# S3 Records = {s3Event.Records.Count}");
            for(var i = 0; i < s3Event.Records.Count; ++i) {
                var record = s3Event.Records[i];
                LogInfo($"EventName = {record.EventName.Value}");
                LogInfo($"EventSource = {record.EventSource}");
                LogInfo($"EventTime = {record.EventTime}");
                LogInfo($"EventVersion = {record.EventVersion}");
                LogInfo($"S3.Bucket.Name = {record.S3.Bucket.Name}");
                LogInfo($"S3.Object.ETag = {record.S3.Object.ETag}");
                LogInfo($"S3.Object.Key = {record.S3.Object.Key}");
                LogInfo($"S3.Object.Size = {record.S3.Object.Size}");
                LogInfo($"S3.Object.VersionId = {record.S3.Object.VersionId}");
                LogInfo($"UserIdentity.PrincipalId = {record.UserIdentity.PrincipalId}");
            }
            return "Ok";
        }
    }
}
