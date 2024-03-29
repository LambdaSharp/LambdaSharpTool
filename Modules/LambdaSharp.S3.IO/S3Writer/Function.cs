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

namespace LambdaSharp.S3.IO.S3Writer;

using Amazon.S3;
using LambdaSharp.CustomResource;

public class S3WriterResourceProperties {

    //--- Properties ---
    public string? ResourceType { get; set; }

    /*
        * LambdaSharp::S3::Unzip
        *
        * DestinationBucket: String
        * DestinationKey: String
        * SourceBucket: String
        * SourceKey: String
        * Encoding: String
        */
    public string? DestinationBucket { get; set; }
    public string? DestinationKey { get; set; }
    public string? SourceBucket { get; set; }
    public string? SourceKey { get; set; }
    public string? Encoding { get; set; }

    /*
        * LambdaSharp::S3::WriteJson
        *
        * Bucket: String
        * Key: String
        * Contents: Json
        */
    public string? Bucket { get; set; }
    public string? Key { get; set; }
    public object? Contents { get; set; }

    /*
        * LambdaSharp::S3::EmptyBucket
        *
        * Bucket: String
        * Enabled: Boolean
        */
    public bool? Enabled { get; set; }

    public string? DestinationBucketName => (DestinationBucket?.StartsWith("arn:") == true)
        ? AwsConverters.ConvertBucketArnToName(DestinationBucket)
        : DestinationBucket;

    public string? SourceBucketName => (SourceBucket?.StartsWith("arn:") == true)
        ? AwsConverters.ConvertBucketArnToName(SourceBucket)
        : SourceBucket;

    public string? BucketName => (Bucket?.StartsWith("arn:") == true)
        ? AwsConverters.ConvertBucketArnToName(Bucket)
        : Bucket;
}

public class S3WriterResourceAttributes {

    //--- Properties ---
    public string? Url { get; set; }
    public string? BucketName { get; set; }
}

public sealed class Function : ALambdaCustomResourceFunction<S3WriterResourceProperties, S3WriterResourceAttributes> {

    //--- Constants ---
    private const string UNZIP = "LambdaSharp::S3::Unzip";
    private const string WRITEJSON = "LambdaSharp::S3::WriteJson";
    private const string EMPTYBUCKET = "LambdaSharp::S3::EmptyBucket";


    //--- Fields ---
    private string? _manifestBucket;
    private IAmazonS3? _s3Client;
    private UnzipLogic? _unzipLogic;
    private WriteJsonLogic? _writeJsonLogic;
    private EmptyBucketLogic? _emptyBucketLogic;

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Properties ---
    private string ManifestBucket => _manifestBucket ?? throw new InvalidOperationException();
    private IAmazonS3 S3Client => _s3Client ?? throw new InvalidOperationException();
    private UnzipLogic UnzipLogic => _unzipLogic ?? throw new InvalidOperationException();
    private WriteJsonLogic WriteJsonLogic => _writeJsonLogic ?? throw new InvalidOperationException();
    private EmptyBucketLogic EmptyBucketLogic => _emptyBucketLogic ?? throw new InvalidOperationException();

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {
        _manifestBucket = config.ReadS3BucketName("ManifestBucket");
        _s3Client = new AmazonS3Client();
        _unzipLogic = new UnzipLogic(Logger, _manifestBucket, _s3Client);
        _writeJsonLogic = new WriteJsonLogic(Logger, _s3Client, LambdaSerializer);
        _emptyBucketLogic = new EmptyBucketLogic(Logger, _s3Client);
    }

    public override async Task<Response<S3WriterResourceAttributes>> ProcessCreateResourceAsync(Request<S3WriterResourceProperties> request, CancellationToken cancellationToken) {
        switch(request.ResourceProperties?.ResourceType) {
        case UNZIP:
            return await UnzipLogic.Create(request.ResourceProperties);
        case WRITEJSON:
            return await WriteJsonLogic.Create(request.ResourceProperties);
        case EMPTYBUCKET:
            return await EmptyBucketLogic.Create(request.ResourceProperties);
        default:
            throw new InvalidOperationException($"unsupported resource type: {request.ResourceType ?? "<null>"}");
        }
    }

    public override async Task<Response<S3WriterResourceAttributes>> ProcessUpdateResourceAsync(Request<S3WriterResourceProperties> request, CancellationToken cancellationToken) {
        if(request.OldResourceProperties == null) {
            throw new InvalidOperationException($"{nameof(request.OldResourceProperties)} is missing");
        }
        switch(request.ResourceProperties?.ResourceType) {
        case UNZIP:
            return await UnzipLogic.Update(request.OldResourceProperties, request.ResourceProperties);
        case WRITEJSON:
            return await WriteJsonLogic.Update(request.OldResourceProperties, request.ResourceProperties);
        case EMPTYBUCKET:
            return await EmptyBucketLogic.Update(request.OldResourceProperties, request.ResourceProperties);
        default:
            throw new InvalidOperationException($"unsupported resource type: {request.ResourceType ?? "<null>"}");
        }
    }

    public override async Task<Response<S3WriterResourceAttributes>> ProcessDeleteResourceAsync(Request<S3WriterResourceProperties> request, CancellationToken cancellationToken) {
        switch(request.ResourceProperties?.ResourceType) {
        case UNZIP:
            return await UnzipLogic.Delete(request.ResourceProperties);
        case WRITEJSON:
            return await WriteJsonLogic.Delete(request.ResourceProperties);
        case EMPTYBUCKET:
            return await EmptyBucketLogic.Delete(request.ResourceProperties);
        default:

            // nothing to do since we didn't process this request successfully in the first place!
            return new Response<S3WriterResourceAttributes>();
        }
    }
}
