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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool {

    public class ModelManifestLoader : AModelProcessor {

        //--- Class Fields ---
        private static HttpClient _httpClient = new HttpClient();

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

        public async Task<ModuleManifest> LoadFromS3Async(ModuleLocation moduleLocation, bool errorIfMissing = true) {

            // download cloudformation template
            var cloudformationText = await GetS3ObjectContents(moduleLocation.SourceBucketName, moduleLocation.ModuleInfo.TemplatePath);
            if(cloudformationText == null) {
                if(errorIfMissing) {
                    LogError($"could not load CloudFormation template from {moduleLocation.S3Url}");
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

        public async Task<ModuleLocation> LocateAsync(string moduleOwner, string moduleName, VersionInfo moduleMinVersion, VersionInfo moduleMaxVersion, string moduleOrigin) {

            // attempt to find the latest matching version in the module origin and deployment buckets
            var moduleOriginFoundVersion = await FindNewestVersion(moduleOrigin);
            var deploymentBucketFoundVersion = (moduleOrigin == Settings.DeploymentBucketName)
                ? moduleOriginFoundVersion
                : await FindNewestVersion(Settings.DeploymentBucketName);

            // determine which bucket to use for the module
            if((moduleOriginFoundVersion != null) && (deploymentBucketFoundVersion != null)) {

                // check which bucket has the newer version
                var compareOriginDeploymentVersions = moduleOriginFoundVersion.CompareToVersion(deploymentBucketFoundVersion);
                if(compareOriginDeploymentVersions == 0) {
                    if(moduleOriginFoundVersion.IsPreRelease) {

                        // always default to module origin for pre-release version
                        return new ModuleLocation(moduleOrigin, new ModuleInfo(moduleOwner, moduleName, moduleOriginFoundVersion, moduleOrigin));
                    } else {

                        // keep version in deployment bucket since version is stable and it matches
                        return new ModuleLocation(Settings.DeploymentBucketName, new ModuleInfo(moduleOwner, moduleName, deploymentBucketFoundVersion, moduleOrigin));
                    }
                } else if(compareOriginDeploymentVersions < 0) {

                    // use version from deployment bucket since it's newer
                    return new ModuleLocation(Settings.DeploymentBucketName, new ModuleInfo(moduleOwner, moduleName, deploymentBucketFoundVersion, moduleOrigin));
                } else if(compareOriginDeploymentVersions > 0) {

                    // use version from origin since it's newer
                    return new ModuleLocation(moduleOrigin, new ModuleInfo(moduleOwner, moduleName, moduleOriginFoundVersion, moduleOrigin));
                } else {
                    LogError($"unable to determine which version to use: {moduleOriginFoundVersion} vs. {deploymentBucketFoundVersion}");
                    return null;
                }
            } else if(moduleOriginFoundVersion != null) {
                return new ModuleLocation(moduleOrigin, new ModuleInfo(moduleOwner, moduleName, moduleOriginFoundVersion, moduleOrigin));
            } else if(deploymentBucketFoundVersion != null) {
                return new ModuleLocation(Settings.DeploymentBucketName, new ModuleInfo(moduleOwner, moduleName, deploymentBucketFoundVersion, moduleOrigin));
            }

            // could not find a matching version
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

            // local functions
            async Task<VersionInfo> FindNewestVersion(string bucketName) {

                // NOTE (2019-06-14, bjorg): we need to determine which region the bucket belongs to
                //  so that we can instantiate the S3 client properly; doing a HEAD request against
                //  the domain name returns a 'x-amz-bucket-region' even when then bucket itself is private.
                var headResponse = await _httpClient.SendAsync(new HttpRequestMessage {
                    Method = HttpMethod.Head,
                    RequestUri = new Uri($"https://{bucketName}.s3.amazonaws.com")
                });

                // check if bucket exists
                if(headResponse.StatusCode == HttpStatusCode.NotFound) {
                    LogWarn($"could not find '{bucketName}'");
                    return null;
                }

                // check for region header of bucket
                if(!headResponse.Headers.TryGetValues("x-amz-bucket-region", out var values) || !values.Any()) {
                    LogWarn($"could not detect region for '{bucketName}'");
                    return null;
                }

                // create region specific S3 client
                var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(values.First()));

                // enumerate versions in bucket
                var versions = new List<VersionInfo>();
                var request = new ListObjectsV2Request {
                    BucketName = bucketName,
                    Prefix = ModuleInfo.GetModuleVersionsBucketPrefix(moduleOwner, moduleName, moduleOrigin),
                    Delimiter = "/",
                    MaxKeys = 100
                };
                do {
                    try {
                        var response = await s3Client.ListObjectsV2Async(request);
                        versions.AddRange(response.CommonPrefixes
                            .Select(prefix => prefix.Substring(request.Prefix.Length).TrimEnd('/'))
                            .Select(found => VersionInfo.Parse(found))
                            .Where(IsVersionMatch)
                        );
                        request.ContinuationToken = response.NextContinuationToken;
                    } catch(AmazonS3Exception e) when(e.Message == "Access Denied") {
                        return null;
                    }
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