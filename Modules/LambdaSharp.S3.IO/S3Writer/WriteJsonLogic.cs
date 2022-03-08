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

namespace LambdaSharp.S3.IO.S3Writer;

using System.Text;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.CustomResource;
using LambdaSharp.Logging;

public class WriteJsonLogic {

    //--- Fields ---
    private readonly ILambdaSharpLogger _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly ILambdaSerializer _jsonSerializer;

    //--- Constructors ---
    public WriteJsonLogic(ILambdaSharpLogger logger, IAmazonS3 s3Client, ILambdaSerializer jsonSerializer) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
    }

    //--- Methods ---
    public async Task<Response<S3WriterResourceAttributes>> Create(S3WriterResourceProperties properties) {
        if(properties.BucketName == null) {
            throw new ArgumentNullException(nameof(properties.Bucket));
        }
        if(properties.Key == null) {
            throw new ArgumentNullException(nameof(properties.Key));
        }
        if(properties.Contents == null) {
            throw new ArgumentNullException(nameof(properties.Contents));
        }
        _logger.LogInfo($"writing JSON file to s3://{properties.BucketName}/{properties.Key}");
        var contents = Serialize(properties.Contents);
        await _s3Client.PutObjectAsync(new PutObjectRequest {
            BucketName = properties.BucketName,
            ContentBody = contents,
            ContentType = "application/json",
            Key = properties.Key
        });
        _logger.LogInfo($"JSON file written ({Encoding.UTF8.GetByteCount(contents):N0} bytes)");
        return new Response<S3WriterResourceAttributes> {
            PhysicalResourceId = $"s3writejson:{properties.BucketName}:{properties.Key}",
            Attributes = new S3WriterResourceAttributes {
                Url = $"s3://{properties.BucketName}/{properties.Key}"
            }
        };
    }

    public Task<Response<S3WriterResourceAttributes>> Update(S3WriterResourceProperties oldProperties, S3WriterResourceProperties properties)
        => Create(properties);

    public async Task<Response<S3WriterResourceAttributes>> Delete(S3WriterResourceProperties properties) {
        _logger.LogInfo($"deleting JSON file at s3://{properties.BucketName}/{properties.Key}");
        try {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {
                BucketName = properties.BucketName,
                Key = properties.Key
            });
        } catch(Exception e) {
            _logger.LogErrorAsWarning(e, "unable to delete JSON file at s3://{0}/{1}", properties.BucketName, properties.Key);
        }
        return new Response<S3WriterResourceAttributes>();
    }

    private string Serialize(object contents) {
        using(var stream = new MemoryStream()) {
            _jsonSerializer.Serialize(contents, stream);
            stream.Position = 0;
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
