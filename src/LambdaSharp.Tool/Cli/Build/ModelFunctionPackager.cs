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
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelFunctionPackager : AModelProcessor {

        //--- Constants ---
        private const string GIT_INFO_FILE = "git-info.json";
        private const string API_MAPPINGS = "api-mappings.json";
        private const string MIN_AWS_LAMBDA_TOOLS_VERSION = "4.0.0";

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
            string buildConfiguration,
            bool forceBuild
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
                        buildConfiguration,
                        forceBuild
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
            string buildConfiguration,
            bool forceBuild
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
                    buildConfiguration,
                    forceBuild
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
            string buildConfiguration,
            bool forceBuild
        ) {
            function.Language = "csharp";

            // check if AWS Lambda Tools extension is installed
            if(!CheckDotNetLambdaToolIsInstalled()) {
                return;
            }

            // collect sources with invoke methods
            var mappings = ExtractMappings(function);
            if(mappings == null) {
                return;
            }

            // check if a function package already exists
            if(!forceBuild) {
                var functionPackage = _existingPackages.FirstOrDefault(p =>
                    Path.GetFileName(p).StartsWith($"function_{_builder.FullName}_{function.LogicalId}_", StringComparison.Ordinal)
                    && p.EndsWith(".zip", StringComparison.Ordinal)
                );

                // to skip the build, we both need the function package and the function schema when mappings are present
                var schemaFile = Path.Combine(Settings.OutputDirectory, $"functionschema_{_builder.FullName}_{function.LogicalId}.json");
                if((functionPackage != null) && (!mappings.Any() || File.Exists(schemaFile))) {
                    LogInfoVerbose($"=> Analyzing function {function.Name} dependencies");

                    // find all files used to create the function package
                    var files = new HashSet<string>();
                    AddProjectFiles(files, MsBuildFileUtilities.MaybeAdjustFilePath("", function.Project));

                    // check if any of the files has been modified more recently than the function package
                    var functionPackageDate = File.GetLastWriteTime(functionPackage);
                    var file = files.FirstOrDefault(f => File.GetLastWriteTime(f) > functionPackageDate);
                    if(file == null) {
                        var success = true;
                        if(mappings.Any()) {

                            // apply function schema to generate REST API and WebSocket models
                            try {
                                success = ApplyInvocationSchemas(function, mappings, schemaFile);
                            } catch(Exception e) {
                                LogError("unable to read create-invoke-methods-schema output", e);
                                return;
                            }
                        }

                        // only skip compilation if we were able to apply the invocation schemas (or didn't have to)
                        if(success) {
                            if(Settings.UseAnsiConsole) {
                                Console.WriteLine($"=> Skipping function {AnsiTerminal.Yellow}{function.Name}{AnsiTerminal.Reset} (no changes found)");
                            } else {
                                Console.WriteLine($"=> Skipping function {function.Name} (no changes found)");
                            }

                            // keep the existing package
                            _existingPackages.Remove(functionPackage);

                            // set the module variable to the final package name
                            _builder.AddArtifact($"{function.FullName}::PackageName", functionPackage);
                            return;
                        }
                    } else {
                        LogInfoVerbose($"... change detected in {file}");
                    }
                }
            } else {
                LogInfoVerbose($"=> Analyzing function {function.Name} dependencies");

                // find all files used to create the function package
                var files = new HashSet<string>();
                AddProjectFiles(files, MsBuildFileUtilities.MaybeAdjustFilePath("", function.Project));

                // loop over all project folders
                foreach(var projectFolder in files.Where(file => file.EndsWith(".csproj", StringComparison.Ordinal)).Select(file => Path.GetDirectoryName(file))) {
                    LogInfoVerbose($"... deleting build folders for {projectFolder}");
                    DeleteFolder(Path.Combine(projectFolder, "obj"));
                    DeleteFolder(Path.Combine(projectFolder, "bin"));

                    // local functions
                    void DeleteFolder(string folder) {
                        if(Directory.Exists(folder)) {
                            try {
                                Directory.Delete(folder, recursive: true);
                            } catch {
                                LogWarn($"unable to delete: {folder}");
                            }
                        }
                    }
                }
            }

            // read settings from project file
            var csproj = XDocument.Load(function.Project, LoadOptions.PreserveWhitespace);
            var mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");
            var targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
            var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
            var projectName = mainPropertyGroup?.Element("AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(function.Project);

            // compile function project
            var isNetCore31OrLater = targetFramework.CompareTo("netcoreapp3.") >= 0;
            var isAmazonLinux2 = Settings.IsAmazonLinux2();
            var isReadyToRun = isNetCore31OrLater && isAmazonLinux2;
            var readyToRunText = isReadyToRun ? ", ReadyToRun" : "";
            if(Settings.UseAnsiConsole) {
                Console.WriteLine($"=> Building function {AnsiTerminal.Yellow}{function.Name}{AnsiTerminal.Reset} [{targetFramework}, {buildConfiguration}{readyToRunText}]");
            } else {
                Console.WriteLine($"=> Building function {function.Name} [{targetFramework}, {buildConfiguration}{readyToRunText}]");
            }
            var projectDirectory = Path.Combine(Settings.WorkingDirectory, Path.GetFileNameWithoutExtension(function.Project));
            var temporaryPackage = Path.Combine(Settings.OutputDirectory, $"function_{_builder.FullName}_{function.LogicalId}_temporary.zip");

            // check if the project contains an obsolete AWS Lambda Tools extension: <DotNetCliToolReference Include="Amazon.Lambda.Tools"/>
            var obsoleteNodes = csproj.Descendants()
                .Where(element =>
                    (element.Name == "DotNetCliToolReference")
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
            if(!DotNetLambdaPackage(targetFramework, buildConfiguration, temporaryPackage, projectDirectory, forceBuild, isNetCore31OrLater, isAmazonLinux2, isReadyToRun)) {

                // nothing to do; error was already reported
                return;
            }

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
            if(_existingPackages.Remove(package)) {

                // remove old, existing package so we can move the new package into location (which also preserves the more recent build timestamp)
                File.Delete(package);
            }
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

            // set the module variable to the final package name
            _builder.AddArtifact($"{function.FullName}::PackageName", package);
        }

        private void AddProjectFiles(HashSet<string> files, string project) {
            try {

                // skip project if project file doesn't exist or has already been added
                if(!File.Exists(project) || files.Contains(project)) {
                    return;
                }
                files.Add(project);
                LogInfoVerbose($"... analyzing {project}");

                // enumerate all files in project folder
                var projectFolder = Path.GetDirectoryName(project);
                AddFiles(projectFolder, SearchOption.AllDirectories);
                files.RemoveWhere(file => file.StartsWith(Path.GetFullPath(Path.Combine(projectFolder, "bin"))));
                files.RemoveWhere(file => file.StartsWith(Path.GetFullPath(Path.Combine(projectFolder, "obj"))));

                // analyze project for references
                var csproj = XDocument.Load(project, LoadOptions.PreserveWhitespace);

                // TODO (2019-10-22, bjorg): enhance precision for understanding elements in .csrpoj files

                // recurse into referenced projects
                foreach(var projectReference in csproj.Descendants("ProjectReference").Where(node => node.Attribute("Include") != null)) {
                    AddProjectFiles(files, GetFilePathFromIncludeAttribute(projectReference));
                }

                // add compile file references
                foreach(var compile in csproj.Descendants("Compile").Where(node => node.Attribute("Include") != null)) {
                    AddFileReferences(GetFilePathFromIncludeAttribute(compile));
                }

                // add content file references
                foreach(var content in csproj.Descendants("Content").Where(node => node.Attribute("Include") != null)) {
                    AddFileReferences(GetFilePathFromIncludeAttribute(content));
                }

                // added embedded resources
                foreach(var embeddedResource in csproj.Descendants("EmbeddedResource").Where(node => node.Attribute("Include") != null)) {
                    AddFileReferences(GetFilePathFromIncludeAttribute(embeddedResource));
                }

                // local functions
                string GetFilePathFromIncludeAttribute(XElement element)
                    => Path.GetFullPath(Path.Combine(projectFolder, MsBuildFileUtilities.MaybeAdjustFilePath(projectFolder, ResolveFilePath(element.Attribute("Include").Value))));

            } catch(Exception e) {
                LogError($"error while analyzing '{project}'", e);
            }

            // local function
            void AddFileReferences(string path) {
                var parts = path.Split(new[] { '/', '\\' });
                if(path.Contains("**")) {

                    // NOTE: path contains a recursive wildcard; take part of path up until segment that contains the recursion wildcard '**'
                    var recursionRootPath = Path.Combine(parts.TakeWhile(part => !part.Contains("**")).ToArray());
                    AddFiles(recursionRootPath, SearchOption.AllDirectories);
                } else if(parts.Take(parts.Length - 1).Any(part => part.Contains("*") || part.Contains("?"))) {

                    // NOTE: path contains a wildcard character in a folder portion of the path; enumerate all contents like we do for '**';
                    var recursionRootPath = Path.Combine(parts.TakeWhile(part => !part.Contains("*") && !part.Contains("?")).ToArray());
                    AddFiles(recursionRootPath, SearchOption.AllDirectories);
                } else if(parts.Last().Contains("*")) {

                    // NOTE: last segment in path contains a wildcard for the filename; enumerate the folder contents without recursion

                    // exclude last path segment that contains the wildcard
                    var rootPath = Path.Combine(parts.Take(parts.Length - 1).ToArray());
                    AddFiles(rootPath, SearchOption.TopDirectoryOnly);
                } else if(Directory.Exists(path)) {
                    AddFiles(path, SearchOption.TopDirectoryOnly);
                } else if(File.Exists(path)) {
                    files.Add(path);
                }
            }

            void AddFiles(string folder, SearchOption option) {
                if(Directory.Exists(folder)) {
                    foreach(var file in Directory.GetFiles(folder, "*.*", option)) {
                        files.Add(file);
                    }
                }
            }

            string ResolveFilePath(string path) => Regex.Replace(path, @"\$\((?!\!)[^\)]+\)", match => {
                var matchText = match.ToString();
                var name = matchText.Substring(2, matchText.Length - 3).Trim();
                return Environment.GetEnvironmentVariable(name) ?? matchText;
            });
        }

        private bool DotNetLambdaPackage(
            string targetFramework,
            string buildConfiguration,
            string outputPackagePath,
            string projectDirectory,
            bool forceBuild,
            bool isNetCore31OrLater,
            bool isAmazonLinux2,
            bool isReadyToRun
        ) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                LogError("failed to find the \"dotnet\" executable in path.");
                return false;
            }

            // set MSBuild optimization parameters
            var msBuildParametersList = new List<string>();
            if(isNetCore31OrLater) {

                // allows disable tiered compilation since Lambda functions are generally short lived
                msBuildParametersList.Add("/p:TieredCompilation=false");
                msBuildParametersList.Add("/p:TieredCompilationQuickJit=false");
            }

            // enable Ready2Run when compiling on Amazon Linux 2
            if(isReadyToRun) {
                msBuildParametersList.Add("/p:PublishReadyToRun=true");
            }
            var msBuildParameters = string.Join(" ", msBuildParametersList);

            // NOTE: with --force-build, we need to explicitly invoke `dotnet build` to pass in the `--no-incremental` and `--force` options;
            //  we do this to ensure that `dotnet build` doesn't create an invalid executable when environment variables, such as LAMBDASHARP, change between builds.
            if(forceBuild && !ProcessLauncher.Execute(
                dotNetExe,
                new[] {
                    "build",
                    "--force",
                    "--no-incremental",
                    "--configuration", buildConfiguration,
                    "--framework", targetFramework,
                    "--runtime", isNetCore31OrLater ? "linux-x64" : "rhel.7.2-x64",
                    msBuildParameters
                },
                projectDirectory,
                Settings.VerboseLevel >= VerboseLevel.Detailed,
                ColorizeOutput
            )) {
                LogError("'dotnet build' command failed");
                return false;
            }
            if(!ProcessLauncher.Execute(
                dotNetExe,
                new[] {
                    "lambda", "package",
                    "--configuration", buildConfiguration,
                    "--framework", targetFramework,
                    "--output-package", outputPackagePath,
                    "--disable-interactive", "true",
                    "--msbuild-parameters", $"\"{msBuildParameters}\""
                },
                projectDirectory,
                Settings.VerboseLevel >= VerboseLevel.Detailed,
                ColorizeOutput
            )) {
                LogError("'dotnet lambda package' command failed");
                return false;
            }
            return true;

            // local functions
            string ColorizeOutput(string line)
                => !Settings.UseAnsiConsole
                    ? line
                    : line.Contains(": error ", StringComparison.Ordinal)
                    ? $"{AnsiTerminal.BrightRed}{line}{AnsiTerminal.Reset}"
                    : line.Contains(": warning ", StringComparison.Ordinal)
                    ? $"{AnsiTerminal.BrightYellow}{line}{AnsiTerminal.Reset}"
                    : line;
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
            if(Settings.UseAnsiConsole) {
                Console.WriteLine($"=> Building function {AnsiTerminal.Yellow}{function.Name}{AnsiTerminal.Reset} [{function.Function.Runtime}]");
            } else {
                Console.WriteLine($"=> Building function {function.Name} [{function.Function.Runtime}]");
            }
            var buildFolder = Path.GetDirectoryName(function.Project);
            var hash = Directory.GetFiles(buildFolder, "*", SearchOption.AllDirectories).ComputeHashForFiles(file => Path.GetRelativePath(buildFolder, file));
            var package = Path.Combine(Settings.OutputDirectory, $"function_{_builder.FullName}_{function.LogicalId}_{hash}.zip");
            _existingPackages.Remove(package);
            CreatePackage(package, gitSha, gitBranch, buildFolder);
            _builder.AddArtifact($"{function.FullName}::PackageName", package);
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
            if(File.Exists(package)) {
                File.Delete(package);
            }
            File.Move(zipTempPackage, package);
        }

        private void ValidateEntryPoint(string buildFolder, string handler) {
            RunLashTool(
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
                        LogError(output.Substring(6).Trim());
                    } else if(output.StartsWith("WARNING:")) {
                        LogError(output.Substring(8).Trim());
                    }
                    return null;
                }
            );
        }

        private List<ApiGatewayInvocationMapping> ExtractMappings(FunctionItem function) {

            // find all REST API and WebSocket mappings function sources
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
                var handler = function.Function.Handler as string;
                if(handler == null) {
                    LogError("either function 'Handler' attribute must be specified as a literal value or all invocation methods must be fully qualified");
                    return null;
                }

                // extract the assembly and class name from the handler
                if(!StringEx.TryParseAssemblyClassMethodReference(
                    handler,
                    out var lambdaFunctionAssemblyName,
                    out var lambdaFunctionClassName,
                    out var lambdaFunctionEntryPointName
                )) {
                    LogError("'Handler' attribute has invalid value");
                    return null;
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
                        return null;
                    }
                    mapping.Method = $"{mappingAssemblyName ?? lambdaFunctionAssemblyName}::{mappingClassName ?? lambdaFunctionClassName}::{mappingMethodName}";
                }
            }
            return mappings;
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
            var success = RunLashTool(arguments, Settings.VerboseLevel >= VerboseLevel.Detailed);
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
            FunctionItem function,
            IEnumerable<ApiGatewayInvocationMapping> mappings,
            string schemaFile
        ) {
            _existingPackages.Remove(schemaFile);
            var schemas = (Dictionary<string, InvocationTargetDefinition>)JsonConvert.DeserializeObject<Dictionary<string, InvocationTargetDefinition>>(File.ReadAllText(schemaFile))
                .ConvertJTokenToNative(type => type == typeof(InvocationTargetDefinition));
            var success = true;
            foreach(var mapping in mappings) {
                if(!schemas.TryGetValue(mapping.Method, out var invocationTarget)) {
                    LogError($"failed to resolve method '{mapping.Method}'");
                    success = false;
                    continue;
                }
                if(invocationTarget.Error != null) {
                    LogError(invocationTarget.Error);
                    success = false;
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
            return success;
        }

        private bool RunLashTool(IEnumerable<string> arguments, bool showOutput, Func<string, string> processOutputLine = null) {

            // check if lambdasharp is installed or if we need to run it using dotnet
            var lambdaSharpFolder = Environment.GetEnvironmentVariable("LAMBDASHARP");
            bool success;
            if(lambdaSharpFolder == null) {

                // check if lash executable exists (it should since we're running)
                var lash = ProcessLauncher.Lash;
                if(string.IsNullOrEmpty(lash)) {
                    LogError("failed to find the \"lash\" executable in path.");
                    return false;
                }
                success = ProcessLauncher.Execute(
                    lash,
                    arguments,
                    Settings.WorkingDirectory,
                    showOutput,
                    processOutputLine
                );
            } else {

                // check if dotnet executable exists
                var dotNetExe = ProcessLauncher.DotNetExe;
                if(string.IsNullOrEmpty(dotNetExe)) {
                    LogError("failed to find the \"dotnet\" executable in path.");
                    return false;
                }
                success = ProcessLauncher.Execute(
                    dotNetExe,
                    new[] {
                        "run", "-p", $"{lambdaSharpFolder}/src/LambdaSharp.Tool", "--"
                    }.Union(arguments).ToList(),
                    Settings.WorkingDirectory,
                    showOutput,
                    processOutputLine
                );
            }
            return success;
        }
    }
}