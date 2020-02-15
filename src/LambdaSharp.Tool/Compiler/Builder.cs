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

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LambdaSharp.Tool.Compiler.Parser;
using LambdaSharp.Tool.Compiler.Parser.Syntax;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Compiler {

    // TODO:
    //  - record declarations
    //  - import missing information
    //      - other modules manifests
    //      - convert secret key alias to ARN
    //      - cloudformation spec (if need be)
    //  - define custom resource types
    //  - validate usage against imported definitions
    //  - validate nested expressions (done: ValidateExpressionsVisitor)
    //  - create derivative resources
    //  - resolve all references
    //  - validate all !GetAtt occurrences (including those inside a !Sub expression)
    //      - check if this declaration should be typechecked
    //          - if(foundDeclaration.HasTypeValidation) ...
    //          - if(foundDeclaration.HasAttribute(literalExpression.Value)) ...
    //          - LogError($"item '{freeItem.FullName}' of type '{freeItem.Type}' does not have attribute '{attributeName}'");
    //  - add optimization phase that simplifies !Sub statements and removed redundant conditional expressions in !If statements
    //  - the !Ref expression can ONLY reference parameters from within a 'Condition' declaration
    //  - validate if attribute name exists on resource type (unless type checking is disabled for this declration)
    //  - for !Ref, must know what types of references are legal (Parameters only -or- Resources and Paramaters)
    //  - register custom resource types for the module
    //  - detect cycle between custom resource handler and an instance of the custom resource in its handler
    //  - CloudFormation expression type validation
    //  - tests
    //      - !If with expression in condition
    //      - !If with literal in condition
    //      - condition declaration with reference to non-parameter declaration
    //      - circular dependencies
    //  - warn on unrecognized pragmas
    //  - nested module parameters can only be scalar or list (correct?)
    //  - lambda environment variable values must be scalar or list (correct?)
    //  - replace `new ArgumentNullException(nameof(value))` with `new ArgumentNullException()` in properties
    //  - throw `InvalidOperationException` when accessing a null property with a non-nullable type
    //  - rename 'Builder' to 'BuildContext'

    public interface IBuilderDependencyProvider : ILogger {

        //--- Properties ---
        string ToolDataDirectory { get; }

        //--- Methods ---
        Task<string> GetS3ObjectContentsAsync(string bucketName, string key);
        Task<IEnumerable<string>> ListS3BucketObjects(string bucketName, string prefix);
    }

    public static class ILoggerSyntaxNodeEx {

        //--- Extension Methods ---
        public static void Log(this ILogger logger, IBuildReportEntry entry, ASyntaxNode node) {
            if(node == null) {
                logger.Log(entry);
            } else if(node.SourceLocation != null) {
                logger.Log(entry, node.SourceLocation, exact: true);
            } else {
                var nearestNode = node.Parents.FirstOrDefault(parent => parent.SourceLocation != null);
                if(nearestNode != null) {
                    logger.Log(entry, nearestNode.SourceLocation, exact: false);
                } else {
                    logger.Log(entry);
                }
            }
        }
    }

    public enum XRayTracingLevel {
        Disabled,
        RootModule,
        AllModules
    }

    public class Grant {

        //--- Constructors ---
        public Grant(string name, string awsType, AExpression reference, SyntaxNodeCollection<LiteralExpression> allow, AExpression condition) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AwsType = awsType;
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            Allow = allow ?? throw new ArgumentNullException(nameof(allow));
            Condition = condition;
        }

        //--- Properties ---
        public string Name { get; }
        public string AwsType { get; }
        public AExpression Reference { get; }
        public SyntaxNodeCollection<LiteralExpression> Allow { get; }
        public AExpression Condition { get; }
    }

    public class Dependency {

        //--- Properties ---
        public ModuleManifest Manifest { get; set; }
        public ModuleLocation ModuleLocation { get; set; }
        public ModuleManifestDependencyType Type;
    }

    // TODO: rename class since it's not really used for building the final result; it's more about tracking meta-data of the module
    public class Builder : ILogger {

        //--- Class Fields ---
        private static Regex ValidResourceNameRegex = new Regex("[a-zA-Z][a-zA-Z0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Fields ---
        private readonly IBuilderDependencyProvider _provider;
        private readonly Dictionary<string, AItemDeclaration> _fullNameDeclarations = new Dictionary<string, AItemDeclaration>();
        private readonly HashSet<string> _logicalIds = new HashSet<string>();
        private readonly List<Grant> _grants = new List<Grant>();
        private IDictionary<string, Dependency> _dependencies = new Dictionary<string, Dependency>();

        //--- Constructors ---
        public Builder(IBuilderDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        //-- Properties ---
        public ModuleDeclaration ModuleDeclaration { get; set; }
        public string ModuleNamespace { get; set; }
        public string ModuleName { get; set; }
        public VersionInfo ModuleVersion { get; set; }
        public CloudFormationSpec CloudformationSpec { get; set; }

        // TODO: initialize the settings
        public VersionInfo CoreServicesReferenceVersion { get; set; }
        public bool AllowCaching { get; set; }
        public string DeploymentBucketName { get; set; }
        public VersionInfo ToolVersion { get; set; }
        public bool NoDependencyValidation { get; set; }

        public string ModuleFullName => $"{ModuleNamespace}.{ModuleName}";
        public ModuleInfo ModuleInfo => new ModuleInfo(ModuleNamespace, ModuleName, ModuleVersion, origin: ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
        public IEnumerable<AItemDeclaration> ItemDeclarations => _fullNameDeclarations.Values;
        public IEnumerable<Grant> Grants => _grants;

        //--- Methods ---
        public void Log(IBuildReportEntry entry, SourceLocation sourceLocation, bool exact)
            => _provider.Log(entry, sourceLocation, exact);

        public bool TryGetItemDeclaration(string fullName, out AItemDeclaration declaration)
            => _fullNameDeclarations.TryGetValue(fullName, out declaration);

        public void RegisterItemDeclaration(AItemDeclaration declaration) {

            // TODO: we shouldn't always assign this expression, because it's not always the correct thing to do
            // assign default reference expression
            declaration.ReferenceExpression = ASyntaxAnalyzer.FnRef(declaration.FullName);

            // check for reserved names
            if(!ValidResourceNameRegex.IsMatch(declaration.ItemName.Value)) {
                _provider.Log(Error.NameMustBeAlphanumeric, declaration);
            } else if(declaration.FullName == "AWS") {
                _provider.Log(Error.NameIsReservedAws, declaration);
            }

            // store properties per-node and per-fullname
            if(!_fullNameDeclarations.TryAdd(declaration.FullName, declaration)) {
                _provider.Log(Error.DuplicateName, declaration);
            }

            // find a valid CloudFormation logical ID
            var baseLogicalId = declaration.FullName.Replace("::", "");
            var logicalIdSuffix = 0;
            var logicalId = baseLogicalId;
            while(!_logicalIds.Add(logicalId)) {
                ++logicalIdSuffix;
                logicalId = baseLogicalId + logicalIdSuffix;
            }
            declaration.LogicalId = logicalId;
        }

        public AExpression GetExportReference(IResourceDeclaration resourceDeclaration) {

            // TODO:
            throw new NotImplementedException();
        }

        public bool IsValidCloudFormationName(string name) => ValidResourceNameRegex.IsMatch(name);

        // TODO: validate grants
        public void AddGrant(string name, string awsType, AExpression reference, SyntaxNodeCollection<LiteralExpression> allow, AExpression condition)
            => _grants.Add(new Grant(name, awsType, reference, allow, condition));

        public async Task<Dependency> AddDependencyAsync(ModuleInfo moduleInfo, ModuleManifestDependencyType dependencyType, ASyntaxNode node) {
            string moduleKey;
            switch(dependencyType) {
            case ModuleManifestDependencyType.Nested:

                // nested dependencies can reference different versions
                moduleKey = moduleInfo.ToString();
                if(_dependencies.ContainsKey(moduleKey)) {
                    return null;
                }
                break;
            case ModuleManifestDependencyType.Shared:

                // shared dependencies can only have one version
                moduleKey = moduleInfo.WithoutVersion().ToString();

                // check if a dependency was already registered
                if(_dependencies.TryGetValue(moduleKey, out var existingDependency)) {
                    if(
                        (moduleInfo.Version == null)
                        || (
                            (existingDependency.ModuleLocation.ModuleInfo.Version != null)
                            && existingDependency.ModuleLocation.ModuleInfo.Version.IsGreaterOrEqualThanVersion(moduleInfo.Version)
                        )
                    ) {

                        // keep existing shared dependency
                        return null;
                    }
                }
                break;
            default:
                _provider.Log(Error.UnsupportedDependencyType(dependencyType.ToString()), node);
                return null;
            }

            // validate dependency
            Dependency dependency;
            if(!NoDependencyValidation) {
                dependency = new Dependency {
                    Type = dependencyType,
                    ModuleLocation = await ResolveInfoToLocationAsync(moduleInfo, dependencyType, allowImport: true, showError: true, allowCaching: true)
                };
                if(dependency.ModuleLocation == null) {

                    // nothing to do; loader already emitted an error
                    return null;
                }
                dependency.Manifest = await LoadManifestFromLocationAsync(dependency.ModuleLocation, allowCaching: true);
                if(dependency.Manifest == null) {

                    // nothing to do; loader already emitted an error
                    return null;
                }
            } else {
                _provider.Log(Warning.UnableToValidateDependency, node);
                dependency = new Dependency {
                    Type = dependencyType,
                    ModuleLocation = new ModuleLocation(DeploymentBucketName, moduleInfo, "00000000000000000000000000000000")
                };
            }
            _dependencies[moduleKey] = dependency;
            return dependency;
        }

        public async Task<ModuleManifest> LoadManifestFromLocationAsync(ModuleLocation moduleLocation, bool errorIfMissing = true, bool allowCaching = false) {
            var stopwatch = Stopwatch.StartNew();
            var cached = false;
            try {
                var cachedManifest = Path.Combine(GetOriginCacheDirectory(moduleLocation.ModuleInfo), moduleLocation.ModuleInfo.Version.ToString());
                if(allowCaching && AllowCaching && !moduleLocation.ModuleInfo.Version.IsPreRelease && File.Exists(cachedManifest)) {
                    cached = true;
                    return JsonConvert.DeserializeObject<ModuleManifest>(await File.ReadAllTextAsync(cachedManifest));
                }

                // download cloudformation template
                var cloudformationText = await _provider.GetS3ObjectContentsAsync(moduleLocation.SourceBucketName, moduleLocation.ModuleTemplateKey);
                if(cloudformationText == null) {
                    if(errorIfMissing) {
                        _provider.Log(Error.ManifestLoaderCouldNotLoadTemplate(moduleLocation.ModuleInfo.ToString()));
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
                    _provider.Log(Error.ManifestLoaderIncompatibleManifestVersion(manifest.Version ?? "<null>"));
                    return null;
                }
                return manifest;
            } finally {
                _provider.LogInfoPerformance($"LoadManifestFromLocationAsync() for {moduleLocation.ModuleInfo}", stopwatch.Elapsed, cached);
            }
        }

        public async Task<ModuleLocation> ResolveInfoToLocationAsync(ModuleInfo moduleInfo, ModuleManifestDependencyType dependencyType, bool allowImport, bool showError, bool allowCaching) {
            _provider.LogInfoVerbose($"... resolving module {moduleInfo}");
            var stopwatch = Stopwatch.StartNew();
            var cached = false;
            try {

                // check if a cached manifest matches
                var cachedDirectory = Path.Combine(GetOriginCacheDirectory(moduleInfo));
                if(allowCaching && AllowCaching && Directory.Exists(cachedDirectory)) {
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
                        var candidateManifestText = File.ReadAllText(Path.Combine(GetOriginCacheDirectory(moduleInfo), candidate.ToString()));
                        manifest = JsonConvert.DeserializeObject<ModuleManifest>(candidateManifestText);

                        // check if module is compatible with this tool
                        return manifest.CoreServicesVersion.IsCoreServicesCompatible(ToolVersion);
                    });
                    if(manifest != null) {
                        cached = true;

                        // TODO (2019-10-08, bjorg): what source bucket name should be used for cached manifests?
                        return MakeModuleLocation(DeploymentBucketName, manifest);
                    }
                }

                // check if module can be found in the deployment bucket
                var result = await FindNewestModuleVersionAsync(DeploymentBucketName);

                // check if the origin bucket needs to be checked
                if(
                    allowImport
                    && (DeploymentBucketName != moduleInfo.Origin)
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
                    if(showError) {
                        _provider.Log(Error.ManifestLoaderCouldNotFindModule(moduleInfo.ToString()));
                    }
                    return null;
                }
                _provider.LogInfoVerbose($"... selected module {moduleInfo.WithVersion(result.Version)} from {result.Origin}");

                // cache found version
                Directory.CreateDirectory(cachedDirectory);
                await File.WriteAllTextAsync(Path.Combine(cachedDirectory, result.Version.ToString()), JsonConvert.SerializeObject(result.Manifest));
                return MakeModuleLocation(result.Origin, result.Manifest);
            } finally {
                _provider.LogInfoPerformance($"ResolveInfoToLocationAsync() for {moduleInfo}", stopwatch.Elapsed, cached);
            }

            async Task<(string Origin, VersionInfo Version, ModuleManifest Manifest)> FindNewestModuleVersionAsync(string bucketName) {
                if(bucketName == null) {
                    return (Origin: null, Version: null, Manifest: null);
                }

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
                ModuleManifest manifest = null;
                var match = VersionInfo.FindLatestMatchingVersion(found, moduleInfo.Version, candidate => {
                    var candidateModuleInfo = new ModuleInfo(moduleInfo.Namespace, moduleInfo.Name, candidate, moduleInfo.Origin);

                    // TODO: caching might be beneficial here if the same manifest can be fetched more than once
                    var candidateManifestText = _provider.GetS3ObjectContentsAsync(bucketName, candidateModuleInfo.VersionPath).Result;
                    manifest = JsonConvert.DeserializeObject<ModuleManifest>(candidateManifestText);

                    // check if module is compatible with this tool
                    return manifest.CoreServicesVersion.IsCoreServicesCompatible(ToolVersion);
                });
                return (Origin: bucketName, Version: match, Manifest: manifest);
            }

            async Task<IEnumerable<VersionInfo>> FindModuleVersionsAsync(string bucketName) {
                var versions = (await _provider.ListS3BucketObjects(bucketName, $"{moduleInfo.Origin ?? DeploymentBucketName}/{moduleInfo.Namespace}/{moduleInfo.Name}/"))
                    .Select(found => VersionInfo.Parse(found))
                    .Where(version => (moduleInfo.Version == null) || version.IsGreaterOrEqualThanVersion(moduleInfo.Version, strict: true))
                    .ToList();
                _provider.LogInfoVerbose($"... found {versions.Count} version{((versions.Count == 1) ? "" : "s")} in {bucketName}");
                return versions;
            }

            ModuleLocation MakeModuleLocation(string sourceBucketName, ModuleManifest manifest)
                => new ModuleLocation(sourceBucketName, manifest.ModuleInfo, manifest.TemplateChecksum);
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
                _provider.Log(Error.ManifestLoaderIncompatibleManifestVersion(manifest.Version ?? "<null>"));
                return null;
            }
            _provider.Log(Error.ManifestLoaderMissingNameMappings);
            return null;
        }

        private string GetOriginCacheDirectory(ModuleInfo moduleInfo)
            => Path.Combine(_provider.ToolDataDirectory, ".origin", moduleInfo.Origin, moduleInfo.Namespace, moduleInfo.Name);
    }
}
