/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Metadata;
using LambdaSharp.Tool.Internal;

namespace LambdaSharp.Tool {
    using ModuleInfo = LambdaSharp.Modules.ModuleInfo;
    using JObject = Newtonsoft.Json.Linq.JObject;
    using JToken = Newtonsoft.Json.Linq.JToken;

    public class ModelManifestLoader : AModelProcessor {

        //--- Types ---
        private class DependencyRecord {

            //--- Properties ---
            public ModuleInfo DependencyOwner { get; set; }
            public ModuleManifest Manifest { get; set; }
            public ModuleLocation ModuleLocation { get; set; }
            public ModuleManifestDependencyType Type { get; set; }
        }

        private class ModuleManifestVersions {

            //--- Properties ---
            public string Region { get; set; }
            public List<VersionInfo> Versions { get; set; } = new List<VersionInfo>();
        }

        //--- Class Fields ---
        private static HttpClient _httpClient = new HttpClient();

        //--- Constructors --
        public ModelManifestLoader(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Fields ---
        private Dictionary<string, IAmazonS3> _s3ClientByBucketName = new Dictionary<string, IAmazonS3>();

        //--- Methods ---
        public bool TryLoadManifestFromCloudFormationFile(string filepath, out ModuleManifest manifest) {
            JObject cloudformation;
            try {

                // load cloudformation template
                var template = File.ReadAllText(filepath);
                cloudformation = JObject.Parse(template);
            } catch(Exception) {
                manifest = null;
                cloudformation = null;
                return false;
            }

            // extract manifest from cloudformation template
            if(
                cloudformation.TryGetValue("Metadata", out var metadataToken)
                && (metadataToken is JObject metadata)
                && metadata.TryGetValue(ModuleManifest.MetadataName, out var manifestToken)
            ) {
                manifest = manifestToken.ToObject<ModuleManifest>();
                if(manifest.Version == ModuleManifest.CurrentVersion) {
                    return true;
                }
                LogError($"Incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
            } else {
                LogError("CloudFormation file does not contain a LambdaSharp manifest");
            }
            manifest = null;
            return false;
        }

        public async Task<(ModuleManifest Manifest, string Reason)> LoadManifestFromLocationAsync(ModuleLocation moduleLocation) {
            StartLogPerformance($"LoadManifestFromLocationAsync() for {moduleLocation.ModuleInfo}");
            var cached = false;
            try {

                // attempt to load manifest from cache
                var cachedManifestFilePath = GetCachedManifestFilePath(moduleLocation);
                if(!Settings.ForceRefresh && (cachedManifestFilePath is not null) && File.Exists(cachedManifestFilePath)) {
                    ModuleManifest result = null;
                    try {
                        result = JsonSerializer.Deserialize<ModuleManifest>(await File.ReadAllTextAsync(cachedManifestFilePath), Settings.JsonSerializerOptions);
                        cached = true;
                        return (Manifest: result, Reason: null);
                    } catch {

                        // cached manifest file is either corrupted or inaccessible; attempt to delete it
                        try {
                            File.Delete(cachedManifestFilePath);
                        } catch {

                            // nothing to do
                        }
                    }
                }

                // download manifest from S3
                var manifestText = await GetS3ObjectContentsAsync(moduleLocation.SourceBucketName, moduleLocation.ModuleInfo.VersionPath);
                if(manifestText == null) {
                    return (Manifest: null, Reason: $"could not load module manifest for {moduleLocation.ModuleInfo}");
                }
                var manifest = JsonSerializer.Deserialize<ModuleManifest>(manifestText, Settings.JsonSerializerOptions);

                // validate manifest
                if(manifest.Version != ModuleManifest.CurrentVersion) {
                    return (Manifest: null, Reason: $"incompatible LambdaSharp manifest version (found: {manifest.Version ?? "<null>"}, expected: {ModuleManifest.CurrentVersion})");
                }

                // keep manifest if we have a valid file path for it
                if(cachedManifestFilePath is not null) {
                    try {
                        await File.WriteAllTextAsync(cachedManifestFilePath, manifestText);
                    } catch {

                        // cached manifest file could not be written; nothing to do
                    }
                }
                return (Manifest: manifest, Reason: null);
            } finally {
                StopLogPerformance(cached);
            }
        }

        public async Task<ModuleLocation> ResolveInfoToLocationAsync(
            ModuleInfo moduleInfo,
            string bucketName,
            ModuleManifestDependencyType dependencyType,
            bool allowImport,
            bool showError
        ) {
            if(bucketName == null) {
                throw new ArgumentNullException(nameof(bucketName));
            }
            LogInfoVerbose($"... resolving module {moduleInfo}");
            StartLogPerformance($"ResolveInfoToLocationAsync() for {moduleInfo}");
            var cached = false;
            try {

                // check if module can be found in the deployment bucket
                var result = await FindNewestModuleVersionInBucketAsync(Settings.DeploymentBucketName);

                // check if the origin bucket needs to be checked
                if(
                    allowImport
                    && (Settings.DeploymentBucketName != bucketName)
                    && (

                        // no version has been found
                        (result.Version == null)

                        // no module version constraint was given; the ultimate floating version
                        || (moduleInfo.Version == null)

                        // the module version constraint is for a pre-release; we always prefer the origin version then
                        || moduleInfo.Version.IsPreRelease()

                        // the module version constraint is floating; we need to check if origin has a newer version
                        || !moduleInfo.Version.Minor.HasValue
                        || !moduleInfo.Version.Patch.HasValue
                    )
                ) {
                    var originResult = await FindNewestModuleVersionInBucketAsync(bucketName);

                    // check if module found at origin should be kept instead
                    if(
                        (originResult.Version != null)
                        && (
                            (result.Version == null)
                            || (moduleInfo.Version?.IsPreRelease() ?? false)
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
                return MakeModuleLocation(result.Origin, result.Manifest);
            } finally {
                StopLogPerformance(cached);
            }

            async Task<(string Origin, VersionInfo Version, ModuleManifest Manifest)> FindNewestModuleVersionInBucketAsync(string bucketName) {
                StartLogPerformance($"FindNewestModuleVersionInBucketAsync() for s3://{bucketName}");
                try {

                    // enumerate versions in bucket
                    var found = await FindModuleVersionsInBucketAsync(bucketName);
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

                        // load module manifest
                        var (candidateManifest, candidateManifestErrorReason) = LoadManifestFromLocationAsync(new ModuleLocation(bucketName, candidateModuleInfo, "<MISSING>")).GetAwaiter().GetResult();
                        if(candidateManifest == null) {
                            LogInfoVerbose($"... rejected v{candidateVersion}: {candidateManifestErrorReason}");
                            return false;
                        }

                        // check if module is compatible with this tool
                        if(!VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(candidateManifest.CoreServicesVersion, Settings.ToolVersion)) {
                            LogInfoVerbose($"... rejected v{candidateVersion}: not compatible with tool version {Settings.ToolVersion}");
                            return false;
                        }

                        // keep this manifest
                        manifest = candidateManifest;
                        return true;
                    });
                    return (Origin: bucketName, Version: match, Manifest: manifest);
                } finally {
                    StopLogPerformance();
                }
            }

            async Task<IEnumerable<VersionInfo>> FindModuleVersionsInBucketAsync(string bucketName) {
                StartLogPerformance($"FindModuleVersionsInBucketAsync() for s3://{bucketName}");
                var cached = false;
                try {
                    var moduleOrigin = moduleInfo.Origin ?? Settings.DeploymentBucketName;
                    List<VersionInfo> versions = null;
                    string region = null;

                    // check if a cached version exists
                    string cachedManifestVersionsFilePath = null;
                    if(!Settings.ForceRefresh) {
                        var cachedManifestFolder = GetCachedManifestDirectory(bucketName, moduleOrigin, moduleInfo.Namespace, moduleInfo.Name);
                        if(cachedManifestFolder != null) {
                            cachedManifestVersionsFilePath = Path.Combine(cachedManifestFolder, "versions.json");
                            if(
                                File.Exists(cachedManifestVersionsFilePath)
                                && (File.GetLastWriteTimeUtc(cachedManifestVersionsFilePath).Add(Settings.CachedManifestListingExpiration) > DateTime.UtcNow)
                            ) {
                                cached = true;
                                var cachedManifestVersions = JsonSerializer.Deserialize<ModuleManifestVersions>(File.ReadAllText(cachedManifestVersionsFilePath), Settings.JsonSerializerOptions);
                                region = cachedManifestVersions.Region;
                                versions = cachedManifestVersions.Versions;
                            }
                        }
                    }

                    // check if data needs to be fetched from S3 bucket
                    if(versions == null) {

                        // get bucket region specific S3 client
                        var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                        if(s3Client == null) {

                            // nothing to do; GetS3ClientByBucketName already emitted an error
                            return new List<VersionInfo>();
                        }

                        // enumerate versions in bucket
                        versions = new List<VersionInfo>();
                        region = s3Client.Config.RegionEndpoint.SystemName;
                        var request = new ListObjectsV2Request {
                            BucketName = bucketName,
                            Prefix = $"{moduleOrigin}/{moduleInfo.Namespace}/{moduleInfo.Name}/",
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
                                );
                                request.ContinuationToken = response.NextContinuationToken;
                            } catch(AmazonS3Exception e) when(e.Message == "Access Denied") {

                                // show message that access was denied for this location
                                LogInfoVerbose($"... access denied to {bucketName} [{s3Client.Config.RegionEndpoint.SystemName}]");
                                return Enumerable.Empty<VersionInfo>();
                            }
                        } while(request.ContinuationToken != null);

                        // cache module versions listing
                        if(cachedManifestVersionsFilePath != null) {
                            try {
                                File.WriteAllText(cachedManifestVersionsFilePath, JsonSerializer.Serialize(new ModuleManifestVersions {
                                    Region = region,
                                    Versions = versions
                                }, Settings.JsonSerializerOptions));
                            } catch {

                                // nothing to do
                            }
                        }
                    }

                    // filter list down to matching versions
                    versions = versions.Where(version => (moduleInfo.Version == null) || version.IsGreaterOrEqualThanVersion(moduleInfo.Version, strict: true)).ToList();
                    LogInfoVerbose($"... found {versions.Count} version{((versions.Count == 1) ? "" : "s")} in {bucketName} [{region}]");
                    return versions;
                } finally {
                    StopLogPerformance(cached);
                }
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
                        var dependencyModuleLocation = await ResolveInfoToLocationAsync(dependency.ModuleInfo, dependency.ModuleInfo.Origin, dependency.Type, allowImport, showError: true);
                        if(dependencyModuleLocation == null) {

                            // nothing to do; loader already emitted an error
                            continue;
                        }

                        // load manifest of dependency and add its dependencies
                        var (dependencyManifest, dependencyManifestErrorReason) = await LoadManifestFromLocationAsync(dependencyModuleLocation);
                        if(dependencyManifest == null) {
                            LogError(dependencyManifestErrorReason);
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
                if(metadata.TryGetValue(ModuleNameMappings.MetadataName, out nameMappingsToken)) {
                    var nameMappings = nameMappingsToken.ToObject<ModuleNameMappings>();
                    if(nameMappings.Version == ModuleNameMappings.CurrentVersion) {
                        return nameMappings;
                    }
                    LogWarn($"Incompatible LambdaSharp name mappings version (found: {nameMappings.Version ?? "<null>"}, expected: {ModuleNameMappings.CurrentVersion})");
                    return null;
                } else if(
                    metadata.TryGetValue(ModuleManifest.MetadataName, out var manifestToken)
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
                && metadata.TryGetValue(ModuleManifest.MetadataName, out var manifestToken)
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

        public async Task<(IEnumerable<ModuleLocation> ModuleLocations, int PrereleaseModuleCount)> ListManifestsAsync(string bucketName, string origin, bool includePreRelease = false) {
            StartLogPerformance($"ListManifests() for s3://{origin}");
            try {
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return (ModuleLocations: Enumerable.Empty<ModuleLocation>(), 0);
                }

                // enumerate versions in bucket
                var moduleLocations = new List<ModuleLocation>();
                var request = new ListObjectsRequest {
                    BucketName = bucketName,
                    MaxKeys = 1_000,
                    Prefix = $"{origin}/",
                    RequestPayer = RequestPayer.Requester
                };
                var prereleaseModuleCount = 0;
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
                            ) {
                                continue;
                            }
                            if(!includePreRelease && version.IsPreRelease()) {
                                ++prereleaseModuleCount;
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
                return (ModuleLocations: moduleLocations, PrereleaseModuleCount: prereleaseModuleCount);
            } finally {
                StopLogPerformance();
            }
        }

        public void ResetCache(string bucketName, ModuleInfo moduleInfo) {

            // remove cached module
            var moduleLocation = new ModuleLocation(bucketName, moduleInfo, "<MISSING>");
            var cachedManifestFilePath = GetCachedManifestFilePath(moduleLocation);
            if(cachedManifestFilePath != null) {
                try {
                    File.Delete(cachedManifestFilePath);
                } catch {

                    // nothing to do
                }
            }

            // remove cached list versions
            var cachedManifestFolder = GetCachedManifestDirectory(bucketName, moduleInfo.Origin ?? bucketName, moduleInfo.Namespace, moduleInfo.Name);
            if(cachedManifestFolder != null) {
                try {
                    File.Delete(Path.Combine(cachedManifestFolder, "versions.json"));
                } catch {

                    // nothing to do
                }
            }
        }

        private async Task<string> GetS3ObjectContentsAsync(string bucketName, string key) {
            StartLogPerformance($"GetS3ObjectContentsAsync() for s3://{bucketName}/{key}");
            try {

                // get bucket region specific S3 client
                var s3Client = await GetS3ClientByBucketNameAsync(bucketName);
                if(s3Client == null) {

                    // nothing to do; GetS3ClientByBucketName already emitted an error
                    return null;
                }
                return await s3Client.GetS3ObjectContentsAsync(bucketName, key);
            } finally {
                StopLogPerformance();
            }
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

                // HACKHACKHACK (2020-08-31, bjorg): the 'x-amz-bucket-region' header is missing sporadically; leaving this code here to make it easier to diagnose
                LogInfo($"... (DEBUG) S3 bucket '{bucketName}' region check response status: {headResponse.StatusCode}");
                foreach(var header in headResponse.Headers) {
                    LogInfo($"... (DEBUG) S3 region check response header: {header.Key} = {string.Join(", ", header.Value)}");
                }
                return null;
            }

            // create a bucket region specific S3 client
            result = new AmazonS3Client(RegionEndpoint.GetBySystemName(values.First()));
            _s3ClientByBucketName[bucketName] = result;
            return result;
        }

        private string GetCachedManifestFilePath(ModuleLocation moduleLocation) {

            // never cache pre-release versions or when the module origin is not set
            if(
                moduleLocation.ModuleInfo.Version.IsPreRelease()
                || (moduleLocation.ModuleInfo.Origin is null)
            ) {
                return null;
            }

            // ensure directory exists since it will be used
            var cachedManifestFolder = GetCachedManifestDirectory(moduleLocation.SourceBucketName, moduleLocation.ModuleInfo.Origin, moduleLocation.ModuleInfo.Namespace, moduleLocation.ModuleInfo.Name);
            if(cachedManifestFolder == null) {
                return null;
            }
            return Path.Combine(cachedManifestFolder, moduleLocation.ModuleInfo.Version.ToString());
        }

        private string GetCachedManifestDirectory(string sourceBucketName, string moduleOrigin, string moduleNamespace, string moduleName) {
            var cachedManifestFolder = Path.Combine(Settings.ToolSettingsDirectory, "Manifests", sourceBucketName, moduleOrigin, moduleNamespace, moduleName);
            try {
                Directory.CreateDirectory(cachedManifestFolder);
            } catch {

                // let the optimal outcome not get in the way of a successful outcome
                return null;
            }
            return cachedManifestFolder;
        }
    }
}