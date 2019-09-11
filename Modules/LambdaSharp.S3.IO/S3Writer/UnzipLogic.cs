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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using LambdaSharp.CustomResource;
using LambdaSharp.Logger;

namespace LambdaSharp.S3.IO.S3Writer {

    public class UnzipLogic {

        //--- Class Methods ---
        private static string ToHexString(IEnumerable<byte> bytes)
            => string.Concat(bytes.Select(x => x.ToString("X2")));

        private static string GetMD5Hash(MemoryStream stream) {
            using(var md5 = MD5.Create()) {
                stream.Position = 0;
                var result = ToHexString(md5.ComputeHash(stream));
                stream.Position = 0;
                return result;
            }
        }

        //--- Constants ---
        private const int MAX_BATCH_DELETE_OBJECTS = 1000;

        //--- Fields ---
        private readonly ILambdaLogLevelLogger _logger;
        private readonly string _manifestBucket;
        private readonly IAmazonS3 _s3Client;
        private readonly TransferUtility _transferUtility;

        //--- Constructors ---
        public UnzipLogic(ILambdaLogLevelLogger logger, string manifestBucket, IAmazonS3 s3Client) {
            _logger = logger;
            _manifestBucket = manifestBucket;
            _s3Client = new AmazonS3Client();
            _transferUtility = new TransferUtility(_s3Client);
        }

        //--- Methods ---
        public async Task<Response<S3WriterResourceAttribute>> Create(S3WriterResourceProperties properties) {
            _logger.LogInfo($"copying package s3://{properties.SourceBucketName}/{properties.SourceKey} to S3 bucket {properties.DestinationBucketName}");

            // download package and copy all files to destination bucket
            var fileEntries = new Dictionary<string, string>();
            if(!await ProcessZipFileItemsAsync(properties.SourceBucketName, properties.SourceKey, async entry => {
                using(var stream = entry.Open()) {
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var hash = GetMD5Hash(memoryStream);
                    var destination = Path.Combine(properties.DestinationKey, entry.FullName).Replace('\\', '/');
                    _logger.LogInfo($"uploading file: {destination}");
                    await _transferUtility.UploadAsync(
                        memoryStream,
                        properties.DestinationBucketName,
                        destination
                    );
                    fileEntries.Add(entry.FullName, hash);
                }
            })) {
                throw new FileNotFoundException("Unable to download source package");
            }

            // create package manifest for future deletion
            _logger.LogInfo($"uploaded {fileEntries.Count:N0} files");
            await WriteManifest(properties, fileEntries);
            return new Response<S3WriterResourceAttribute> {
                PhysicalResourceId = $"s3unzip:{properties.DestinationBucketName}:{properties.DestinationKey}",
                Attributes = new S3WriterResourceAttribute {
                    Url = $"s3://{properties.DestinationBucketName}/{properties.DestinationKey}"
                }
            };
        }

        public async Task<Response<S3WriterResourceAttribute>> Update(S3WriterResourceProperties oldProperties, S3WriterResourceProperties properties) {

            // check if the unzip properties have changed
            if(
                (oldProperties.DestinationBucketName != properties.DestinationBucketName)
                || (oldProperties.DestinationKey != properties.DestinationKey)
            ) {
                _logger.LogInfo($"replacing package s3://{properties.SourceBucketName}/{properties.SourceKey} in S3 bucket {properties.DestinationBucketName}");

                // remove old file and upload new ones; don't try to compute a diff
                await Delete(oldProperties);
                return await Create(properties);
            } else {
                _logger.LogInfo($"updating package {properties.SourceKey} in S3 bucket {properties.DestinationBucketName}");

                // download old package manifest
                var oldFileEntries = await ReadAndDeleteManifest(oldProperties);
                if(oldFileEntries == null) {

                    // unable to download the old manifest; continue with uploading new files
                    return await Create(properties);
                }

                // download new source package
                var newFileEntries = new Dictionary<string, string>();
                var uploadedCount = 0;
                var skippedCount = 0;
                if(!await ProcessZipFileItemsAsync(properties.SourceBucketName, properties.SourceKey, async entry => {
                    using(var stream = entry.Open()) {
                        var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        var hash = GetMD5Hash(memoryStream);

                        // only upload file if new or the contents have changed
                        if(!oldFileEntries.TryGetValue(entry.FullName, out var existingHash) || (existingHash != hash)) {
                            var destination = Path.Combine(properties.DestinationKey, entry.FullName).Replace('\\', '/');
                            _logger.LogInfo($"uploading file: {destination}");
                            await _transferUtility.UploadAsync(
                                memoryStream,
                                properties.DestinationBucketName,
                                destination
                            );
                            ++uploadedCount;
                        } else {
                           ++skippedCount;
                        }
                        newFileEntries.Add(entry.FullName, hash);
                    }
                })) {
                    throw new FileNotFoundException("Unable to download source package");
                }

                // create package manifest for future deletion
                _logger.LogInfo($"uploaded {uploadedCount:N0} files");
                _logger.LogInfo($"skipped {skippedCount:N0} unchanged files");
                await WriteManifest(properties, newFileEntries);

                // delete files that are no longer needed
                await BatchDeleteFiles(properties.DestinationBucketName, oldFileEntries.Where(kv => !newFileEntries.ContainsKey(kv.Key)).Select(kv => Path.Combine(properties.DestinationKey, kv.Key)).ToList());
                return new Response<S3WriterResourceAttribute> {
                    PhysicalResourceId = $"s3unzip:{properties.DestinationBucketName}:{properties.DestinationKey}",
                    Attributes = new S3WriterResourceAttribute {
                        Url = $"s3://{properties.DestinationBucketName}/{properties.DestinationKey}"
                    }
                };
            }
        }

        public async Task<Response<S3WriterResourceAttribute>> Delete(S3WriterResourceProperties properties) {
            _logger.LogInfo($"deleting package {properties.SourceKey} from S3 bucket {properties.DestinationBucketName}");

            // download package manifest
            var fileEntries = await ReadAndDeleteManifest(properties);
            if(fileEntries == null) {
                return new Response<S3WriterResourceAttribute>();
            }

            // delete all files from manifest
            await BatchDeleteFiles(
                properties.DestinationBucketName,
                fileEntries.Select(kv => Path.Combine(properties.DestinationKey, kv.Key)).ToList()
            );
            return new Response<S3WriterResourceAttribute>();
        }

        private async Task<bool> ProcessZipFileItemsAsync(string bucketName, string key, Func<ZipArchiveEntry, Task> callbackAsync) {
            var tmpFilename = Path.GetTempFileName() + ".zip";
            try {
                _logger.LogInfo($"downloading s3://{bucketName}/{key}");
                await _transferUtility.DownloadAsync(new TransferUtilityDownloadRequest {
                    BucketName = bucketName,
                    Key = key,
                    FilePath = tmpFilename
                });
            } catch(Exception e) {
                _logger.LogErrorAsWarning(e, "s3 download failed");
                return false;
            }
            try {
                using(var zip = ZipFile.Open(tmpFilename, ZipArchiveMode.Read)) {
                    foreach(var entry in zip.Entries) {
                        await callbackAsync(entry);
                    }
                }
            } finally {
                try {
                    File.Delete(tmpFilename);
                } catch { }
            }
            return true;
        }

        private async Task WriteManifest(S3WriterResourceProperties properties, Dictionary<string, string> fileEntries) {
            var manifestStream = new MemoryStream();
            using(var manifest = new ZipArchive(manifestStream, ZipArchiveMode.Create, leaveOpen: true))
            using(var manifestEntryStream = manifest.CreateEntry("manifest.txt").Open())
            using(var manifestEntryWriter = new StreamWriter(manifestEntryStream)) {
                await manifestEntryWriter.WriteAsync(string.Join("\n", fileEntries.Select(file => $"{file.Key}\t{file.Value}")));
            }
            await _transferUtility.UploadAsync(
                manifestStream,
                _manifestBucket,
                $"{properties.DestinationBucketName}/{properties.SourceKey}"
            );
       }

        private async Task<Dictionary<string, string>> ReadAndDeleteManifest(S3WriterResourceProperties properties) {

            // download package manifest
            var fileEntries = new Dictionary<string, string>();
            var key = $"{properties.DestinationBucketName}/{properties.SourceKey}";
            if(!await ProcessZipFileItemsAsync(
                _manifestBucket,
                key,
                async entry => {
                    using(var stream = entry.Open())
                    using(var reader = new StreamReader(stream)) {
                        var manifest = await reader.ReadToEndAsync();
                        foreach(var line in manifest.Split('\n')) {
                            var columns = line.Split('\t');
                            fileEntries.Add(columns[0], columns[1]);
                        }
                    }
                }
            )) {
                _logger.LogWarn($"unable to dowload zip file from s3://{_manifestBucket}/{key}");
                return null;
            }

            // delete manifest after reading it
            try {
                await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {
                    BucketName = _manifestBucket,
                    Key = key
                });
            } catch {
                _logger.LogWarn($"unable to delete manifest file at s3://{_manifestBucket}/{key}");
            }
           return fileEntries;
        }

        private async Task BatchDeleteFiles(string bucketName, IEnumerable<string> keys) {
            if(!keys.Any()) {
                return;
            }
            _logger.LogInfo($"deleting {keys.Count():N0} files");

            // delete all files from manifest
            while(keys.Any()) {
                var batch = keys
                    .Take(MAX_BATCH_DELETE_OBJECTS)
                    .Select(key => key.Replace('\\', '/'))
                    .ToList();
                _logger.LogInfo($"deleting files: {string.Join(", ", batch)}");
                await _s3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                    BucketName = bucketName,
                    Objects = batch.Select(filepath => new KeyVersion {
                        Key = filepath
                    }).ToList(),
                    Quiet = true
                });
                keys = keys.Skip(MAX_BATCH_DELETE_OBJECTS).ToList();
            }
       }
    }
}