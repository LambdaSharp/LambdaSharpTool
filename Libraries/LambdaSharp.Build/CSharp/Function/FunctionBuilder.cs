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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LambdaSharp.Build.CSharp.Internal;
using LambdaSharp.Build.Internal;
using LambdaSharp.Modules;

namespace LambdaSharp.Build.CSharp.Function {

    public class FunctionBuilder : ABuildEventsSource {

        //--- Constants ---
        private const string GIT_INFO_FILE = "git-info.json";
        private const string API_MAPPINGS = "api-mappings.json";
        private const string MIN_AWS_LAMBDA_TOOLS_VERSION = "4.0.0";

        //--- Types --
        private class ApiGatewayInvocationMappings {

            //--- Properties ---
            public List<ApiGatewayInvocationMapping> Mappings { get; set; } = new List<ApiGatewayInvocationMapping>();
        }

        private class ApiGatewayInvocationMapping {

            //--- Properties ---
            public string? RestApi { get; set; }
            public string? WebSocket { get; set; }
            public string? Method { get; set; }
            public IFunctionRestApiSource? RestApiSource { get; set; }
            public IFunctionWebSocketSource? WebSocketSource { get; set; }
        }

        public class ModuleManifestGitInfo {

            //--- Properties ---
            public string? Branch { get; set; }
            public string? SHA { get; set; }
        }

        //--- Class Fields ---
        private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions {
            WriteIndented = false,
            IgnoreNullValues = true
        };

        //--- Class Methods ---
        public static bool TryParseAssemblyClassMethodReference(string reference, out string? assemblyName, out string? className, out string? methodName) {
            var parts = reference.Split("::").Reverse().ToArray();
            if(parts.Length > 3) {
                assemblyName = null;
                className = null;
                methodName = null;
                return false;
            }
            methodName = parts.FirstOrDefault();
            className = parts.Skip(1).FirstOrDefault();
            assemblyName = parts.Skip(2).FirstOrDefault();
            return true;
        }

        //--- Constructors ---
        public FunctionBuilder(IFunctionBuilderDependencyProvider settings, BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) {
            Provider = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        //--- Properties ---
        public IFunctionBuilderDependencyProvider Provider { get; }

        //--- Methods ---
        public void Build(
            IFunction function,
            bool noCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration,
            bool forceBuild
        ) {

            // collect sources with invoke methods
            var mappings = ExtractMappings(function);
            if(mappings == null) {

                // nothing to log since error was already logged
                return;
            }

            // check if a function package already exists
            if(!forceBuild) {
                var functionPackage = Provider.ExistingPackages.FirstOrDefault(p =>
                    Path.GetFileName(p).StartsWith($"function_{Provider.ModuleFullName}_{function.LogicalId}_", StringComparison.Ordinal)
                    && p.EndsWith(".zip", StringComparison.Ordinal)
                );

                // to skip the build, we both need the function package and the function schema when mappings are present
                var schemaFile = Path.Combine(Provider.OutputDirectory, $"functionschema_{Provider.ModuleFullName}_{function.LogicalId}.json");
                if((functionPackage != null) && (!mappings.Any() || File.Exists(schemaFile))) {
                    LogInfoVerbose($"=> Analyzing function {function.FullName} dependencies");

                    // find all files used to create the function package
                    var files = new HashSet<string>();
                    CSharpProjectFile.DiscoverDependencies(
                        files,
                        function.Project,
                        filePath => LogInfoVerbose($"... analyzing {filePath}"),
                        (message, exception) => LogError(message, exception)
                    );

                    // check if any of the files has been modified more recently than the function package
                    var functionPackageDate = File.GetLastWriteTime(functionPackage);
                    var file = files.FirstOrDefault(f => File.GetLastWriteTime(f) > functionPackageDate);
                    if(file == null) {
                        var success = true;
                        if(mappings.Any()) {

                            // apply function schema to generate REST API and WebSocket models
                            try {
                                if(!ApplyInvocationSchemas(function, mappings, schemaFile, silent: true)) {
                                    success = false;

                                    // reset the mappings as the call to ApplyInvocationSchemas() may have modified them
                                    mappings = ExtractMappings(function);
                                    if(mappings == null) {

                                        // nothing to log since error was already logged
                                        return;
                                    }
                                }
                            } catch(Exception e) {
                                LogError("unable to read create-invoke-methods-schema output", e);
                                return;
                            }

                            // check if the mappings have changed by comparing the new data-structure to the one inside the zip file
                            if(success) {
                                var newMappingsJson = JsonSerializer.Serialize(new ApiGatewayInvocationMappings {
                                    Mappings = mappings
                                }, _jsonOptions);
                                using(var zipArchive = ZipFile.Open(functionPackage, ZipArchiveMode.Read)) {
                                    var entry = zipArchive.Entries.FirstOrDefault(entry => entry.FullName == API_MAPPINGS);
                                    if(entry != null) {
                                        using(var stream = entry.Open())
                                        using(var reader = new StreamReader(stream)) {
                                            if(newMappingsJson != reader.ReadToEnd()) {

                                                // module mappings have change
                                                success = false;
                                                LogInfoVerbose($"... api mappings updated");
                                            }
                                        }
                                    } else {

                                        // we now have mappings and we didn't use to
                                        success = false;
                                        LogInfoVerbose($"... api mappings updated");
                                    }
                                }

                            }
                        }

                        // only skip compilation if we were able to apply the invocation schemas (or didn't have to)
                        if(success) {
                            Provider.WriteLine($"=> Skipping function {Provider.InfoColor}{function.FullName}{Provider.ResetColor} (no changes found)");

                            // keep the existing package
                            Provider.ExistingPackages.Remove(functionPackage);

                            // set the module variable to the final package name
                            Provider.AddArtifact($"{function.FullName}::PackageName", functionPackage);
                            return;
                        }
                    } else {
                        LogInfoVerbose($"... change detected in {file}");
                    }
                }
            } else {
                LogInfoVerbose($"=> Analyzing function {function.FullName} dependencies");

                // find all files used to create the function package
                var files = new HashSet<string>();
                CSharpProjectFile.DiscoverDependencies(
                    files,
                    function.Project,
                    filePath => LogInfoVerbose($"... analyzing {filePath}"),
                    (message, exception) => LogError(message, exception)
                );

                // loop over all project folders
                new CleanBuildFolders(BuildEventsConfig).Do(files);
            }

            // read settings from project file
            var projectFile = new CSharpProjectFile(function.Project);

            // compile function project
            var isNetCore31OrLater = VersionInfoCompatibility.IsNetCore3OrLater(projectFile.TargetFramework);
            var isAmazonLinux2 = Provider.IsAmazonLinux2();
            var isReadyToRun = isNetCore31OrLater && isAmazonLinux2;
            var isSelfContained = (projectFile.OutputType == "Exe")
                || (projectFile.AssemblyName == "bootstrap");
            var readyToRunText = isReadyToRun ? ", ReadyToRun" : "";
            var selfContained = isSelfContained ? ", SelfContained" : "";
            Provider.WriteLine($"=> Building function {Provider.InfoColor}{function.FullName}{Provider.ResetColor} [{projectFile.TargetFramework}, {buildConfiguration}{readyToRunText}{selfContained}]");
            var projectDirectory = Path.Combine(Provider.WorkingDirectory, Path.GetFileNameWithoutExtension(function.Project));

            // check if the project contains an obsolete AWS Lambda Tools extension: <DotNetCliToolReference Include="Amazon.Lambda.Tools"/>
            if(projectFile.RemoveAmazonLambdaToolsReference()) {
                LogWarn($"removing obsolete AWS Lambda Tools extension from {Path.GetRelativePath(Provider.WorkingDirectory, function.Project)}");
                projectFile.Save(function.Project);
            }

            // validate project properties for self-contained functions
            if(
                isSelfContained
                && (
                    (projectFile.OutputType != "Exe")
                    || (projectFile.AssemblyName != "bootstrap")
                )
            ) {
                LogError("function project must have <OutputType>Exe</OutputType> and <AssemblyName>bootstrap</AssemblyName> properties");
                return;
            }

            // validate the project is using the most recent lambdasharp assembly references
            if(
                !noAssemblyValidation &&
                function.HasAssemblyValidation &&
                !projectFile.ValidateLambdaSharpPackageReferences(Provider.ToolVersion, LogWarn, LogError)
            ) {
                return;
            }
            if(noCompile) {
                return;
            }

            // build project with AWS dotnet CLI lambda tool
            if(!DotNetPublish(projectFile.TargetFramework, buildConfiguration, projectDirectory, forceBuild, isNetCore31OrLater, isAmazonLinux2, isReadyToRun, isSelfContained, out var publishFolder)) {

                // nothing to do; error was already reported
                return;
            }

            // check if the assembly entry-point needs to be validated
            if(function.HasHandlerValidation) {
                if(isSelfContained) {

                    // TODO (2021-02-08, bjorg): validate the assembly has a Main() entry point
                } else {

                    // verify the function handler can be found in the compiled assembly
                    if(function.Handler != null) {
                        if(!ValidateEntryPoint(
                            publishFolder,
                            function.Handler
                        )) {
                            return;
                        }
                    }
                }
            }

            // add api mappings JSON file(s)
            if(mappings.Any()) {

                // self-contained assemblies cannot be inspected
                if(isSelfContained) {
                    LogError("API Gateway mappings are not supported for self-contained Lambda functions");
                    return;
                }

                // create request/response schemas for invocation methods
                if(!LambdaSharpCreateInvocationSchemas(
                    function,
                    publishFolder,
                    projectFile.RootNamespace,
                    function.Handler,
                    mappings
                )) {
                    LogError($"'{Provider.Lash} util create-invoke-methods-schema' command failed");
                    return;
                }

                // write api-mappings.json file to publish folder
                File.WriteAllText(Path.Combine(publishFolder, API_MAPPINGS), JsonSerializer.Serialize(new ApiGatewayInvocationMappings {
                    Mappings = mappings
                }, _jsonOptions));
            }

            // compute hash o publish folder
            string hash;
            using(var md5 = MD5.Create())
            using(var hashStream = new CryptoStream(Stream.Null, md5, CryptoStreamMode.Write)) {
                foreach(var publishedFile in Directory.GetFiles(publishFolder, "*", SearchOption.AllDirectories).OrderBy(filePath => filePath)) {

                    // hash file path
                    var filePathBytes = Encoding.UTF8.GetBytes(Path.GetRelativePath(publishFolder, publishedFile).Replace('\\', '/'));
                    hashStream.Write(filePathBytes, 0, filePathBytes.Length);

                    // hash file contents
                    using(var stream = File.OpenRead(publishedFile)) {
                        stream.CopyTo(hashStream);
                    }
                }
                hashStream.FlushFinalBlock();
                hash = md5.Hash.ToHexString();
            }

            // genereate function package with hash
            var package = Path.Combine(Provider.OutputDirectory, $"function_{Provider.ModuleFullName}_{function.LogicalId}_{hash}.zip");
            if(Provider.ExistingPackages.Remove(package)) {

                // remove old, existing package so we can create the new package in the same location (which also preserves the more recent build timestamp)
                File.Delete(package);
            }

            // write git-info.json file to publish folder
            File.WriteAllText(Path.Combine(publishFolder, GIT_INFO_FILE), JsonSerializer.Serialize(new ModuleManifestGitInfo {
                SHA = gitSha,
                Branch = gitBranch
            }, _jsonOptions));

            // zip files in publishing folder
            new ZipTool(BuildEventsConfig).ZipFolderWithExecutable(package, publishFolder);

            // set the module variable to the final package name
            Provider.AddArtifact($"{function.FullName}::PackageName", package);
        }

        private bool DotNetPublish(
            string targetFramework,
            string buildConfiguration,
            string projectDirectory,
            bool forceBuild,
            bool isNetCore31OrLater,
            bool isAmazonLinux2,
            bool isReadyToRun,
            bool isSelfContained,
            [NotNullWhen(true)] out string? publishFolder
        ) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                LogError("failed to find the \"dotnet\" executable in path.");
                publishFolder = null;
                return false;
            }

            // delete publishing folder to avoid residual files from previous builds
            publishFolder = Path.Combine(projectDirectory, "bin", buildConfiguration, targetFramework, "linux-x64", "publish");
            if(Directory.Exists(publishFolder)) {
                try {
                    Directory.Delete(publishFolder, recursive: true);
                } catch {
                    LogWarn($"unable to delete publishing folder; it may contain unneeded files: {publishFolder}");
                }
            }

            // set default publish parameters
            var publishParameters = new List<string> {
                "publish",
                "--configuration", buildConfiguration,
                "--framework", targetFramework,
                "--runtime", "linux-x64",
                "--output", publishFolder,
                "/p:GenerateRuntimeConfigurationFiles=true"
            };

            // for .NET Core 3.1 and later, disable tiered compilation since Lambda functions are generally short lived
            if(isNetCore31OrLater) {
                publishParameters.Add("/p:TieredCompilation=false");
                publishParameters.Add("/p:TieredCompilationQuickJit=false");
            }

            // for Amazon Linux 2, enable Ready2Run when compiling
            if(isReadyToRun) {
                publishParameters.Add("/p:PublishReadyToRun=true");
            }

            // run `dotnet publish`
            if(isSelfContained) {

                // build a self-contained package
                publishParameters.Add("--self-contained");
            } else {

                // build a framework-dependent package
                publishParameters.Add("--no-self-contained");
            }
            if(!new ProcessLauncher(BuildEventsConfig).Execute(
                dotNetExe,
                publishParameters,
                projectDirectory,
                Provider.DetailedOutput,
                ColorizeOutput
            )) {
                LogError("'dotnet publish' command failed");
                return false;
            }
            return true;

            // local functions
            string ColorizeOutput(string line)
                => line.Contains(": error ", StringComparison.Ordinal)
                    ? $"{Provider.ErrorColor}{line}{Provider.ResetColor}"
                    : line.Contains(": warning ", StringComparison.Ordinal)
                    ? $"{Provider.WarningColor}{line}{Provider.ResetColor}"
                    : line;
        }

        private bool ValidateEntryPoint(string buildFolder, string handler) {
            var hasErrors = false;
            new LambdaSharpTool(
                BuildEventsConfig).RunLashTool(Provider.WorkingDirectory,
                new[] {
                    "util", "validate-assembly",
                    "--directory", buildFolder,
                    "--entry-point", handler,
                    "--quiet",
                    "--no-ansi"
                },
                showOutput: false,
                output => {
                    if(output.StartsWith("ERROR:", StringComparison.Ordinal)) {
                        hasErrors = true;
                        LogError(output.Substring(6).Trim());
                    } else if(output.StartsWith("WARNING:")) {
                        hasErrors = true;
                        LogError(output.Substring(8).Trim());
                    }
                    return null;
                }
            );
            return !hasErrors;
        }

        private List<ApiGatewayInvocationMapping>? ExtractMappings(IFunction function) {

            // find all REST API and WebSocket mappings function sources
            var mappings = Enumerable.Empty<ApiGatewayInvocationMapping>()
                .Union(function.RestApiSources
                    .Where(source => source.Invoke != null)
                    .Select(source => new ApiGatewayInvocationMapping {
                        RestApi = $"{source.HttpMethod}:/{string.Join("/", source.Path)}",
                        Method = source.Invoke,
                        RestApiSource = source
                    })
                )
                .Union(function.WebSocketSources
                    .Where(source => source.Invoke != null)
                    .Select(source => new ApiGatewayInvocationMapping {
                        WebSocket = source.RouteKey,
                        Method = source.Invoke,
                        WebSocketSource = source
                    })
                )
                .ToList();

            // check if we have enough information to resolve the invocation methods
            var incompleteMappings = mappings.Where(mapping =>
                !TryParseAssemblyClassMethodReference(
                    mapping.Method ?? throw new InvalidOperationException($"Method cannot be null for {mapping.RestApi ?? mapping.WebSocket ?? "<MISSING>"}"),
                    out var mappingAssemblyName,
                    out var mappingClassName,
                    out var mappingMethodName
                )
                || (mappingAssemblyName == null)
                || (mappingClassName == null)
            ).ToList();
            if(incompleteMappings.Any()) {
                if(function.Handler == null) {
                    LogError("either function 'Handler' attribute must be specified as a literal value or all invocation methods must be fully qualified");
                    return null;
                }

                // extract the assembly and class name from the handler
                if(!TryParseAssemblyClassMethodReference(
                    function.Handler,
                    out var lambdaFunctionAssemblyName,
                    out var lambdaFunctionClassName,
                    out var lambdaFunctionEntryPointName
                )) {
                    LogError("'Handler' attribute has invalid value");
                    return null;
                }

                // set default qualifier to the class name of the function handler
                foreach(var mapping in incompleteMappings) {
                    if(!TryParseAssemblyClassMethodReference(
                        mapping.Method ?? throw new InvalidOperationException($"Method cannot be null for {mapping.RestApi ?? mapping.WebSocket ?? "<MISSING>"}"),
                        out var mappingAssemblyName,
                        out var mappingClassName,
                        out var mappingMethodName
                    )) {
                        LogError("'Invoke' attribute has invalid value");
                        return null;
                    }
                    mapping.Method = $"{mappingAssemblyName ?? lambdaFunctionAssemblyName}::{mappingClassName ?? lambdaFunctionClassName}::{mappingMethodName}";

                }
            }
            return mappings;
        }

        private bool LambdaSharpCreateInvocationSchemas(
            IFunction function,
            string buildFolder,
            string? rootNamespace,
            string? handler,
            IEnumerable<ApiGatewayInvocationMapping> mappings
        ) {

            // check if there is anything to do
            if(!mappings.Any()) {
                return true;
            }

            // root namespace is required to be able to resolve
            if(rootNamespace == null) {
                LogError($"missing <RootNamespace> declaration for function {function.FullName}");
                return false;
            }

            // build invocation arguments
            string schemaFile = Path.Combine(Provider.OutputDirectory, $"functionschema_{Provider.ModuleFullName}_{function.LogicalId}.json");
            Provider.ExistingPackages.Remove(schemaFile);
            var arguments = new[] {
                "util", "create-invoke-methods-schema",
                "--directory", buildFolder,
                "--default-namespace", rootNamespace,
                "--out", schemaFile,
                "--quiet",
                "--no-ansi"
            }
                .Union(mappings.Select(mapping => $"--method={mapping.Method}"))
                .ToList();

            // execute `lash util create-invoke-methods-schema` command
            var success = new LambdaSharpTool(BuildEventsConfig).RunLashTool(Provider.WorkingDirectory, arguments, Provider.DetailedOutput);
            if(!success) {
                return false;
            }
            try {
                ApplyInvocationSchemas(function, mappings, schemaFile);
            } catch(Exception e) {
                LogError("unable to read create-invoke-methods-schema output", e);
                return false;
            }
            return true;
        }

        private bool ApplyInvocationSchemas(
            IFunction function,
            IEnumerable<ApiGatewayInvocationMapping> mappings,
            string schemaFile,
            bool silent = false
        ) {
            Provider.ExistingPackages.Remove(schemaFile);
            var schemas = (Dictionary<string, InvocationTargetDefinition>)JsonSerializer.Deserialize<Dictionary<string, InvocationTargetDefinition>>(File.ReadAllText(schemaFile));

            // process schema contents
            var success = true;
            foreach(var mapping in mappings) {
                if(!schemas.TryGetValue(mapping.Method ?? "<MISSING>", out var invocationTarget)) {
                    if(!silent) {
                        LogError($"failed to resolve method '{mapping.Method ?? "<MISSING>"}'");
                    }
                    success = false;
                    continue;
                }
                if(invocationTarget.Error != null) {
                    if(!silent) {
                        LogError(invocationTarget.Error);
                    }
                    success = false;
                    continue;
                }

                // update mapping information
                mapping.Method = $"{invocationTarget.Assembly}::{invocationTarget.Type}::{invocationTarget.Method}";
                if(mapping.RestApiSource != null) {
                    mapping.RestApiSource.RequestContentType ??= invocationTarget.RequestContentType;
                    mapping.RestApiSource.RequestSchema ??= invocationTarget.RequestSchema;
                    mapping.RestApiSource.RequestSchemaName ??= invocationTarget.RequestSchemaName;
                    mapping.RestApiSource.ResponseContentType ??= invocationTarget.ResponseContentType;
                    mapping.RestApiSource.ResponseSchema ??= invocationTarget.ResponseSchema;
                    mapping.RestApiSource.ResponseSchemaName ??= invocationTarget.ResponseSchemaName;
                    mapping.RestApiSource.OperationName ??= invocationTarget.OperationName;

                    // determine which uri parameters come from the request path vs. the query-string
                    var uriParameters = new Dictionary<string, bool>(invocationTarget.UriParameters ?? Enumerable.Empty<KeyValuePair<string, bool>>());
                    foreach(var pathParameter in mapping.RestApiSource.Path
                        .Where(segment => segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal))
                        .Select(segment => segment.ToIdentifier())
                        .ToArray()
                    ) {
                        if(!uriParameters.Remove(pathParameter)) {
                            if(!silent) {
                                LogError($"path parameter '{pathParameter}' is missing in method declaration '{invocationTarget.Type}::{invocationTarget.Method}'");
                            }
                            success = false;
                        }
                    }

                    // remaining uri parameters must be supplied as query parameters
                    if(uriParameters.Any()) {
                        if(mapping.RestApiSource.QueryStringParameters == null) {
                            mapping.RestApiSource.QueryStringParameters = new Dictionary<string, bool>();
                        }
                        foreach(var uriParameter in uriParameters) {

                            // either record new query-string parameter or upgrade requirements for an existing one
                            if(!mapping.RestApiSource.QueryStringParameters.TryGetValue(uriParameter.Key, out var existingRequiredValue) || !existingRequiredValue) {
                                mapping.RestApiSource.QueryStringParameters[uriParameter.Key] = uriParameter.Value;
                            }
                        }
                    }
                }
                if(mapping.WebSocketSource != null) {
                    mapping.WebSocketSource.RequestContentType = mapping.WebSocketSource.RequestContentType ?? invocationTarget.RequestContentType;
                    mapping.WebSocketSource.RequestSchema = mapping.WebSocketSource.RequestSchema ?? invocationTarget.RequestSchema;
                    mapping.WebSocketSource.RequestSchemaName = mapping.WebSocketSource.RequestSchemaName ?? invocationTarget.RequestSchemaName;
                    mapping.WebSocketSource.ResponseContentType = mapping.WebSocketSource.ResponseContentType ?? invocationTarget.ResponseContentType;
                    mapping.WebSocketSource.ResponseSchema = mapping.WebSocketSource.ResponseSchema ?? invocationTarget.ResponseSchema;
                    mapping.WebSocketSource.ResponseSchemaName = mapping.WebSocketSource.ResponseSchemaName ?? invocationTarget.ResponseSchemaName;
                    mapping.WebSocketSource.OperationName = mapping.WebSocketSource.OperationName ?? invocationTarget.OperationName;

                    // check if method defined any uri parameters
                    var uriParameters = new Dictionary<string, bool>(invocationTarget.UriParameters ?? Enumerable.Empty<KeyValuePair<string, bool>>());
                    if(uriParameters.Any()) {

                        // uri parameters are only valid for $connect route
                        if(mapping.WebSocketSource.RouteKey == "$connect") {

                            // API Gateway V2 cannot be configured to enforce required parameters; so all parameters must be optional
                            foreach(var requiredParameter in uriParameters.Where(uriParameter => uriParameter.Value)) {
                                if(!silent) {
                                    LogError($"uri parameter '{requiredParameter.Key}' for '{mapping.WebSocketSource.RouteKey}' route must be optional");
                                }
                                success = false;
                            }
                        } else {
                            foreach(var uriParameter in uriParameters) {
                                if(!silent) {
                                    LogError($"'{mapping.WebSocketSource.RouteKey}' route cannot have uri parameter '{uriParameter.Key}'");
                                }
                                success = false;
                            }
                        }
                    }
                }
            }
            return success;
        }
    }
}