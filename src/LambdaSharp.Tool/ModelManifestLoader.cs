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
            public ModuleInfo DependencyOwner { get; set; }
            public ModuleManifest Manifest { get; set; }
            public ModuleLocation ModuleLocation { get; set; }
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
            return manifest != null;
        }

        public async Task<ModuleManifest> LoadManifestFromLocationAsync(ModuleLocation moduleLocation, bool errorIfMissing = true) {

            // download cloudformation template
            var cloudformationText = await GetS3ObjectContentsAsync(moduleLocation.SourceBucketName, moduleLocation.ModuleTemplateKey);
            if(cloudformationText == null) {
                if(errorIfMissing) {
                    LogError($"could not load CloudFormation template for {moduleLocation.ModuleInfo.ToModuleReference()}");
                }
                return null;
            }

            // extract manifest
            var cloudformation = JsonConvert.DeserializeObject<JObject>(cloudformationText);
            var manifest = GetManifest(cloudformation);
            if(manifest == null) {
                return null;
            }

            // validate manifest
            if(manifest.Version != ModuleManifest.CurrentVersion) {
                LogError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                return null;
            }
            return manifest;
        }

        public async Task<ModuleLocation> ResolveInfoToLocationAsync(ModuleInfo moduleInfo) {

            // TODO: shouldn't we first check our deployment bucket for a custom version before we check the origin?

            // attempt to find the latest matching version in the module origin and deployment buckets
            var moduleOriginFoundVersion = (moduleInfo.Origin != null)
                ? await FindNewestVersion(moduleInfo.Origin)
                : null;
            var deploymentBucketFoundVersion = (moduleInfo.Origin != Settings.DeploymentBucketName)
                ? await FindNewestVersion(Settings.DeploymentBucketName)
                : moduleOriginFoundVersion;

            // determine which bucket to use for the module
            if((moduleOriginFoundVersion != null) && (deploymentBucketFoundVersion != null)) {

                // check which bucket has the newer version
                var compareOriginDeploymentVersions = moduleOriginFoundVersion.CompareToVersion(deploymentBucketFoundVersion);
                if(compareOriginDeploymentVersions == 0) {
                    if(moduleOriginFoundVersion.IsPreRelease) {

                        // always default to module origin for pre-release version
                        return await MakeModuleLocation(moduleInfo.Origin, moduleInfo.WithVersion(moduleOriginFoundVersion));
                    } else {

                        // keep version in deployment bucket since version is stable and it matches
                        return await MakeModuleLocation(Settings.DeploymentBucketName, moduleInfo.WithVersion(deploymentBucketFoundVersion));
                    }
                } else if(compareOriginDeploymentVersions < 0) {

                    // use version from deployment bucket since it's newer
                    return await MakeModuleLocation(Settings.DeploymentBucketName, moduleInfo.WithVersion(deploymentBucketFoundVersion));
                } else if(compareOriginDeploymentVersions > 0) {

                    // use version from origin since it's newer
                    return await MakeModuleLocation(moduleInfo.Origin, moduleInfo.WithVersion(moduleOriginFoundVersion));
                } else {
                    LogError($"unable to determine which version to use for {moduleInfo.ToModuleReference()}: {moduleOriginFoundVersion} (origin) vs. {deploymentBucketFoundVersion} (deployment bucket)");
                    return null;
                }
            } else if(moduleOriginFoundVersion != null) {
                return await MakeModuleLocation(moduleInfo.Origin, moduleInfo.WithVersion(moduleOriginFoundVersion));
            } else if(deploymentBucketFoundVersion != null) {
                return await MakeModuleLocation(Settings.DeploymentBucketName, moduleInfo.WithVersion(deploymentBucketFoundVersion));
            }

            // could not find a matching version
            var versionConstraint = (moduleInfo.Version != null)
                ? $"v{moduleInfo.Version} or later"
                : "any version";
            LogError($"could not find module: {moduleInfo.ToModuleReference()} ({versionConstraint})");
            return null;

            // local functions
            async Task<VersionInfo> FindNewestVersion(string bucketName) {

                // get bucket region specific S3 client
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return null;
                }

                // enumerate versions in bucket
                var versions = new List<VersionInfo>();
                var request = new ListObjectsV2Request {
                    BucketName = bucketName,
                    Prefix = $"{moduleInfo.Origin ?? Settings.DeploymentBucketName}/{moduleInfo.Owner}/{moduleInfo.Name}/",
                    Delimiter = "/",
                    MaxKeys = 100,
                    RequestPayer = RequestPayer.Requester
                };
                do {
                    try {
                        var response = await s3Client.ListObjectsV2Async(request);
                        versions.AddRange(response.S3Objects
                            .Select(s3Object => s3Object.Key.Substring(request.Prefix.Length))
                            .Select(found => VersionInfo.Parse(found))
                            .Where(version => version.MatchesConstraints(moduleInfo.Version, moduleInfo.Version))
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

            async Task<ModuleLocation> MakeModuleLocation(string sourceBucketName, ModuleInfo info) {

                // fetch module manifest for version
                var manifestText = await GetS3ObjectContentsAsync(sourceBucketName, info.VersionPath);
                if(manifestText == null) {
                    LogError($"could not load module manifest for {info.ToModuleReference()}");
                    return null;
                }
                var manifest = JsonConvert.DeserializeObject<ModuleManifest>(manifestText);
                if(manifest == null) {
                    return null;
                }

                // create module location reference with found manifest hash
                return new ModuleLocation(sourceBucketName, info, manifest.TemplateChecksum);
            }
        }

        public async Task<IEnumerable<(ModuleManifest Manifest, ModuleLocation ModuleLocation)>> DiscoverAllDependenciesAsync(ModuleManifest manifest, bool checkExisting) {
            var deployments = new List<DependencyRecord>();
            var existing = new List<DependencyRecord>();
            var inProgress = new List<DependencyRecord>();

            // create a topological sort of dependencies
            await Recurse(manifest);
            return deployments.Select(tuple => (tuple.Manifest, tuple.ModuleLocation)).ToList();

            // local functions
            async Task Recurse(ModuleManifest current) {
                foreach(var dependency in current.Dependencies) {

                    // check if we have already discovered this dependency
                    if(IsDependencyInList(current.GetFullName(), dependency, existing) || IsDependencyInList(current.GetFullName(), dependency, deployments))  {
                        continue;
                    }

                    // check if this dependency needs to be deployed
                    var existingDependency = checkExisting
                        ? await FindExistingDependencyAsync(dependency)
                        : null;
                    if(existingDependency != null) {
                        existing.Add(existingDependency);
                    } else if(inProgress.Any(d => d.Manifest.Module.FullName == dependency.ModuleInfo.FullName)) {

                        // circular dependency detected
                        LogError($"circular dependency detected: {string.Join(" -> ", inProgress.Select(d => d.Manifest.GetFullName()))}");
                        return;
                    } else {

                        // resolve dependencies for dependency module
                        var dependencyModuleLocation = await ResolveInfoToLocationAsync(dependency.ModuleInfo);
                        if(dependencyModuleLocation == null) {

                            // error has already been reported
                            continue;
                        }

                        // load manifest of dependency and add its dependencies
                        var dependencyManifest = await LoadManifestFromLocationAsync(dependencyModuleLocation);
                        if(dependencyManifest == null) {

                            // error has already been reported
                            continue;
                        }
                        var nestedDependency = new DependencyRecord {
                            DependencyOwner = current.Module,
                            Manifest = dependencyManifest,
                            ModuleLocation = dependencyModuleLocation
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

            bool IsDependencyInList(string dependentModuleFullName, ModuleManifestDependency dependency, IEnumerable<DependencyRecord> deployedModules) {
                var deployedModule = deployedModules.FirstOrDefault(deployed => (deployed.ModuleLocation.ModuleInfo.Origin == dependency.ModuleInfo.Origin) && (deployed.ModuleLocation.ModuleInfo.FullName == dependency.ModuleInfo.FullName));
                if(deployedModule == null) {
                    return false;
                }
                var deployedOwner = (deployedModule.DependencyOwner == null)
                    ? "existing module"
                    : $"module '{deployedModule.DependencyOwner.ToModuleReference()}'";

                // confirm that the dependency version is in a valid range
                var deployedVersion = deployedModule.ModuleLocation.ModuleInfo.Version;
                if(!deployedModule.ModuleLocation.ModuleInfo.Version.MatchesConstraints(dependency.ModuleInfo.Version, dependency.ModuleInfo.Version)) {
                    LogError($"version conflict for module '{dependency.ModuleInfo.FullName}': module '{dependentModuleFullName}' requires v{dependency.ModuleInfo.Version}, but {deployedOwner} uses v{deployedVersion})");
                }
                return true;
            }

            async Task<DependencyRecord> FindExistingDependencyAsync(ModuleManifestDependency dependency) {

                // attempt to find an existing, deployed stack matching the dependency
                var stackName = Settings.GetStackName(dependency.ModuleInfo.FullName);
                var deployedModule = await Settings.CfnClient.GetStackAsync(stackName, LogError);
                if(deployedModule.Stack == null) {
                    return null;
                }
                if(!ModuleInfo.TryParse(deployedModule.Stack.GetModuleVersionText(), out var deployedModuleInfo)) {
                    LogWarn($"unable to retrieve module version from CloudFormation stack '{stackName}'");
                    return null;
                }
                var result = new DependencyRecord {
                    ModuleLocation = new ModuleLocation(Settings.DeploymentBucketName, deployedModuleInfo, deployedModule.Stack.GetModuleManifestChecksum())
                };

                // confirm that the module name, version and hash match
                if(deployedModuleInfo.FullName != dependency.ModuleInfo.FullName) {
                    LogError($"deployed dependent module name ({deployedModuleInfo.FullName}) does not match {dependency.ModuleInfo.FullName}");
                } else if(!deployedModuleInfo.Version.MatchesConstraints(dependency.ModuleInfo.Version, dependency.ModuleInfo.Version)) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with v{dependency.ModuleInfo.Version}");
                }
                return result;
            }
        }

        private async Task<string> GetS3ObjectContentsAsync(string bucketName, string key) {

            // get bucket region specific S3 client
            var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
            if(s3Client == null) {

                // nothing to do; GetS3ClientByBucketName already emitted an error
                return null;
            }
            return await s3Client.GetS3ObjectContents(bucketName, key);
        }

        private ModuleManifest GetManifest(JObject cloudformation) {
            if(
                cloudformation.TryGetValue("Metadata", out var metadataToken)
                && (metadataToken is JObject metadata)
                && metadata.TryGetValue("LambdaSharp::Manifest", out var manifestToken)
            ) {
                var manifest = manifestToken.ToObject<ModuleManifest>();
                if(manifest.Version == ModuleManifest.CurrentVersion) {
                    return manifest;
                }
                LogError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                return null;
            }
            LogError("CloudFormation file does not contain a LambdaSharp manifest");
            return null;
        }

        private async Task<IAmazonS3> GetS3ClientByBucketNameAsync(string bucketName) {
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