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
using System.Linq;
using System.IO;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using LambdaSharp.Build;
using LambdaSharp.Modules;
using LambdaSharp.Build.CSharp.Function;

namespace LambdaSharp.Tool.Cli.Build {

    public class ModelFunctionPackager : AModelProcessor {

        //--- Constants ---
        private const string GIT_INFO_FILE = "git-info.json";

        //--- Types ---
        private class FunctionBuilderDependencyProvider : IFunctionBuilderDependencyProvider {

            //--- Fields ---
            private readonly ModuleBuilder _builder;
            private readonly Settings _settings;
            private readonly HashSet<string> _existingPackages;

            //--- Constructors ---
            public FunctionBuilderDependencyProvider(ModuleBuilder builder, Settings settings, HashSet<string> existingPackages) {
                _builder = builder ?? throw new ArgumentNullException(nameof(builder));
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
                _existingPackages = existingPackages ?? throw new ArgumentNullException(nameof(existingPackages));
            }

            //--- Properties ---
            public string ModuleFullName => _builder.FullName;
            public string WorkingDirectory => _settings.WorkingDirectory;
            public string OutputDirectory => _settings.OutputDirectory;
            public string InfoColor => Settings.InfoColor;
            public string WarningColor => Settings.WarningColor;
            public string ErrorColor => Settings.ErrorColor;
            public string ResetColor => Settings.ResetColor;
            public string Lash => Settings.Lash;
            public HashSet<string> ExistingPackages => _existingPackages;
            public bool DetailedOutput => Settings.VerboseLevel >= VerboseLevel.Detailed;
            public VersionInfo ToolVersion => _settings.ToolVersion;

            //--- Methods ---
            public void AddArtifact(string fullName, string artifact) => _builder.AddArtifact(fullName, artifact);
            public bool IsAmazonLinux2() => Settings.IsAmazonLinux2();
            public void WriteLine(string message) => Console.WriteLine(message);
        }

        //--- Fields ---
        private ModuleBuilder _builder;
        private HashSet<string> _existingPackages;

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

                // set function language
                function.Language = "csharp";

                // build C# function
                new FunctionBuilder(
                    new FunctionBuilderDependencyProvider(_builder, Settings, _existingPackages),
                    BuildEventsConfig
                ).Build(
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
            default:
                LogError("could not determine the function language");
                return;
            }
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
            Console.WriteLine($"=> Building function {Settings.InfoColor}{function.Name}{Settings.ResetColor} [{function.Function.Runtime}]");
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
            if(!new ZipTool(BuildEventsConfig).ZipData(zipTempPackage, folder, showOutput: Settings.VerboseLevel >= VerboseLevel.Detailed)) {
                return;
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
    }
}