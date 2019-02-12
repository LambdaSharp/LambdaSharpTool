/*
 * MindTouch λ#
 * Copyright (C) 2006-2018-2019 MindTouch, Inc.
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
using LambdaSharp.Tool.Cli.Build;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli {

    public class CliNewCommand : ACliCommand {

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("new", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create new LambdaSharp module, function, or resource";

                // function sub-command
                cmd.Command("function", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp function";

                    // sub-command options
                    var namespaceOption = subCmd.Option("--namespace <NAME>", "(optional) Root namespace for project (default: same as function name)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory <PATH>", "(optional) New function project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var frameworkOption = subCmd.Option("--framework|-f <NAME>", "(optional) Target .NET framework (default: 'netcoreapp2.1')", CommandOptionType.SingleValue);
                    var languageOption = subCmd.Option("--language|-l <LANGUAGE>", "(optional) Select programming language for generated code (default: csharp)", CommandOptionType.SingleValue);
                    var inputFileOption = cmd.Option("--input <FILE>", "(optional) File path to YAML module definition (default: Module.yml)", CommandOptionType.SingleValue);
                    inputFileOption.ShowInHelpText = false;
                    var useProjectReferenceOption = subCmd.Option("--use-project-reference", "(optional) Reference LambdaSharp libraries using a project reference (default behavior when LAMBDASHARP environment variable is set)", CommandOptionType.NoValue);
                    var useNugetReferenceOption = subCmd.Option("--use-nuget-reference", "(optional) Reference LambdaSharp libraries using nuget references", CommandOptionType.NoValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new project (e.g. MyFunction)");
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
                        var functionMemory = 256;
                        var functionTimeout = 30;

                        // determine function name
                        string functionName;
                        if(nameArgument.Values.Any()) {
                            functionName = nameArgument.Value;
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
                    var nameOption = subCmd.Option("--name|-n <NAME>", "Name of new module (e.g. My.NewModule)", CommandOptionType.SingleValue);
                    nameOption.ShowInHelpText = false;
                    var directoryOption = subCmd.Option("--working-directory <PATH>", "(optional) New module directory (default: current directory)", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new module (e.g. My.NewModule)");
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        if(nameArgument.Values.Any() && nameOption.HasValue()) {
                            AddError("cannot specify --name and an argument at the same time");
                            return;
                        }
                        string moduleName;
                        if(nameOption.HasValue()) {
                            moduleName = nameOption.Value();
                        } else if(nameArgument.Values.Any()) {
                            moduleName = nameArgument.Value;
                        } else {
                            AddError("missing module name argument");
                            return;
                        }

                        // prepend default owner string
                        if(!moduleName.Contains('.')) {
                            moduleName = "My." + moduleName;
                        }
                        NewModule(
                            moduleName,
                            Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory())
                        );
                    });
                });

                // resource sub-command
                cmd.Command("resource", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp resource definition";
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new resource (e.g. MyResource)");
                    var typeArgument = subCmd.Argument("<TYPE>", "AWS resource type (e.g. AWS::SNS::Topic)");

                    // sub-command options
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        if(!nameArgument.Values.Any()) {
                            AddError("missing resource name");
                            return;
                        }
                        if(!typeArgument.Values.Any()) {
                            AddError("missing resource type");
                            return;
                        }
                        NewResource(
                            moduleFile: Path.Combine(Directory.GetCurrentDirectory(), "Module.yml"),
                            resourceName: nameArgument.Value,
                            resourceTypeName: typeArgument.Value
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
                AddError($"module definition '{moduleFile}' already exists");
                return;
            }
            try {
                var module = ReadResource("NewModule.yml", new Dictionary<string, string> {
                    ["MODULENAME"] = moduleName
                });
                File.WriteAllText(moduleFile, module);
                Console.WriteLine($"Created module definition: {Path.GetRelativePath(Directory.GetCurrentDirectory(), moduleFile)}");
            } catch(Exception e) {
                AddError($"unable to create module definition '{moduleFile}'", e);
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

            // parse yaml module definition
            if(!File.Exists(moduleFile)) {
                AddError($"could not find module '{moduleFile}'");
                return;
            }
            var moduleContents = File.ReadAllText(moduleFile);
            var module = new ModelYamlToAstConverter(new Settings(), moduleFile).Parse(moduleContents);
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

            // insert function definition
            InsertModuleItemsLines(moduleFile, new[] {
                $"  - Function: {functionName}",
                $"    Description: TODO - update function description",
                $"    Memory: {functionMemory}",
                $"    Timeout: {functionTimeout}"
            });
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
                ["LAMBDASHARP_PROJECT"] = useProjectReference
                    ? Path.GetRelativePath(projectDirectory, Path.Combine(lambdasharpDirectory, "src", "LambdaSharp", "LambdaSharp.csproj"))
                    : "(not used)",
                ["LAMBDASHARP_VERSION"] = Version.GetWildcardVersion()
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

        public void NewResource(string moduleFile, string resourceName, string resourceTypeName) {
            if(!ResourceMapping.CloudformationSpec.ResourceTypes.TryGetValue(resourceTypeName, out var resourceType)) {
                AddError($"unknown resource type '{resourceTypeName}'");
                return;
            }

            // create resource definition
            var types = new HashSet<string>();
            var lines = new List<string>();
            lines.Add($"  - Resource: {resourceName}");
            lines.Add($"    Description: TODO - update resource description");
            lines.Add($"    # Scope: List of functions to be given the name of the resource");
            lines.Add($"    Type: {resourceTypeName}");
            lines.Add($"    # Allow: Shorthand or allowed actions");
            lines.Add($"    Properties:");
            WriteResourceProperties(resourceTypeName, resourceType, 3, startList: false);
            InsertModuleItemsLines(moduleFile, lines);
            Console.WriteLine($"Added resource '{resourceName}' [{resourceTypeName}]");

            // local functions
            void WriteResourceProperties(string currentTypeName, ResourceType currentType, int indentation, bool startList) {

                // check for recursion since some types are recursive (e.g. AWS::EMR::Cluster)
                if(types.Contains(currentTypeName)) {
                    AddLine($"{currentTypeName} # Recursive");
                    return;
                }
                types.Add(currentTypeName);
                foreach(var entry in currentType.Properties.OrderBy(kv => kv.Key)) {
                    var property = entry.Value;
                    var line = $"{entry.Key}:";
                    if(property.PrimitiveType != null) {
                        line += $" {property.PrimitiveType}";
                    }
                    if(property.Required) {
                        line += " # Required";
                    }
                    AddLine(line);
                    ++indentation;
                    switch(property.Type) {
                    case null:
                        break;
                    case "List":
                        if(property.PrimitiveItemType != null) {
                            AddLine($"- {property.PrimitiveItemType}");
                        } else if(TryGetType(property.ItemType, out var nestedListType)) {
                            WriteResourceProperties(property.ItemType, nestedListType, indentation + 1, startList: true);
                        } else {
                            AddError($"could not find property type '{resourceTypeName}.{property.ItemType}'");
                        }
                        break;
                    case "Map":
                        if(property.PrimitiveItemType != null) {
                            AddLine($"String: {property.PrimitiveItemType}");
                        } else if(TryGetType(property.ItemType, out var nestedMapType)) {
                            AddLine($"String:");
                            WriteResourceProperties(property.ItemType, nestedMapType, indentation + 2, startList: true);
                        } else {
                            AddError($"could not find property type '{resourceTypeName}.{property.ItemType}'");
                        }
                        break;
                    default:
                        if(TryGetType(property.Type, out var nestedType)) {
                            WriteResourceProperties(property.Type, nestedType, indentation, startList: false);
                        } else {
                            AddError($"could not find property type '{resourceTypeName}.{property.Type}'");
                        }
                        break;
                    }
                    --indentation;
                }
                types.Remove(currentTypeName);

                // local functions
                string Indent(int count) => new string(' ', count * 2);

                bool TryGetType(string typeName, out ResourceType type)
                    => ResourceMapping.CloudformationSpec.PropertyTypes.TryGetValue(typeName, out type)
                        || ResourceMapping.CloudformationSpec.PropertyTypes.TryGetValue(resourceTypeName + "." + typeName, out type);

                void AddLine(string line) {
                    if(startList) {
                        lines.Add(Indent(indentation - 1) + "- " + line);
                        startList = false;
                    } else {
                        lines.Add(Indent(indentation) + line);
                    }
                }
            }
        }

        private void InsertModuleItemsLines(string moduleFile, IEnumerable<string> lines) {

            // parse yaml module definition
            if(!File.Exists(moduleFile)) {
                AddError($"could not find module '{moduleFile}'");
                return;
            }

            // update YAML module definition
            var moduleLines = File.ReadAllLines(moduleFile).ToList();

            // check if `Items:` section needs to be added
            var functionIndex = moduleLines.FindIndex(line => line.StartsWith("Items:", StringComparison.Ordinal));
            if(functionIndex < 0) {
                moduleLines.Add("Items:");
                moduleLines.Add("");
                functionIndex = moduleLines.Count;
            } else {

                // find the last line of the section
                var blankLineIndex = -1;
                ++functionIndex;
                while(functionIndex < moduleLines.Count) {
                    var line = moduleLines[functionIndex];
                    if(line.Trim() == "") {
                        if(blankLineIndex == -1) {
                            blankLineIndex = functionIndex;
                        }
                    } else if(char.IsWhiteSpace(line[0])) {
                        blankLineIndex = -1;
                    } else {
                        break;
                    }
                    ++functionIndex;
                }

                // check if we found a blank line
                if(blankLineIndex == -1) {
                    moduleLines.Insert(functionIndex, "");
                    ++functionIndex;
                } else {
                    functionIndex = blankLineIndex + 1;
                }

                // add another blank line after if we stopped before the last line of the file
                if(functionIndex < moduleLines.Count) {
                    moduleLines.Insert(functionIndex, "");
                }
            }

            // insert function definition
            moduleLines.InsertRange(functionIndex, lines);
            File.WriteAllLines(moduleFile, moduleLines);
        }
    }
}
