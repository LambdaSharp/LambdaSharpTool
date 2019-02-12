/*
 * MindTouch λ#
 * Copyright (C) 2006-2018-2019 MindTouch, Inc.
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3.Model;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool {

    public class ModelManifestLoader : AModelProcessor {

        //--- Constructors --
        public ModelManifestLoader(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public async Task<ModuleManifest> LoadFromFileAsync(string filepath) {

            // load cloudformation template
            var template = await File.ReadAllTextAsync(filepath);
            var cloudformation = JsonConvert.DeserializeObject<JObject>(template);

            // extract manifest
            var manifest = GetManifest(cloudformation);
            if(manifest == null) {
                AddError("CloudFormation file does not contain a LambdaSharp manifest");
                return null;
            }

            // validate manifest
            if(manifest.Version != ModuleManifest.CurrentVersion) {
                AddError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                return null;
            }
            return manifest;
        }

        public async Task<ModuleManifest> LoadFromS3Async(string bucketName, string templatePath) {

            // download cloudformation template
            var cloudformationText = await GetS3ObjectContents(bucketName, templatePath);
            if(cloudformationText == null) {
                AddError($"could not load CloudFormation template from s3://{bucketName}/{templatePath}");
                return null;
            }

            // extract manifest
            var cloudformation = JsonConvert.DeserializeObject<JObject>(cloudformationText);
            var manifest = GetManifest(cloudformation);
            if(manifest == null) {
                AddError("CloudFormation file does not contain a LambdaSharp manifest");
                return null;
            }

            // validate manifest
            if(manifest.Version != ModuleManifest.CurrentVersion) {
                AddError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                return null;
            }
            return manifest;
        }

        public async Task<ModuleLocation> LocateAsync(string moduleReference) {

            // module reference formats:
            // * ModuleOwner.ModuleName
            // * ModuleOwner.ModuleName:*
            // * ModuleOwner.ModuleName:Version
            // * ModuleOwner.ModuleName@Bucket
            // * ModuleOwner.ModuleName:*@Bucket
            // * ModuleOwner.ModuleName:Version@Bucket
            // * s3://bucket-name/{ModuleOwner}/Modules/{ModuleName}/Versions/{Version}/
            // * s3://bucket-name/{ModuleOwner}/Modules/{ModuleName}/Versions/{Version}/cloudformation.json

            if(!moduleReference.TryParseModuleDescriptor(
                out string moduleOwner,
                out string moduleName,
                out VersionInfo moduleVersion,
                out string moduleBucketName
            )) {
                return null;
            }
            if((moduleVersion == null) || (moduleBucketName == null)) {

                // find compatible module version
                return await LocateAsync(moduleOwner, moduleName, moduleVersion, moduleVersion, moduleBucketName);
            }
            return new ModuleLocation {
                ModuleFullName = $"{moduleOwner}.{moduleName}",
                ModuleVersion = moduleVersion,
                ModuleBucketName = moduleBucketName,
                TemplatePath = $"{moduleOwner}/Modules/{moduleName}/Versions/{moduleVersion}/cloudformation.json"
            };
        }

        public async Task<ModuleLocation> LocateAsync(string moduleOwner, string moduleName, VersionInfo minVersion, VersionInfo maxVersion, string bucketName) {

            // by default, attempt to find the module in the deployment bucket and then the regional lambdasharp bucket
            var searchBucketNames = (bucketName != null)
                ? new List<string> { bucketName.Replace("${AWS::Region}", Settings.AwsRegion) }
                : Settings.ModuleBucketNames;

            // attempt to find a matching version
            VersionInfo foundVersion = null;
            string foundBucketName = null;
            foreach(var bucket in searchBucketNames) {
                foundVersion = await FindNewestVersion(Settings, bucket, moduleOwner, moduleName, minVersion, maxVersion);
                if(foundVersion != null) {
                    foundBucketName = bucket;
                    break;
                }
            }
            if(foundVersion == null) {
                var versionConstraint = "any version";
                if((minVersion != null) && (maxVersion != null)) {
                    if(minVersion == maxVersion) {
                        versionConstraint = $"v{minVersion}";
                    } else {
                        versionConstraint = $"v{minVersion}..v{maxVersion}";
                    }
                } else if(minVersion != null) {
                    versionConstraint = $"v{minVersion} or later";
                } else if(maxVersion != null) {
                    versionConstraint = $"v{maxVersion} or earlier";
                }
                AddError($"could not find module: {moduleOwner}.{moduleName} ({versionConstraint})");
                return null;
            }
            return new ModuleLocation(moduleOwner, moduleName, foundVersion, foundBucketName) {
                TemplatePath = $"{moduleOwner}/Modules/{moduleName}/Versions/{foundVersion}/cloudformation.json"
            };
        }

        private async Task<string> GetS3ObjectContents(string bucketName, string key) {
            try {
                var response = await Settings.S3Client.GetObjectAsync(new GetObjectRequest {
                    BucketName = bucketName,
                    Key = key
                });
                using(var stream = new MemoryStream()) {
                    await response.ResponseStream.CopyToAsync(stream);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            } catch {
                return null;
            }
        }

        private ModuleManifest GetManifest(JObject cloudformation) {
            if(
                cloudformation.TryGetValue("Metadata", out var metadataToken)
                && (metadataToken is JObject metadata)
                && metadata.TryGetValue("LambdaSharp::Manifest", out var manifestToken)
            ) {
                return manifestToken.ToObject<ModuleManifest>();
            }
            return null;
        }

        private async Task<VersionInfo> FindNewestVersion(Settings settings, string bucketName, string moduleOwner, string moduleName, VersionInfo minVersion, VersionInfo maxVersion) {

            // enumerate versions in bucket
            var versions = new List<VersionInfo>();
            var request = new ListObjectsV2Request {
                BucketName = bucketName,
                Prefix = $"{moduleOwner}/Modules/{moduleName}/Versions/",
                Delimiter = "/",
                MaxKeys = 100
            };
            do {
                var response = await settings.S3Client.ListObjectsV2Async(request);
                versions.AddRange(response.CommonPrefixes
                    .Select(prefix => prefix.Substring(request.Prefix.Length).TrimEnd('/'))
                    .Select(found => VersionInfo.Parse(found))
                    .Where(IsVersionMatch)
                );
                request.ContinuationToken = response.NextContinuationToken;
            } while(request.ContinuationToken != null);
            if(!versions.Any()) {
                return null;
            }

            // attempt to identify the newest version
            return versions.Max();

            // local function
            bool IsVersionMatch(VersionInfo version) {
                if((minVersion == null) && (maxVersion == null)) {
                    return !version.IsPreRelease;
                }
                if(maxVersion == minVersion) {
                    return version.IsCompatibleWith(minVersion);
                }
                if((minVersion != null) && (version < minVersion)) {
                    return false;
                }
                if((maxVersion != null) && (version > maxVersion)) {
                    return false;
                }
                return true;
            }
        }
    }
}