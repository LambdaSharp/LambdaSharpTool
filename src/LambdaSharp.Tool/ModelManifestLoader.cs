/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
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

        public async Task<ModuleManifest> LoadManifestFromLocationAsync(ModuleLocation moduleLocation, bool errorIfMissing = true, bool allowCaching = false) {
            var stopwatch = Stopwatch.StartNew();
            var cached = false;
            try {
                var cachedManifest = Path.Combine(Settings.GetOriginCacheDirectory(moduleLocation.ModuleInfo), moduleLocation.ModuleInfo.Version.ToString());
                if(allowCaching && Settings.AllowCaching && !moduleLocation.ModuleInfo.Version.IsPreRelease && File.Exists(cachedManifest)) {
                    cached = true;
                    return JsonConvert.DeserializeObject<ModuleManifest>(await File.ReadAllTextAsync(cachedManifest));
                }

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
            } finally {
                LogInfoPerformance($"LoadManifestFromLocationAsync() for {moduleLocation.ModuleInfo}", stopwatch.Elapsed, cached);
            }
        }

        public Task<ModuleLocation> ResolveInfoToLocationAsync(ModuleInfo moduleInfo, ModuleManifestDependencyType dependencyType, bool allowImport, bool showError, bool allowCaching = false)
            => ResolveInfoToLocationAsync(moduleInfo, moduleInfo.Origin, dependencyType, allowImport, showError, allowCaching);

        public async Task<ModuleLocation> ResolveInfoToLocationAsync(
            ModuleInfo moduleInfo,
            string originBucketName,
            ModuleManifestDependencyType dependencyType,
            bool allowImport,
            bool showError,
            bool allowCaching = false
        ) {
            if(originBucketName == null) {
                throw new ArgumentNullException(nameof(originBucketName));
            }
            LogInfoVerbose($"... resolving module {moduleInfo}");
            var stopwatch = Stopwatch.StartNew();
            var cached = false;
            try {

                // check if a cached manifest matches
                var cachedDirectory = Path.Combine(Settings.GetOriginCacheDirectory(moduleInfo));
                if(allowCaching && Settings.AllowCaching && Directory.Exists(cachedDirectory)) {
                    var foundCached = Directory.GetFiles(cachedDirectory)
                        .Select(found => VersionInfo.Parse(Path.GetFileName(found)))
                        .Where(version => (moduleInfo.Version == null) || version.IsGreaterOrEqualThanVersion(moduleInfo.Version, strict: true));

                    // NOTE (2019-08-12, bjorg): unless the module is shared, we filter the list of found versions to
                    //  only contain versions that meet the module version constraint; for shared modules, we want to
                    //  keep the latest version that is compatible with the tool and is equal-or-greater than the
                    //  module version constraint.
                    if((dependencyType != ModuleManifestDependencyType.Shared) && (moduleInfo.Version != null)) {
                        foundCached = foundCached.Where(version => version.MatchesConstraint(moduleInfo.Version)).ToList();
                    }

                    // attempt to identify the newest module version compatible with the tool
                    ModuleManifest manifest = null;
                    var match = VersionInfo.FindLatestMatchingVersion(foundCached, moduleInfo.Version, candidate => {
                        var candidateManifestText = File.ReadAllText(Path.Combine(Settings.GetOriginCacheDirectory(moduleInfo), candidate.ToString()));
                        manifest = JsonConvert.DeserializeObject<ModuleManifest>(candidateManifestText);

                        // check if module is compatible with this tool
                        return manifest.CoreServicesVersion.IsCoreServicesCompatible(Settings.ToolVersion);
                    });
                    if(manifest != null) {
                        cached = true;

                        // TODO (2019-10-08, bjorg): what source bucket name should be used for cached manifests?
                        return MakeModuleLocation(Settings.DeploymentBucketName, manifest);
                    }
                }

                // check if module can be found in the deployment bucket
                var result = await FindNewestModuleVersionAsync(Settings.DeploymentBucketName);

                // check if the origin bucket needs to be checked
                if(
                    allowImport
                    && (Settings.DeploymentBucketName != originBucketName)
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
                    var originResult = await FindNewestModuleVersionAsync(originBucketName);

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
                        : "any released version";
                    if(showError) {
                        if(allowImport) {
                            LogError($"could not find module '{moduleInfo}' ({versionConstraint})");
                        } else {
                            LogError($"missing module dependency must be imported explicitly '{moduleInfo}' ({versionConstraint})");
                        }
                    }
                    return null;
                }
                LogInfoVerbose($"... selected module {moduleInfo.WithVersion(result.Version)} from {result.Origin}");

                // cache found version
                Directory.CreateDirectory(cachedDirectory);
                await File.WriteAllTextAsync(Path.Combine(cachedDirectory, result.Version.ToString()), JsonConvert.SerializeObject(result.Manifest));
                return MakeModuleLocation(result.Origin, result.Manifest);
            } finally {
                LogInfoPerformance($"ResolveInfoToLocationAsync() for {moduleInfo}", stopwatch.Elapsed, cached);
            }

            async Task<(string Origin, VersionInfo Version, ModuleManifest Manifest)> FindNewestModuleVersionAsync(string bucketName) {

                // enumerate versions in bucket
                var found = await FindModuleVersionsAsync(bucketName);
                if(!found.Any()) {
                    return (Origin: bucketName, Version: null, Manifest: null);
                }

                // NOTE (2019-08-12, bjorg): if the module is nested, we filter the list of found versions to
                //  only contain versions that meet the module version constraint; for shared modules, we want to
                //  keep the latest version that is compatible with the tool and is equal-or-greater than the
                //  module version constraint.
                if((dependencyType == ModuleManifestDependencyType.Nested) && (moduleInfo.Version != null)) {
                    found = found.Where(version => {
                        if(!version.MatchesConstraint(moduleInfo.Version)) {
                            LogInfoVerbose($"... rejected v{version}: does not match version constraint {moduleInfo.Version}");
                            return false;
                        }
                        return true;
                    }).ToList();
                }

                // attempt to identify the newest module version compatible with the tool
                ModuleManifest manifest = null;
                var match = VersionInfo.FindLatestMatchingVersion(found, moduleInfo.Version, candidateVersion => {
                    var candidateModuleInfo = new ModuleInfo(moduleInfo.Namespace, moduleInfo.Name, candidateVersion, moduleInfo.Origin);

                    // check if the module version is allowed by the build policy
                    if(!(Settings.BuildPolicy?.Modules?.Allow?.Contains(candidateModuleInfo.ToString()) ?? true)) {
                        LogInfoVerbose($"... rejected v{candidateVersion}: not allowed by build policy");
                        return false;
                    }

                    // check if module is compatible with this tool
                    var candidateManifestText = GetS3ObjectContentsAsync(bucketName, candidateModuleInfo.VersionPath).GetAwaiter().GetResult();
                    manifest = JsonConvert.DeserializeObject<ModuleManifest>(candidateManifestText);
                    if(!manifest.CoreServicesVersion.IsCoreServicesCompatible(Settings.ToolVersion)) {
                        LogInfoVerbose($"... rejected v{candidateVersion}: not compatible with tool version {Settings.ToolVersion}");
                        return false;
                    }
                    return true;
                });
                return (Origin: bucketName, Version: match, Manifest: manifest);
            }

            async Task<IEnumerable<VersionInfo>> FindModuleVersionsAsync(string bucketName) {

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
                LogInfoVerbose($"... found {versions.Count} version{((versions.Count == 1) ? "" : "s")} in {bucketName} [{s3Client.Config.RegionEndpoint.SystemName}]");
                return versions;
            }

            ModuleLocation MakeModuleLocation(string sourceBucketName, ModuleManifest manifest)
                => new ModuleLocation(sourceBucketName, manifest.ModuleInfo, manifest.TemplateChecksum);
        }

        public async Task<IEnumerable<(ModuleManifest Manifest, ModuleLocation ModuleLocation, ModuleManifestDependencyType Type)>> DiscoverAllDependenciesAsync(
            ModuleManifest manifest,
            bool checkExisting,
            bool allowImport,
            bool allowDependencyUpgrades
        ) {
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
                        var dependencyModuleLocation = await ResolveInfoToLocationAsync(dependency.ModuleInfo, dependency.Type, allowImport, showError: true);
                        if(dependencyModuleLocation == null) {

                            // nothing to do; loader already emitted an error
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
                        LogInfoVerbose($"... resolved dependency '{dependency.ModuleInfo.FullName}' to {dependencyModuleLocation.ModuleInfo}");
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
                if(deployedModule.Stack.GetModuleManifestChecksum() == null) {
                    LogWarn($"unable to retrieve module checksum from CloudFormation stack '{stackName}'");
                    return null;
                }
                var result = new DependencyRecord {
                    ModuleLocation = new ModuleLocation(Settings.DeploymentBucketName, deployedModuleInfo, deployedModule.Stack.GetModuleManifestChecksum())
                };

                // confirm that the module name, version and hash match
                if(deployedModuleInfo.FullName != dependency.ModuleInfo.FullName) {
                    LogError($"deployed dependent module name ({deployedModuleInfo.FullName}) does not match {dependency.ModuleInfo.FullName}");
                } else if(!deployedModuleInfo.Version.MatchesConstraint(dependency.ModuleInfo.Version)) {

                    // for out-of-date dependencies, handle them as if they didn't exist when upgrades are allowed
                    if(allowDependencyUpgrades) {
                        return null;
                    }
                    LogError($"deployed dependent module {dependency.ModuleInfo.FullName} (v{deployedModuleInfo.Version}) is not compatible with v{dependency.ModuleInfo.Version}");
                }
                return result;
            }
        }

        public async Task<ModuleNameMappings> GetNameMappingsFromLocationAsync(ModuleLocation moduleLocation) {
            var template = await GetS3ObjectContentsAsync(moduleLocation.SourceBucketName, moduleLocation.ModuleTemplateKey);
            return GetNameMappingsFromTemplate(template);
        }

        public async Task<ModuleNameMappings> GetNameMappingsFromCloudFormationStackAsync(string stackName) {
            var template = (await Settings.CfnClient.GetTemplateAsync(new GetTemplateRequest {
                StackName = stackName,
                TemplateStage = TemplateStage.Original
            })).TemplateBody;
            return GetNameMappingsFromTemplate(template);
        }

        public ModuleNameMappings GetNameMappingsFromTemplate(string template) {

            // NOTE (2020-04-12, bjorg): some templates (like the bootstrap) are written in YAML instead of JSON
            if(!template.TrimStart().StartsWith("{")) {
                return null;
            }

            // parse template as a JSON object
            var cloudformation = JObject.Parse(template);
            if(
                cloudformation.TryGetValue("Metadata", out var metadataToken)
                && (metadataToken is JObject metadata)
            ) {
                JToken nameMappingsToken;
                if(metadata.TryGetValue("LambdaSharp::NameMappings", out nameMappingsToken)) {
                    var nameMappings = nameMappingsToken.ToObject<ModuleNameMappings>();
                    if(nameMappings.Version == ModuleNameMappings.CurrentVersion) {
                        return nameMappings;
                    }
                    LogWarn($"Incompatible LambdaSharp name mappings version (found: {nameMappings.Version ?? "<null>"}, expected: {ModuleNameMappings.CurrentVersion})");
                    return null;
                } else if(
                    metadata.TryGetValue("LambdaSharp::Manifest", out var manifestToken)
                    && (manifestToken is JObject manifest)
                    && manifest.TryGetValue("ResourceNameMappings", out nameMappingsToken)
                ) {

                    // check if the name mappings are in the old manifest format (pre v0.7)
                    var nameMappings = nameMappingsToken.ToObject<ModuleNameMappings>();
                    if((nameMappings.Version != null) && (nameMappings.ResourceNameMappings != null) && (nameMappings.TypeNameMappings != null)) {
                        return nameMappings;
                    }
                    LogWarn($"Incompatible LambdaSharp name mappings version (found: {nameMappings.Version ?? "<null>"}, expected: {ModuleNameMappings.CurrentVersion})");
                    return null;
                }
            }
            LogWarn("CloudFormation file does not contain LambdaSharp name mappings");
            return null;
        }

        public IEnumerable<string> GetArtifactsFromTemplate(string template) {

            // NOTE (2020-04-12, bjorg): some templates (like the bootstrap) are written in YAML instead of JSON
            if(!template.TrimStart().StartsWith("{")) {
                return null;
            }

            // parse template as a JSON object
            var cloudformation = JObject.Parse(template);
            if(
                cloudformation.TryGetValue("Metadata", out var metadataToken)
                && (metadataToken is JObject metadata)
                && metadata.TryGetValue("LambdaSharp::Manifest", out var manifestToken)
            ) {
                var manifest = manifestToken.ToObject<ModuleManifest>();
                if(manifest.Version != null) {
                    return manifest.Artifacts;
                }
                LogWarn($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleNameMappings.CurrentVersion})");
                return null;
            }
            LogWarn("CloudFormation file does not contain LambdaSharp artifacts");
            return null;
        }

        public async Task<IEnumerable<ModuleLocation>> ListManifestsAsync(string bucketName, string origin, bool includePreRelease = false) {
            var stopwatch = Stopwatch.StartNew();
            try {
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return null;
                }

                // enumerate versions in bucket
                var moduleLocations = new List<ModuleLocation>();
                var request = new ListObjectsRequest {
                    BucketName = bucketName,
                    MaxKeys = 1_000,
                    Prefix = $"{origin}/",
                    RequestPayer = RequestPayer.Requester
                };
                do {
                    try {
                        var response = await s3Client.ListObjectsAsync(request);
                        foreach(var entry in response.S3Objects) {

                            // check if key has the right format
                            var parts = entry.Key.Split('/');
                            if(
                                (parts.Length != 4)
                                || (parts[0] != origin)
                                || !VersionInfo.TryParse(parts[3], out var version)
                                || (!includePreRelease && version.IsPreRelease)
                            ) {
                                continue;
                            }

                            // convert key to module info
                            moduleLocations.Add(new ModuleLocation(bucketName, new ModuleInfo(parts[1], parts[2], version, origin), hash: "00000000000000000000000000000000"));
                        }
                        request.Marker = response.NextMarker;
                    } catch(AmazonS3Exception e) when(e.Message == "Access Denied") {
                        break;
                    }
                } while(request.Marker != null);
                return moduleLocations;
            } finally {
                LogInfoPerformance($"ListManifests() for s3://{origin}", stopwatch.Elapsed);
            }
        }

        private async Task<string> GetS3ObjectContentsAsync(string bucketName, string key) {
            var stopwatch = Stopwatch.StartNew();
            try {

                // get bucket region specific S3 client
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return null;
                }
                return await s3Client.GetS3ObjectContentsAsync(bucketName, key);
            } finally {
                LogInfoPerformance($"GetS3ObjectContentsAsync() for s3://{bucketName}/{key}", stopwatch.Elapsed);
            }
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
                LogWarn($"could not find '{bucketName}' bucket");
                return null;
            }

            // check for region header of bucket
            if(!headResponse.Headers.TryGetValues("x-amz-bucket-region", out var values) || !values.Any()) {
                LogWarn($"could not detect region for '{bucketName}' bucket");

                // TODO: the 'x-amz-bucket-region' header is missing sporadically; leaving this code here to make it easier to diagnose
                LogInfoVerbose($"... (DEBUG) S3 bucket '{bucketName}' region check response status: {headResponse.StatusCode}");
                foreach(var header in headResponse.Headers) {
                    LogInfoVerbose($"... (DEBUG) S3 region check response header: {header.Key} = {string.Join(", ", header.Value)}");
                }
                return null;
            }

            // create a bucket region specific S3 client
            result = new AmazonS3Client(RegionEndpoint.GetBySystemName(values.First()));
            _s3ClientByBucketName[bucketName] = result;
            return result;
       }
    }
}