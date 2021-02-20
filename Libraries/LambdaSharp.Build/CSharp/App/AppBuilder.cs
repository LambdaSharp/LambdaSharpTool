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
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using LambdaSharp.Build.CSharp.Internal;
using LambdaSharp.Build.Internal;
using LambdaSharp.Modules;

namespace LambdaSharp.Build.CSharp {

    public interface IAppBuilderDependencyProvider {

        //--- Properties ---
        string ModuleFullName { get; }
        string WorkingDirectory { get; }
        string OutputDirectory { get; }
        string InfoColor  { get; }
        string WarningColor { get; }
        string ErrorColor { get; }
        string ResetColor { get; }
        string Lash { get; }
        HashSet<string> ExistingPackages { get; }
        bool DetailedOutput { get; }
        VersionInfo ToolVersion { get; }

        //--- Methods ---
        void WriteLine(string message);
        void AddArtifact(string fullName, string artifact);
    }

    public class AppBuilder : ABuildEventsSource {

        //--- Constants ---
        public const string AppSettingsJsonFileName = "appsettings.json";

        // NOTE (2020-08-09, bjorg): "appsettings.Production.json" is automatically loaded on boot
        //  by the Blazor application and injected into the app's configuration.
        public const string AppSettingsProductionJsonFileName = "appsettings.Production.json";

        // NOTE (2020-08-09, bjorg): "appsettings.Production.json" is automatically loaded on boot
        //  by the Blazor application and injected into the app's configuration.
        public const string AppSettingsDevelopmentJsonFileName = "appsettings.Development.json";

        //--- Types ---
        private class ForwardSlashEncoder : UTF8Encoding {

            //--- Constructors ---
            public ForwardSlashEncoder() : base(true) { }

            //--- Methods ---
            public override byte[] GetBytes(string text)
                => base.GetBytes(text.Replace(@"\", "/"));
        }

        //--- Constructors ---
        public AppBuilder(IAppBuilderDependencyProvider settings, BuildEventsConfig? buildEventsConfig = null) : base(buildEventsConfig) {
            Provider = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        //--- Properties ---
        public IAppBuilderDependencyProvider Provider { get; }

        //--- Methods ---
        public void Build(
            IApp app,
            bool noCompile,
            bool noAssemblyValidation,
            string? gitSha,
            string? gitBranch,
            string buildConfiguration,
            bool forceBuild,
            out string platform,
            out string framework,
            out string appVersionId
        ) {

            // read settings from project file
            var projectFile = new CSharpProjectFile(app.Project);
            var targetFramework = projectFile.TargetFramework;

            // set output parameters
            platform = "Blazor WebAssembly";
            framework = targetFramework;

            // check if any app files are newer than the most recently built package; otherwise, skip build
            var appMetadataFilepath = Path.Combine(Provider.OutputDirectory, $"appmetadata_{Provider.ModuleFullName}_{app.LogicalId}.json");
            if(!forceBuild) {
                var appPackage = Provider.ExistingPackages.FirstOrDefault(p =>
                    Path.GetFileName(p).StartsWith($"app_{Provider.ModuleFullName}_{app.LogicalId}_", StringComparison.Ordinal)
                    && p.EndsWith(".zip", StringComparison.Ordinal)
                );
                if((appPackage != null) && File.Exists(appMetadataFilepath)) {
                    LogInfoVerbose($"=> Analyzing app {app.FullName} dependencies");

                    // find all files used to create the app package
                    var files = new HashSet<string>();
                    CSharpProjectFile.DiscoverDependencies(
                        files,
                        app.Project,
                        filePath => LogInfoVerbose($"... analyzing {filePath}"),
                        (message, exception) => LogError(message, exception)
                    );

                    // check if any of the files has been modified more recently than the app package
                    var appPackageDate = File.GetLastWriteTime(appPackage);
                    var file = files.FirstOrDefault(f => File.GetLastWriteTime(f) > appPackageDate);
                    if(file == null) {
                        try {

                            // attempt to load extract assembly metadata
                            var appMetadata = JsonSerializer.Deserialize<LambdaSharpTool.AssemblyMetadata>(File.ReadAllText(appMetadataFilepath));
                            if(appMetadata.ModuleVersionId != null) {
                                Provider.WriteLine($"=> Skipping app {Provider.InfoColor}{app.FullName}{Provider.ResetColor} (no changes found)");

                                // keep the existing files
                                Provider.ExistingPackages.Remove(appMetadataFilepath);
                                Provider.ExistingPackages.Remove(appPackage);

                                // set the module variable to the final package name
                                appVersionId = appMetadata.ModuleVersionId;
                                Provider.AddArtifact($"{app.FullName}::PackageName", appPackage);
                                return;
                            }
                        } catch {

                            // ignore exception and continue
                        }
                    } else {
                        LogInfoVerbose($"... found newer file: {file}");
                    }
                }
            } else {
                LogInfoVerbose($"=> Analyzing app {app.FullName} dependencies");

                // find all files used to create the app package
                var files = new HashSet<string>();
                CSharpProjectFile.DiscoverDependencies(
                    files,
                    app.Project,
                    filePath => LogInfoVerbose($"... analyzing {filePath}"),
                    (message, exception) => LogError(message, exception)
                );

                // loop over all project folders
                new CleanBuildFolders(BuildEventsConfig).Do(files);
            }

            // validate the project is using the most recent lambdasharp assembly references
            if(
                !noAssemblyValidation &&
                app.HasAssemblyValidation &&
                !projectFile.ValidateLambdaSharpPackageReferences(Provider.ToolVersion, LogWarn, LogError)
            ) {
                appVersionId = "<MISSING>";
                return;
            }
            if(noCompile) {
                appVersionId = "<MISSING>";
                return;
            }

            // compile app project
            Provider.WriteLine($"=> Building app {Provider.InfoColor}{app.FullName}{Provider.ResetColor} [{projectFile.TargetFramework}, {buildConfiguration}]");
            var projectDirectory = Path.Combine(Provider.WorkingDirectory, Path.GetFileNameWithoutExtension(app.Project));
            if(File.Exists(appMetadataFilepath)) {
                File.Delete(appMetadataFilepath);
            }

            // build and publish Blazor app
            if(!DotNetBuildBlazor(projectFile.TargetFramework, buildConfiguration, projectDirectory)) {

                // nothing to do; error was already reported
                appVersionId = "<MISSING>";
                return;
            }

            // extract version id from app
            var assemblyFilepath = (string.Compare(targetFramework, "netstandard2.1", StringComparison.Ordinal) == 0)
                ? Path.Combine(projectDirectory, "bin", buildConfiguration, targetFramework, "publish", "wwwroot", "_framework", "_bin", $"{Path.GetFileNameWithoutExtension(app.Project)}.dll")
                : Path.Combine(projectDirectory, "bin", buildConfiguration, targetFramework, "publish", "wwwroot", "_framework", $"{Path.GetFileNameWithoutExtension(app.Project)}.dll");
            var assemblyMetadata = LambdaSharpAppAssemblyInformation(assemblyFilepath, appMetadataFilepath);
            if(assemblyMetadata?.ModuleVersionId == null) {
                LogError($"unable to extract assembly metadata");
                appVersionId = "<MISSING>";
                return;
            }
            Provider.ExistingPackages.Remove(appMetadataFilepath);
            LogInfoVerbose($"... assembly version id: {assemblyMetadata.ModuleVersionId}");
            appVersionId = assemblyMetadata.ModuleVersionId;

            // update `blazor.boot.json` file by adding `appsettings.Production.json` to config list
            var wwwRootFolder = Path.Combine(projectDirectory, "bin", buildConfiguration, targetFramework, "publish", "wwwroot");
            if(File.Exists(Path.Combine(wwwRootFolder, AppSettingsProductionJsonFileName))) {
                LogError($"'{AppSettingsProductionJsonFileName}' is reserved for loading deployment generated configuration settings and cannot be used explicitly");
                return;
            }

            var blazorBootJsonFileName = Path.Combine(wwwRootFolder, "_framework", "blazor.boot.json");
            var blazorBootJson = JsonToNativeConverter.ParseObject(File.ReadAllText(blazorBootJsonFileName));
            if(
                (blazorBootJson != null)
                && (blazorBootJson.TryGetValue("config", out var blazorBootJsonConfig))
                && (blazorBootJsonConfig is List<object?> blazorBootJsonConfigList)
            ) {
                var blazorBootJsonModified = false;
                if(!blazorBootJsonConfigList.Contains(AppSettingsJsonFileName) && ((gitSha != null) || (gitBranch != null))) {

                    // add instruction to load appsettings.json
                    blazorBootJsonConfigList.Add(AppSettingsJsonFileName);
                    blazorBootJsonModified = true;
                }
                if(!blazorBootJsonConfigList.Contains(AppSettingsProductionJsonFileName)) {

                    // add instruction to load appsettings.Production.json
                    blazorBootJsonConfigList.Add(AppSettingsProductionJsonFileName);
                    blazorBootJsonModified = true;
                }
                if(blazorBootJsonModified) {
                    LogInfoVerbose("... updating 'blazor.boot.json' configuration file");
                    File.WriteAllText(blazorBootJsonFileName, JsonSerializer.Serialize(blazorBootJson));
                }
            } else {
                LogError($"unable to update {blazorBootJsonFileName}");
            }

            // zip output folder
            BuildPackage(app, gitSha, gitBranch, wwwRootFolder);
        }

        private bool DotNetBuildBlazor(
            string targetFramework,
            string buildConfiguration,
            string projectDirectory
        ) {
            var dotNetExe = ProcessLauncher.DotNetExe;
            if(string.IsNullOrEmpty(dotNetExe)) {
                LogError("failed to find the \"dotnet\" executable in path.");
                return false;
            }
            if(!new ProcessLauncher(BuildEventsConfig).Execute(
                dotNetExe,
                new[] {
                    "publish",
                    "--configuration", buildConfiguration,
                    "--framework", targetFramework,
                    "-p:BlazorEnableCompression=false"
                },
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

        private void BuildPackage(IApp app, string? gitSha, string? gitBranch, string folder) {

            // discover files to package
            var files = new List<KeyValuePair<string, string>>();
            if(Directory.Exists(folder)) {
                foreach(var filePath in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) {
                    var relativeFilePathName = Path.GetRelativePath(folder, filePath);

                    // NOTE (2020-10-18, bjorg): skip 'appsettings.Development.json' file; since it's only useful for running locally and might contain sensitive information
                    if(relativeFilePathName == AppSettingsDevelopmentJsonFileName) {
                        continue;
                    }
                    files.Add(new KeyValuePair<string, string>(relativeFilePathName, filePath));
                }
                files = files.OrderBy(file => file.Key).ToList();
            } else {
                LogError($"cannot find folder '{folder}'");
                return;
            }

            // compute hash for all files
            var fileValueToFileKey = files.ToDictionary(kv => kv.Value, kv => kv.Key);
            var hash = files.Select(kv => kv.Value).ComputeHashForFiles(file => fileValueToFileKey[file]);
            var package = Path.Combine(Provider.OutputDirectory, $"app_{Provider.ModuleFullName}_{app.LogicalId}_{hash}.zip");

            // only build package if it doesn't exist
            if(!Provider.ExistingPackages.Remove(package)) {

                // check appsettings.json file exists
                var appSettingsFilepath = Path.Combine(folder, AppSettingsJsonFileName);
                var appSettingsExists = File.Exists(appSettingsFilepath);
                if(!appSettingsExists && ((gitSha != null) || (gitBranch != null))) {

                    // add fake entry into list of files
                    files.Add(new KeyValuePair<string, string>(AppSettingsJsonFileName, appSettingsFilepath));
                }

                // package contents with built-in zip library
                using(var zipArchive = System.IO.Compression.ZipFile.Open(package, ZipArchiveMode.Create, new ForwardSlashEncoder())) {
                    foreach(var file in files) {
                        if(Provider.DetailedOutput) {
                            Console.WriteLine($"... zipping: {file.Key}");
                        }

                        // check if appsettings.json is being processed
                        if(file.Key == AppSettingsJsonFileName) {
                            var zipEntry = zipArchive.CreateEntry(file.Key);

                            // check if appsettings.json exists
                            string appSettingsText;
                            if(appSettingsExists) {

                                // read original file and attempt to augment it
                                appSettingsText = File.ReadAllText(file.Value);
                                var appSettings = JsonToNativeConverter.ParseObject(appSettingsText);
                                if(appSettings != null) {
                                    AddLambdaSharpSettings(appSettings);
                                    appSettingsText = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions {
                                        IgnoreNullValues = true,
                                        WriteIndented = false
                                    });
                                }
                            } else {

                                // create default appsettings.json file
                                var appSettings = new Dictionary<string, object?>();
                                AddLambdaSharpSettings(appSettings);
                                appSettingsText = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions {
                                    IgnoreNullValues = true,
                                    WriteIndented = false
                                });
                            }
                            using(var zipStream = zipEntry.Open()) {
                                zipStream.Write(Encoding.UTF8.GetBytes(appSettingsText));
                            }
                        } else {
                            zipArchive.CreateEntryFromFile(file.Value, file.Key);
                        }
                    }
                }
            } else {

                // update last write time on file
                File.SetLastWriteTimeUtc(package, DateTime.UtcNow);
            }
            Provider.AddArtifact($"{app.FullName}::PackageName", package);

            // local functions
            void AddLambdaSharpSettings(Dictionary<string, object?> appSettings) {
                var lambdaSharpSettings = new Dictionary<string, object?>();
                if(gitSha != null) {
                    lambdaSharpSettings["GitSha"] = gitSha;
                }
                if(gitBranch != null) {
                    lambdaSharpSettings["GitBranch"] = gitBranch;
                }
                appSettings["LambdaSharp"] = lambdaSharpSettings;
            }
        }

        private LambdaSharpTool.AssemblyMetadata? LambdaSharpAppAssemblyInformation(string assemblyFilepath, string appMetadataFilepath) {

            // execute `lash util extract-assembly-metadata` command
            var success = new LambdaSharpTool(BuildEventsConfig).RunLashTool(
                Provider.WorkingDirectory,
                new[] {
                    "util", "extract-assembly-metadata",
                    "--assembly", assemblyFilepath,
                    "--out", appMetadataFilepath,
                    "--quiet",
                    "--no-ansi"
                },
                Provider.DetailedOutput
            );
            if(!success) {
                return null;
            }
            return JsonSerializer.Deserialize<LambdaSharpTool.AssemblyMetadata>(File.ReadAllText(appMetadataFilepath));
        }
    }
}