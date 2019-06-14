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
        public bool TryLoadFromFile(string filepath, out ModuleManifest manifest) {
            JObject cloudformation;
            try {

                // load cloudformation template
                var template = File.ReadAllText(filepath);
                cloudformation = JObject.Parse(template);
            } catch(Exception) {
                LogError($"invalid CloudFormation template: {filepath}");
                manifest = null;
                cloudformation = null;
                return false;
            }

            // extract manifest
            manifest = GetManifest(cloudformation);
            if(manifest == null) {
                LogError("CloudFormation file does not contain a LambdaSharp manifest");
                return false;
            }

            // validate manifest
            if(manifest.Version != ModuleManifest.CurrentVersion) {
                LogError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                return false;
            }
            return true;
        }

        public async Task<ModuleManifest> LoadFromS3Async(ModuleInfo moduleInfo, bool errorIfMissing = true) {

            // TODO: need to also search 'Settings.DeploymentBucketName'

            // download cloudformation template
            var origin = moduleInfo.Origin ?? Settings.DeploymentBucketName;
            var cloudformationText = await GetS3ObjectContents(origin, moduleInfo.TemplatePath);
            if(cloudformationText == null) {
                if(errorIfMissing) {
                    LogError($"could not load CloudFormation template from s3://{origin}/{moduleInfo.TemplatePath}");
                }
                return null;
            }

            // extract manifest
            var cloudformation = JsonConvert.DeserializeObject<JObject>(cloudformationText);
            var manifest = GetManifest(cloudformation);
            if(manifest == null) {
                LogError("CloudFormation file does not contain a LambdaSharp manifest");
                return null;
            }

            // validate manifest
            if(manifest.Version != ModuleManifest.CurrentVersion) {
                LogError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                return null;
            }
            return manifest;
        }

        public async Task<ModuleInfo> LocateAsync(string moduleOwner, string moduleName, VersionInfo moduleMinVersion, VersionInfo moduleMaxVersion, string moduleOrigin) {

            // by default, attempt to find the module in the deployment bucket and then the regional lambdasharp bucket
            var searchBucketNames = (moduleOrigin != null)
                ? new List<string> { moduleOrigin.Replace("${AWS::Region}", Settings.AwsRegion) }
                : (Settings.ModuleBucketNames ?? new[] { $"lambdasharp-{Settings.AwsRegion}" });

            // attempt to find a matching version
            VersionInfo foundVersion = null;
            string foundOrigin = null;
            foreach(var bucketName in searchBucketNames) {
                foundVersion = await FindNewestVersion(bucketName);
                if(foundVersion != null) {
                    foundOrigin = bucketName;
                    break;
                }
            }
            if(foundVersion == null) {
                var versionConstraint = "any version";
                if((moduleMinVersion != null) && (moduleMaxVersion != null)) {
                    var versionCompare = moduleMinVersion.CompareToVersion(moduleMaxVersion);
                    if(versionCompare == 0) {
                        versionConstraint = $"v{moduleMinVersion}";
                    } else if(versionCompare != null) {
                        versionConstraint = $"v{moduleMinVersion}..v{moduleMaxVersion}";
                    } else {
                        versionConstraint = $"invalid range from v{moduleMinVersion} to v{moduleMaxVersion}";
                    }
                } else if(moduleMinVersion != null) {
                    versionConstraint = $"v{moduleMinVersion} or later";
                } else if(moduleMaxVersion != null) {
                    versionConstraint = $"v{moduleMaxVersion} or earlier";
                }
                LogError($"could not find module: {moduleOwner}.{moduleName} ({versionConstraint})");
                return null;
            }
            return new ModuleInfo(moduleOwner, moduleName, foundVersion, foundOrigin);

            // local functions
            async Task<VersionInfo> FindNewestVersion(string bucketName) {

                // enumerate versions in bucket
                var versions = new List<VersionInfo>();
                var request = new ListObjectsV2Request {
                    BucketName = bucketName,
                    Prefix = ModuleInfo.GetModuleBucketPrefix(moduleOwner, moduleName, moduleOrigin),
                    Delimiter = "/",
                    MaxKeys = 100
                };
                do {
                    var response = await Settings.S3Client.ListObjectsV2Async(request);
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

                // local functions
                bool IsVersionMatch(VersionInfo version) {

                    // if there are no min-max version constraints, accept any non pre-release version
                    if((moduleMinVersion == null) && (moduleMaxVersion == null)) {
                        return !version.IsPreRelease;
                    }

                    // ensure min-version constraint is met
                    if((moduleMinVersion != null) && version.IsLessThanVersion(moduleMinVersion)) {
                        return false;
                    }

                    // TODO: the following test prevents us from picking up patch releases!

                    // ensure max-version constraint is met
                    if((moduleMaxVersion != null) && version.IsGreaterThanVersion(moduleMaxVersion)) {
                        return false;
                    }
                    return true;
                }
            }
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
    }
}