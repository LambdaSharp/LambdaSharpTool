/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using Amazon;
using System.Text.RegularExpressions;
using LambdaSharp.Modules;
using System.IO.Compression;
using System.Text;
using LambdaSharp.CloudFormation.TypeSystem;

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
        Finalizer,
        Event,
        SelfContained
    }

    public class CliNewCommand : ACliCommand {

        //--- Constants ---
        private const string CLOUDFORMATION_ID_PATTERN = "^[a-zA-Z][a-zA-Z0-9]*$";

        //--- Class Methods ---
        private static bool IsValidResourceName(string name) => Regex.IsMatch(name, CLOUDFORMATION_ID_PATTERN);

        //--- Fields ---
        private IList<string> _functionTypes = typeof(FunctionType).GetEnumNames()
            .Where(value => value != FunctionType.Unknown.ToString())
            .OrderBy(value => value)
            .ToArray();

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("new", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create new LambdaSharp module, function, app, or resource";

                // add command options
                var directoryOption = cmd.Option("--working-directory <PATH>", "(optional) Module directory (default: current directory)", CommandOptionType.SingleValue);
                var inputFileOption = cmd.Option("--input <FILE>", "(optional) File path to YAML module definition (default: Module.yml)", CommandOptionType.SingleValue);
                inputFileOption.ShowInHelpText = false;
                AddStandardCommandOptions(cmd);

                // function sub-command
                cmd.Command("function", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp function";

                    // sub-command options
                    var namespaceOption = subCmd.Option("--namespace <NAME>", "(optional) Root namespace for project (default: same as function name)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory <PATH>", "(optional) New function project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var frameworkOption = subCmd.Option("--framework|-f <NAME>", "(optional) Target .NET framework (default: 'net6.0')", CommandOptionType.SingleValue);
                    var languageOption = subCmd.Option("--language|-l <LANGUAGE>", "(optional) Select programming language for generated code (default: csharp)", CommandOptionType.SingleValue);
                    var inputFileOption = subCmd.Option("--input <FILE>", "(optional) File path to YAML module definition (default: Module.yml)", CommandOptionType.SingleValue);
                    inputFileOption.ShowInHelpText = false;
                    var functionTypeOption = subCmd.Option("--type|-t <TYPE>", $"(optional) Function type (one of: {string.Join(", ", _functionTypes).ToLowerInvariant()}; default: prompt)", CommandOptionType.SingleValue);
                    var functionTimeoutOption = subCmd.Option("--timeout <SECONDS>", "(optional) Function timeout in seconds (default: 30)", CommandOptionType.SingleValue);
                    var functionMemoryOption = subCmd.Option("--memory <MB>", "(optional) Function memory in megabytes (default: 1769)", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new project (e.g. MyFunction)");
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(() => {
                        ExecuteCommandActions(subCmd);
                        var settings = new Settings(Version);

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
                        if(!int.TryParse(functionMemoryOption.Value() ?? "1769", out var functionMemory)) {
                            LogError("invalid value for --memory option");
                            return;
                        }
                        var workingDirectory = Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory());
                        NewFunction(
                            settings,
                            functionName,
                            namespaceOption.Value(),
                            frameworkOption.Value() ?? "net6.0",
                            workingDirectory,
                            Path.Combine(workingDirectory, inputFileOption.Value() ?? "Module.yml"),
                            languageOption.Value() ?? "csharp",
                            functionMemory,
                            functionTimeout,
                            functionType
                        );
                    });
                });

                // app sub-command
                cmd.Command("app", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp app";

                    // sub-command options
                    var namespaceOption = subCmd.Option("--namespace <NAME>", "(optional) Root namespace for project (default: same as app name)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory <PATH>", "(optional) New app project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var inputFileOption = subCmd.Option("--input <FILE>", "(optional) File path to YAML module definition (default: Module.yml)", CommandOptionType.SingleValue);
                    inputFileOption.ShowInHelpText = false;
                    var nameArgument = subCmd.Argument("<NAME>", "Name of new project (e.g. MyApp)");
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(() => {
                        ExecuteCommandActions(subCmd);
                        var settings = new Settings(Version);

                        // get app name
                        var appName = nameArgument.Value;
                        while(string.IsNullOrEmpty(appName)) {
                            appName = settings.PromptString("Enter the app name");
                        }
                        var workingDirectory = Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory());
                        NewApp(
                            settings,
                            appName,
                            namespaceOption.Value(),
                            workingDirectory,
                            Path.Combine(workingDirectory, inputFileOption.Value() ?? "Module.yml")
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
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(() => {
                        ExecuteCommandActions(subCmd);
                        var settings = new Settings(Version);

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
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(() => {
                        ExecuteCommandActions(subCmd);
                        var settings = new Settings(Version);

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
                            settings,
                            moduleFile: Path.Combine(Directory.GetCurrentDirectory(), "Module.yml"),
                            resourceName: name,
                            resourceTypeName: type
                        );
                    });
                });

                // public-bucket sub-command
                cmd.Command("public-bucket", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new public S3 bucket with Requester Pays access";
                    var awsProfileOption = subCmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
                    var awsRegionOption = subCmd.Option("--aws-region <NAME>", "(optional) Use a specific AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of the S3 bucket");
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);

                        // initialize AWS profile
                        var awsAccount = await InitializeAwsProfile(
                            awsProfileOption.Value(),
                            awsRegion: awsRegionOption.Value()
                        );

                        // initialize settings instance
                        var settings = new Settings(Version) {
                            CfnClient = new AmazonCloudFormationClient(AWSConfigs.RegionEndpoint),
                            S3Client = new AmazonS3Client(AWSConfigs.RegionEndpoint),
                            AwsRegion = awsAccount.Region,
                            AwsAccountId = awsAccount.AccountId,
                            AwsUserArn = awsAccount.UserArn
                        };

                        // get the resource name
                        var bucketName = nameArgument.Value;
                        while(string.IsNullOrEmpty(bucketName)) {
                            bucketName = settings.PromptString("Enter the S3 bucket name");
                        }
                        if(await NewPublicBucket(settings, bucketName)) {
                            Console.WriteLine();
                            Console.WriteLine($"=> S3 Bucket ARN: {Settings.OutputColor}arn:aws:s3:::{bucketName}{Settings.ResetColor}");
                        }
                    });
                });

                // expiring-bucket sub-command
                cmd.Command("expiring-bucket", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create an S3 bucket that self-deletes after expiration";
                    var awsProfileOption = subCmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
                    var awsRegionOption = subCmd.Option("--aws-region <NAME>", "(optional) Use a specific AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
                    var expirationInDaysOption = subCmd.Option("--expiration-in-days <VALUE>", "(optional) Number of days until the bucket expires and is deleted (default: 7 days)", CommandOptionType.SingleValue);
                    var nameArgument = subCmd.Argument("<NAME>", "Name of the S3 bucket");
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);

                        // initialize AWS profile
                        var awsAccount = await InitializeAwsProfile(
                            awsProfileOption.Value(),
                            awsRegion: awsRegionOption.Value()
                        );

                        // initialize settings instance
                        var settings = new Settings(Version) {
                            CfnClient = new AmazonCloudFormationClient(AWSConfigs.RegionEndpoint),
                            S3Client = new AmazonS3Client(AWSConfigs.RegionEndpoint),
                            AwsRegion = awsAccount.Region,
                            AwsAccountId = awsAccount.AccountId,
                            AwsUserArn = awsAccount.UserArn
                        };

                        // get the resource name
                        var bucketName = nameArgument.Value;
                        while(string.IsNullOrEmpty(bucketName)) {
                            bucketName = settings.PromptString("Enter the S3 bucket name");
                        }

                        // get --expiration-in-days option
                        if(
                            !int.TryParse(expirationInDaysOption.Value() ?? "7", out var expirationInDays)
                            || (expirationInDays < 1)
                            || (expirationInDays > 365)
                        ) {
                            LogError("invalid value for --expiration-in-days option");
                        }
                        if(await NewExpiringBucket(settings, bucketName, expirationInDays)) {
                            Console.WriteLine();
                            Console.WriteLine($"=> S3 Bucket ARN: {Settings.OutputColor}arn:aws:s3:::{bucketName}{Settings.ResetColor}");
                        }
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    ExecuteCommandActions(cmd);
                    var settings = new Settings(Version);
                    var workingDirectory = Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory());
                    var moduleFilePath = Path.Combine(workingDirectory, inputFileOption.Value() ?? "Module.yml");

                    // check if module file exists; if none, create module
                    if(!File.Exists(moduleFilePath)) {

                        // get the module name
                        var moduleName = "";
                        while(string.IsNullOrEmpty(moduleName)) {
                            moduleName = settings.PromptString("Enter the module name");
                        }

                        // prepend default namespace string
                        if(!moduleName.Contains('.')) {
                            moduleName = "My." + moduleName;
                        }
                        NewModule(
                            moduleName,
                            workingDirectory
                        );
                        return;
                    }

                    // prompt for declaration type
                    const string BlazorApp = "Blazor WebAssembly App";
                    const string LambdaFunction = "Lambda Function";
                    const string AwsResource = "AWS Resource";
                    var declarationType = settings.PromptChoice("Select declaration to add", new[] {
                        AwsResource,
                        BlazorApp,
                        LambdaFunction
                    });
                    Console.WriteLine();
                    switch(declarationType) {
                    case BlazorApp:

                        // get app name
                        var appName = "";
                        while(string.IsNullOrEmpty(appName)) {
                            appName = settings.PromptString("Enter the app name");
                        }
                        Console.WriteLine();
                        NewApp(
                            settings,
                            appName,
                            rootNamespace: null,
                            workingDirectory,
                            moduleFilePath
                        );
                        break;
                    case LambdaFunction:

                        // get function name
                        var functionName = "";
                        while(string.IsNullOrEmpty(functionName)) {
                            functionName = settings.PromptString("Enter the function name");
                        }
                        Console.WriteLine();
                        NewFunction(
                            settings,
                            functionName,
                            rootNamespace: null,
                            "netcoreapp3.1",
                            workingDirectory,
                            Path.Combine(workingDirectory, inputFileOption.Value() ?? "Module.yml"),
                            "csharp",
                            functionMemory: 1769,
                            functionTimeout: 30,
                            FunctionType.Unknown
                        );
                        break;
                    case AwsResource:

                        // get the resource name
                        var resourceName = "";
                        while(string.IsNullOrEmpty(resourceName)) {
                            resourceName = settings.PromptString("Enter the resource name");
                        }
                        Console.WriteLine();

                        // get the resource type
                        var resourceType = "";
                        while(string.IsNullOrEmpty(resourceType)) {
                            resourceType = settings.PromptString("Enter the resource type");
                        }
                        NewResource(
                            settings,
                            moduleFilePath,
                            resourceName,
                            resourceType
                        );
                        break;
                    default:
                        throw new ArgumentException("unexpected value");
                    }
                    return;
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

            // validate name
            if(!IsValidResourceName(functionName)) {
                LogError("function name is not valid");
                return;
            }

            // parse yaml module definition
            if(!File.Exists(moduleFile)) {
                LogError($"could not find module '{moduleFile}'");
                return;
            }
            var moduleContents = File.ReadAllText(moduleFile);
            var module = new ModelYamlToAstConverter(new Settings(Version), moduleFile).Convert(moduleContents, selector: null);
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
                functionTimeout = 900;
            } else if(functionType == FunctionType.Unknown) {

                // prompt for function type
                functionType = Enum.Parse<FunctionType>(settings.PromptChoice("Select function type", _functionTypes), ignoreCase: true);
            }

            // fetch resource names for this function type
            var frameworkFolder = framework.Replace(".", "");
            var sourceResourceNamesPrefix = $"{frameworkFolder}.NewCSharpFunction-{functionType}.";
            var sourceResourceNames = GetResourceNames(sourceResourceNamesPrefix);
            if(sourceResourceNames.Length == 0) {
                LogError("function type is not supported for selected framework");
                return;
            }

            // create files for the project
            var substitutions = new Dictionary<string, string> {
                ["FRAMEWORK"] = framework,
                ["ROOTNAMESPACE"] = rootNamespace,
                ["LAMBDASHARP_VERSION"] = VersionInfoCompatibility.GetLambdaSharpAssemblyWildcardVersion(settings.ToolVersion, framework)
            };
            var projectSourceResourceName = $"{sourceResourceNamesPrefix}xml";
            foreach(var sourceResourceName in sourceResourceNames) {
                var sourceContents = ReadResource(sourceResourceName, substitutions);
                if(sourceResourceName == projectSourceResourceName) {

                    // create function project
                    var projectFile = Path.Combine(projectDirectory, functionName + ".csproj");
                    try {
                        File.WriteAllText(projectFile, sourceContents);
                        Console.WriteLine($"Created project file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), projectFile)}");
                    } catch(Exception e) {
                        LogError($"unable to create project file '{projectFile}'", e);
                        return;
                    }
                } else {

                    // create source file
                    var otherFile = Path.Combine(projectDirectory, Path.GetFileNameWithoutExtension(sourceResourceName.Substring(sourceResourceNamesPrefix.Length)).Replace("_", "."));
                    try {
                        File.WriteAllText(otherFile, sourceContents);
                        Console.WriteLine($"Created file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), otherFile)}");
                    } catch(Exception e) {
                        LogError($"unable to create file '{otherFile}'", e);
                        return;
                    }
                }
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

        public void NewApp(
            Settings settings,
            string appName,
            string rootNamespace,
            string workingDirectory,
            string moduleFile
        ) {

            // validate name
            if(!IsValidResourceName(appName)) {
                LogError("app name is not valid");
                return;
            }

            // parse yaml module definition
            if(!File.Exists(moduleFile)) {
                LogError($"could not find module '{moduleFile}'");
                return;
            }
            var moduleContents = File.ReadAllText(moduleFile);
            var module = new ModelYamlToAstConverter(new Settings(Version), moduleFile).Convert(moduleContents, selector: null);
            if(HasErrors) {
                return;
            }

            // set default namespace if none is set
            if(rootNamespace == null) {
                rootNamespace = $"{module.Module}.{appName}";
            }

            // create directory for app project
            var projectDirectory = Path.Combine(workingDirectory, appName);
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

            // generate project
            var substitutions = new Dictionary<string, string> {
                ["ROOTNAMESPACE"] = rootNamespace,
                ["APPNAME"] = appName
            };
            using(var projectStream = typeof(CliNewCommand).Assembly.GetManifestResourceStream("LambdaSharp.Tool.Resources.BlazorProjectTemplate.zip"))
            using(var projectArchive = new ZipArchive(projectStream)) {
                foreach(var entry in projectArchive.Entries) {
                    var entryPath = Path.Combine(projectDirectory, entry.FullName);
                    if(entry.FullName.EndsWith("/")) {
                        Directory.CreateDirectory(entryPath);
                    } else {

                        // use app name for .csproj file
                        entryPath = (entry.Name == "MyApp._csproj")
                            ? Path.Combine(projectDirectory, $"{appName}.csproj")
                            : entryPath;
                        using(var file = File.OpenWrite(entryPath))
                        using(var entryStream = entry.Open()) {
                            var fileExtension = Path.GetExtension(entryPath);
                            switch(fileExtension) {
                            case ".cs":
                            case ".csproj":
                            case ".razor":
                            case ".html":

                                // apply substitutions to source files
                                using(var entryReader = new StreamReader(entryStream)) {
                                    var contents = entryReader.ReadToEnd();
                                    foreach(var kv in substitutions) {
                                        contents = contents.Replace($"%%{kv.Key}%%", kv.Value);
                                    }
                                    var contentsBytes = Encoding.UTF8.GetBytes(contents);
                                    file.Write(contentsBytes, 0, contentsBytes.Length);
                                }
                                break;
                            default:

                                // copy zip contents verbatim
                                entryStream.CopyTo(file);
                                break;
                            }
                        }
                        Console.WriteLine($"Created file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), entryPath)}");
                    }
                }
            }

            // insert app definition and a variable to output the app website URL
            InsertModuleItemsLines(moduleFile, new[] {
                $"  - App: {appName}",
                $"    Description: TO-DO - update app description"
            });
            InsertModuleItemsLines(moduleFile, new[] {
                $"  - Variable: {appName}WebsiteUrl",
                $"    Description: {appName} Website URL",
                $"    Scope: stack",
                $"    Value: !GetAtt {appName}::Bucket.Outputs.WebsiteUrl"
            });
        }

        public void NewResource(Settings settings, string moduleFile, string resourceName, string resourceTypeName) {

            // validate name
            if(!IsValidResourceName(resourceName)) {
                LogError("resource name is not valid");
                return;
            }

            // determine if we have a precise resource type match
            var matches = settings.GetCloudFormationSpec()
                .ResourceTypes
                .Where(item => item.Name.ToLowerInvariant().Contains(resourceTypeName.ToLowerInvariant()))
                .ToDictionary(item => item.Name, item => item);

            // check if we have an exact match
            if(!matches.TryGetValue(resourceTypeName, out var resourceType)) {
                if(matches.Count == 0) {

                    // no match, error out
                    LogError($"unable to find a match for '{resourceTypeName}'");
                    return;
                } else if(matches.Count == 1) {

                    // not an exact match, but still unambiguous
                    resourceTypeName = matches.First().Key;
                    resourceType = matches.First().Value;
                } else if(matches.Count < 10) {

                    // a few multiple matches, let's prompt to disambiguate
                    var choices = matches.OrderBy(item => item.Key)
                        .Select(kv => kv.Key)
                        .ToList();
                    Console.WriteLine();
                    resourceTypeName = settings.PromptChoice($"Select resource type", choices);
                    Console.WriteLine();
                    resourceType = matches[resourceTypeName];
                } else {

                    // too many matches, error out
                    Console.WriteLine();
                    Console.WriteLine($"Found too many partial matches for '{resourceTypeName}'");
                    foreach(var kv in matches.OrderBy(item => item.Key)) {
                        Console.WriteLine($"    {kv.Key}");
                    }
                    LogError($"unable to find exact match for '{resourceTypeName}'");
                    return;
                }
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
            WriteResourceProperties(resourceType, 3, startList: false);
            InsertModuleItemsLines(moduleFile, lines);
            Console.WriteLine($"Added resource '{resourceName}' [{resourceTypeName}]");

            // local functions
            void WriteResourceProperties(IResourceType currentType, int indentation, bool startList) {

                // check for recursion since some types are recursive (e.g. AWS::EMR::Cluster)
                if(types.Contains(currentType.Name)) {
                    AddLine($"{currentType.Name} # Recursive");
                    return;
                }
                types.Add(currentType.Name);
                foreach(var property in currentType.Properties.OrderBy(kv => kv.Name)) {
                    var line = $"{property.Name}:";
                    if(
                        (property.CollectionType == ResourceCollectionType.NoCollection)
                        && (property.ItemType != ResourceItemType.ComplexType)
                    ) {
                        line += $" {property.ItemType}";
                    }
                    if(property.Required) {
                        line += " # Required";
                    }
                    AddLine(line);
                    ++indentation;
                    switch(property.CollectionType) {
                    case ResourceCollectionType.List:
                        if(property.ItemType == ResourceItemType.ComplexType) {
                            WriteResourceProperties(property.ComplexType, indentation + 1, startList: true);
                        } else {
                            AddLine($"- {property.ItemType}");
                        }
                        break;
                    case ResourceCollectionType.Map:
                        if(property.ItemType == ResourceItemType.ComplexType) {
                            AddLine($"String:");
                            WriteResourceProperties(property.ComplexType, indentation + 2, startList: true);
                        } else {
                            AddLine($"String: {property.ItemType}");
                        }
                        break;
                    case ResourceCollectionType.NoCollection when property.ItemType == ResourceItemType.ComplexType:
                        WriteResourceProperties(property.ComplexType, indentation, startList: false);
                        break;
                    default:

                        // nothing to do
                        break;
                    }
                    --indentation;
                }
                types.Remove(currentType.Name);

                // local functions
                string Indent(int count) => new string(' ', count * 2);

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

        public async Task<bool> NewPublicBucket(Settings settings, string bucketName) {

            // create bucket using template
            var template = ReadResource("LambdaSharpBucketPublic.yml", new Dictionary<string, string> {
                ["TOOL-VERSION"] = settings.ToolVersion.ToString(),
            });
            var stackName = $"Bucket-{bucketName}";
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
                return false;
            }

            // TODO (2020-07-23, bjorg): consider creating an embedded finalizer to set Requester Pays access

            // update bucket to require requester pays
            Console.WriteLine("=> Updating S3 Bucket for Requester Pays access");
            await settings.S3Client.PutBucketRequestPaymentAsync(bucketName, new RequestPaymentConfiguration {
                Payer = "Requester"
            });
            return true;
        }

        public async Task<bool> NewExpiringBucket(Settings settings, string bucketName, int expirationInDays) {

            // create bucket using template
            var template = ReadResource("LambdaSharpBucketExpiring.yml", new Dictionary<string, string> {
                ["TOOL-VERSION"] = settings.ToolVersion.ToString(),
            });
            var stackName = $"Bucket-{bucketName}";
            var response = await settings.CfnClient.CreateStackAsync(new CreateStackRequest {
                StackName = stackName,
                Capabilities = new List<string> {
                    "CAPABILITY_NAMED_IAM"
                },
                OnFailure = OnFailure.DELETE,
                Parameters = new List<Parameter> {
                    new Parameter {
                        ParameterKey = "BucketName",
                        ParameterValue = bucketName
                    },
                    new Parameter {
                        ParameterKey = "ExpirationInDays",
                        ParameterValue = expirationInDays.ToString()
                    },
                },
                TemplateBody = template,
                Tags = new List<Tag> {
                    new Tag {
                        Key = "LambdaSharp:BuildBucket",
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
                return false;
            }
            return true;
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
