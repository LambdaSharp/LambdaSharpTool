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
using System.Linq;
using System.Net;
using System.Net.Http;
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
            public ModuleManifestDependencyType Type { get; set; }
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
                    LogError($"could not load CloudFormation template for {moduleLocation.ModuleInfo}");
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

        public async Task<ModuleLocation> ResolveInfoToLocationAsync(ModuleInfo moduleInfo, ModuleManifestDependencyType dependencyType, bool allowImport) {
            LogInfoVerbose($"=> Resolving module {moduleInfo}");

            // check if module can be found in the deployment bucket
            var result = await FindNewestModuleVersionAsync(Settings.DeploymentBucketName);

            // check if the origin bucket needs to be checked
            if(
                allowImport
                && (Settings.DeploymentBucketName != moduleInfo.Origin)
                && (

                    // no version has been found
                    (result.Version == null)

                    // no module version constraint was given; the ultimate floating version
                    || (moduleInfo.Version == null)

                    // the module version constraint is for a pre-release; we always prefer the origin version then
                    || moduleInfo.Version.IsPreRelease

                    // the module version constraint is floating; we need to check if origin has a newer version
                    || moduleInfo.Version.HasFloatingConstraints
                )
            ) {
                var originResult = await FindNewestModuleVersionAsync(moduleInfo.Origin);

                // check if module found at origin should be kept instead
                if(
                    (originResult.Version != null)
                    && (
                        (result.Version == null)
                        || (moduleInfo.Version?.IsPreRelease ?? false)
                        || originResult.Version.IsGreaterThanVersion(result.Version)
                    )
                ) {
                    result = originResult;
                }
            }

            // check if a module was found
            if(result.Version == null) {

                // could not find a matching version
                var versionConstraint = (moduleInfo.Version != null)
                    ? $"v{moduleInfo.Version} or later"
                    : "any version";
                LogError($"could not find module '{moduleInfo}' ({versionConstraint})");
                return null;
            }
            LogInfoVerbose($"=> Selected module {moduleInfo.WithVersion(result.Version)} from {result.Origin}");
            return MakeModuleLocation(result.Origin, result.Manifest);

            // local functions
            async Task<(string Origin, VersionInfo Version, ModuleManifest Manifest)> FindNewestModuleVersionAsync(string bucketName) {

                // enumerate versions in bucket
                var found = await FindModuleVersionsAsync(bucketName);
                if(!found.Any()) {
                    return (Origin: bucketName, Version: null, Manifest: null);
                }

                // NOTE (2019-08-12, bjorg): unless the module is shared, we filter the list of found versions to
                //  only contain versions that meet the module version constraint; for shared modules, we want to
                //  keep the latest version that is compatible with the tool and is equal-or-greater than the
                //  module version constraint.
                if((dependencyType != ModuleManifestDependencyType.Shared) && (moduleInfo.Version != null)) {
                    found = found.Where(version => version.MatchesConstraint(moduleInfo.Version)).ToList();
                }

                // attempt to identify the newest module version compatible with the tool
                while(found.Any()) {
                    var latest = VersionInfo.Max(found, strict: true);

                    // check if latest version meets minimum version constraint
                    if(moduleInfo.Version?.IsGreaterThanVersion(latest) ?? false) {
                        break;
                    }
                    var latestModuleInfo = new ModuleInfo(moduleInfo.Namespace, moduleInfo.Name, latest, moduleInfo.Origin);
                    var manifestText = await GetS3ObjectContentsAsync(bucketName, latestModuleInfo.VersionPath);
                    var manifest = JsonConvert.DeserializeObject<ModuleManifest>(manifestText);

                    // check if module is compatible with this tool
                    if(manifest.CoreServicesVersion.IsCoreServicesCompatible(Settings.ToolVersion)) {
                        return (Origin: bucketName, Version: latest, Manifest: manifest);
                    }

                    // remove latest version since it didn't meet the constraints
                    found.Remove(latest);
                }
                return (Origin: bucketName, Version: null, Manifest: null);
            }

            async Task<List<VersionInfo>> FindModuleVersionsAsync(string bucketName) {

                // get bucket region specific S3 client
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return new List<VersionInfo>();
                }

                // enumerate versions in bucket
                var versions = new List<VersionInfo>();
                var request = new ListObjectsV2Request {
                    BucketName = bucketName,
                    Prefix = $"{moduleInfo.Origin ?? Settings.DeploymentBucketName}/{moduleInfo.Namespace}/{moduleInfo.Name}/",
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
                            .Where(version => (moduleInfo.Version == null) || version.IsGreaterOrEqualThanVersion(moduleInfo.Version, strict: true))
                        );
                        request.ContinuationToken = response.NextContinuationToken;
                    } catch(AmazonS3Exception e) when(e.Message == "Access Denied") {
                        break;
                    }
                } while(request.ContinuationToken != null);
                LogInfoVerbose($"==> Found {versions.Count} version{((versions.Count == 1) ? "" : "s")} in {bucketName} [{s3Client.Config.RegionEndpoint.SystemName}]");
                return versions;
            }

            ModuleLocation MakeModuleLocation(string sourceBucketName, ModuleManifest manifest)
                => new ModuleLocation(sourceBucketName, manifest.ModuleInfo, manifest.TemplateChecksum);
        }

        public async Task<IEnumerable<(ModuleManifest Manifest, ModuleLocation ModuleLocation, ModuleManifestDependencyType Type)>> DiscoverAllDependenciesAsync(ModuleManifest manifest, bool checkExisting, bool allowImport) {
            var deployments = new List<DependencyRecord>();
            var existing = new List<DependencyRecord>();
            var inProgress = new List<DependencyRecord>();

            // create a topological sort of dependencies
            await Recurse(manifest);
            return deployments.Select(record => (record.Manifest, record.ModuleLocation, record.Type)).ToList();

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
                    } else if(inProgress.Any(d => d.Manifest.ModuleInfo.FullName == dependency.ModuleInfo.FullName)) {

                        // circular dependency detected
                        LogError($"circular dependency detected: {string.Join(" -> ", inProgress.Select(d => d.Manifest.GetFullName()))}");
                        return;
                    } else {

                        // resolve dependencies for dependency module
                        var dependencyModuleLocation = await ResolveInfoToLocationAsync(dependency.ModuleInfo, dependency.Type, allowImport);
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
                            DependencyOwner = current.ModuleInfo,
                            Manifest = dependencyManifest,
                            ModuleLocation = dependencyModuleLocation,
                            Type = dependency.Type
                        };

                        // keep marker for in-progress resolutions so that circular errors can be detected
                        inProgress.Add(nestedDependency);
                        await Recurse(dependencyManifest);
                        inProgress.Remove(nestedDependency);

                        // append dependency now that all nested dependencies have been resolved
                        LogInfoVerbose($"=> Resolved dependency '{dependency.ModuleInfo.FullName}' to {dependencyModuleLocation.ModuleInfo}");
                        deployments.Add(nestedDependency);
                    }
                }
            }

            bool IsDependencyInList(string dependentModuleFullName, ModuleManifestDependency dependency, IEnumerable<DependencyRecord> deployedModules) {
                var deployedModule = deployedModules.FirstOrDefault(deployed => deployed.ModuleLocation.ModuleInfo.FullName == dependency.ModuleInfo.FullName);
                if(deployedModule == null) {
                    return false;
                }
                var deployedOwner = (deployedModule.DependencyOwner == null)
                    ? "existing module"
                    : $"module '{deployedModule.DependencyOwner}'";

                // confirm the requested version by the dependency is not greater than the deployed version
                var deployedVersion = deployedModule.ModuleLocation.ModuleInfo.Version;
                if(dependency.ModuleInfo.Version?.IsGreaterThanVersion(deployedVersion) ?? false) {
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
                } else if(!deployedModuleInfo.Version.MatchesConstraint(dependency.ModuleInfo.Version)) {
                    LogError($"deployed dependent module version (v{deployedModuleInfo.Version}) is not compatible with v{dependency.ModuleInfo.Version}");
                }
                return result;
            }
        }

        public async Task<ModuleNameMappings> GetNameMappingsFromLocationAsync(ModuleLocation moduleLocation) {
            var template = await GetS3ObjectContentsAsync(moduleLocation.SourceBucketName, moduleLocation.ModuleTemplateKey);
            return GetNameMappingsFromTemplate(template);
        }

        public ModuleNameMappings GetNameMappingsFromTemplate(string template) {
            var cloudformation = JObject.Parse(template);
            if(
                cloudformation.TryGetValue("Metadata", out var metadataToken)
                && (metadataToken is JObject metadata)
                && metadata.TryGetValue("LambdaSharp::NameMappings", out var nameMappingsToken)
            ) {
                var nameMappings = nameMappingsToken.ToObject<ModuleNameMappings>();
                if(nameMappings.Version == ModuleNameMappings.CurrentVersion) {
                    return nameMappings;
                }
                LogError($"Incompatible LambdaSharp name mappings version (found: {nameMappings.Version ?? "<null>"}, expected: {ModuleNameMappings.CurrentVersion})");
                return null;
            }
            LogError("CloudFormation file does not contain LambdaSharp name mappings");
            return null;
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