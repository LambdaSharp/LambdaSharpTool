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

namespace LambdaSharp.Cloud.UpdateIamSpecFunction;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.CloudFormation.Converter;
using LambdaSharp.Schedule;
using LambdaSharp.Serialization;

public sealed class Function : ALambdaScheduleFunction {

    //--- Fields ---
    private string? _destinationBucketName;
    private IamPermissionsConverter? _converter;
    private IAmazonS3? _s3Client;

    //--- Properties ---
    private string DestinationBucketName => _destinationBucketName ?? throw new InvalidOperationException();
    private IamPermissionsConverter Converter => _converter ?? throw new InvalidOperationException();
    private IAmazonS3 S3Client => _s3Client ?? throw new InvalidOperationException();

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // read function settings
        _destinationBucketName = config.ReadS3BucketName("DestinationBucket");

        // initialize clients
        _converter = new IamPermissionsConverter(
            HttpClient,
            logInfo: message => LogInfo(message),
            logWarn: message => LogWarn(message),
            logError: (exception, message) => LogError(exception, message)
        );
        _s3Client = new AmazonS3Client();
    }

    public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {

        // generate extended IAM specification for region
        LogInfo($"fetching latest IAM specification");
        var specification = await Converter.GenerateIamSpecificationAsync();

        // serialize IAM specification into a brotli compressed stream
        var compressedJsonSpecificationStream = new MemoryStream();
        using(var brotliStream = new BrotliStream(compressedJsonSpecificationStream, CompressionLevel.Optimal, leaveOpen: true)) {
            await JsonSerializer.SerializeAsync(brotliStream, specification, new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            brotliStream.Flush();
        }
        compressedJsonSpecificationStream.Position = 0;

        // compute MD5 of new compressed IAM specification
        byte[] newMD5Hash;
        using(var md5 = MD5.Create()) {
            newMD5Hash = md5.ComputeHash(compressedJsonSpecificationStream);
            compressedJsonSpecificationStream.Position = 0;
        }
        var newETag = $"\"{string.Concat(newMD5Hash.Select(x => x.ToString("x2")))}\"";
        LogInfo($"compressed IAM specification ETag is {newETag} (size: {compressedJsonSpecificationStream.Length:N0} bytes)");

        // update compressed IAM specification in S3
        LogInfo($"uploading new IAM specification");
        await S3Client.PutObjectAsync(new PutObjectRequest {
            BucketName = DestinationBucketName,
            Key = $"AWS/IamSpecification.json.br",
            InputStream = compressedJsonSpecificationStream,
            MD5Digest = Convert.ToBase64String(newMD5Hash),
            Headers = {
                ContentEncoding = "br",
                ContentType = "application/json; charset=utf-8",
                ContentMD5 = newETag
            }
        });
        LogInfo($"done");
    }
}
