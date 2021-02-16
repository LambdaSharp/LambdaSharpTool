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
using LambdaSharp.Build.CSharp;
using LambdaSharp.Modules;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Build {
    public class ModelAppPackager: AModelProcessor {

        //--- Types ---
        private class AppBuilderDependencyProvider : IAppBuilderDependencyProvider {

            //--- Fields ---
            private readonly ModuleBuilder _builder;
            private readonly Settings _settings;
            private readonly HashSet<string> _existingPackages;

            //--- Constructors ---
            public AppBuilderDependencyProvider(ModuleBuilder builder, Settings settings, HashSet<string> existingPackages) {
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
            public void WriteLine(string message) => Console.WriteLine(message);
        }

        //--- Fields ---
        private ModuleBuilder _builder;
        private HashSet<string> _existingPackages;

        //--- Constructors ---
        public ModelAppPackager(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

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
                    foreach(var file in Directory.GetFiles(Settings.OutputDirectory, "app*.*")) {
                        try {
                            File.Delete(file);
                        } catch { }
                    }
                }
                return;
            }

            // check if there are any functions to package
            var apps = builder.Items.OfType<AppItem>();
            if(!apps.Any()) {
                return;
            }

            // collect list of previously built functions
            if(!Directory.Exists(Settings.OutputDirectory)) {
                Directory.CreateDirectory(Settings.OutputDirectory);
            }
            _existingPackages = new HashSet<string>(Directory.GetFiles(Settings.OutputDirectory, "app*.*"));

            // build each app
            foreach(var app in apps) {
                AtLocation(app.FullName, () => {
                    switch(Path.GetExtension(app.Project).ToLowerInvariant()) {
                    case ".csproj":
                        new AppBuilder(
                            new AppBuilderDependencyProvider(_builder, Settings, _existingPackages),
                            BuildEventsConfig
                        ).Build(
                            app,
                            noCompile,
                            noAssemblyValidation,
                            gitSha,
                            gitBranch,
                            buildConfiguration,
                            forceBuild,
                            out var appPlatform,
                            out var appFramework,
                            out var appVersionId
                        );

                        // set app properties
                        _builder.GetItem($"{app.FullName}::AppPlatform").Reference = appPlatform;
                        _builder.GetItem($"{app.FullName}::AppFramework").Reference = appFramework;
                        _builder.GetItem($"{app.FullName}::AppLanguage").Reference = "csharp";
                        _builder.GetItem($"{app.FullName}::VersionId").Reference = appVersionId;
                        break;
                    default:
                        LogError("could not determine the app language");
                        return;
                    }
                });
            }

            // delete remaining built functions, they are out-of-date
            foreach(var leftoverPackage in _existingPackages) {
                try {
                    File.Delete(leftoverPackage);
                } catch { }
            }
        }
    }
}