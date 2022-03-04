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

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.CloudFormation.Specification.TypeSystem;
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.CloudFormation {

    public static class CloudFormationLoader {

        //--- Class Methods ---
        public static async Task<(ITypeSystem TypeSystem, bool Cached)> LoadCloudFormationSpecificationAsync(
            string cacheDirecotry,
            string region,
            bool forceRefresh,
            Action<string>? log
        ) {
            if(region is null) {
                throw new ArgumentNullException(nameof(region));
            }
            var cached = false;

            // check if we already have a CloudFormation specification downloaded
            var cloudFormationSpecFile = Path.Combine(cacheDirecotry, "AWS", region, "CloudFormationResourceSpecification.json.br");
            var exists = File.Exists(cloudFormationSpecFile);
            var modifiedSince = exists
                ? File.GetLastWriteTimeUtc(cloudFormationSpecFile)
                : DateTime.MinValue;
            var now = DateTime.UtcNow;

            // check if we have to refresh, if CloudFormation specification doesn't exist, or if it's too old
            if(forceRefresh || !exists || (modifiedSince.AddDays(1) <= now)) {

                // fetch new CloudFormation specification, but only if it has been modified
                var cloudFormationSpecificationKey = $"AWS/{region}/CloudFormationResourceSpecification.json.br";
                var s3ClientUSEast1 = new AmazonS3Client(RegionEndpoint.USEast1);
                try {
                    var response = await s3ClientUSEast1.GetObjectAsync(new GetObjectRequest {
                        BucketName = "lambdasharp",
                        Key = cloudFormationSpecificationKey,
                        RequestPayer = RequestPayer.Requester,
                        ModifiedSinceDateUtc = modifiedSince
                    });
                    log?.Invoke("downloading new CloudFormation specification");

                    // write new CloudFormation specification
                    var cloudFormationSpecFolder = Path.GetDirectoryName(cloudFormationSpecFile) ?? throw new InvalidOperationException("Path.GetDirectoryName(cloudFormationSpecFile) returned null");
                    Directory.CreateDirectory(cloudFormationSpecFolder);
                    using(var outputStream = File.OpenWrite(cloudFormationSpecFile)) {
                        await response.ResponseStream.CopyToAsync(outputStream);
                    }

                    // check if we need to update the LambdaSharp developer copy
                    var lambdaSharpDirectory = Environment.GetEnvironmentVariable("LAMBDASHARP");
                    if(lambdaSharpDirectory != null) {
                        log?.Invoke("updating LambdaSharp contributor CloudFormation specification");
                        using var specFile = File.OpenRead(cloudFormationSpecFile);
                        using var decompressionStream = new BrotliStream(specFile, CompressionMode.Decompress);
                        var document = await JsonSerializer.DeserializeAsync<object>(decompressionStream);
                        await File.WriteAllTextAsync(Path.Combine(lambdaSharpDirectory, "src", "CloudFormationResourceSpecification.json"), JsonSerializer.Serialize(document, new JsonSerializerOptions {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));
                    }
                } catch(AmazonS3Exception e) when(e.StatusCode == HttpStatusCode.NotModified) {
                    log?.Invoke("CloudFormation specification is up-to-date");
                    cached = true;

                    // touch CloudFormation specification to avoid check until is expires again in 24 hours
                    File.SetLastWriteTimeUtc(cloudFormationSpecFile, now);
                }
            } else {
                cached = true;
            }

            // load CloudFormation specification
            using(var stream = File.OpenRead(cloudFormationSpecFile)) {
                using var compression = new BrotliStream(stream, CompressionMode.Decompress);
                var specification = CloudFormationTypeSystem.LoadFromAsync(region, compression).GetAwaiter().GetResult();
                log?.Invoke($"using CloudFormation specification v{specification.Version}");
                return (TypeSystem: specification, Cached: cached);
            }
        }
    }
}