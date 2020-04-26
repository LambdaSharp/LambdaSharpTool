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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp;
using LambdaSharp.Finalizer;

namespace FinalizerSample.Finalizer {

    public class Function : ALambdaFinalizerFunction {

        //--- Fields ---
        private IAmazonS3 _s3Client;
        private string _bucketName;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _s3Client = new AmazonS3Client();

            // read configuration settings
            _bucketName = config.ReadS3BucketName("MyBucket");
        }

        public override async Task CreateDeployment(FinalizerProperties current) {
            LogInfo($"Creating Deployment: {current.DeploymentChecksum}");
        }

        public override async Task UpdateDeployment(FinalizerProperties next, FinalizerProperties previous) {
            LogInfo($"Updating Deployment: {previous.DeploymentChecksum} -> {next.DeploymentChecksum}");
        }

        public override async Task DeleteDeployment(FinalizerProperties current) {
            LogInfo($"Deleting Deployment: {current.DeploymentChecksum}");

            // enumerate all S3 objects
            var request = new ListObjectsV2Request {
                BucketName = _bucketName
            };
            var counter = 0;
            var deletions = new List<Task>();
            do {
                var response = await _s3Client.ListObjectsV2Async(request);

                // delete any objects found
                if(response.S3Objects.Any()) {
                    deletions.Add(_s3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                        BucketName = _bucketName,
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

            // wait for all deletions to complete
            await Task.WhenAll(deletions);
            LogInfo($"Deleted {counter:N0} objects");
        }
    }
}