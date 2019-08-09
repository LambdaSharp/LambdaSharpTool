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

using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using LambdaSharp;
using LambdaSharp.CustomResource;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.S3.IO.S3Writer {

    public class S3WriterResourceProperties {

        //--- Properties ---
        public string ResourceType { get; set; }

        /*
         * LambdaSharp::S3::Unzip
         *
         * DestinationBucket: String
         * DestinationKey: String
         * SourceBucket: String
         * SourceKey: String
         */
        public string DestinationBucket { get; set; }
        public string DestinationKey { get; set; }
        public string SourceBucket { get; set; }
        public string SourceKey { get; set; }

        /*
         * LambdaSharp::S3::WriteJson
         *
         * Bucket: String
         * Key: String
         * Contents: Json
         */
        public string Bucket { get; set; }
        public string Key { get; set; }
        public object Contents { get; set; }

        /*
         * LambdaSharp::S3::EmptyBucket
         *
         * Bucket: String
         * Enabled: Boolean
         */
        public bool? Enabled { get; set; }

        public string DestinationBucketName => (DestinationBucket?.StartsWith("arn:") == true)
            ? AwsConverters.ConvertBucketArnToName(DestinationBucket)
            : DestinationBucket;

        public string SourceBucketName => (SourceBucket?.StartsWith("arn:") == true)
            ? AwsConverters.ConvertBucketArnToName(SourceBucket)
            : SourceBucket;

        public string BucketName => (Bucket?.StartsWith("arn:") == true)
            ? AwsConverters.ConvertBucketArnToName(Bucket)
            : Bucket;
    }

    public class S3WriterResourceAttribute {

        //--- Properties ---
        public string Url { get; set; }
        public string BucketName { get; set; }
    }

    public class Function : ALambdaCustomResourceFunction<S3WriterResourceProperties, S3WriterResourceAttribute> {

        //--- Fields ---
        private string _manifestBucket;
        private IAmazonS3 _s3Client;
        private UnzipLogic _unzipLogic;
        private WriteJsonLogic _writeJsonLogic;
        private EmptyBucketLogic _emptyBucketLogic;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _manifestBucket = config.ReadS3BucketName("ManifestBucket");
            _s3Client = new AmazonS3Client();
            _unzipLogic = new UnzipLogic(Logger, _manifestBucket, _s3Client);
            _writeJsonLogic = new WriteJsonLogic(Logger, _s3Client);
            _emptyBucketLogic = new EmptyBucketLogic(Logger, _s3Client);
        }

        public override async Task<Response<S3WriterResourceAttribute>> ProcessCreateResourceAsync(Request<S3WriterResourceProperties> request) {
            switch(request.ResourceProperties.ResourceType) {
            case "LambdaSharp::S3::Unzip":
                return await _unzipLogic.Create(request.ResourceProperties);
            case "LambdaSharp::S3::WriteJson":
                return await _writeJsonLogic.Create(request.ResourceProperties);
            case "LambdaSharp::S3::EmptyBucket":
                return await _emptyBucketLogic.Create(request.ResourceProperties);
            default:
                throw new InvalidOperationException($"unsupported resource type: {request.ResourceType}");
            }
        }

        public override async Task<Response<S3WriterResourceAttribute>> ProcessUpdateResourceAsync(Request<S3WriterResourceProperties> request) {
            switch(request.ResourceProperties.ResourceType) {
            case "LambdaSharp::S3::Unzip":
                return await _unzipLogic.Update(request.OldResourceProperties, request.ResourceProperties);
            case "LambdaSharp::S3::WriteJson":
                return await _writeJsonLogic.Update(request.OldResourceProperties, request.ResourceProperties);
            case "LambdaSharp::S3::EmptyBucket":
                return await _emptyBucketLogic.Update(request.OldResourceProperties, request.ResourceProperties);
            default:
                throw new InvalidOperationException($"unsupported resource type: {request.ResourceType}");
            }
        }

        public override async Task<Response<S3WriterResourceAttribute>> ProcessDeleteResourceAsync(Request<S3WriterResourceProperties> request) {
            switch(request.ResourceProperties.ResourceType) {
            case "LambdaSharp::S3::Unzip":
                return await _unzipLogic.Delete(request.ResourceProperties);
            case "LambdaSharp::S3::WriteJson":
                return await _writeJsonLogic.Delete(request.ResourceProperties);
            case "LambdaSharp::S3::EmptyBucket":
                return await _emptyBucketLogic.Delete(request.ResourceProperties);
            default:

                // nothing to do since we didn't process this request successfully in the first place!
                return new Response<S3WriterResourceAttribute>();
           }
        }
    }
}