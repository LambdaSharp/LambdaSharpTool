/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using LambdaSharp.CustomResource;

namespace LambdaSharp.S3.IO.S3Writer {

    public class EmptyBucketLogic {

        //--- Fields ---
        private readonly ILambdaLogger _logger;
        private readonly IAmazonS3 _s3Client;

        //--- Constructors ---
        public EmptyBucketLogic(ILambdaLogger logger, IAmazonS3 s3Client) {
            _logger = logger;
            _s3Client = s3Client;
        }

        //--- Methods ---
        public async Task<Response<ResponseProperties>> Create(RequestProperties properties) {

            // nothing to do on create
            return new Response<ResponseProperties> {
                PhysicalResourceId = $"s3emptybucket:{properties.BucketName}",
                Properties = new ResponseProperties {
                    BucketName = properties.BucketName
                }
            };
        }

        public Task<Response<ResponseProperties>> Update(RequestProperties oldProperties, RequestProperties properties)
            => Create(properties);

        public async Task<Response<ResponseProperties>> Delete(RequestProperties properties) {
            if(properties.Enabled == false) {

                // don't do anything if disabled
                return new Response<ResponseProperties>();
            }
            var bucketName = properties.BucketName;
            _logger.LogInfo($"emptying bucket: {bucketName}");

            // enumerate all S3 objects
            var request = new ListObjectsV2Request {
                BucketName = bucketName
            };
            var counter = 0;
            var deletions = new List<Task>();
            do {
                var response = await _s3Client.ListObjectsV2Async(request);

                // delete any objects found
                if(response.S3Objects.Any()) {
                    deletions.Add(_s3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                        BucketName = bucketName,
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
            _logger.LogInfo($"deleted {counter:N0} objects");
            return new Response<ResponseProperties>();
        }
    }
}