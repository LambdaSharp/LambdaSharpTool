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
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections;

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelFunctionPackager : AModelProcessor {

        //--- Constants ---
        private const string GIT_INFO_FILE = "git-info.json";
        private const string API_MAPPINGS = "api-mappings.json";
        private const string MIN_AWS_LAMBDA_TOOLS_VERSION = "3.2.3";

        //--- Types ---
        private class CustomAssemblyResolver : BaseAssemblyResolver {

            //--- Fields ---
            private string _directory;
            private List<AssemblyDefinition> _loadedAssemblies = new List<AssemblyDefinition>();

            //--- Constructors ---
            public CustomAssemblyResolver(string directory) {
                _directory = directory;
            }

            //--- Methods ---
            public override AssemblyDefinition Resolve(AssemblyNameReference name) {
                var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(_directory, $"{name.Name}.dll"), new ReaderParameters {
                    AssemblyResolver = this
                });
                if(assembly != null) {
                    _loadedAssemblies.Add(assembly);
                }
                return assembly;
            }

            protected override void Dispose(bool disposing) {
                base.Dispose(disposing);
                if(disposing) {
                    foreach(var assembly in _loadedAssemblies) {
                        assembly.Dispose();
                    }
                }
            }
        }

        private class ApiGatewayInvocationMappings {

            //--- Properties ---
            public List<ApiGatewayInvocationMapping> Mappings = new List<ApiGatewayInvocationMapping>();
        }

        private class ApiGatewayInvocationMapping {

            //--- Properties ---
            public string RestApi;
            public string WebSocket;
            public string Method;
            public RestApiSource RestApiSource;
            public WebSocketSource WebSocketSource;
        }

        //--- Fields ---
        private ModuleBuilder _builder;
        private HashSet<string> _existingPackages;
        private bool _dotnetLambdaToolVersionChecked;
        private bool _dotnetLambdaToolVersionValid;

        //--- Constructors ---
        public ModelFunctionPackager(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Package(
            ModuleBuilder builder,
            bool noCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration
        ) {
            _builder = builder;

            // delete old packages
            if(noCompile) {
                if(Directory.Exists(Settings.OutputDirectory)) {
                    foreach(var file in Directory.GetFiles(Settings.OutputDirectory, "function*.*")) {
                        try {
                            File.Delete(file);
                        } catch { }
                    }
                }
                return;
            }

            // check if there are any functions to package
            var functions = builder.Items.OfType<FunctionItem>();
            if(!functions.Any()) {
                return;
            }

            // collect list of previously built functions
            if(!Directory.Exists(Settings.OutputDirectory)) {
                Directory.CreateDirectory(Settings.OutputDirectory);
            }
            _existingPackages = new HashSet<string>(Directory.GetFiles(Settings.OutputDirectory, "function*.*"));

            // build each function
            foreach(var function in functions) {
                AtLocation(function.FullName, () => {
                    Process(
                        function,
                        noCompile,
                        noAssemblyValidation,
                        gitSha,
                        gitBranch,
                        buildConfiguration
                    );
                });
            }

            // delete remaining built functions, they are out-of-date
            foreach(var leftoverPackage in _existingPackages) {
                try {
                    File.Delete(leftoverPackage);
                } catch { }
            }
        }

        private void Process(
            FunctionItem function,
            bool noCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration
        ) {
            switch(Path.GetExtension(function.Project).ToLowerInvariant()) {
            case "":

                // inline project; nothing to do
                break;
            case ".csproj":
                ProcessDotNet(
                    function,
                    noCompile,
                    noAssemblyValidation,
                    gitSha,
                    gitBranch,
                    buildConfiguration
                );
                break;
            case ".js":
                ProcessJavascript(
                    function,
                    noCompile,
                    noAssemblyValidation,
                    gitSha,
                    gitBranch,
                    buildConfiguration
                );
                break;
            case ".sbt":
                ScalaPackager.ProcessScala(
                    function,
                    noCompile,
                    noAssemblyValidation,
                    gitSha,
                    gitBranch,
                    buildConfiguration,
                    Settings.OutputDirectory,
                    _existingPackages,
                    GIT_INFO_FILE,
                    _builder
                );
                break;

            default:
                LogError("could not determine the function language");
                return;
            }
        }

        private void ProcessDotNet(
            FunctionItem function,
            bool noCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration
        ) {
            function.Language = "csharp";

            // check if AWS Lambda Tools extension is installed
            if(!CheckDotNetLambdaToolIsInstalled()) {
                return;
            }

            // read settings from project file
            var csproj = XDocument.Load(function.Project, LoadOptions.PreserveWhitespace);
            var mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");
            var targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
            var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
            var projectName = mainPropertyGroup?.Element("AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(function.Project);

            // compile function project
            Console.WriteLine($"=> Building function {function.Name} [{targetFramework}, {buildConfiguration}]");
            var projectDirectory = Path.Combine(Settings.WorkingDirectory, Path.GetFileNameWithoutExtension(function.Project));
            var temporaryPackage = Path.Combine(Settings.OutputDirectory, $"function_{_builder.FullName}_{function.LogicalId}_temporary.zip");

            // check if the project contains an obsolete AWS Lambda Tools extension: <DotNetCliToolReference Include="Amazon.Lambda.Tools"/>
            var obsoleteNodes = csproj.DescendantNodes()
                .Where(node =>
                    (node is XElement element)
                    && (element.Name == "DotNetCliToolReference")
                    && ((string)element.Attribute("Include") == "Amazon.Lambda.Tools")
                )
                .ToList();
            if(obsoleteNodes.Any()) {
                LogWarn($"removing obsolete AWS Lambda Tools extension from {Path.GetRelativePath(Settings.WorkingDirectory, function.Project)}");
                foreach(var obsoleteNode in obsoleteNodes) {
                    var parent = obsoleteNode.Parent;

                    // remove obsolete node
                    obsoleteNode.Remove();

                    // remove parent if no children are left
                    if(!parent.Elements().Any()) {
                        parent.Remove();
                    }
                }
                csproj.Save(function.Project);
            }

            // validate the project is using the most recent lambdasharp assembly references
            if(!noAssemblyValidation && function.HasAssemblyValidation) {
                var includes = csproj.Element("Project")
                    ?.Descendants("PackageReference")
                    .Where(elem => elem.Attribute("Include")?.Value.StartsWith("LambdaSharp", StringComparison.Ordinal) ?? false)
                    ?? Enumerable.Empty<XElement>();
                foreach(var include in includes) {
                    var expectedVersion = (Settings.ToolVersion.Major == 0)
                        ? VersionInfo.Parse($"{Settings.ToolVersion.Major}.{Settings.ToolVersion.Minor}.{Settings.ToolVersion.Patch ?? 0}{Settings.ToolVersion.Suffix}")
                        : VersionInfo.Parse($"{Settings.ToolVersion.Major}.{Settings.ToolVersion.Minor}{Settings.ToolVersion.Suffix}");
                    var library = include.Attribute("Include").Value;
                    var libraryVersionText = include.Attribute("Version")?.Value;
                    if(libraryVersionText == null) {
                        LogError($"csproj file is missing a version attribute in its assembly reference for {library} (expected version: '{expectedVersion}')");
                    } else if(libraryVersionText.EndsWith(".*", StringComparison.Ordinal)) {
                        if(!VersionInfo.TryParse(libraryVersionText.Substring(0, libraryVersionText.Length - 2), out var libraryVersion)) {
                            LogError($"csproj file contains an invalid wildcard version in its assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                        } else if(!libraryVersion.IsAssemblyCompatibleWith(expectedVersion)) {

                            // check if we're compiling a conditional package reference in contributor mode
                            if((include.Attribute("Condition")?.Value != null) && (Environment.GetEnvironmentVariable("LAMBDASHARP") != null)) {

                                // show error as warning instead since this package reference will not be used anyway
                                LogWarn($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                            } else {
                                LogError($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                            }
                        }
                    } else if(!VersionInfo.TryParse(libraryVersionText, out var libraryVersion)) {
                        LogError($"csproj file contains an invalid version in its assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                    } else if(!libraryVersion.IsAssemblyCompatibleWith(expectedVersion)) {

                        // check if we're compiling a conditional package reference in contributor mode
                        if((include.Attribute("Condition")?.Value != null) && (Environment.GetEnvironmentVariable("LAMBDASHARP") != null)) {
                            LogWarn($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                        } else {
                            LogError($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                        }
                    }
                }
                if(Settings.HasErrors) {
                    return;
                }
            }
            if(noCompile) {
                return;
            }

            // build project with AWS dotnet CLI lambda tool
            if(!DotNetLambdaPackage(targetFramework, buildConfiguration, temporaryPackage, projectDirectory)) {
                LogError("'dotnet lambda package' command failed");
                return;
            }

            // collect sources with invoke methods
            var mappings = Enumerable.Empty<ApiGatewayInvocationMapping>()
                .Union(function.Sources
                    .OfType<RestApiSource>()
                    .Where(source => source.Invoke != null)
                    .Select(source => new ApiGatewayInvocationMapping {
                        RestApi = $"{source.HttpMethod}:/{string.Join("/", source.Path)}",
                        Method = source.Invoke,
                        RestApiSource = source
                    })
                )
                .Union(function.Sources
                    .OfType<WebSocketSource>()
                    .Where(source => source.Invoke != null)
                    .Select(source => new ApiGatewayInvocationMapping {
                        WebSocket = source.RouteKey,
                        Method = source.Invoke,
                        WebSocketSource = source
                    })
                )
                .ToList();

            // verify the function handler can be found in the compiled assembly
            var buildFolder = Path.Combine(projectDirectory, "bin", buildConfiguration, targetFramework, "publish");
            if(function.HasHandlerValidation) {
                if(function.Function.Handler is string handler) {
                    ValidateEntryPoint(
                        buildFolder,
                        handler
                    );
                }
            }

            // create request/response schemas for invocation methods
            if(!LambdaSharpCreateInvocationSchemas(
                function,
                buildFolder,
                rootNamespace,
                function.Function.Handler as string,
                mappings
            )) {
                LogError("'lash util create-invoke-methods-schema' command failed");
                return;
            }

            // add api mappings JSON file(s)
            if(mappings.Any()) {
                using(var zipArchive = ZipFile.Open(temporaryPackage, ZipArchiveMode.Update)) {
                    var entry = zipArchive.CreateEntry(API_MAPPINGS);

                    // Set RW-R--R-- permissions attributes on non-Windows operating system
                    if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        entry.ExternalAttributes = 0b1_000_000_110_100_100 << 16;
                    }
                    using(var stream = entry.Open()) {
                        stream.Write(Encoding.UTF8.GetBytes(JObject.FromObject(new ApiGatewayInvocationMappings {
                            Mappings = mappings
                        }).ToString(Formatting.None)));
                    }
                }
            }

            // compute hash for zip contents
            string hash;
            using(var zipArchive = ZipFile.OpenRead(temporaryPackage)) {
                using(var md5 = MD5.Create())
                using(var hashStream = new CryptoStream(Stream.Null, md5, CryptoStreamMode.Write)) {
                    foreach(var entry in zipArchive.Entries.OrderBy(e => e.FullName)) {

                        // hash file path
                        var filePathBytes = Encoding.UTF8.GetBytes(entry.FullName.Replace('\\', '/'));
                        hashStream.Write(filePathBytes, 0, filePathBytes.Length);

                        // hash file contents
                        using(var stream = entry.Open()) {
                            stream.CopyTo(hashStream);
                        }
                    }
                    hashStream.FlushFinalBlock();
                    hash = md5.Hash.ToHexString();
                }
            }

            // rename function package with hash
            var package = Path.Combine(Settings.OutputDirectory, $"function_{_builder.FullName}_{function.LogicalId}_{hash}.zip");
            if(!_existingPackages.Remove(package)) {
                File.Move(temporaryPackage, package);

                // add git-info.json file
                using(var zipArchive = ZipFile.Open(package, ZipArchiveMode.Update)) {
                    var entry = zipArchive.CreateEntry(GIT_INFO_FILE);

                    // Set RW-R--R-- permissions attributes on non-Windows operating system
                    if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        entry.ExternalAttributes = 0b1_000_000_110_100_100 << 16;
                    }
                    using(var stream = entry.Open()) {
                        stream.Write(Encoding.UTF8.GetBytes(JObject.FromObject(new ModuleManifestGitInfo {
                            SHA = gitSha,
                            Branch = gitBranch
                        }).ToString(Formatting.None)));
                    }
                }
            } else {
                File.Delete(temporaryPackage);
            }

            // set the module variable to the final package name
            _builder.AddAsset($"{function.FullName}::PackageName", package);
        }

        private bool DotNetLambdaPackage(string targetFramework, string buildConfiguration, string outputPackagePath, string projectDirectory) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                LogError("failed to find the \"dotnet\" executable in path.");
                return false;
            }
            return ProcessLauncher.Execute(
                dotNetExe,
                new[] {
                    "lambda", "package",
                    "--configuration", buildConfiguration,
                    "--framework", targetFramework,
                    "--output-package", outputPackagePath,
                    "--disable-interactive", "true"
                },
                projectDirectory,
                Settings.VerboseLevel >= VerboseLevel.Detailed
            );
        }

        private bool CheckDotNetLambdaToolIsInstalled() {

            // only run check once
            if(_dotnetLambdaToolVersionChecked) {
                return _dotnetLambdaToolVersionValid;
            }
            _dotnetLambdaToolVersionChecked = true;

            // check if dotnet executable can be found
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                LogError("failed to find the \"dotnet\" executable in path.");
                return false;
            }

            // check if AWS Lambda Tools extension is installed
            var result = ProcessLauncher.ExecuteWithOutputCapture(
                dotNetExe,
                new[] { "lambda", "tool", "help" },
                workingFolder: null
            );
            if(result == null) {

                // attempt to install the AWS Lambda Tools extension
                if(!ProcessLauncher.Execute(
                    dotNetExe,
                    new[] { "tool", "install", "-g", "Amazon.Lambda.Tools" },
                    workingFolder: null,
                    showOutput: false
                )) {
                    LogError("'dotnet tool install -g Amazon.Lambda.Tools' command failed");
                    return false;
                }

                // latest version is now installed, we're good to proceed
                _dotnetLambdaToolVersionValid = true;
                return true;
            }

            // check version of installed AWS Lambda Tools extension
            var match = Regex.Match(result, @"\((?<Version>.*)\)");
            if(!match.Success || !VersionInfo.TryParse(match.Groups["Version"].Value, out var version)) {
                LogWarn("proceeding compilation with unknown version of 'Amazon.Lambda.Tools'; please ensure latest version is installed");
                _dotnetLambdaToolVersionValid = true;
                return true;
            }
            if(version.IsLessThanVersion(VersionInfo.Parse(MIN_AWS_LAMBDA_TOOLS_VERSION), strict: true)) {

                // attempt to install the AWS Lambda Tools extension
                if(!ProcessLauncher.Execute(
                    dotNetExe,
                    new[] { "tool", "update", "-g", "Amazon.Lambda.Tools" },
                    workingFolder: null,
                    showOutput: false
                )) {
                    LogError("'dotnet tool update -g Amazon.Lambda.Tools' command failed");
                    return false;
                }
            }
            _dotnetLambdaToolVersionValid = true;
            return true;
        }

        private bool ZipWithTool(string zipArchivePath, string zipFolder) {
            var zipTool = ProcessLauncher.ZipExe;
            if(string.IsNullOrEmpty(zipTool)) {
                LogError("failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.");
                return false;
            }
            return ProcessLauncher.Execute(
                zipTool,
                new[] { "-r", zipArchivePath, "." },
                zipFolder,
                Settings.VerboseLevel >= VerboseLevel.Detailed
            );
        }

        private void ProcessJavascript(
            FunctionItem function,
            bool noCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration
        ) {
            if(noCompile) {
                return;
            }
            Console.WriteLine($"=> Building function {function.Name} [{function.Function.Runtime}]");
            var buildFolder = Path.GetDirectoryName(function.Project);
            var hash = Directory.GetFiles(buildFolder, "*", SearchOption.AllDirectories).ComputeHashForFiles(file => Path.GetRelativePath(buildFolder, file));
            var package = Path.Combine(Settings.OutputDirectory, $"function_{_builder.FullName}_{function.LogicalId}_{hash}.zip");
            if(!_existingPackages.Remove(package)) {
                CreatePackage(package, gitSha, gitBranch, buildFolder);
            }
            _builder.AddAsset($"{function.FullName}::PackageName", package);
        }

        private void CreatePackage(string package, string gitSha, string gitBranch, string folder) {

            // add 'git-info.json' if git sha or git branch is supplied
            var gitInfoFileCreated = false;
            if((gitSha != null) || (gitBranch != null)) {
                gitInfoFileCreated = true;
                File.WriteAllText(Path.Combine(folder, GIT_INFO_FILE), JObject.FromObject(new ModuleManifestGitInfo {
                    SHA = gitSha,
                    Branch = gitBranch
                }).ToString(Formatting.None));
            }

            // compress temp folder into new package
            var zipTempPackage = Path.GetTempFileName() + ".zip";
            if(File.Exists(zipTempPackage)) {
                File.Delete(zipTempPackage);
            }

            // compress folder contents
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                using(var zipArchive = ZipFile.Open(zipTempPackage, ZipArchiveMode.Create)) {
                    foreach(var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) {
                        var filename = Path.GetRelativePath(folder, file);
                        zipArchive.CreateEntryFromFile(file, filename);
                    }
                }
            } else {
                if(!ZipWithTool(zipTempPackage, folder)) {
                    LogError("'zip' command failed");
                    return;
                }
            }
            if(gitInfoFileCreated) {
                try {
                    File.Delete(Path.Combine(folder, GIT_INFO_FILE));
                } catch { }
            }
            if(!Directory.Exists(Settings.OutputDirectory)) {
                Directory.CreateDirectory(Settings.OutputDirectory);
            }
            File.Move(zipTempPackage, package);
        }

        private void ValidateEntryPoint(string directory, string handler) {
            var parts = handler.Split("::");
            if(parts.Length != 3) {
                LogError("'Handler' attribute has invalid value");
                return;
            }
            try {
                var lambdaFunctionAssemblyName = parts[0];
                var lambdaFunctionClassName = parts[1];
                var lambdaFunctionEntryPointName = parts[2];
                using(var resolver = new CustomAssemblyResolver(directory))
                using(var lambdaFunctionAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(directory, $"{lambdaFunctionAssemblyName}.dll"), new ReaderParameters {
                    AssemblyResolver = resolver
                })) {
                    if(lambdaFunctionAssembly == null) {
                        LogError("could not load assembly");
                        return;
                    }
                    var functionClassType = lambdaFunctionAssembly.MainModule.GetType(lambdaFunctionClassName);
                    if(functionClassType == null) {
                        LogError($"could not find type '{lambdaFunctionClassName}' in assembly");
                        return;
                    }
                    FindMethod(functionClassType, lambdaFunctionEntryPointName);

                    // local functions
                    void FindMethod(TypeDefinition methodClassType, string methodName) {
                        again:
                            var functionMethod = methodClassType.Methods.FirstOrDefault(method => method.Name == methodName);
                            if(functionMethod == null) {
                                if((methodClassType.BaseType == null) || (methodClassType.BaseType.FullName == "System.Object")) {
                                    LogError($"could not find method '{methodName}' in class '{lambdaFunctionClassName}'");
                                    return;
                                }
                                methodClassType = methodClassType.BaseType.Resolve();
                                goto again;
                            }
                    }
                }
            } catch(Exception e) {
                if(Settings.VerboseLevel >= VerboseLevel.Exceptions) {
                    LogError(e);
                } else {
                    LogWarn("unable to validate function entry-point due to an internal error");
                }
            }
        }

        private bool LambdaSharpCreateInvocationSchemas(
            FunctionItem function,
            string buildFolder,
            string rootNamespace,
            string handler,
            IEnumerable<ApiGatewayInvocationMapping> mappings
        ) {

            // check if there is anything to do
            if(!mappings.Any()) {
                return true;
            }

            // check if we have enough information to resolve the invocation methods
            var incompleteMappings = mappings.Where(mapping =>
                !StringEx.TryParseAssemblyClassMethodReference(
                    mapping.Method,
                    out var mappingAssemblyName,
                    out var mappingClassName,
                    out var mappingMethodName
                )
                || (mappingAssemblyName == null)
                || (mappingClassName == null)
            ).ToList();
            if(incompleteMappings.Any()) {
                if(handler == null) {
                    LogError("either function 'Handler' attribute must be specified as a literal value or all invocation methods must be fully qualified");
                    return false;
                }

                // extract the assembly and class name from the handler
                if(!StringEx.TryParseAssemblyClassMethodReference(
                    handler,
                    out var lambdaFunctionAssemblyName,
                    out var lambdaFunctionClassName,
                    out var lambdaFunctionEntryPointName
                )) {
                    LogError("'Handler' attribute has invalid value");
                    return false;
                }

                // set default qualifier to the class name of the function handler
                foreach(var mapping in incompleteMappings) {
                    if(!StringEx.TryParseAssemblyClassMethodReference(
                        mapping.Method,
                        out var mappingAssemblyName,
                        out var mappingClassName,
                        out var mappingMethodName
                    )) {
                        LogError("'Invoke' attribute has invalid value");
                        return false;
                    }
                    mapping.Method = $"{mappingAssemblyName ?? lambdaFunctionAssemblyName}::{mappingClassName ?? lambdaFunctionClassName}::{mappingMethodName}";
                }
            }

            // build invocation arguments
            string schemaFile = Path.Combine(Settings.OutputDirectory, $"functionschema_{_builder.FullName}_{function.LogicalId}.json");
            _existingPackages.Remove(schemaFile);
            var arguments = new[] {
                "util", "create-invoke-methods-schema",
                "--directory", buildFolder,
                "--default-namespace", rootNamespace,
                "--out", schemaFile,
                "--quiet"
            }
                .Union(mappings.Select(mapping => $"--method={mapping.Method}"))
                .ToList();

            // check if lambdasharp is installed or if we need to run it using dotnet
            var lambdaSharpFolder = Environment.GetEnvironmentVariable("LAMBDASHARP");
            if(lambdaSharpFolder == null) {

                // check if lash executable exists (it should since we're running)
                var lash = ProcessLauncher.Lash;
                if(string.IsNullOrEmpty(lash)) {
                    LogError("failed to find the \"lash\" executable in path.");
                    return false;
                }
                var success = ProcessLauncher.Execute(
                    lash,
                    arguments,
                    Settings.WorkingDirectory,
                    Settings.VerboseLevel >= VerboseLevel.Detailed
                );
            } else {

                // check if dotnet executable exists
                var dotNetExe = ProcessLauncher.DotNetExe;
                if(string.IsNullOrEmpty(dotNetExe)) {
                    LogError("failed to find the \"dotnet\" executable in path.");
                    return false;
                }
                var success = ProcessLauncher.Execute(
                    dotNetExe,
                    new[] {
                        "run", "-p", $"{lambdaSharpFolder}/src/LambdaSharp.Tool", "--"
                    }.Union(arguments).ToList(),
                    Settings.WorkingDirectory,
                    Settings.VerboseLevel >= VerboseLevel.Detailed
                );
            }
            try {
                var schemas = (Dictionary<string, InvocationTargetDefinition>)JsonConvert.DeserializeObject<Dictionary<string, InvocationTargetDefinition>>(File.ReadAllText(schemaFile)).ConvertJTokenToNative(type => type == typeof(InvocationTargetDefinition));
                foreach(var mapping in mappings) {
                    if(!schemas.TryGetValue(mapping.Method, out var invocationTarget)) {
                        LogError($"failed to resolve method '{mapping.Method}'");
                        continue;
                    }
                    if(invocationTarget.Error != null) {
                        LogError(invocationTarget.Error);
                        continue;
                    }

                    // update mapping information
                    mapping.Method = $"{invocationTarget.Assembly}::{invocationTarget.Type}::{invocationTarget.Method}";
                    if(mapping.RestApiSource != null) {
                        mapping.RestApiSource.RequestContentType = mapping.RestApiSource.RequestContentType ?? invocationTarget.RequestContentType;
                        mapping.RestApiSource.RequestSchema = mapping.RestApiSource.RequestSchema ?? invocationTarget.RequestSchema;
                        mapping.RestApiSource.RequestSchemaName = mapping.RestApiSource.RequestSchemaName ?? invocationTarget.RequestSchemaName;
                        mapping.RestApiSource.ResponseContentType = mapping.RestApiSource.ResponseContentType ?? invocationTarget.ResponseContentType;
                        mapping.RestApiSource.ResponseSchema = mapping.RestApiSource.ResponseSchema ?? invocationTarget.ResponseSchema;
                        mapping.RestApiSource.ResponseSchemaName = mapping.RestApiSource.ResponseSchemaName ?? invocationTarget.ResponseSchemaName;
                        mapping.RestApiSource.OperationName = mapping.RestApiSource.OperationName ?? invocationTarget.OperationName;

                        // determine which uri parameters come from the request path vs. the query-string
                        var uriParameters = new Dictionary<string, bool>(invocationTarget.UriParameters ?? Enumerable.Empty<KeyValuePair<string, bool>>());
                        foreach(var pathParameter in mapping.RestApiSource.Path
                            .Where(segment => segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal))
                            .Select(segment => segment.ToIdentifier())
                            .ToArray()
                        ) {
                            if(!uriParameters.Remove(pathParameter)) {
                                LogError($"path parameter '{pathParameter}' is missing in method declaration '{invocationTarget.Type}::{invocationTarget.Method}'");
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
                                    LogError($"uri parameter '{requiredParameter.Key}' for '{mapping.WebSocketSource.RouteKey}' route must be optional");
                                }
                            } else {
                                foreach(var uriParameter in uriParameters) {
                                    LogError($"'{mapping.WebSocketSource.RouteKey}' route cannot have uri parameter '{uriParameter.Key}'");
                                }
                            }
                        }
                    }
                }
            } catch(Exception e) {
                LogError("unable to read create-invoke-methods-schema output", e);
                return false;
            }
            return true;
        }
    }
}