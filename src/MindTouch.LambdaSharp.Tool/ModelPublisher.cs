/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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

using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using MindTouch.LambdaSharp.Tool.Model;
using Newtonsoft.Json;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelPublisher : AModelProcessor {

        //--- Fields ---
        private readonly TransferUtility _transferUtility;
        private bool _changesDetected;

        //--- Constructors ---
        public ModelPublisher(Settings settings, string sourceFilename) : base(settings, sourceFilename) {
            _transferUtility = new TransferUtility(settings.S3Client);
        }

        //--- Methods ---
        public async Task<string> PublishAsync(ModuleManifest manifest) {
            Console.WriteLine($"Publishing module: {manifest.ModuleName}");
            _changesDetected = false;

            // verify that all files referenced by manifest exist
            foreach(var file in manifest.FunctionAssets
                .Union(manifest.PackageAssets)
                .Append(manifest.Template)
                .Append("manifest.json")
            ) {
                var filepath =Path.Combine(Settings.OutputDirectory, file);
                if(!File.Exists(filepath)) {
                    AddError($"could not find: '{filepath}'");
                }
            }
            if(Settings.HasErrors) {
                return null;
            }

            // upload assets
            for(var i = 0; i < manifest.FunctionAssets.Count; ++i) {
                manifest.FunctionAssets[i] = await UploadPackageAsync(manifest, manifest.FunctionAssets[i], "function");
            }
            for(var i = 0; i < manifest.PackageAssets.Count; ++i) {
                manifest.PackageAssets[i] = await UploadPackageAsync(manifest, manifest.PackageAssets[i], "package");
            }

            // upload CloudFormation template
            manifest.Template = await UploadTemplateFileAsync(manifest, manifest.Template, "template");

            // upload LambdaSharp manifest
            var manifestPath = await UploadManifestAsync(manifest, "manifest");
            if(_changesDetected) {

                // store copy of cloudformation template under version number
                await Settings.S3Client.CopyObjectAsync(new CopyObjectRequest {
                    SourceBucket = Settings.DeploymentBucketName,
                    SourceKey = manifest.Template,
                    DestinationBucket = Settings.DeploymentBucketName,
                    DestinationKey = $"Modules/{manifest.ModuleName}/Versions/{manifest.ModuleVersion}/cloudformation.json",
                    ContentType = "application/json"
                });

                // store copy of cloudformation template under version
                await Settings.S3Client.CopyObjectAsync(new CopyObjectRequest {
                    SourceBucket = Settings.DeploymentBucketName,
                    SourceKey = manifestPath,
                    DestinationBucket = Settings.DeploymentBucketName,
                    DestinationKey = $"Modules/{manifest.ModuleName}/Versions/{manifest.ModuleVersion}/manifest.json"
                });
            } else {
                Console.WriteLine($"=> No changes found to publish");
            }
            return $"s3://{Settings.DeploymentBucketName}/{manifestPath}";
        }

        private async Task<string> UploadTemplateFileAsync(ModuleManifest manifest, string relativeFilePath, string description) {
            var filePath = Path.Combine(Settings.OutputDirectory, relativeFilePath);
            var minified = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(File.ReadAllText(filePath)), Formatting.None);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            // upload minified json
            var key = $"Modules/{manifest.ModuleName}/Assets/cloudformation_v{manifest.ModuleVersion}_{manifest.Hash}.json";
            if(!await S3ObjectExistsAsync(key)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{key}");
                await Settings.S3Client.PutObjectAsync(new PutObjectRequest {
                    BucketName = Settings.DeploymentBucketName,
                    ContentBody = minified,
                    ContentType = "application/json",
                    Key = key,
                });
                _changesDetected = true;
            }
            return key;
        }

        private async Task<string> UploadManifestAsync(ModuleManifest manifest, string description) {
            var minified = JsonConvert.SerializeObject(manifest, Formatting.None);
            var filenameWithoutExtension = "manifest";
            var key = $"Modules/{manifest.ModuleName}/Assets/{filenameWithoutExtension}_v{manifest.ModuleVersion}_{manifest.Hash}.json";

            // upload minified json
            if(!await S3ObjectExistsAsync(key)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{key}");
                await Settings.S3Client.PutObjectAsync(new PutObjectRequest {
                    BucketName = Settings.DeploymentBucketName,
                    ContentBody = minified,
                    ContentType = "application/json",
                    Key = key,
                });
                _changesDetected = true;
            }
            return key;
        }

        private async Task<string> UploadPackageAsync(ModuleManifest manifest, string relativeFilePath, string description) {
            var filePath = Path.Combine(Settings.OutputDirectory, relativeFilePath);
            var key = $"Modules/{manifest.ModuleName}/Assets/{Path.GetFileName(filePath)}";

            // only upload files that don't exist
            if(!await S3ObjectExistsAsync(key)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{key}");
                await _transferUtility.UploadAsync(filePath, Settings.DeploymentBucketName, key);
                _changesDetected = true;
            }
            return key;
        }

        private async Task<bool> S3ObjectExistsAsync(string key) {
            var found = false;
            try {
                await Settings.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                    BucketName = Settings.DeploymentBucketName,
                    Key = key
                });
                found = true;
            } catch { }
            return found;
        }
    }
}