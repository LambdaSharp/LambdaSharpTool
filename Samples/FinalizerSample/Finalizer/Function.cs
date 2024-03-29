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

namespace FinalizerSample.Finalizer;

using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp;
using LambdaSharp.Finalizer;

public sealed class Function : ALambdaFinalizerFunction {

    //--- Fields ---
    private IAmazonS3? _s3Client;
    private string? _bucketName;

    //--- Properties ---
    private IAmazonS3 S3Client => _s3Client ?? throw new InvalidOperationException();
    private string BucketName => _bucketName ?? throw new InvalidOperationException();

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {
        _s3Client = new AmazonS3Client();

        // read configuration settings
        _bucketName = config.ReadS3BucketName("MyBucket");
    }

    public override async Task CreateDeploymentAsync(FinalizerProperties current, CancellationToken cancellationToken) {
        LogInfo($"Creating Deployment: {current.DeploymentChecksum}");
    }

    public override async Task UpdateDeploymentAsync(FinalizerProperties current, FinalizerProperties previous, CancellationToken cancellationToken) {
        LogInfo($"Updating Deployment: {previous.DeploymentChecksum} -> {current.DeploymentChecksum}");
    }

    public override async Task DeleteDeploymentAsync(FinalizerProperties current, CancellationToken cancellationToken) {
        LogInfo($"Deleting Deployment: {current.DeploymentChecksum}");

        // enumerate all S3 objects
        var request = new ListObjectsV2Request {
            BucketName = BucketName
        };
        var counter = 0;
        var deletions = new List<Task>();
        do {
            var response = await S3Client.ListObjectsV2Async(request);

            // delete any objects found
            if(response.S3Objects.Any()) {
                deletions.Add(S3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
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
