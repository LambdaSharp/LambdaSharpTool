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

        //--- Types ---
        private class DependencyRecord {

            //--- Properties ---
            public string DependencyOwner { get; set; }
            public ModuleManifest Manifest { get; set; }
            public ModuleInfo ModuleInfo { get; set; }
        }

        //--- Class Fields ---
        private static HttpClient _httpClient = new HttpClient();

        //--- Constructors --
        public ModelManifestLoader(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Fields ---
        private Dictionary<string, IAmazonS3> _s3ClientByBucketName = new Dictionary<string, IAmazonS3>();

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

        public Task<ModuleLocation> LocateAsync(ModuleInfo moduleInfo)
            => LocateAsync(moduleInfo.Owner, moduleInfo.Name, moduleInfo.Version, moduleInfo.Version, moduleInfo.Origin);

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
            if(moduleOrigin == ModuleInfo.MODULE_ORIGIN_PLACEHOLDER) {
                LogError($"could not find module: {moduleOwner}.{moduleName} ({versionConstraint})");
            } else {
                LogError($"could not find module: {moduleOwner}.{moduleName}@{moduleOrigin} ({versionConstraint})");
            }
            return null;

            // local functions
            async Task<VersionInfo> FindNewestVersion(string bucketName) {

                // check if bucket name is the origin placeholder
                if(bucketName == ModuleInfo.MODULE_ORIGIN_PLACEHOLDER) {
                    bucketName = Settings.DeploymentBucketName;
                }

                // get bucket region specific S3 client
                var s3Client = await GetS3ClientByBucketName(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return null;
                }

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
                            .Where(version => version.MatchesConstraints(moduleMinVersion, moduleMaxVersion))
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
                var latest = versions.First();
                foreach(var version in versions.Skip(1)) {
                    if(version.IsGreaterThanVersion(latest)) {
                        latest = version;
                    }
                }
                return latest;
            }
        }

        public async Task<IEnumerable<(ModuleManifest Manifest, ModuleInfo ModuleInfo)>> DiscoverAllDependenciesAsync(ModuleManifest manifest, bool checkExisting) {
            var deployments = new List<DependencyRecord>();
            var existing = new List<DependencyRecord>();
            var inProgress = new List<DependencyRecord>();

            // create a topological sort of dependencies
            await Recurse(manifest);
            return deployments.Select(tuple => (tuple.Manifest, tuple.ModuleInfo)).ToList();

            // local functions
            async Task Recurse(ModuleManifest current) {
                foreach(var dependency in current.Dependencies) {

                    // check if we have already discovered this dependency
                    if(IsDependencyInList(current.GetFullName(), dependency, existing) || IsDependencyInList(current.GetFullName(), dependency, deployments))  {
                        continue;
                    }

                    // check if this dependency needs to be deployed
                    var deployedModuleInfo = checkExisting
                        ? await FindExistingDependencyAsync(dependency)
                        : null;
                    if(deployedModuleInfo != null) {
                        existing.Add(new DependencyRecord {
                            ModuleInfo = deployedModuleInfo
                        });
                    } else if(inProgress.Any(d => d.Manifest.GetModuleInfo().FullName == dependency.ModuleInfo.FullName)) {

                        // circular dependency detected
                        LogError($"circular dependency detected: {string.Join(" -> ", inProgress.Select(d => d.Manifest.GetFullName()))}");
                        return;
                    } else {

                        // resolve dependencies for dependency module
                        var dependencyModuleLocation = await LocateAsync(dependency.ModuleInfo.Owner, dependency.ModuleInfo.Name, dependency.MinVersion, dependency.MaxVersion, dependency.ModuleInfo.Origin);
                        if(dependencyModuleLocation == null) {

                            // error has already been reported
                            continue;
                        }

                        // load manifest of dependency and add its dependencies
                        var dependencyManifest = await LoadFromS3Async(dependencyModuleLocation);
                        if(dependencyManifest == null) {

                            // error has already been reported
                            continue;
                        }
                        var nestedDependency = new DependencyRecord {
                            DependencyOwner = current.Module,
                            Manifest = dependencyManifest,
                            ModuleInfo = dependencyModuleLocation.ModuleInfo
                        };

                        // keep marker for in-progress resolutions so that circular errors can be detected
                        inProgress.Add(nestedDependency);
                        await Recurse(dependencyManifest);
                        inProgress.Remove(nestedDependency);

                        // append dependency now that all nested dependencies have been resolved
                        Console.WriteLine($"=> Resolved dependency '{dependency.ModuleInfo.FullName}' to {dependencyModuleLocation.ModuleInfo.ToModuleReference()}");
                        deployments.Add(nestedDependency);
                    }
                }
            }
        }

        private bool IsDependencyInList(string fullName, ModuleManifestDependency dependency, IEnumerable<DependencyRecord> deployedModules) {
            var deployedModule = deployedModules.FirstOrDefault(deployed => (deployed.ModuleInfo.Origin == dependency.ModuleInfo.Origin) && (deployed.ModuleInfo.FullName == dependency.ModuleInfo.FullName));
            if(deployedModule == null) {
                return false;
            }
            var deployedOwner = (deployedModule.DependencyOwner == null)
                ? "existing module"
                : $"module '{deployedModule.DependencyOwner}'";

            // confirm that the dependency version is in a valid range
            var deployedVersion = deployedModule.ModuleInfo.Version;
            if(!deployedModule.ModuleInfo.Version.MatchesConstraints(dependency.MinVersion, dependency.MaxVersion)) {
                LogError($"version conflict for module '{dependency.ModuleInfo.FullName}': module '{fullName}' requires v{dependency.MinVersion}..v{dependency.MaxVersion}, but {deployedOwner} uses v{deployedVersion})");
            }
            return true;
        }

        private async Task<ModuleInfo> FindExistingDependencyAsync(ModuleManifestDependency dependency) {
            var existing = await Settings.CfnClient.GetStackAsync(Settings.GetStackName(dependency.ModuleInfo.FullName), LogError);
            if(!existing.Success || (existing.Stack == null)) {
                return null;
            }
            var deployedOutputs = existing.Stack.Outputs;
            var deployedModuleInfoText = deployedOutputs?.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
            var success = ModuleInfo.TryParse(deployedModuleInfoText, out var deployedModuleInfo);
            if(!success) {
                LogWarn($"unable to retrieve information of the deployed dependent module");
                return null;
            }

            // confirm that the module name matches
            if(deployedModuleInfo.FullName != dependency.ModuleInfo.FullName) {
                LogError($"deployed dependent module name ({deployedModuleInfo.FullName}) does not match {dependency.ModuleInfo.FullName}");
                return deployedModuleInfo;
            }

            // confirm that the module version is in a valid range
            if((dependency.MinVersion != null) && (dependency.MaxVersion != null)) {
                if(!deployedModuleInfo.Version.MatchesConstraints(dependency.MinVersion, dependency.MaxVersion)) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with v{dependency.MinVersion} to v{dependency.MaxVersion}");
                    return deployedModuleInfo;
                }
            } else if(dependency.MaxVersion != null) {
                var deployedToMinVersionComparison = deployedModuleInfo.Version.CompareToVersion(dependency.MaxVersion);
                if(deployedToMinVersionComparison >= 0) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is newer than max version constraint v{dependency.MaxVersion}");
                    return deployedModuleInfo;
                } else if(deployedToMinVersionComparison == null) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with max version constraint v{dependency.MaxVersion}");
                    return deployedModuleInfo;
                }
            } else if(dependency.MinVersion != null) {
                var deployedToMinVersionComparison = deployedModuleInfo.Version.CompareToVersion(dependency.MinVersion);
                if(deployedToMinVersionComparison < 0) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is older than min version constraint v{dependency.MinVersion}");
                    return deployedModuleInfo;
                } else if(deployedToMinVersionComparison == null) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with min version constraint v{dependency.MinVersion}");
                    return deployedModuleInfo;
                }
            }
            return deployedModuleInfo;
        }

        private async Task<string> GetS3ObjectContents(string bucketName, string key) {

            // get bucket region specific S3 client
            var s3Client = await GetS3ClientByBucketName(bucketName);
            if(s3Client == null) {

                // nothing to do; GetS3ClientByBucketName already emitted an error
                return null;
            }
            try {
                var response = await s3Client.GetObjectAsync(new GetObjectRequest {
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

        private async Task<IAmazonS3> GetS3ClientByBucketName(string bucketName) {
            if(bucketName == null) {
                return null;
            } if(_s3ClientByBucketName.TryGetValue(bucketName, out var result)) {
                return result;
            }

            // NOTE (2019-06-14, bjorg): we need to determine which region the bucket belongs to
            //  so that we can instantiate the S3 client properly; doing a HEAD request against
            //  the domain name returns a 'x-amz-bucket-region' even when then bucket is private.
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

            // create a bucket region specific S3 client
            result = new AmazonS3Client(RegionEndpoint.GetBySystemName(values.First()));
            _s3ClientByBucketName[bucketName] = result;
            return result;
       }
    }
}