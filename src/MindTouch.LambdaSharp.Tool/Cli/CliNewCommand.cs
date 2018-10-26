/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliNewCommand : ACliCommand {

        //--- Class Methods ---
        private static string ReadResource(string resourceName, IDictionary<string, string> substitutions = null) {
            string result;
            var assembly = typeof(CliNewCommand).Assembly;
            using(var resource = assembly.GetManifestResourceStream($"MindTouch.LambdaSharp.Tool.Resources.{resourceName}"))
            using(var reader = new StreamReader(resource, Encoding.UTF8)) {
                result = reader.ReadToEnd();
            }
            if(substitutions != null) {
                foreach(var kv in substitutions) {
                    result = result.Replace($"%%{kv.Key}%%", kv.Value);
                }
            }
            return result;
        }

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("new", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create new LambdaSharp module or function";

                // function sub-command
                cmd.Command("function", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp function";

                    // sub-command options
                    var nameOption = subCmd.Option("--name|-n <NAME>", "Name of new function (e.g. MyFunction)", CommandOptionType.SingleValue);
                    var namespaceOption = subCmd.Option("--namespace|-ns <NAME>", "(optional) Root namespace for project (default: same as function name)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory|-wd <PATH>", "(optional) New function project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var frameworkOption = subCmd.Option("--framework|-f <NAME>", "(optional) Target .NET framework (default: 'netcoreapp2.1')", CommandOptionType.SingleValue);
                    var languageOption = subCmd.Option("--language|-l <LANGUAGE>", "(optional) Select programming language for generated code (either: csharp, javascript; default: csharp)", CommandOptionType.SingleValue);
                    var inputFileOption = cmd.Option("--input <FILE>", "(optional) File path to YAML module file (default: Module.yml)", CommandOptionType.SingleValue);
                    inputFileOption.ShowInHelpText = false;
                    var useProjectReferenceOption = subCmd.Option("--use-project-reference", "Reference LambdaSharp libraries using a project reference (default behavior when LAMBDASHARP environment variable is set)", CommandOptionType.NoValue);
                    var useNugetReferenceOption = subCmd.Option("--use-nuget-reference", "Reference LambdaSharp libraries using nuget references", CommandOptionType.NoValue);
                    var cmdArgument = subCmd.Argument("<NAME>", "Name of new project (e.g. MyFunction)");
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        var lambdasharpDirectory = Environment.GetEnvironmentVariable("LAMBDASHARP");

                        // validate project vs. nuget reference options
                        bool useProjectReference;
                        if(useProjectReferenceOption.HasValue() && useNugetReferenceOption.HasValue()) {
                            AddError("cannot use --use-project-reference and --use-nuget-reference at the same time");
                            return;
                        }
                        if(useProjectReferenceOption.HasValue()) {
                            if(lambdasharpDirectory == null) {
                                AddError("missing LAMBDASHARP environment variable");
                                return;
                            }
                            useProjectReference = true;
                        } else if(useNugetReferenceOption.HasValue()) {
                            useProjectReference = false;
                        } else if(lambdasharpDirectory != null) {
                            useProjectReference = true;
                        } else {
                            useProjectReference = false;
                        }

                        // TODO (2018-09-13, bjorg): allow following settings to be configurable via command line options
                        var functionMemory = 128;
                        var functionTimeout = 30;

                        // determine function name
                        if(cmdArgument.Values.Any() && nameOption.HasValue()) {
                            AddError("cannot specify --name and an argument at the same time");
                            return;
                        }
                        string functionName;
                        if(nameOption.HasValue()) {
                            functionName = nameOption.Value();
                        } else if(cmdArgument.Values.Any()) {
                            functionName = cmdArgument.Values.First();
                        } else {
                            AddError("missing function name argument");
                            return;
                        }
                        var workingDirectory = Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory());
                        NewFunction(
                            lambdasharpDirectory,
                            functionName,
                            namespaceOption.Value(),
                            frameworkOption.Value() ?? "netcoreapp2.1",
                            useProjectReference,
                            workingDirectory,
                            Path.Combine(workingDirectory, inputFileOption.Value() ?? "Module.yml"),
                            languageOption.Value() ?? "csharp",
                            functionMemory,
                            functionTimeout
                        );
                    });
                });

                // module sub-command
                cmd.Command("module", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp module";

                    // sub-command options
                    var nameOption = subCmd.Option("--name|-n <NAME>", "Name of new module (e.g. MyModule)", CommandOptionType.SingleValue);
                    nameOption.ShowInHelpText = false;
                    var directoryOption = subCmd.Option("--working-directory|-wd <PATH>", "(optional) New module directory (default: current directory)", CommandOptionType.SingleValue);
                    var cmdArgument = subCmd.Argument("<NAME>", "Name of new module (e.g. MyModule)");
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        if(cmdArgument.Values.Any() && nameOption.HasValue()) {
                            AddError("cannot specify --name and an argument at the same time");
                            return;
                        }
                        string moduleName;
                        if(nameOption.HasValue()) {
                            moduleName = nameOption.Value();
                        } else if(cmdArgument.Values.Any()) {
                            moduleName = cmdArgument.Values.First();
                        } else {
                            AddError("missing module name argument");
                            return;
                        }
                        NewModule(
                            moduleName,
                            Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory())
                        );
                    });
                });

                // tier sub-command
                cmd.Command("tier", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Setup LambdaSharp environment";

                    // setup options
                    var allowDataLossOption = subCmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                    var protectStackOption = subCmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                    var forceDeployOption = subCmd.Option("--force-deploy", "(optional) Force module deployment", CommandOptionType.NoValue);
                    var versionOption = subCmd.Option("--version", "(optional) Specify version for LambdaSharp modules (default: same as tool version)", CommandOptionType.SingleValue);
                    var localOption = subCmd.Option("--local", "(optional) Provide a path to a local check-out of the LambdaSharp bootstrap modules (default: LAMBDASHARP environment variable)", CommandOptionType.SingleValue);
                    var remoteOption = subCmd.Option("--no-local", "(optional) Force the setup command to use the published LambdaSharp bootstrap modules", CommandOptionType.NoValue);
                    var cmdArgument = subCmd.Argument("<NAME>", "Name of new project (e.g. MyFunction)");
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return;
                        }

                        // determine if we want to install modules from a local check-out
                        string localPath = null;
                        if(!remoteOption.HasValue()) {
                            if(localOption.HasValue()) {
                                localPath = localOption.Value();
                            } else {
                                var env = Environment.GetEnvironmentVariable("LAMBDASHARP");
                                if(env != null) {
                                    localPath = Path.Combine(env, "Bootstrap");
                                }
                            }
                        }
                        await NewTier(
                            settings,
                            allowDataLossOption.HasValue(),
                            protectStackOption.HasValue(),
                            forceDeployOption.HasValue(),
                            versionOption.HasValue() ? VersionInfo.Parse(versionOption.Value()) : Version,
                            localPath
                        );
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        public void NewModule(string moduleName, string moduleDirectory) {
            try {
                Directory.CreateDirectory(moduleDirectory);
            } catch(Exception e) {
                AddError($"unable to create directory '{moduleDirectory}'", e);
                return;
            }
            var moduleFile = Path.Combine(moduleDirectory, "Module.yml");
            if(File.Exists(moduleFile)) {
                AddError($"module file '{moduleFile}' already exists");
                return;
            }
            try {
                var module = ReadResource("NewModule.yml", new Dictionary<string, string> {
                    ["MODULENAME"] = moduleName
                });
                File.WriteAllText(moduleFile, module);
                Console.WriteLine($"Created module file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), moduleFile)}");
            } catch(Exception e) {
                AddError($"unable to create module file '{moduleFile}'", e);
            }
        }

        public void NewFunction(
            string lambdasharpDirectory,
            string functionName,
            string rootNamespace,
            string framework,
            bool useProjectReference,
            string workingDirectory,
            string moduleFile,
            string language,
            int functionMemory,
            int functionTimeout
        ) {

            // parse yaml module file
            if(!File.Exists(moduleFile)) {
                AddError($"could not find module '{moduleFile}'");
                return;
            }
            var moduleContents = File.ReadAllText(moduleFile);
            var module = new ModelParser(new Settings(), moduleFile).Parse(moduleContents);
            if(HasErrors) {
                return;
            }

            // set default namespace if none is set
            if(rootNamespace == null) {
                rootNamespace = $"{module.Module}.{functionName}";
            }

            // create directory for function project
            var projectDirectory = Path.Combine(workingDirectory, functionName);
            if(Directory.Exists(projectDirectory)) {
                AddError($"project directory '{projectDirectory}' already exists");
                return;
            }
            try {
                Directory.CreateDirectory(projectDirectory);
            } catch(Exception e) {
                AddError($"unable to create directory '{projectDirectory}'", e);
                return;
            }

            // create function file
            switch(language) {
            case "csharp":
                NewCSharpFunction(
                    lambdasharpDirectory,
                    functionName,
                    rootNamespace,
                    framework,
                    useProjectReference,
                    workingDirectory,
                    moduleFile,
                    functionMemory,
                    functionTimeout,
                    projectDirectory
                );
                break;
            case "javascript":
                NewJavascriptFunction(
                    lambdasharpDirectory,
                    functionName,
                    rootNamespace,
                    framework,
                    useProjectReference,
                    workingDirectory,
                    moduleFile,
                    functionMemory,
                    functionTimeout,
                    projectDirectory
                );
                break;
            }

            // update YAML module file
            var moduleLines = File.ReadAllLines(moduleFile).ToList();

            // check if `Functions:` section needs to be added
            var functionsIndex = moduleLines.FindIndex(line => line.StartsWith("Functions:", StringComparison.Ordinal));
            if(functionsIndex < 0) {

                // add empty separator line if the last line of the file is not empty
                if(moduleLines.Any() && (moduleLines.Last().Trim() != "")) {
                    moduleLines.Add("");
                }
                functionsIndex = moduleLines.Count;
                moduleLines.Add("Functions:");
            }
            ++functionsIndex;

            // insert function definition
            moduleLines.InsertRange(functionsIndex, new[] {
                "",
                $"  - Function: {functionName}",
                $"    Description: TODO - update {functionName} description",
                $"    Memory: {functionMemory}",
                $"    Timeout: {functionTimeout}",
            });
            File.WriteAllLines(moduleFile, moduleLines);
        }

        public void NewCSharpFunction(
            string lambdasharpDirectory,
            string functionName,
            string rootNamespace,
            string framework,
            bool useProjectReference,
            string workingDirectory,
            string moduleFile,
            int functionMemory,
            int functionTimeout,
            string projectDirectory
        ) {

            // create function project
            var projectFile = Path.Combine(projectDirectory, functionName + ".csproj");
            var substitutions = new Dictionary<string, string> {
                ["FRAMEWORK"] = framework,
                ["ROOTNAMESPACE"] = rootNamespace,
                ["LAMBDASHARP_PROJECT"] = Path.GetRelativePath(projectDirectory, Path.Combine(lambdasharpDirectory, "src", "MindTouch.LambdaSharp", "MindTouch.LambdaSharp.csproj")),
                ["LAMBDASHARP_VERSION"] = $"{Version.Major}.{Version.Minor}.*"
            };
            try {
                var projectContents = ReadResource(
                    useProjectReference
                        ? "NewCSharpFunctionProjectLocal.xml"
                        : "NewCSharpFunctionProjectNuget.xml",
                    substitutions
                );
                File.WriteAllText(projectFile, projectContents);
                Console.WriteLine($"Created project file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), projectFile)}");
            } catch(Exception e) {
                AddError($"unable to create project file '{projectFile}'", e);
                return;
            }

            // create function source code
            var functionFile = Path.Combine(projectDirectory, "Function.cs");
            var functionContents = ReadResource("NewCSharpFunction.txt", substitutions);
            try {
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                AddError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }

        public void NewJavascriptFunction(
            string lambdasharpDirectory,
            string functionName,
            string rootNamespace,
            string framework,
            bool useProjectReference,
            string workingDirectory,
            string moduleFile,
            int functionMemory,
            int functionTimeout,
            string projectDirectory
        ) {

            // create function source code
            var functionFile = Path.Combine(projectDirectory, "index.js");
            var functionContents = ReadResource("NewJSFunction.txt");
            try {
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                AddError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }

        public async Task NewTier(
            Settings settings,
            bool allowDataLoos,
            bool protectStack,
            bool forceDeploy,
            VersionInfo version,
            string localPath
        ) {
            Console.WriteLine($"Creating new deployment tier '{settings.Tier}'");
            foreach(var module in new[] {
                "LambdaSharp",
                "LambdaSharpRegistrar",
                "LambdaSharpS3Subscriber",
                "LambdaSharpS3PackageLoader"
            }) {
                var command = new CliBuildPublishDeployCommand();
                var moduleKey = $"{module}:{version}";

                // check if the module must be built and published first
                if(localPath != null) {
                    var moduleSource = Path.Combine(localPath, module, "Module.yml");
                    settings.WorkingDirectory = Path.GetDirectoryName(moduleSource);
                    settings.OutputDirectory = Path.Combine(settings.WorkingDirectory, "bin");

                    // build local module
                    if(!await command.BuildStepAsync(
                        settings,
                        Path.Combine(settings.OutputDirectory, "cloudformation.json"),
                        skipAssemblyValidation: true,
                        skipFunctionBuild: false,
                        gitsha: null,
                        buildConfiguration: "Release",
                        selector: null,
                        moduleSource: moduleSource
                    )) {
                        break;
                    }

                    // publish module
                    moduleKey = await command.PublishStepAsync(settings);
                    if(moduleKey == null) {
                        break;
                    }
                }

                // deploy published module
                if(!await command.DeployStepAsync(
                    settings,
                    dryRun: null,
                    moduleKey: moduleKey,
                    instanceName: null,
                    allowDataLoos: allowDataLoos,
                    protectStack: protectStack,
                    inputs: new Dictionary<string, string>(),
                    forceDeploy: forceDeploy
                )) {
                    break;
                }
            }
        }
    }
}
