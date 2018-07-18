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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.KeyManagementService;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SimpleSystemsManagement;
using Humidifier.Json;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model;

namespace MindTouch.LambdaSharp.Tool {

    public enum VerboseLevel {
        Quiet,
        Normal,
        Detailed,
        Exceptions
    }

    public enum DryRunLevel {
        Everything,
        CloudFormation
    }

    public static class Program {

        //--- Class Fields ---
        private static IList<(string Message, Exception Exception)> _errors = new List<(string Message, Exception Exception)>();
        private static VerboseLevel _verboseLevel = VerboseLevel.Normal;
        private static Version _version = typeof(Program).Assembly.GetName().Version;

        //--- Class Methods ---
        public static int Main(string[] args) {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false) {
                Name = "MindTouch.LambdaSharp.Tool",
                FullName = $"MindTouch LambdaSharp Tool (v{_version.Major}.{_version.Minor})"
            };
            app.HelpOption();

            // info command
            app.Command("info", cmd => {
                cmd.HelpOption();
                cmd.Description = "Show LambdaSharp settings";
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    Info(settings);
                });
            });

            // deploy command
            app.Command("deploy", cmd => {
                cmd.HelpOption();
                cmd.Description = "Deploy LambdaSharp app";
                var inputFileOption = cmd.Option("--input <FILE>", "(optional) YAML app deployment file (default: Deploy.yml)", CommandOptionType.SingleValue);
                var dryRunOption = cmd.Option("--dryrun:<LEVEL>", "(optional) Generate output assets without deploying (0=everything, 1=cloudformation)", CommandOptionType.SingleOrNoValue);
                var outputFilename = cmd.Option("--output <FILE>", "(optional) Name of generated CloudFormation template file (default: cloudformation.json)", CommandOptionType.SingleValue);
                var allowDataLossOption = cmd.Option("--allow-data-loss", "(optional) Allow CloudFormation resource update operations that could lead to data loss", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    DryRunLevel? dryRun = null;
                    if(dryRunOption.HasValue()) {
                        DryRunLevel value;
                        if(!TryParseEnumOption(dryRunOption, DryRunLevel.Everything, out value)) {
                            return;
                        }
                        dryRun = value;
                    }
                    await Deploy(
                        settings,
                        inputFileOption.Value() ?? "Deploy.yml",
                        dryRun,
                        outputFilename.Value() ?? "cloudformation.json",
                        allowDataLossOption.HasValue()
                    );
                });
            });

            // new command
            app.Command("new", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create new LambdaSharp asset";

                // function sub-command
                cmd.Command("function", nestedCmd => {
                    nestedCmd.HelpOption();
                    var nameOption = nestedCmd.Option("--name|-n <VALUE>", "Name of new project (e.g. App.Function)", CommandOptionType.SingleValue);
                    var namespaceOption = nestedCmd.Option("--namespace|-ns <VALUE>", "(optional) Root namespace for project (default: same as function name)", CommandOptionType.SingleValue);
                    var directoryOption = nestedCmd.Option("--working-directory|-wd <VALUE>", "(optional) New function project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var frameworkOption = nestedCmd.Option("--framework|-f <VALUE>", "(optional) Target .NET framework (default: 'netcoreapp2.1')", CommandOptionType.SingleValue);
                    var useProjectReferenceOption = nestedCmd.Option("--use-project-reference", "Reference LambdaSharp libraries using project references (default: use nuget package reference)", CommandOptionType.NoValue);
                    nestedCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        var lambdasharpDirectory = Environment.GetEnvironmentVariable("LAMBDASHARP");
                        if(lambdasharpDirectory == null) {
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
            app.OnExecute(() => {
                Console.WriteLine(app.GetHelpText());
            });

            // execute command line options and report any errors
            try {
                app.Execute(args);
            } catch(Exception e) {
                AddError(e);
            }
            if(_errors.Any()) {
                Console.WriteLine();
                Console.WriteLine($"FAILED: {_errors.Count():N0} errors encountered");
                foreach (var error in _errors) {
                    if((error.Exception != null) && (_verboseLevel >= VerboseLevel.Exceptions)) {
                        Console.WriteLine("ERROR: " + error.Message + Environment.NewLine + error.Exception);
                    } else {
                        Console.WriteLine("ERROR: " + error.Message);
                    }
                }
                return -1;
            }
            return 0;
        }

        private static void Info(Settings settings) {
            Console.WriteLine($"Deployment: {settings.Deployment ?? "<NOT SET>"}");
            Console.WriteLine($"Git SHA: {settings.GitSha ?? "<NOT SET>"}");
            Console.WriteLine($"AWS Region: {settings.AwsRegion ?? "<NOT SET>"}");
            Console.WriteLine($"AWS Account Id: {settings.AwsAccountId ?? "<NOT SET>"}");
            Console.WriteLine($"LambdaSharp S3 Bucket: {settings.DeploymentBucketName ?? "<NOT SET>"}");
            Console.WriteLine($"LambdaSharp Dead-Letter Queue: {settings.DeadLetterQueueUrl ?? "<NOT SET>"}");
            Console.WriteLine($"LambdaSharp CloudFormation Notification Topic: {settings.DeploymentNotificationTopicArn ?? "<NOT SET>"}");
            Console.WriteLine($"LambdaSharp Rollbar Custom Resource Topic: {settings.RollbarCustomResourceTopicArn ?? "<NOT SET>"}");
        }

        private static async Task Deploy(
            Settings settings,
            string inputFile,
            DryRunLevel? dryRun,
            string outputFilename,
            bool allowDataLoos
        ) {
            if(settings == null) {
                return;
            }

            // read input file
            settings.FileName = Path.GetFullPath(inputFile);
            if(!File.Exists(settings.FileName)) {
                AddError($"could not find '{settings.FileName}'");
                return;
            }
            Console.WriteLine($"Loading '{inputFile}'");
            var source = await File.ReadAllTextAsync(settings.FileName);

            // preprocess file
            Console.WriteLine("Pre-processing");
            var parser = new ModelPreprocessor(settings).Preprocess(source);
            if(_errors.Any()) {
                return;
            }

            // parse yaml app file
            Console.WriteLine("Analyzing");
            var app = new ModelParser(settings).Parse(parser, dryRun == DryRunLevel.CloudFormation);
            if(_errors.Any()) {
                return;
            }

            // generate cloudformation template
            var generator = new ModelGenerator();
            var stack = generator.Generate(app);
            if(_errors.Any()) {
                return;
            }

            // serialize stack to disk
            var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFile));
            var outputPath = Path.Combine(workingDirectory, outputFilename);
            var template = new JsonStackSerializer().Serialize(stack);
            File.WriteAllText(outputPath, template);
            if(dryRun == null) {
                await new StackUpdater().Deploy(app, template, allowDataLoos);

                // remove dryrun file if it exists
                if(File.Exists(outputPath)) {
                    try {
                        File.Delete(outputPath);
                    } catch { }
                }
            }
        }

        private static Func<Task<Settings>> CreateSettingsInitializer(CommandLineApplication command) {
            var deploymentOption = command.Option("--deployment|-D <NAME>", "(optional) Name of deployment (default: LAMBDASHARPDEPLOYMENT environment variable)", CommandOptionType.SingleValue);
            var awsProfileOption = command.Option("--profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
            var verboseLevelOption = command.Option("--verbose|-V:<LEVEL>", "(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)", CommandOptionType.SingleOrNoValue);
            var gitShaOption = command.Option("--gitsha <VALUE>", "(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)", CommandOptionType.SingleValue);
            var awsAccountIdOption = command.Option("--aws-account-id <VALUE>", "(test only) Override AWS account Id (default: read from AWS profile)", CommandOptionType.SingleValue);
            var awsRegionOption = command.Option("--aws-region <NAME>", "(test only) Override AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
            var deploymentBucketNameOption = command.Option("--deployment-bucket-name <NAME>", "(test only) S3 Bucket used to deploying assets (default: read from LambdaSharp configuration)", CommandOptionType.SingleValue);
            var deploymentDeadletterQueueUrlOption = command.Option("--deployment-deadletter-queue-url <URL>", "(test only) SQS Deadletter queue used by function (default: read from LambdaSharp configuration)", CommandOptionType.SingleValue);
            var deploymentNotificationTopicArnOption = command.Option("--deployment-notification-topic-arn <ARN>", "(test only) SNS Topic used by CloudFormation deploymetions (default: read from LambdaSharp configuration)", CommandOptionType.SingleValue);
            var boostrapOption = command.Option("--bootstrap", "(boostrap only) Don't read LambdaSharp initialization values", CommandOptionType.NoValue);
            var deploymentRollbarCustomResourceTopicArnOption = command.Option("--deployment-rollbar-customresource-topic-arn <ARN>", "(test only) SNS Topic for creating Rollbar projects (default: read from LambdaSharp configuration)", CommandOptionType.SingleValue);
            return async () => {
                var boostrap = boostrapOption.HasValue();

                // initialize logging level
                if(verboseLevelOption.HasValue()) {
                    if(!TryParseEnumOption(verboseLevelOption, VerboseLevel.Detailed, out _verboseLevel)) {
                        return null;
                    }
                }

                // initialize deployment value
                var deployment = deploymentOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARPDEPLOYMENT");
                if(deployment == null) {
                    AddError("missing 'deployment' name");
                    return null;
                }
                if(deployment == "Default") {
                    AddError("deployment cannot be 'Default' because it is a reserved name");
                    return null;
                }

                // initialize gitSha value
                var gitSha = gitShaOption.Value();
                if(gitSha == null) {

                    // read the gitSha using `git` directly
                    var process = new Process() {
                        StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "HEAD" })) {
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        }
                    };
                    process.Start();
                    gitSha = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if(process.ExitCode != 0) {
                        Console.WriteLine($"WARNING: unable to get git-sha `git rev-parse HEAD` failed with exit code = {process.ExitCode}");
                        gitSha = null;
                    }
                }

                // initialize AWS account Id and region
                var awsProfile = awsProfileOption.Value();
                if(awsProfile != null) {

                    // select an alternate AWS profile by setting the AWS_PROFILE environment variable
                    Environment.SetEnvironmentVariable("AWS_PROFILE", awsProfile);
                }
                var awsAccountId = awsAccountIdOption.Value();
                var awsRegion = awsRegionOption.Value();
                if((awsAccountId == null) || (awsRegion == null)) {

                    // determine AWS region and account
                    try {
                        var stsClient = new AmazonSecurityTokenServiceClient();
                        var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
                        awsAccountId = awsAccountId ?? response.Account;
                        awsRegion = awsRegion ?? stsClient.Config.RegionEndpoint.SystemName;
                    } catch(Exception e) {
                        AddError("unable to determine the AWS Account Id and Region", e);
                        return null;
                    }
                }

                // create AWS clients
                var ssmClient = new AmazonSimpleSystemsManagementClient();
                var cfClient = new AmazonCloudFormationClient();
                var kmsClient = new AmazonKeyManagementServiceClient();
                var s3Client = new AmazonS3Client();

                // initialize LambdaSharp deployment values
                var deploymentBucketName = deploymentBucketNameOption.Value();
                var deploymentDeadletterQueueUrl = deploymentDeadletterQueueUrlOption.Value();
                var deploymentNotificationTopicArn = deploymentNotificationTopicArnOption.Value();
                var deploymentRollbarCustomResourceTopicArn = deploymentRollbarCustomResourceTopicArnOption.Value();
                if(boostrap) {
                    Console.WriteLine($"Bootstrapping LambdaSharp for `{deployment}'");
                } else if(
                    (deploymentBucketName == null)
                    || (deploymentDeadletterQueueUrl == null)
                    || (deploymentNotificationTopicArn == null)
                    || (deploymentRollbarCustomResourceTopicArn == null)
                ) {
                    Console.WriteLine($"Retrieving LambdaSharp settings for `{deployment}'");

                    // import lambdasharp parameters
                    var lambdaSharpPath = $"/{deployment}/LambdaSharp/";
                    var lambdaSharpSettings = await ssmClient.GetAllParametersByPathAsync(lambdaSharpPath);
                    deploymentBucketName = deploymentBucketName ?? GetLambdaSharpSetting("DeploymentBucket");
                    if(deploymentBucketName == null) {
                        AddError("unable to determine the LambdaSharp S3 Bucket");
                        return null;
                    }
                    deploymentDeadletterQueueUrl = deploymentDeadletterQueueUrl ?? GetLambdaSharpSetting("DeadLetterQueue");
                    if(deploymentDeadletterQueueUrl == null) {
                        AddError("unable to determine the LambdaSharp Dead-Letter Queue");
                        return null;
                    }
                    deploymentNotificationTopicArn = deploymentNotificationTopicArn ?? GetLambdaSharpSetting("DeploymentNotificationTopic");
                    if(deploymentNotificationTopicArn == null) {
                        AddError("unable to determine the LambdaSharp CloudFormation Notification Topic");
                        return null;
                    }

                    // Rollbar custom topic is optional, so don't check for null
                    deploymentRollbarCustomResourceTopicArn = deploymentRollbarCustomResourceTopicArn ?? GetLambdaSharpSetting("RollbarCustomResourceTopic");

                    // local functions
                    string GetLambdaSharpSetting(string name) {
                        lambdaSharpSettings.TryGetValue(lambdaSharpPath + name, out KeyValuePair<string, string> result);
                        return result.Value;
                    }
                }
                return new Settings {
                    Deployment = deployment,
                    GitSha = gitSha,
                    AwsRegion = awsRegion,
                    AwsAccountId = awsAccountId,
                    DeploymentBucketName = deploymentBucketName,
                    DeadLetterQueueUrl = deploymentDeadletterQueueUrl,
                    DeploymentNotificationTopicArn = deploymentNotificationTopicArn,
                    RollbarCustomResourceTopicArn = deploymentRollbarCustomResourceTopicArn,
                    ResourceMapping = new ResourceMapping(),
                    SsmClient = ssmClient,
                    CfClient = cfClient,
                    KmsClient = kmsClient,
                    S3Client = s3Client,
                    ErrorCallback = AddError,
                    VerboseLevel = _verboseLevel
                };
            };
        }

        private static void NewFunction(
            string lambdasharpDirectory,
            string functionName,
            string rootNamespace,
            string framework,
            bool useProjectReference,
            string baseDirectory
        ) {
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
            var lambdasharpProject = Path.GetRelativePath(projectDirectory, Path.Combine(lambdasharpDirectory, "src", "MindTouch.LambdaSharp", "MindTouch.LambdaSharp.csproj"));
            var projectFile = Path.Combine(projectDirectory, functionName + ".csproj");
            try {
                var projectContents = useProjectReference
? @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>" + framework + @"</TargetFramework>
    <Deterministic>true</Deterministic>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <RootNamespace>" + rootNamespace + @"</RootNamespace>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Amazon.Lambda.Core"" Version=""1.0.0""/>
    <PackageReference Include=""Amazon.Lambda.Serialization.Json"" Version=""1.2.0""/>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""" + lambdasharpProject + @""" />
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Amazon.Lambda.Tools"" Version=""2.2.0""/>
  </ItemGroup>
</Project>"
:  @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>" + framework + @"</TargetFramework>
    <Deterministic>true</Deterministic>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <RootNamespace>" + rootNamespace + @"</RootNamespace>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Amazon.Lambda.Core"" Version=""1.0.0""/>
    <PackageReference Include=""Amazon.Lambda.Serialization.Json"" Version=""1.2.0""/>
    <PackageReference Include=""MindTouch.LambdaSharp"" Version=""" + _version.Major + "." + _version.Minor + @"""/>
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Amazon.Lambda.Tools"" Version=""2.2.0""/>
  </ItemGroup>
</Project>";
                File.WriteAllText(projectFile, projectContents);
                Console.WriteLine($"Created project file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), projectFile)}");
            } catch(Exception e) {
                AddError($"unable to create project file '{projectFile}'", e);
                return;
            }
            var functionFile = Path.Combine(projectDirectory, "Function.cs");
            try {
                var functionContents = 
@"using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace " + rootNamespace + @" {

    public class Function : ALambdaFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context) {
            using(var reader = new StreamReader(stream)) {
                LogInfo(await reader.ReadToEndAsync());
            }
            return ""Ok"";
        }
    }
}";
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                AddError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }

        private static bool TryParseEnumOption<T>(CommandOption option, T defaultvalue, out T result) where T : struct {
            if(option.Value() == null) {
                result = defaultvalue;
                return true;
            }
            if(int.TryParse(option.Value(), out int intValue)) {
                if(!Enum.GetValues(typeof(T)).Cast<int>().Any(v => v == intValue)) {
                    goto failed;
                }
                result = (T)Convert.ChangeType(Enum.ToObject(typeof(T), intValue), typeof(T));
                return true;
            }
            if(Enum.TryParse(typeof(T), option.Value(), ignoreCase: true, result: out object enumValue)) {
                result = (T)Convert.ChangeType(enumValue, typeof(T));
                return true;
            }
        failed:
            var pairs = Enum.GetValues(typeof(T)).Cast<int>().Zip(Enum.GetNames(typeof(T)).Cast<string>(), (value, name) => $"{value}={name.ToLowerInvariant()}");
            AddError($"value for {option.Template} must be one of {string.Join(", ", pairs)}");
            result = defaultvalue;
            return false;
        }

        private static void AddError(string message, Exception exception = null)
            => _errors.Add((Message: message, Exception: exception));

        private static void AddError(Exception exception)
            => AddError(exception.Message, exception);
    }
}
