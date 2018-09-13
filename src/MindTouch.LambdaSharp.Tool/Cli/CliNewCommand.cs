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
using System.Text;
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

                // module sub-command
                cmd.Command("module", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp module";

                    // sub-command options
                    var nameOption = subCmd.Option("--name|-n <VALUE>", "Name of new module (e.g. MyModule)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory|-wd <VALUE>", "(optional) New module directory (default: current directory)", CommandOptionType.SingleValue);
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        if(!nameOption.HasValue()) {
                            AddError("missing module '--name' option");
                            return;
                        }
                        NewModule(
                            nameOption.Value(),
                            Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory())
                        );
                    });
                });

                // function sub-command
                cmd.Command("function", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp function";

                    // sub-command options
                    var nameOption = subCmd.Option("--name|-n <VALUE>", "Name of new project with module name prefix (e.g. Module.Function)", CommandOptionType.SingleValue);
                    var namespaceOption = subCmd.Option("--namespace|-ns <VALUE>", "(optional) Root namespace for project (default: same as function name)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory|-wd <VALUE>", "(optional) New function project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var frameworkOption = subCmd.Option("--framework|-f <VALUE>", "(optional) Target .NET framework (default: 'netcoreapp2.1')", CommandOptionType.SingleValue);
                    var useProjectReferenceOption = subCmd.Option("--use-project-reference", "Reference LambdaSharp libraries using project references (default: use nuget package reference)", CommandOptionType.NoValue);
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        var lambdasharpDirectory = Environment.GetEnvironmentVariable("LAMBDASHARP");
                        if(useProjectReferenceOption.HasValue() && (lambdasharpDirectory == null)) {
                            AddError("missing LAMBDASHARP environment variable");
                            return;
                        }
                        if(!nameOption.HasValue()) {
                            AddError("missing project '--name' option");
                            return;
                        }
                        NewFunction(
                            lambdasharpDirectory,
                            nameOption.Value(),
                            namespaceOption.Value() ?? nameOption.Value(),
                            frameworkOption.Value() ?? "netcoreapp2.1",
                            useProjectReferenceOption.HasValue(),
                            Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory())
                        );
                    });
                });
                cmd.OnExecute(() => {
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        private void NewModule(string moduleName, string moduleDirectory) {
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

        private void NewFunction(
            string lambdasharpDirectory,
            string functionName,
            string rootNamespace,
            string framework,
            bool useProjectReference,
            string baseDirectory
        ) {

            // create directory for function project
            var projectDirectory = Path.Combine(baseDirectory, functionName);
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

            // create function project
            var projectFile = Path.Combine(projectDirectory, functionName + ".csproj");
            var substitutions = new Dictionary<string, string> {
                ["FRAMEWORK"] = framework,
                ["ROOTNAMESPACE"] = rootNamespace,
                ["LAMBDASHARPPROJECT"] = Path.GetRelativePath(projectDirectory, Path.Combine(lambdasharpDirectory, "src", "MindTouch.LambdaSharp", "MindTouch.LambdaSharp.csproj")),
                ["LAMBDASHARPVERSION"] = Version.ToString()
            };
            try {
                var projectContents = ReadResource(
                    useProjectReference
                        ? "NewFunctionProjectLocal.xml"
                        : "NewFunctionProjectNuget.xml",
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
            try {
                var functionContents = ReadResource("NewFunction.cs.txt", substitutions);
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                AddError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }
    }
}
