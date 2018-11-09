/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using MindTouch.LambdaSharp.Tool.Model.AST;
using MindTouch.LambdaSharp.Tool.Internal;
using System.Text;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelFunctionPackager : AModelProcessor {

        //--- Constants ---
        private const string GITSHAFILE = "gitsha.txt";

        //--- Constructors ---
        public ModelFunctionPackager(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        public void Process(
            ModuleNode module,
            VersionInfo version,
            bool skipCompile,
            bool skipAssemblyValidation,
            string gitsha,
            string buildConfiguration
        ) {
            foreach(var function in module.Functions) {
                AtLocation(function.Function, () => {
                    Process(
                        module,
                        function,
                        version,
                        skipCompile,
                        skipAssemblyValidation,
                        gitsha,
                        buildConfiguration
                    );
                });
            }
        }

        private void Process(
            ModuleNode module,
            FunctionNode function,
            VersionInfo version,
            bool skipCompile,
            bool skipAssemblyValidation,
            string gitsha,
            string buildConfiguration
        ) {

            // identify folder for function
            var folderName = new[] {
                function.Function,
                $"{module.Module}.{function.Function}"
            }.FirstOrDefault(name => Directory.Exists(Path.Combine(Settings.WorkingDirectory, name)));
            if(folderName == null) {
                AddError($"could not locate function directory");
                return;
            }

            // delete old packages
            if(Directory.Exists(Settings.OutputDirectory)) {
                foreach(var file in Directory.GetFiles(Settings.OutputDirectory, $"function_{function.Function}*.zip")) {
                    try {
                        File.Delete(file);
                    } catch { }
                }
            }

            // determine the function project
            var projectPath = function.Project ?? new [] {
                Path.Combine(Settings.WorkingDirectory, folderName, $"{folderName}.csproj"),
                Path.Combine(Settings.WorkingDirectory, folderName, "index.js")
            }.FirstOrDefault(path => File.Exists(path));
            if(projectPath == null) {
                AddError("could not locate the function project");
                return;
            }
            switch(Path.GetExtension(projectPath).ToLowerInvariant()) {
            case ".csproj":
                ProcessDotNet(
                    module,
                    function,
                    version,
                    skipCompile,
                    skipAssemblyValidation,
                    gitsha,
                    buildConfiguration,
                    projectPath
                );
                break;
            case ".js":
                ProcessJavascript(
                    module,
                    function,
                    version,
                    skipCompile,
                    skipAssemblyValidation,
                    gitsha,
                    buildConfiguration,
                    projectPath
                );
                break;
            default:
                AddError("could not determine the function language");
                return;
            }
        }

        private void ProcessDotNet(
            ModuleNode module,
            FunctionNode function,
            VersionInfo version,
            bool skipCompile,
            bool skipAssemblyValidation,
            string gitsha,
            string buildConfiguration,
            string project
        ) {
            function.Language = "csharp";

            // compile function project
            var projectName = Path.GetFileNameWithoutExtension(project);
            XDocument csproj = null;
            XElement mainPropertyGroup = null;
            if(!AtLocation("Project", () => {

                // check if the handler/runtime were provided or if they need to be extracted from the project file
                csproj = XDocument.Load(project);
                mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");

                // make sure the .csproj file contains the lambda tooling
                var hasAwsLambdaTools = csproj.Element("Project")
                    ?.Elements("ItemGroup")
                    .Any(el => (string)el.Element("DotNetCliToolReference")?.Attribute("Include") == "Amazon.Lambda.Tools") ?? false;
                if(!hasAwsLambdaTools) {
                    AddError($"the project is missing the AWS lambda tool defintion; make sure that {project} includes <DotNetCliToolReference Include=\"Amazon.Lambda.Tools\"/>");
                    return false;
                }
                return true;
            }, false)) {
                return;
            }

            // check if we need to read the project file <RootNamespace> element to determine the handler name
            if(function.Handler == null) {
                AtLocation("Handler", () => {
                    var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
                    if(rootNamespace != null) {
                        function.Handler = $"{projectName}::{rootNamespace}.Function::FunctionHandlerAsync";
                    } else {
                        AddError("could not auto-determine handler; either add Function field or <RootNamespace> to project file");
                    }
                });
            }

            // check if we need to parse the <TargetFramework> element to determine the lambda runtime
            var targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
            if(function.Runtime == null) {
                AtLocation("Runtime", () => {
                    switch(targetFramework) {
                    case "netcoreapp1.0":
                        function.Runtime = "dotnetcore1.0";
                        break;
                    case "netcoreapp2.0":
                        function.Runtime = "dotnetcore2.0";
                        break;
                    case "netcoreapp2.1":
                        function.Runtime = "dotnetcore2.1";
                        break;
                    default:
                        AddError("could not auto-determine handler; add Runtime field");
                        break;
                    }
                });
            }

            // validate the project is using the most recent lambdasharp assembly references
            if(!skipAssemblyValidation) {
                var includes = csproj.Element("Project")
                    ?.Elements("ItemGroup")
                    .Elements("PackageReference")
                    .Where(elem => elem.Attribute("Include")?.Value.StartsWith("MindTouch.LambdaSharp", StringComparison.Ordinal) ?? false);
                if(includes != null) {
                    foreach(var include in includes) {
                        var expectedVersion = VersionInfo.Parse($"{version.Major}.{version.Minor}{version.Suffix}");
                        var library = include.Attribute("Include").Value;
                        var libraryVersionText = include.Attribute("Version")?.Value;
                        if(libraryVersionText == null) {
                            AddError($"csproj file is missing a version attribute in its assembly reference for {library} (expected version: '{expectedVersion}')");
                        } if(!VersionInfo.TryParse(libraryVersionText, out VersionInfo libraryVersion)) {
                            AddError($"csproj file contains an invalid version in its assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                        } else if(!libraryVersion.IsCompatibleWith(expectedVersion)) {
                            AddError($"csproj file contains a mismatched assembly reference for {library} (expected version: '{expectedVersion}', found: '{libraryVersionText}')");
                        }
                    }
                    if(Settings.HasErrors) {
                        return;
                    }
                }
            }
            if(skipCompile) {
                function.PackagePath = $"{function.Function}-NOCOMPILE.zip";
                return;
            }

            // dotnet tools have to be run from the project folder; otherwise specialized tooling is not picked up from the .csproj file
            var projectDirectory = Path.Combine(Settings.WorkingDirectory, projectName);
            Console.WriteLine($"=> Building function {function.Function} [{targetFramework}, {buildConfiguration}]");

            // restore project dependencies
            if(!DotNetRestore(projectDirectory)) {
                AddError("`dotnet restore` command failed");
                return;
            }

            // compile project
            var dotnetOutputPackage = Path.Combine(Settings.OutputDirectory, function.Function + ".zip");
            if(!DotNetLambdaPackage(targetFramework, buildConfiguration, dotnetOutputPackage, projectDirectory)) {
                AddError("`dotnet lambda package` command failed");
                return;
            }

            // check if the project zip file was created
            if(!File.Exists(dotnetOutputPackage)) {
                AddError($"could not find project package: {dotnetOutputPackage}");
                return;
            }

            // decompress project zip into temporary folder so we can add the `GITSHAFILE` files
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try {

                // extract existing package into temp folder
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    ZipFile.ExtractToDirectory(dotnetOutputPackage, tempDirectory);
                    File.Delete(dotnetOutputPackage);
                } else {
                    Directory.CreateDirectory(tempDirectory);
                    if(!UnzipWithTool(dotnetOutputPackage, tempDirectory)) {
                        AddError("`unzip` command failed");
                        return;
                    }
                }
                function.PackagePath = CreatePackage(function.Function, gitsha, tempDirectory);
            } finally {
                if(Directory.Exists(tempDirectory)) {
                    try {
                        Directory.Delete(tempDirectory, recursive: true);
                    } catch {
                        Console.WriteLine($"WARNING: clean-up failed for temporary directory: {tempDirectory}");
                    }
                }
            }
        }

        private bool DotNetRestore(string projectDirectory) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                AddError("failed to find the \"dotnet\" executable in path.");
                return false;
            }
            return ProcessLauncher.Execute(
                dotNetExe,
                new[] { "restore" },
                projectDirectory,
                Settings.VerboseLevel >= VerboseLevel.Detailed
            );
        }

        private bool DotNetLambdaPackage(string targetFramework, string buildConfiguration, string outputPackagePath, string projectDirectory) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                AddError("failed to find the \"dotnet\" executable in path.");
                return false;
            }
            return ProcessLauncher.Execute(
                dotNetExe,
                new[] { "lambda", "package", "-c", buildConfiguration, "-f", targetFramework, "-o", outputPackagePath },
                projectDirectory,
                Settings.VerboseLevel >= VerboseLevel.Detailed
            );
        }

        private bool ZipWithTool(string zipArchivePath, string zipFolder) {
            var zipTool = ProcessLauncher.ZipExe;
            if(string.IsNullOrEmpty(zipTool)) {
                AddError("failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.");
                return false;
            }
            return ProcessLauncher.Execute(
                zipTool,
                new[] { "-r", zipArchivePath, "." },
                zipFolder,
                Settings.VerboseLevel >= VerboseLevel.Detailed
            );
        }

        private bool UnzipWithTool(string zipArchivePath, string unzipFolder) {
            var unzipTool = ProcessLauncher.UnzipExe;
            if(unzipTool == null) {
                AddError("failed to find the \"unzip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.");
                return false;
            }
            return ProcessLauncher.Execute(
                unzipTool,
                new[] { zipArchivePath, "-d", unzipFolder },
                Directory.GetCurrentDirectory(),
                Settings.VerboseLevel >= VerboseLevel.Detailed
            );
        }

        private void ProcessJavascript(
            ModuleNode module,
            FunctionNode function,
            VersionInfo version,
            bool skipCompile,
            bool skipAssemblyValidation,
            string gitsha,
            string buildConfiguration,
            string project
        ) {
            function.Language = "javascript";

            // check if we need to set a default handler
            if(function.Handler == null) {
                function.Handler = "index.handler";
            }

            // check if we need to set a default runtime
            if(function.Runtime == null) {
                function.Runtime = "nodejs8.10";
            }
            if(skipCompile) {
                function.PackagePath = $"{function.Function}-NOCOMPILE.zip";
                return;
            }
            Console.WriteLine($"=> Building function {function.Function} [{function.Runtime}]");
            function.PackagePath = CreatePackage(function.Function, gitsha, Path.GetDirectoryName(project));
        }

        private string CreatePackage(string functionName, string gitsha, string folder) {
            string package;

            // add `gitsha.txt` if GitSha is supplied
            if(gitsha != null) {
                File.WriteAllText(Path.Combine(folder, GITSHAFILE), gitsha);
            }

            // compress temp folder into new package
            var zipTempPackage = Path.GetTempFileName() + ".zip";
            if(File.Exists(zipTempPackage)) {
                File.Delete(zipTempPackage);
            }

            // compute MD5 hash for lambda function
            var files = new List<string>();
            using(var md5 = MD5.Create()) {
                var bytes = new List<byte>();
                files.AddRange(Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
                files.Sort();
                foreach(var file in files) {
                    var relativeFilePath = Path.GetRelativePath(folder, file);
                    var filename = Path.GetFileName(file);

                    // don't include the `gitsha.txt` since it changes with every build
                    if(filename != GITSHAFILE) {
                        using(var stream = File.OpenRead(file)) {
                            bytes.AddRange(Encoding.UTF8.GetBytes(relativeFilePath));
                            var fileHash = md5.ComputeHash(stream);
                            bytes.AddRange(fileHash);
                            if(Settings.VerboseLevel >= VerboseLevel.Detailed) {
                                Console.WriteLine($"... computing md5: {relativeFilePath} => {fileHash.ToHexString()}");
                            }
                        }
                    }
                }
                package = Path.Combine(Settings.OutputDirectory, $"function_{functionName}_{md5.ComputeHash(bytes.ToArray()).ToHexString()}.zip");
            }

            // compress folder contents
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                using(var zipArchive = ZipFile.Open(zipTempPackage, ZipArchiveMode.Create)) {
                    foreach(var file in files) {
                        var filename = Path.GetRelativePath(folder, file);
                        zipArchive.CreateEntryFromFile(file, filename);
                    }
                }
            } else {
                if(!ZipWithTool(zipTempPackage, folder)) {
                    AddError("`zip` command failed");
                    return null;
                }
            }
            if(gitsha != null) {
                try {
                    File.Delete(Path.Combine(folder, GITSHAFILE));
                } catch { }
            }
            if(!Directory.Exists(Settings.OutputDirectory)) {
                Directory.CreateDirectory(Settings.OutputDirectory);
            }
            File.Move(zipTempPackage, package);
            return package;
        }
    }
}