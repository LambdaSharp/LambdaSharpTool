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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Publish {

    public class PublishStep : AModelProcessor {

        //--- Fields ---
        private TransferUtility _transferUtility;
        private bool _changesDetected;
        private bool _forcePublish;

        //--- Constructors ---
        public PublishStep(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods---
        public async Task<ModuleInfo> DoAsync(string cloudformationFile, bool forcePublish) {
            _forcePublish = forcePublish;
            _changesDetected = false;
            _transferUtility = new TransferUtility(Settings.S3Client);

            // make sure there is a deployment bucket
            if(Settings.DeploymentBucketName == null) {
                LogError("missing deployment bucket", new LambdaSharpDeploymentTierSetupException(Settings.Tier));
                return null;
            }

            // load cloudformation template
            if(!File.Exists(cloudformationFile)) {
                LogError("folder does not contain a CloudFormation file for publishing");
                return null;
            }

            // load cloudformation file
            var manifest = await new ModelManifestLoader(Settings, "cloudformation.json").LoadFromFileAsync(cloudformationFile);
            if(manifest == null) {
                return null;
            }
            if(!ModuleInfo.TryParse(manifest.Module, out var moduleInfo)) {
                throw new ApplicationException("invalid module info");
            }

            // check if we want to always publish, regardless of version or detected changes
            if(!forcePublish) {

                // check if module has a stable version, but is compiled from a dirty git branch
                if(
                    !moduleInfo.Version.IsPreRelease
                    && (manifest.Git.SHA?.StartsWith("DIRTY-") ?? false)
                ) {
                    LogError($"attempting to publish an immutable release of {moduleInfo.FullName} (v{moduleInfo.Version}) with uncommitted/untracked changes; use --force-publish to proceed anyway");
                    return null;
                }

                // check if a manifest already exists for this version
                var existingManifest = await new ModelManifestLoader(Settings, "cloudformation.json").LoadFromS3Async(moduleInfo, errorIfMissing: false);
                if(existingManifest != null) {
                    if(!moduleInfo.Version.IsPreRelease) {
                        LogWarn($"{moduleInfo.FullName} (v{moduleInfo.Version}) is already published; use --force-publish to proceed anyway");
                        return null;
                    }
                }
            }

            // publish module
            Console.WriteLine($"Publishing module: {manifest.GetFullName()}");

            // verify that all files referenced by manifest exist (NOTE: source file was already checked)
            foreach(var file in manifest.Assets) {
                var filepath = Path.Combine(Settings.OutputDirectory, file);
                if(!File.Exists(filepath)) {
                    LogError($"could not find: '{filepath}'");
                }
            }
            if(Settings.HasErrors) {
                return null;
            }

            // upload assets
            for(var i = 0; i < manifest.Assets.Count; ++i) {
                manifest.Assets[i] = await UploadPackageAsync(manifest, manifest.Assets[i], "asset");
            }

            // upload CloudFormation template
            var template = await UploadTemplateFileAsync(manifest, "template");

            // store copy of cloudformation template under version number
            await Settings.S3Client.CopyObjectAsync(new CopyObjectRequest {
                SourceBucket = Settings.DeploymentBucketName,
                SourceKey = template,
                DestinationBucket = Settings.DeploymentBucketName,
                DestinationKey = moduleInfo.TemplatePath,
                ContentType = "application/json"
            });
            if(!_changesDetected) {

                // NOTE: this message should never appear since we already do a similar check earlier
                Console.WriteLine($"=> No changes found to upload");
            }
            return manifest.GetModuleInfo();
        }

        private async Task<string> UploadTemplateFileAsync(ModuleManifest manifest, string description) {

            // update cloudformation template with manifest and minify it
            var cloudformation = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(SourceFilename));
            ((JObject)cloudformation["Metadata"])["LambdaSharp::Manifest"] = JObject.FromObject(manifest, new JsonSerializer {
                NullValueHandling = NullValueHandling.Ignore
            });
            var minified = JsonConvert.SerializeObject(cloudformation, new JsonSerializerSettings {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });

            // upload minified json
            if(!ModuleInfo.TryParse(manifest.Module, out var moduleInfo)) {
                throw new ApplicationException("invalid module info");
            }

            // TODO: put this in 'ModuleInfo' class since 'TemplatePath' is already there
            var key = $"{moduleInfo.Owner}/Modules/{moduleInfo.Name}/Assets/cloudformation_v{moduleInfo.Version}_{manifest.Hash}.json";
            if(_forcePublish || !await DoesS3ObjectExistsAsync(key)) {
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
            if(!ModuleInfo.TryParse(manifest.Module, out var moduleInfo)) {
                throw new ApplicationException("invalid module info");
            }
            var filePath = Path.Combine(Settings.OutputDirectory, relativeFilePath);

            // TODO: put this in 'ModuleInfo' class since 'TemplatePath' is already there
            var key = $"{moduleInfo.Owner}/Modules/{moduleInfo.Name}/Assets/{Path.GetFileName(filePath)}";

            // only upload files that don't exist
            if(_forcePublish || !await DoesS3ObjectExistsAsync(key)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{key}");
                await _transferUtility.UploadAsync(filePath, Settings.DeploymentBucketName, key);
                _changesDetected = true;
            }
            return key;
        }

        private async Task<bool> DoesS3ObjectExistsAsync(string key) {
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