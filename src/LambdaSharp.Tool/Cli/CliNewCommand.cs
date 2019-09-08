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
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using LambdaSharp.Tool.Cli.Build;
using LambdaSharp.Tool.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.Tool.Internal;
using System.Threading.Tasks;

namespace LambdaSharp.Tool.Cli {
    using Tag = Amazon.CloudFormation.Model.Tag;

    public enum FunctionType {
        Unknown,
        Generic,
        ApiGateway,
        ApiGatewayProxy,
        CustomResource,
        Schedule,
        Queue,
        Topic,
        WebSocket,
        WebSocketProxy,
        Finalizer
    }

    public class CliNewCommand : ACliCommand {

        //--- Fields ---
        private IList<string> _functionTypes = typeof(FunctionType).GetEnumNames()
            .Where(value => value != FunctionType.Unknown.ToString())
            .OrderBy(value => value)
            .ToArray();

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
                    var inputFileOption = subCmd.Option("--input <FILE>", "(optional) File path to YAML module definition (default: Module.yml)", CommandOptionType.SingleValue);
                    inputFileOption.ShowInHelpText = false;
                    var functionTypeOption = subCmd.Option("--type|-t <TYPE>", $"(optional) Function type (one of: {string.Join(", ", _functionTypes).ToLowerInvariant()}; default: prompt)", CommandOptionType.SingleValue);
                    var functionTimeoutOption = subCmd.Option("--timeout <SECONDS>", "(optional) Function timeout in seconds (default: 30)", CommandOptionType.SingleValue);
                    var functionMemoryOption = subCmd.Option("--memory <MB>", "(optional) Function memory in megabytes (default: 256)", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new project (e.g. MyFunction)");
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        var settings = new Settings { };

                        // get function name
                        var functionName = nameArgument.Value;
                        while(string.IsNullOrEmpty(functionName)) {
                            functionName = settings.PromptString("Enter the function name");
                        }

                        // get function type
                        if(!TryParseEnumOption(functionTypeOption, FunctionType.Unknown, FunctionType.Unknown, out var functionType)) {

                            // NOTE (2019-08-12, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                            return;
                        }
                        if(!int.TryParse(functionTimeoutOption.Value() ?? "30", out var functionTimeout)) {
                            LogError("invalid value for --timeout option");
                            return;
                        }
                        if(!int.TryParse(functionMemoryOption.Value() ?? "256", out var functionMemory)) {
                            LogError("invalid value for --memory option");
                            return;
                        }
                        var workingDirectory = Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory());
                        NewFunction(
                            settings,
                            functionName,
                            namespaceOption.Value(),
                            frameworkOption.Value() ?? "netcoreapp2.1",
                            workingDirectory,
                            Path.Combine(workingDirectory, inputFileOption.Value() ?? "Module.yml"),
                            languageOption.Value() ?? "csharp",
                            functionMemory,
                            functionTimeout,
                            functionType
                        );
                    });
                });

                // module sub-command
                cmd.Command("module", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp module";

                    // sub-command options
                    var directoryOption = subCmd.Option("--working-directory <PATH>", "(optional) New module directory (default: current directory)", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new module (e.g. My.NewModule)");
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        var settings = new Settings { };

                        // get the module name
                        var moduleName = nameArgument.Value;
                        while(string.IsNullOrEmpty(moduleName)) {
                            moduleName = settings.PromptString("Enter the module name");
                        }

                        // prepend default namespace string
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
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        var settings = new Settings { };

                        // get the resource name
                        var name = nameArgument.Value;
                        while(string.IsNullOrEmpty(name)) {
                            name = settings.PromptString("Enter the resource name");
                        }

                        // get the resource type
                        var type = typeArgument.Value;
                        while(string.IsNullOrEmpty(type)) {
                            type = settings.PromptString("Enter the resource type");
                        }
                        NewResource(
                            moduleFile: Path.Combine(Directory.GetCurrentDirectory(), "Module.yml"),
                            resourceName: name,
                            resourceTypeName: type
                        );
                    });
                });

                // bucket sub-command
                cmd.Command("bucket", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new public S3 bucket for sharing LambdaSharp modules";
                    var awsProfileOption = subCmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of the S3 bucket");

                    // sub-command options
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");

                        // initialize AWS profile
                        var awsAccount = await InitializeAwsProfile(awsProfileOption.Value());

                        // initialize settings instance
                        var settings = new Settings {
                            CfnClient = new AmazonCloudFormationClient(),
                            S3Client = new AmazonS3Client(),
                            AwsRegion = awsAccount.Region,
                            AwsAccountId = awsAccount.AccountId,
                            AwsUserArn = awsAccount.UserArn
                        };

                        // get the resource name
                        var bucketName = nameArgument.Value;
                        while(string.IsNullOrEmpty(bucketName)) {
                            bucketName = settings.PromptString("Enter the S3 bucket name");
                        }
                        await NewBucket(settings, bucketName);
                        Console.WriteLine();
                        if(Settings.UseAnsiConsole) {
                            Console.WriteLine($"=> S3 Bucket ARN: {AnsiTerminal.Green}arn:aws:s3:::{bucketName}{AnsiTerminal.Reset}");
                        } else {
                            Console.WriteLine($"=> S3 Bucket ARN: arn:aws:s3:::{bucketName}");
                        }
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
                LogError($"unable to create directory '{moduleDirectory}'", e);
                return;
            }
            var moduleFile = Path.Combine(moduleDirectory, "Module.yml");
            if(File.Exists(moduleFile)) {
                LogError($"module definition '{moduleFile}' already exists");
                return;
            }
            try {
                var module = ReadResource("NewModule.yml", new Dictionary<string, string> {
                    ["MODULENAME"] = moduleName
                });
                File.WriteAllText(moduleFile, module);
                Console.WriteLine($"Created module definition: {Path.GetRelativePath(Directory.GetCurrentDirectory(), moduleFile)}");
            } catch(Exception e) {
                LogError($"unable to create module definition '{moduleFile}'", e);
            }
        }

        public void NewFunction(
            Settings settings,
            string functionName,
            string rootNamespace,
            string framework,
            string workingDirectory,
            string moduleFile,
            string language,
            int functionMemory,
            int functionTimeout,
            FunctionType functionType
        ) {

            // parse yaml module definition
            if(!File.Exists(moduleFile)) {
                LogError($"could not find module '{moduleFile}'");
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
                LogError($"project directory '{projectDirectory}' already exists");
                return;
            }
            try {
                Directory.CreateDirectory(projectDirectory);
            } catch(Exception e) {
                LogError($"unable to create directory '{projectDirectory}'", e);
                return;
            }

            // create function file
            switch(language) {
            case "csharp":
                NewCSharpFunction(
                    settings,
                    functionName,
                    rootNamespace,
                    framework,
                    workingDirectory,
                    moduleFile,
                    functionMemory,
                    functionTimeout,
                    projectDirectory,
                    functionType
                );
                break;
            case "javascript":
                NewJavascriptFunction(
                    settings,
                    functionName,
                    rootNamespace,
                    framework,
                    workingDirectory,
                    moduleFile,
                    functionMemory,
                    functionTimeout,
                    projectDirectory,
                    functionType
                );
                break;
            }

            // insert function definition
            InsertModuleItemsLines(moduleFile, new[] {
                $"  - Function: {functionName}",
                $"    Description: TO-DO - update function description",
                $"    Memory: {functionMemory}",
                $"    Timeout: {functionTimeout}"
            });
        }

        public void NewCSharpFunction(
            Settings settings,
            string functionName,
            string rootNamespace,
            string framework,
            string workingDirectory,
            string moduleFile,
            int functionMemory,
            int functionTimeout,
            string projectDirectory,
            FunctionType functionType
        ) {
            if(functionName == "Finalizer") {

                // always of type finalizer
                functionType = FunctionType.Finalizer;
            } else if(functionType == FunctionType.Unknown) {

                // prompt for function type
                functionType = Enum.Parse<FunctionType>(settings.PromptChoice("Select function type", _functionTypes), ignoreCase: true);
            }

            // create function project
            var projectFile = Path.Combine(projectDirectory, functionName + ".csproj");
            var substitutions = new Dictionary<string, string> {
                ["FRAMEWORK"] = framework,
                ["ROOTNAMESPACE"] = rootNamespace,
                ["LAMBDASHARP_VERSION"] = Version.GetWildcardVersion()
            };
            try {
                var projectContents = ReadResource("NewCSharpFunctionProject.xml", substitutions);
                File.WriteAllText(projectFile, projectContents);
                Console.WriteLine($"Created project file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), projectFile)}");
            } catch(Exception e) {
                LogError($"unable to create project file '{projectFile}'", e);
                return;
            }

            // create function source code
            var functionFile = Path.Combine(projectDirectory, "Function.cs");
            var functionContents = ReadResource($"NewCSharpFunction-{functionType}.txt", substitutions);
            try {
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                LogError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }

        public void NewJavascriptFunction(
            Settings settings,
            string functionName,
            string rootNamespace,
            string framework,
            string workingDirectory,
            string moduleFile,
            int functionMemory,
            int functionTimeout,
            string projectDirectory,
            FunctionType functionType
        ) {
            if(functionType != FunctionType.Unknown) {
                LogError("--type option is not support for javascript functions");
                return;
            }

            // create function source code
            var functionFile = Path.Combine(projectDirectory, "index.js");
            var functionContents = ReadResource("NewJSFunction.txt");
            try {
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                LogError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }

        public void NewResource(string moduleFile, string resourceName, string resourceTypeName) {

            // determine if we have a precise resource type match
            var matches = ResourceMapping.CloudformationSpec.ResourceTypes
                .Where(item => item.Key.ToLowerInvariant().Contains(resourceTypeName.ToLowerInvariant()))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // check if we have an exact match
            if(!matches.TryGetValue(resourceTypeName, out var resourceType)) {
                if(matches.Count == 0) {
                    LogError($"unable to find a match for '{resourceTypeName}'");
                } else {
                    Console.WriteLine();
                    Console.WriteLine($"Found partial matches for '{resourceTypeName}'");
                    foreach(var key in matches.OrderBy(item => item.Key)) {
                        Console.WriteLine($"    {key}");
                    }
                    LogError($"unable to find exact match for '{resourceTypeName}'");
                }
                return;
            } else {
                resourceType = matches.First().Value;
            }

            // create resource definition
            var types = new HashSet<string>();
            var lines = new List<string>();
            lines.Add($"  - Resource: {resourceName}");
            lines.Add($"    Description: TO-DO - update resource description");
            lines.Add($"    Type: {resourceTypeName}");
            lines.Add($"    Properties:");
            if(resourceType.Documentation != null) {
                lines.Add($"      # Documentation: {resourceType.Documentation}");
            }
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
                            LogError($"could not find property type '{resourceTypeName}.{property.ItemType}'");
                        }
                        break;
                    case "Map":
                        if(property.PrimitiveItemType != null) {
                            AddLine($"String: {property.PrimitiveItemType}");
                        } else if(TryGetType(property.ItemType, out var nestedMapType)) {
                            AddLine($"String:");
                            WriteResourceProperties(property.ItemType, nestedMapType, indentation + 2, startList: true);
                        } else {
                            LogError($"could not find property type '{resourceTypeName}.{property.ItemType}'");
                        }
                        break;
                    default:
                        if(TryGetType(property.Type, out var nestedType)) {
                            WriteResourceProperties(property.Type, nestedType, indentation, startList: false);
                        } else {
                            LogError($"could not find property type '{resourceTypeName}.{property.Type}'");
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

        public async Task NewBucket(Settings settings, string bucketName) {

            // create bucket using template
            var template = ReadResource("PublicLambdaSharpBucket.yml");
            var stackName = $"PublicLambdaSharpBucket-{bucketName}";
            var response = await settings.CfnClient.CreateStackAsync(new CreateStackRequest {
                StackName = stackName,
                Capabilities = new List<string> { },
                OnFailure = OnFailure.DELETE,
                Parameters = new List<Parameter> {
                    new Parameter {
                        ParameterKey = "BucketName",
                        ParameterValue = bucketName
                    }
                },
                TemplateBody = template,
                Tags = new List<Tag> {
                    new Tag {
                        Key = "LambdaSharp:PublicBucket",
                        Value = bucketName
                    },
                    new Tag {
                        Key = "LambdaSharp:DeployedBy",
                        Value = settings.AwsUserArn.Split(':').Last()
                    }
                }
            });
            var created = await settings.CfnClient.TrackStackUpdateAsync(stackName, response.StackId, mostRecentStackEventId: null, logError: LogError);
            if(created.Success) {
                Console.WriteLine("=> Stack creation finished");
            } else {
                Console.WriteLine("=> Stack creation FAILED");
                return;
            }

            // update bucket to require requester pays
            Console.WriteLine($"=> Updating S3 Bucket for Requester Pays access");
            await settings.S3Client.PutBucketRequestPaymentAsync(bucketName, new RequestPaymentConfiguration {
                Payer = "Requester"
            });
        }

        private void InsertModuleItemsLines(string moduleFile, IEnumerable<string> lines) {

            // parse yaml module definition
            if(!File.Exists(moduleFile)) {
                LogError($"could not find module '{moduleFile}'");
                return;
            }

            // update YAML module definition
            var moduleLines = File.ReadAllLines(moduleFile).ToList();

            // check if 'Items:' section needs to be added
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
