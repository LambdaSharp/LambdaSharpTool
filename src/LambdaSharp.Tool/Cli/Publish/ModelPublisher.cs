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
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Publish {

    public class ModelPublisher : AModelProcessor {

        //--- Fields ---
        private readonly TransferUtility _transferUtility;
        private bool _changesDetected;
        private bool _forcePublish;

        //--- Constructors ---
        public ModelPublisher(Settings settings, string sourceFilename) : base(settings, sourceFilename) {
            _transferUtility = new TransferUtility(settings.S3Client);
        }

        //--- Methods ---
        public async Task<string> PublishAsync(ModuleManifest manifest, bool forcePublish) {
            Console.WriteLine($"Publishing module: {manifest.GetFullName()}");
            _forcePublish = forcePublish;
            _changesDetected = false;

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

            // verify that manifest is either a pre-release or its version has not been published yet
            if(!manifest.Module.TryParseModuleDescriptor(
                out string moduleOwner,
                out string moduleName,
                out VersionInfo moduleVersion,
                out string _
            )) {
                throw new ApplicationException("invalid module info");
            }
            var destinationKey = $"{moduleOwner}/Modules/{moduleName}/Versions/{moduleVersion}/cloudformation.json";

            // check if a manifest already exists for this version
            var existingManifest = await new ModelManifestLoader(Settings, "cloudformation.json").LoadFromS3Async(Settings.DeploymentBucketName, destinationKey, errorIfMissing: false);
            if(existingManifest != null) {
                if(existingManifest.Hash == manifest.Hash) {

                    // manifest matches, nothing further to do
                    Console.WriteLine($"=> No changes found to publish");
                    return $"s3://{Settings.DeploymentBucketName}/{destinationKey}";
                } else if(!moduleVersion.IsPreRelease && !forcePublish) {

                    // don't allow publishing over an existing, stable version
                    LogError($"{moduleOwner}.{moduleName} (v{moduleVersion}) is already published; use --force-publish to proceed anyway");
                    return null;
                }
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
                DestinationKey = destinationKey,
                ContentType = "application/json"
            });
            if(!_changesDetected) {

                // NOTE: this message should never appear since we already do a similar check earlier
                Console.WriteLine($"=> No changes found to upload");
            }
            return $"s3://{Settings.DeploymentBucketName}/{destinationKey}";
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
            if(!manifest.Module.TryParseModuleDescriptor(
                out string moduleOwner,
                out string moduleName,
                out VersionInfo moduleVersion,
                out string _
            )) {
                throw new ApplicationException("invalid module info");
            }
            var key = $"{moduleOwner}/Modules/{moduleName}/Assets/cloudformation_v{moduleVersion}_{manifest.Hash}.json";
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
            if(!manifest.Module.TryParseModuleDescriptor(
                out string moduleOwner,
                out string moduleName,
                out VersionInfo _,
                out string _
            )) {
                throw new ApplicationException("invalid module info");
            }
            var filePath = Path.Combine(Settings.OutputDirectory, relativeFilePath);
            var key = $"{moduleOwner}/Modules/{moduleName}/Assets/{Path.GetFileName(filePath)}";

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