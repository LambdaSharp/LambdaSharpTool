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

using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.Finalizer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Core.Finalizer {

    public class Function : ALambdaFinalizerFunction {

        //--- Fields ---
        private IAmazonS3 _s3Client;
        private string _deploymentBucketName;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _s3Client = new AmazonS3Client();
            _deploymentBucketName = config.ReadS3BucketName("DeploymentBucket");
        }

        public override async Task DeleteDeployment(FinalizerProperties current) {
            LogInfo($"Emptying Deployment Bucket: {_deploymentBucketName}");

            // enumerate all S3 objects
            var request = new ListObjectsV2Request {
                BucketName = _deploymentBucketName
            };
            var counter = 0;
            do {
                var response = await _s3Client.ListObjectsV2Async(request);

                // delete asynchronously found objects
                if(response.S3Objects.Any()) {
                    AddPendingTask(_s3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                        BucketName = _deploymentBucketName,
                        Objects = response.S3Objects.Select(s3 => new KeyVersion {
                            Key = s3.Key
                        }).ToList(),
                        Quiet = true
                    }));
                    counter += response.S3Objects.Count;
                }

                // continue until no more objects can be fetched
                request.ContinuationToken = response.NextContinuationToken;
            } while(request.ContinuationToken != null);
            LogInfo($"Deleted {counter:N0} objects");
        }
    }
}
