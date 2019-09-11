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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.CustomResource;
using LambdaSharp.Logger;

namespace LambdaSharp.S3.IO.S3Writer {

    public class WriteJsonLogic {

        //--- Fields ---
        private readonly ILambdaLogLevelLogger _logger;
        private readonly IAmazonS3 _s3Client;
        private readonly ILambdaSerializer _jsonSerializer;

        //--- Constructors ---
        public WriteJsonLogic(ILambdaLogLevelLogger logger, IAmazonS3 s3Client) {
            _logger = logger;
            _s3Client = s3Client;
            _jsonSerializer = new JsonSerializer();
        }

        //--- Methods ---
        public async Task<Response<S3WriterResourceAttribute>> Create(S3WriterResourceProperties properties) {
            _logger.LogInfo($"writing JSON file to s3://{properties.BucketName}/{properties.Key}");
            var contents = Serialize(properties.Contents);
            await _s3Client.PutObjectAsync(new PutObjectRequest {
                BucketName = properties.BucketName,
                ContentBody = contents,
                ContentType = "application/json",
                Key = properties.Key
            });
            _logger.LogInfo($"JSON file written ({Encoding.UTF8.GetByteCount(contents):N0} bytes)");
            return new Response<S3WriterResourceAttribute> {
                PhysicalResourceId = $"s3writejson:{properties.BucketName}:{properties.Key}",
                Attributes = new S3WriterResourceAttribute {
                    Url = $"s3://{properties.BucketName}/{properties.Key}"
                }
            };
        }

        public Task<Response<S3WriterResourceAttribute>> Update(S3WriterResourceProperties oldProperties, S3WriterResourceProperties properties)
            => Create(properties);

        public async Task<Response<S3WriterResourceAttribute>> Delete(S3WriterResourceProperties properties) {
            _logger.LogInfo($"deleting JSON file at s3://{properties.BucketName}/{properties.Key}");
            try {
                await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {
                    BucketName = properties.BucketName,
                    Key = properties.Key
                });
            } catch(Exception e) {
                _logger.LogErrorAsWarning(e, "unable to delete JSON file at s3://{0}/{1}", properties.BucketName, properties.Key);
            }
            return new Response<S3WriterResourceAttribute>();
        }

        private string Serialize(object contents) {
            using(var stream = new MemoryStream()) {
                _jsonSerializer.Serialize(contents, stream);
                stream.Position = 0;
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}