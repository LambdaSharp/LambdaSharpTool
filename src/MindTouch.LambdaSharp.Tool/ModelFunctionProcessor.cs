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
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MindTouch.LambdaSharp.Tool.Model;
using MindTouch.LambdaSharp.Tool.Model.AST;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MindTouch.LambdaSharp.Tool.Internal;
using System.Text;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelFunctionProcessor : AModelProcessor {

        //--- Constants ---
        private const string GITSHAFILE = "gitsha.txt";

        //--- Constructors ---
        public ModelFunctionProcessor(Settings settings) : base(settings) { }

        public void Process(ModuleNode module, bool skipCompile) {
            var index = 0;
            foreach(var function in module.Functions) {
                AtLocation(function.Name ?? $"[{index}]", () => {
                    Validate(function.Name != null, "missing Name field");
                    Process(module, function, skipCompile);
                });
                ++index;
            }
        }

        private void Process(ModuleNode module, FunctionNode function, bool skipCompile) {

            // compile function project
            AtLocation("Project", () => {
                var projectName = function.Project ?? $"{module.Name}.{function.Name}";
                var project = Path.Combine(Settings.WorkingDirectory, projectName, projectName + ".csproj");
                string targetFramework;

                // check if csproj file exists in project folder
                if(!File.Exists(project)) {
                    AddError($"could not find function project: {project}");
                    return;
                }

                // check if the handler/runtime were provided or if they need to be extracted from the project file
                var csproj = XDocument.Load(project);
                var mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");

                // make sure the .csproj file contains the lambda tooling
                var hasAwsLambdaTools = csproj.Element("Project")
                    ?.Elements("ItemGroup")
                    .Any(el => (string)el.Element("DotNetCliToolReference")?.Attribute("Include") == "Amazon.Lambda.Tools") ?? false;
                if(!hasAwsLambdaTools) {
                    AddError($"the project is missing the AWS lambda tool defintion; make sure that {project} includes <DotNetCliToolReference Include=\"Amazon.Lambda.Tools\"/>");
                    return;
                }

                // check if we need to read the project file <RootNamespace> element to determine the handler name
                if(function.Handler == null) {
                    var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
                    if(rootNamespace != null) {
                        function.Handler = $"{projectName}::{rootNamespace}.Function::FunctionHandlerAsync";
                    } else {
                        AddError("could not auto-determine handler; either add Function field or <RootNamespace> to project file");
                    }
                }

                // check if we need to parse the <TargetFramework> element to determine the lambda runtime
                targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
                if(function.Runtime == null) {
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
                }
                if(skipCompile) {
                    function.Package = $"{projectName}-NOCOMPILE.zip";
                    return;
                }

                // dotnet tools have to be run from the project folder; otherwise specialized tooling is not picked up from the .csproj file
                var projectDirectory = Path.Combine(Settings.WorkingDirectory, projectName);
                foreach(var file in Directory.GetFiles(Settings.WorkingDirectory, $"{projectName}-*.zip")) {
                    try {
                        File.Delete(file);
                    } catch { }
                }
                var buildConfiguration = Settings.BuildConfiguration;
                Console.WriteLine($"Building function {projectName} [{targetFramework}, {buildConfiguration}]");

                // restore project dependencies
                Console.WriteLine("=> Restoring project dependencies");
                if(!DotNetRestore(projectDirectory)) {
                    AddError("`dotnet restore` command failed");
                    return;
                }

                // compile project
                Console.WriteLine("=> Building AWS Lambda package");
                if(!DotNetLambdaPackage(targetFramework, buildConfiguration, projectName, projectDirectory)) {
                    AddError("`dotnet lambda package` command failed");
                    return;
                }

                // check if the project zip file was created
                var zipOriginalPackage = Path.Combine(Settings.WorkingDirectory, projectName, projectName + ".zip");
                if(!File.Exists(zipOriginalPackage)) {
                    AddError($"could not find project package: {zipOriginalPackage}");
                    return;
                }

                // decompress project zip into temporary folder so we can add the `parameters.json` and `GITSHAFILE` files
                string package;
                var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                try {

                    // extract existing package into temp folder
                    Console.WriteLine("=> Decompressing AWS Lambda package");
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        ZipFile.ExtractToDirectory(zipOriginalPackage, tempDirectory);
                        File.Delete(zipOriginalPackage);
                    } else {
                        Directory.CreateDirectory(tempDirectory);
                        if(!UnzipWithTool(zipOriginalPackage, tempDirectory)) {
                            AddError("`unzip` command failed");
                            return;
                        }
                    }

                    // add `gitsha.txt` if GitSha is supplied
                    if(Settings.GitSha != null) {
                        File.WriteAllText(Path.Combine(tempDirectory, GITSHAFILE), Settings.GitSha);
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
                        files.AddRange(Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories));
                        files.Sort();
                        foreach(var file in files) {
                            var relativeFilePath = Path.GetRelativePath(tempDirectory, file);
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
                        package = Path.Combine(Settings.WorkingDirectory, $"{projectName}-{md5.ComputeHash(bytes.ToArray()).ToHexString()}.zip");
                    }

                    // compress folder contents
                    Console.WriteLine("=> Finalizing AWS Lambda package");
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        using(var zipArchive = ZipFile.Open(zipTempPackage, ZipArchiveMode.Create)) {
                            foreach(var file in files) {

                                // TODO (2018-07-24, bjorg): I doubt this works correctly for files in subfolders
                                var filename = Path.GetFileName(file);
                                zipArchive.CreateEntryFromFile(file, filename);
                            }
                        }
                    } else {
                        if(!ZipWithTool(zipTempPackage, tempDirectory)) {
                            AddError("`zip` command failed");
                            return;
                        }
                    }
                    File.Move(zipTempPackage, package);
                } finally {
                    if(Directory.Exists(tempDirectory)) {
                        try {
                            Directory.Delete(tempDirectory, recursive: true);
                        } catch {
                            Console.WriteLine($"WARNING: clean-up failed for temporary directory: {tempDirectory}");
                        }
                    }
                }
                function.Package = package;
            });
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

        private bool DotNetLambdaPackage(string targetFramework, string buildConfiguration, string projectName, string projectDirectory) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                AddError("failed to find the \"dotnet\" executable in path.");
                return false;
            }
            return ProcessLauncher.Execute(
                dotNetExe,
                new[] { "lambda", "package", "-c", buildConfiguration, "-f", targetFramework, "-o", projectName + ".zip" },
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
    }
}