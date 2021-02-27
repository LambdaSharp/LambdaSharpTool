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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.CloudFormation.Converter;
using LambdaSharp.Schedule;
using Newtonsoft.Json;

namespace LambdaSharp.Cloud.UpdateCloudFormationSpecFunction {

    public sealed class Function : ALambdaScheduleFunction {

        //--- Fields ---
        private string _destinationBucketName;
        private CloudFormationSpecificationConverter _converter;
        private IAmazonS3 _s3Client;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read function settings
            _destinationBucketName = config.ReadS3BucketName("DestinationBucket");

            // initialize clients
            _converter = new CloudFormationSpecificationConverter(HttpClient);
            _s3Client = new AmazonS3Client();
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {
            if(schedule.Name == null) {
                LogWarn("missing region in scheduled event");
                return;
            }
            await GenerateCloudFormationSpecificationAsync(schedule.Name);
        }

        private async Task GenerateCloudFormationSpecificationAsync(string region) {

            // generate extended CloudFormation specification for region
            LogInfo($"fetching latest CloudFormation specification for {region}");
            var specification = await _converter.GenerateCloudFormationSpecificationAsync(region);
            if(specification.Warnings.Any()) {
                LogWarn($"CloudFormation specification generator [{region}]:\n{string.Join("\n", specification.Warnings)}");
            }

            // serialize CloudFormation specification into a brotli compressed stream
            var compressedJsonSpecificationStream = new MemoryStream();
            using(var brotliStream = new BrotliStream(compressedJsonSpecificationStream, CompressionLevel.Optimal, leaveOpen: true))
            using(var streamWriter = new StreamWriter(brotliStream))
            using(var jsonTextWriter = new JsonTextWriter(streamWriter)) {
                new JsonSerializer().Serialize(jsonTextWriter, specification.Document);
            }
            compressedJsonSpecificationStream.Position = 0;

            // compute MD5 of new compressed CloudFormation specification
            byte[] newMD5Hash;
            using(var md5 = MD5.Create()) {
                newMD5Hash = md5.ComputeHash(compressedJsonSpecificationStream);
                compressedJsonSpecificationStream.Position = 0;
            }
            var newETag = $"\"{string.Concat(newMD5Hash.Select(x => x.ToString("x2")))}\"";
            LogInfo($"compressed CloudFormation specification ETag is {newETag} (size: {compressedJsonSpecificationStream.Length:N0} bytes) [{region}]");

            // check if a new CloudFormation specification was generated
            var destinationKey = $"AWS/{region}/CloudFormationResourceSpecification.json.br";
            var existingETag = await GetExistingETagAsync(_destinationBucketName, destinationKey);
            LogInfo($"existing CloudFormation specification ETag is {existingETag ?? "<null>"} [{region}]");
            if(string.Equals(newETag, existingETag ?? "", StringComparison.Ordinal)) {
                LogInfo($"CloudFormation specifications are the same; nothing further to do");
                return;
            }

            // update compressed CloudFormation specification in S3
            LogInfo($"uploading new CloudFormation specification [{region}]");
            await _s3Client.PutObjectAsync(new PutObjectRequest {
                BucketName = _destinationBucketName,
                Key = $"AWS/{region}/CloudFormationResourceSpecification.json.br",
                InputStream = compressedJsonSpecificationStream,
                MD5Digest = Convert.ToBase64String(newMD5Hash),
                Headers = {
                    ContentEncoding = "br",
                    ContentType = "application/json; charset=utf-8",
                    ContentMD5 = newETag
                }
            });
            LogInfo($"done [{region}]");
        }

        private async Task<string> GetExistingETagAsync(string bucketName, string key) {
            try {
                var metadata = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                    BucketName = bucketName,
                    Key = key,
                    RequestPayer = RequestPayer.Requester
                });
                return metadata.ETag;
            } catch {
                return null;
            }
        }
    }
}
