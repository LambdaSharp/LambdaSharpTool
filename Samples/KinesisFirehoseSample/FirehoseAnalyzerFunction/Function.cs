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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.KinesisFirehoseEvents;
using LambdaSharp;

namespace Sample.KinesisFirehose.FirehoseAnalyzerFunction {

    public sealed class Function : ALambdaFunction<KinesisFirehoseEvent, KinesisFirehoseResponse> {

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) { }

        public override async Task<KinesisFirehoseResponse> ProcessMessageAsync(KinesisFirehoseEvent request) {
            LogInfo($"InvocationId: {request.InvocationId}");
            LogInfo($"DeliveryStreamArn: {request.DeliveryStreamArn}");
            LogInfo($"Region: {request.Region}");
            var response = new KinesisFirehoseResponse {
                Records = new List<KinesisFirehoseResponse.FirehoseRecord>()
            };
            foreach(var record in request.Records) {
                LogInfo($"RecordId: {record.RecordId}");
                LogInfo($"ApproximateArrivalEpoch: {record.ApproximateArrivalEpoch}");
                LogInfo($"ApproximateArrivalTimestamp: {record.ApproximateArrivalTimestamp}");

                // decode and decompress data element
                string data;
                using(var sourceStream = new MemoryStream(Convert.FromBase64String(record.Base64EncodedData)))
                using(var destinationStream = new MemoryStream()) {
                    using(var gzip = new GZipStream(sourceStream, CompressionMode.Decompress)) {
                        gzip.CopyTo(destinationStream);
                        destinationStream.Position = 0;
                    }
                    data = Encoding.UTF8.GetString(destinationStream.ToArray());
                }
                LogInfo($"Data: {data}");

                // transform data: For example ToUpper the data
                response.Records.Add(new KinesisFirehoseResponse.FirehoseRecord {
                    RecordId = record.RecordId,
                    Result = KinesisFirehoseResponse.TRANSFORMED_STATE_OK,
                    Base64EncodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(data.ToUpper()))
                });
            }
            return response;
        }
    }
}
