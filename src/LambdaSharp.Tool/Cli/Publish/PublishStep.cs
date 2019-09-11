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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Publish {

    public class PublishStep : AModelProcessor {

        //--- Constants ---
        private const string AMAZON_METADATA_ORIGIN = "x-amz-meta-lambdasharp-origin";

        //--- Fields ---
        private readonly ModelManifestLoader _loader;
        private readonly TransferUtility _transferUtility;
        private bool _changesDetected;
        private bool _forcePublish;

        //--- Constructors ---
        public PublishStep(Settings settings, string sourceFilename) : base(settings, sourceFilename) {
            _loader = new ModelManifestLoader(Settings, "cloudformation.json");
            _transferUtility = new TransferUtility(Settings.S3Client);
        }

        //--- Methods---
        public async Task<ModuleInfo> DoAsync(
            string cloudformationFile,
            bool forcePublish,
            string moduleOrigin
        ) {
            _forcePublish = forcePublish;
            _changesDetected = false;

            // make sure there is a deployment bucket
            if(Settings.DeploymentBucketName == null) {
                LogError("missing deployment bucket", new LambdaSharpDeploymentTierSetupException(Settings.TierName));
                return null;
            }

            // load cloudformation template
            if(!File.Exists(cloudformationFile)) {
                LogError("folder does not contain a CloudFormation file for publishing");
                return null;
            }

            // load cloudformation file
            if(!_loader.TryLoadFromFile(cloudformationFile, out var manifest)) {
                return null;
            }

            // update module origin
            var moduleInfo = manifest.ModuleInfo.WithOrigin(moduleOrigin ?? Settings.DeploymentBucketName);
            manifest.ModuleInfo = moduleInfo;

            // check if we want to always publish
            if(!forcePublish) {

                // check if module has a stable version, but is compiled from a dirty git branch
                if(!moduleInfo.Version.IsPreRelease && (manifest.Git.SHA?.StartsWith("DIRTY-") ?? false)) {
                    LogError($"attempting to publish an immutable release of {moduleInfo.FullName} (v{moduleInfo.Version}) with uncommitted/untracked changes; use --force-publish to proceed anyway");
                    return null;
                }

                // check if a manifest already exists for this version
                if(!moduleInfo.Version.IsPreRelease && await Settings.S3Client.DoesS3ObjectExistAsync(Settings.DeploymentBucketName, moduleInfo.VersionPath)) {
                    LogError($"{moduleInfo.FullName} (v{moduleInfo.Version}) is already published; use --force-publish to proceed anyway");
                    return null;
                }
            }

            // publish module
            Console.WriteLine($"Publishing module: {manifest.GetFullName()}");

            // verify that all files referenced by manifest exist (NOTE: source file was already checked)
            foreach(var file in manifest.Artifacts) {
                var filepath = Path.Combine(Settings.OutputDirectory, file);
                if(!File.Exists(filepath)) {
                    LogError($"could not find: '{filepath}'");
                }
            }
            if(HasErrors) {
                return null;
            }

            // import module dependencies
            if(!await ImportDependencies(manifest)) {
                return null;
            }

            // upload artifacts
            for(var i = 0; i < manifest.Artifacts.Count; ++i) {
                await UploadArtifactAsync(manifest, manifest.Artifacts[i], "artifact");
            }

            // upload CloudFormation template
            var templateKey = await UploadTemplateFileAsync(manifest, "template");

            // upload manifest under version number
            if(_changesDetected) {
                var request = new TransferUtilityUploadRequest {
                    InputStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(manifest, new JsonSerializerSettings {
                        Formatting = Formatting.None,
                        NullValueHandling = NullValueHandling.Ignore
                    }))),
                    BucketName = Settings.DeploymentBucketName,
                    ContentType = "application/json",
                    Key = moduleInfo.VersionPath
                };
                request.Metadata[AMAZON_METADATA_ORIGIN] = Settings.DeploymentBucketName;
                await _transferUtility.UploadAsync(request);
            } else {
                Settings.WriteAnsiLine($"=> No changes found to upload", AnsiTerminal.BrightBlack);
            }
            return manifest.ModuleInfo;
        }

        public async Task<bool> DoImportAsync(ModuleInfo moduleInfo, bool forcePublish) {

            // check if module has already been imported
            if(
                !forcePublish
                && !(moduleInfo.Version?.IsPreRelease ?? false)
                && await Settings.S3Client.DoesS3ObjectExistAsync(Settings.DeploymentBucketName, moduleInfo.VersionPath)
            ) {
                return true;
            }

            // find manifest for module to import
            var moduleLocation = await _loader.ResolveInfoToLocationAsync(moduleInfo, ModuleManifestDependencyType.Root, allowImport: true, showError: true);
            if(moduleLocation == null) {

                // nothing to do; loader already emitted an error
                return false;
            }
            var manifest = await _loader.LoadManifestFromLocationAsync(moduleLocation);
            if(manifest == null) {

                // error has already been reported
                return false;
            }

            // import module dependencies
            if(!await ImportDependencies(manifest)) {

                // error has already been reported
                return false;
            }

            // import module
            var imported = false;
            foreach(var artifact in manifest.Artifacts) {
                imported = imported | await ImportS3Object(moduleInfo.Origin, artifact, replace: forcePublish);
            }
            imported = imported | await ImportS3Object(moduleInfo.Origin, moduleInfo.VersionPath, replace: forcePublish || moduleInfo.Version.IsPreRelease);
            if(imported) {
                Console.WriteLine($"=> Imported {moduleInfo}");
            } else {
                Console.WriteLine($"=> Nothing to do");
            }
            return true;
        }

        private async Task<bool> ImportDependencies(ModuleManifest manifest) {

            // discover module dependencies
            var dependencies = await _loader.DiscoverAllDependenciesAsync(manifest, checkExisting: false, allowImport: true);
            if(HasErrors) {
                return false;
            }

            // copy all dependencies to deployment bucket that are missing or have a pre-release version
            foreach(var dependency in dependencies.Where(dependency => dependency.ModuleLocation.SourceBucketName != Settings.DeploymentBucketName)) {
                var imported = false;

                // copy check-summed module artifacts (guaranteed immutable)
                foreach(var artifact in dependency.Manifest.Artifacts) {
                    imported = imported | await ImportS3Object(dependency.ModuleLocation.ModuleInfo.Origin, artifact);
                }

                // copy version manifest
                imported = imported | await ImportS3Object(dependency.ModuleLocation.ModuleInfo.Origin, dependency.ModuleLocation.ModuleInfo.VersionPath, replace: dependency.ModuleLocation.ModuleInfo.Version.IsPreRelease);

                // show message if any artifacts were imported
                if(imported) {
                    Console.WriteLine($"=> Imported {dependency.ModuleLocation.ModuleInfo}");
                }
            }
            return true;
        }

        private async Task<string> UploadTemplateFileAsync(ModuleManifest manifest, string description) {
            var moduleInfo = manifest.ModuleInfo;

            // rewrite artifacts in manifest to have an absolute path
            manifest.Artifacts = manifest.Artifacts
                .OrderBy(artifact => artifact)
                .Select(artifact => moduleInfo.GetArtifactPath(artifact)).ToList();

            // add template to list of artifacts
            var destinationKey = manifest.GetModuleTemplatePath();
            manifest.Artifacts.Insert(0, destinationKey);

            // update cloudformation template with manifest and minify it
            var template = File.ReadAllText(SourceFilename)
                .Replace(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER, moduleInfo.Origin ?? throw new ApplicationException("missing Origin information"));
            var cloudformation = JObject.Parse(template);
            ((JObject)cloudformation["Metadata"])["LambdaSharp::Manifest"] = JObject.FromObject(manifest, new JsonSerializer {
                NullValueHandling = NullValueHandling.Ignore
            });
            var minified = JsonConvert.SerializeObject(cloudformation, new JsonSerializerSettings {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });

            // upload minified json
            if(_forcePublish || !await DoesS3ObjectExistsAsync(destinationKey)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{destinationKey}");
                var request = new TransferUtilityUploadRequest {
                    InputStream = new MemoryStream(Encoding.UTF8.GetBytes(minified)),
                    BucketName = Settings.DeploymentBucketName,
                    ContentType = "application/json",
                    Key = destinationKey
                };
                request.Metadata[AMAZON_METADATA_ORIGIN] = Settings.DeploymentBucketName;
                await _transferUtility.UploadAsync(request);
                _changesDetected = true;
            }
            return destinationKey;
        }

        private async Task<string> UploadArtifactAsync(ModuleManifest manifest, string relativeFilePath, string description) {
            var filePath = Path.Combine(Settings.OutputDirectory, relativeFilePath);

            // only upload files that don't exist
            var destinationKey = manifest.ModuleInfo.GetArtifactPath(Path.GetFileName(filePath));
            if(_forcePublish || !await DoesS3ObjectExistsAsync(destinationKey)) {
                Console.WriteLine($"=> Uploading {description}: s3://{Settings.DeploymentBucketName}/{destinationKey}");
                var request = new TransferUtilityUploadRequest {
                    FilePath = filePath,
                    BucketName = Settings.DeploymentBucketName,
                    Key = destinationKey
                };
                request.Metadata[AMAZON_METADATA_ORIGIN] = Settings.DeploymentBucketName;
                await _transferUtility.UploadAsync(request);
                _changesDetected = true;
            }
            return destinationKey;
        }

        private Task<bool> DoesS3ObjectExistsAsync(string key) => Settings.S3Client.DoesS3ObjectExistAsync(Settings.DeploymentBucketName, key);

        private async Task<bool> ImportS3Object(string sourceBucket, string key, bool replace = false) {

            // check if target object already exists
            var found = false;
            try {
                var existing = await Settings.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                    BucketName = Settings.DeploymentBucketName,
                    Key = key,
                    RequestPayer = RequestPayer.Requester
                });
                found = true;

                // check if this object was uploaded locally and therefore should not be replaced
                if(existing.Metadata[AMAZON_METADATA_ORIGIN] == Settings.DeploymentBucketName) {
                    LogWarn($"skipping import of 's3://{sourceBucket}/{key}' because it was published locally");
                    return false;
                }
            } catch { }
            if(!found || replace) {
                var request = new CopyObjectRequest {
                    SourceBucket = sourceBucket,
                    SourceKey = key,
                    DestinationBucket = Settings.DeploymentBucketName,
                    DestinationKey = key,
                    MetadataDirective = Amazon.S3.S3MetadataDirective.COPY,
                    RequestPayer = RequestPayer.Requester
                };

                // capture the origin of this object
                request.Metadata[AMAZON_METADATA_ORIGIN] = sourceBucket;
                try {
                    await Settings.S3Client.CopyObjectAsync(request);
                } catch(AmazonS3Exception) {
                    LogError($"unable to copy 's3://{sourceBucket}/{key}' to deployment bucket");
                    return false;
                }
                 _changesDetected = true;
                return true;
           }
           return false;
        }
    }
}