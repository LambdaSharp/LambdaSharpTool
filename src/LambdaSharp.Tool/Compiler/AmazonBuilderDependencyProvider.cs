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

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace LambdaSharp.Tool.Compiler {

    public class AmazonBuilderDependencyProvider : IBuilderDependencyProvider {

        //--- Fields ---
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, IAmazonS3> _s3ClientByBucketName = new Dictionary<string, IAmazonS3>();

        //--- Constructors ---
        public AmazonBuilderDependencyProvider(ILogger logger, HttpClient httpClient) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        //--- Properties ---
        public string ToolDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LambdaSharp");

        //--- Methods ---
        public async Task<string> GetS3ObjectContentsAsync(string bucketName, string key) {

            // TODO: it might beneficial to always cache a successful response
            var stopwatch = Stopwatch.StartNew();
            try {

                // get bucket region specific S3 client
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return null;
                }
                try {
                    var response = await s3Client.GetObjectAsync(new GetObjectRequest {
                        BucketName = bucketName,
                        Key = key,
                        RequestPayer = RequestPayer.Requester
                    });
                    using(var stream = new MemoryStream()) {
                        await response.ResponseStream.CopyToAsync(stream);
                        return Encoding.UTF8.GetString(stream.ToArray());
                    }
                } catch(AmazonS3Exception) {
                    return null;
                }
            } finally {
                _logger.LogInfoPerformance($"GetS3ObjectContentsAsync() for s3://{bucketName}/{key}", stopwatch.Elapsed, cached: null);
            }
        }

        public async Task<IEnumerable<string>> ListS3BucketObjects(string bucketName, string prefix) {

            // get bucket region specific S3 client
            var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
            if(s3Client == null) {

                // nothing to do; GetS3ClientByBucketName already emitted an error
                return Enumerable.Empty<string>();
            }

            // enumerate objects in bucket
            var result = new List<string>();
            var request = new ListObjectsV2Request {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = "/",
                MaxKeys = 100,
                RequestPayer = RequestPayer.Requester
            };
            do {
                try {
                    var response = await s3Client.ListObjectsV2Async(request);
                    result.AddRange(response.S3Objects.Select(s3Object => s3Object.Key.Substring(request.Prefix.Length)));
                    request.ContinuationToken = response.NextContinuationToken;
                } catch(AmazonS3Exception e) when(e.Message == "Access Denied") {
                    break;
                }
            } while(request.ContinuationToken != null);
            return result;
        }

        private async Task<IAmazonS3> GetS3ClientByBucketNameAsync(string bucketName) {
            if(bucketName == null) {
                return null;
            } if(_s3ClientByBucketName.TryGetValue(bucketName, out var result)) {
                return result;
            }

            // NOTE (2019-06-14, bjorg): we need to determine which region the bucket belongs to
            //  so that we can instantiate the S3 client properly; doing a HEAD request against
            //  the domain name returns a 'x-amz-bucket-region' even when then bucket is private.
            var headResponse = await _httpClient.SendAsync(new HttpRequestMessage {
                Method = HttpMethod.Head,
                RequestUri = new Uri($"https://{bucketName}.s3.amazonaws.com")
            });

            // check if bucket exists
            if(headResponse.StatusCode == HttpStatusCode.NotFound) {
                _logger.Log(Warning.ManifestLoaderCouldNotFindBucket(bucketName));
                return null;
            }

            // check for region header of bucket
            if(!headResponse.Headers.TryGetValues("x-amz-bucket-region", out var values) || !values.Any()) {
                _logger.Log(Warning.ManifestLoaderCouldNotDetectBucketRegion(bucketName));
                return null;
            }

            // create a bucket region specific S3 client
            result = new AmazonS3Client(RegionEndpoint.GetBySystemName(values.First()));
            _s3ClientByBucketName[bucketName] = result;
            return result;
        }

        //--- ILogger Members ---
        void ILogger.Log(IBuildReportEntry entry, SourceLocation sourceLocation, bool exact)
            => _logger.Log(entry, sourceLocation, exact);
    }
}
