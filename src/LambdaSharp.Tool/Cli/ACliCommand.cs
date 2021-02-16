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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.APIGateway;
using Amazon.CloudFormation;
using Amazon.IdentityManagement;
using Amazon.KeyManagementService;
using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SimpleSystemsManagement;
using LambdaSharp.Build;
using LambdaSharp.Modules;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace LambdaSharp.Tool.Cli {

    public class AwsAccountInfo {

        //--- Properties ---
        public string Region { get; set; }
        public string AccountId { get; set; }
        public string UserArn { get; set; }
    }

    public class CachedDeploymentTierSettingsInfo {

        //--- Properties ---
        public string DeploymentBucketName { get; set; }
        public string LoggingBucketName { get; set; }
        public VersionInfo TierVersion { get; set; }
        public CoreServices CoreServices { get; set; }
    }

    public abstract class ACliCommand : CliBase {

        //--- Class Properties ---
        public static string CredentialsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws");
        public static string CredentialsFilePath = Path.Combine(CredentialsFolder, "credentials");

        //--- Class Methods ---
        public static string ReadResource(string resourceName, IDictionary<string, string> substitutions = null) {
            var result = typeof(ACliCommand).Assembly.ReadManifestResource($"LambdaSharp.Tool.Resources.{resourceName}");
            if(substitutions != null) {
                foreach(var kv in substitutions) {
                    result = result.Replace($"%%{kv.Key}%%", kv.Value);
                }
            }
            return result;
        }

        //--- Fields ---
        private readonly Dictionary<CommandLineApplication, List<Action>> _commandOptions = new Dictionary<CommandLineApplication, List<Action>>();

        //--- Constructors ---
        protected ACliCommand() {

            // setup configuration for catching build logging events
            BuildEventsConfig = new BuildEventsConfig();
            BuildEventsConfig.OnLogErrorEvent += (sender, args) => Settings.LogError(args.Message, args.Exception);
            BuildEventsConfig.OnLogWarnEvent += (sender, args) => Settings.LogWarn(args.Message);
            BuildEventsConfig.OnLogInfoEvent += (sender, args) => Settings.LogInfo(args.Message);
            BuildEventsConfig.OnLogInfoVerboseEvent += (sender, args) => Settings.LogInfoVerbose(args.Message);
            BuildEventsConfig.OnLogInfoPerformanceEvent += (sender, args) => Settings.LogInfoPerformance(args.Message, args.Duration);
        }

        //--- Properties ---
        protected BuildEventsConfig BuildEventsConfig { get; }

        //--- Methods ---
        protected async Task<AwsAccountInfo> InitializeAwsProfile(
            string awsProfile,
            string awsAccountId = null,
            string awsRegion = null,
            string awsUserArn = null,
            bool allowCaching = false
        ) {
            var stopwatch = Stopwatch.StartNew();
            var cached = false;
            try {

                // check if .aws/credentials file exists
                if(
                    !File.Exists(CredentialsFilePath)
                    && (
                        (Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") == null)
                        || (Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") == null)
                    )
                ) {
                    LogError($"IMPORTANT: run '{Settings.Lash} init' to create an AWS profile");
                    return null;
                }

                // initialize AWS profile
                if(awsProfile == null) {
                    awsProfile = Settings.AwsProfileEnvironmentVariable;
                }

                // consistently set the AWS profile by setting the AWS_PROFILE/AWS_DEFAULT_PROFILE environment variables
                AWSConfigs.AWSProfileName = awsProfile;
                Environment.SetEnvironmentVariable("AWS_PROFILE", awsProfile);
                Environment.SetEnvironmentVariable("AWS_DEFAULT_PROFILE", awsProfile);

                // check for  cached AWS profile
                var cachedProfile = Path.Combine(Settings.AwsProfileCacheDirectory, "profile.json");
                if(allowCaching && Settings.AllowCaching && File.Exists(cachedProfile) && ((DateTime.UtcNow - File.GetLastWriteTimeUtc(cachedProfile)) < Settings.MaxCacheAge)) {
                    cached = true;
                    return JsonConvert.DeserializeObject<AwsAccountInfo>(await File.ReadAllTextAsync(cachedProfile));
                }

                // determine default AWS region
                if((awsAccountId == null) || (awsRegion == null) || (awsUserArn == null)) {

                    // determine AWS region and account
                    try {
                        var stsClient = new AmazonSecurityTokenServiceClient();
                        var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
                        awsRegion = awsRegion ?? stsClient.Config.RegionEndpoint.SystemName ?? "us-east-1";
                        awsAccountId = awsAccountId ?? response.Account;
                        awsUserArn = awsUserArn ?? response.Arn;
                    } catch(HttpRequestException e) when(e.Message == "No such host is known") {
                        LogError("an Internet connection is required to determine the AWS Account Id and Region");
                        return null;
                    } catch(Exception e) {
                        LogError("unable to determine the AWS Account Id and Region", e);
                        return null;
                    }
                }

                // set AWS region for library and spawned processes
                AWSConfigs.AWSRegion = awsRegion;
                Environment.SetEnvironmentVariable("AWS_REGION", awsRegion);
                Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", awsRegion);
                var result = new AwsAccountInfo {
                    Region = awsRegion,
                    AccountId = awsAccountId,
                    UserArn = awsUserArn
                };
                if(allowCaching && Settings.AllowCaching) {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachedProfile));
                    await File.WriteAllTextAsync(cachedProfile, JsonConvert.SerializeObject(result));
                }
                return result;
            } finally {
                Settings.LogInfoPerformance($"InitializeAwsProfile()", stopwatch.Elapsed, cached);
            }
        }

        protected Func<Task<Settings>> CreateSettingsInitializer(
            CommandLineApplication cmd,
            bool requireAwsProfile = true
        ) {
            CommandOption awsProfileOption = null;
            CommandOption awsRegionOption = null;

            // add misc options
            var tierOption = cmd.Option("--tier|-T <NAME>", "(optional) Name of deployment tier (default: LAMBDASHARP_TIER environment variable)", CommandOptionType.SingleValue);
            if(requireAwsProfile) {
                awsProfileOption = cmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
                awsRegionOption = cmd.Option("--aws-region <NAME>", "(optional) Use a specific AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
            }

            // add hidden testing options
            var awsAccountIdOption = cmd.Option("--aws-account-id <VALUE>", "(test only) Override AWS account Id (default: read from AWS profile)", CommandOptionType.SingleValue);
            var awsUserArnOption = cmd.Option("--aws-user-arn <ARN>", "(test only) Override AWS user ARN (default: read from AWS profile)", CommandOptionType.SingleValue);
            var toolVersionOption = cmd.Option("--cli-version <VALUE>", "(test only) LambdaSharp CLI version for profile", CommandOptionType.SingleValue);
            var deploymentBucketNameOption = cmd.Option("--deployment-bucket-name <NAME>", "(test only) S3 Bucket name used to deploy modules (default: read from LambdaSharp CLI configuration)", CommandOptionType.SingleValue);
            var tierVersionOption = cmd.Option("--tier-version <VERSION>", "(test only) LambdaSharp tier version (default: read from deployment tier)", CommandOptionType.SingleValue);
            var promptsAsErrorsOption = cmd.Option("--prompts-as-errors", "(optional) Missing parameters cause an error instead of a prompts (use for CI/CD to avoid unattended prompts)", CommandOptionType.NoValue);
            awsAccountIdOption.ShowInHelpText = false;
            awsUserArnOption.ShowInHelpText = false;
            toolVersionOption.ShowInHelpText = false;
            deploymentBucketNameOption.ShowInHelpText = false;
            tierVersionOption.ShowInHelpText = false;
            return async () => {

                // check if experimental caching feature is enabled
                Settings.AllowCaching = string.Equals((Environment.GetEnvironmentVariable("LAMBDASHARP_FEATURE_CACHING") ?? "false"), "true", StringComparison.OrdinalIgnoreCase);

                // initialize deployment tier
                string tier = tierOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_TIER") ?? "";

                // initialize AWS profile
                try {
                    AwsAccountInfo awsAccount = null;
                    IAmazonSimpleSystemsManagement ssmClient = null;
                    IAmazonCloudFormation cfnClient = null;
                    IAmazonKeyManagementService kmsClient = null;
                    IAmazonS3 s3Client = null;
                    IAmazonAPIGateway apiGatewayClient = null;
                    IAmazonIdentityManagementService iamClient = null;
                    IAmazonLambda lambdaClient = null;
                    if(requireAwsProfile) {
                        awsAccount = await InitializeAwsProfile(
                            awsProfileOption.Value(),
                            awsAccountIdOption.Value(),
                            awsRegionOption.Value(),
                            awsUserArnOption.Value(),

                            // TODO (2019-10-08, bjorg): provide option to disable profile caching (or at least force a reset)
                            allowCaching: true
                        );
                        if(awsAccount == null) {
                            return null;
                        }

                        // create AWS clients
                        ssmClient = new AmazonSimpleSystemsManagementClient(AWSConfigs.RegionEndpoint);
                        cfnClient = new AmazonCloudFormationClient(AWSConfigs.RegionEndpoint);
                        kmsClient = new AmazonKeyManagementServiceClient(AWSConfigs.RegionEndpoint);
                        s3Client = new AmazonS3Client(AWSConfigs.RegionEndpoint);
                        apiGatewayClient = new AmazonAPIGatewayClient(AWSConfigs.RegionEndpoint);
                        iamClient = new AmazonIdentityManagementServiceClient(AWSConfigs.RegionEndpoint);
                        lambdaClient = new AmazonLambdaClient(AWSConfigs.RegionEndpoint);
                    }
                    if(HasErrors) {
                        return null;
                    }

                    // initialize LambdaSharp deployment values
                    var tierVersion = tierVersionOption.Value();
                    var deploymentBucketName = deploymentBucketNameOption.Value();

                    // initialize LambdaSharp testing values
                    VersionInfo toolVersion = null;
                    if(toolVersionOption.HasValue()) {
                        toolVersion = VersionInfo.Parse(toolVersionOption.Value());
                    }

                    // create a settings instance for each module filename
                    return new Settings(toolVersion ?? Version) {
                        TierVersion = (tierVersion != null) ? VersionInfo.Parse(tierVersion) : null,
                        Tier = tier,
                        AwsRegion = awsAccount?.Region,
                        AwsAccountId = awsAccount?.AccountId,
                        AwsUserArn = awsAccount?.UserArn,
                        DeploymentBucketName = deploymentBucketName,
                        SsmClient = ssmClient,
                        CfnClient = cfnClient,
                        KmsClient = kmsClient,
                        S3Client = s3Client,
                        ApiGatewayClient = apiGatewayClient,
                        IamClient = iamClient,
                        LambdaClient = lambdaClient,
                        PromptsAsErrors = promptsAsErrorsOption.HasValue()
                    };
                } catch(AmazonClientException e) when(e.Message == "No RegionEndpoint or ServiceURL configured") {
                    LogError("AWS profile is missing a region specifier");
                    return null;
                }
            };
        }

        protected bool TryParseEnumOption<T>(CommandOption option, T missingValue, T defaultvalue, out T result) where T : struct {
            if(!option.HasValue()) {
                result = missingValue;
                return true;
            }
            if(option.Value() == null) {
                result = defaultvalue;
                return true;
            }
            if(int.TryParse(option.Value(), out var intValue)) {
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
            LogError($"value for --{option.LongName} must be one of {string.Join(", ", pairs)}");
            result = defaultvalue;
            return false;
        }

        protected async Task<bool> PopulateDeploymentTierSettingsAsync(
            Settings settings,
            bool requireBucketName = true,
            bool requireCoreServices = true,
            bool requireVersionCheck = true,
            bool optional = false,
            bool force = false,
            bool allowCaching = false
        ) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cached = false;
            try {
                if(
                    (settings.DeploymentBucketName == null)
                    || (settings.TierVersion == null)
                    || force
                ) {
                    var cachedDeploymentTierSettings = Path.Combine(Settings.AwsProfileCacheDirectory, $"{settings.TierPrefix}tier.json");
                    if(!force && allowCaching && Settings.AllowCaching && File.Exists(cachedDeploymentTierSettings)) {
                        var cachedInfo = JsonConvert.DeserializeObject<CachedDeploymentTierSettingsInfo>(await File.ReadAllTextAsync(cachedDeploymentTierSettings));

                        // initialize settings
                        settings.DeploymentBucketName = cachedInfo.DeploymentBucketName;
                        settings.LoggingBucketName = cachedInfo.LoggingBucketName;
                        settings.TierVersion = cachedInfo.TierVersion;
                        settings.CoreServices = cachedInfo.CoreServices;
                        cached = true;
                        return true;
                    }

                    // attempt to find an existing core module
                    var stackName = $"{settings.TierPrefix}LambdaSharp-Core";
                    var existing = await settings.CfnClient.GetStackAsync(stackName, LogError);
                    if(existing.Stack == null) {
                        if(!optional) {
                            LogError($"LambdaSharp tier {settings.TierName} does not exist", new LambdaSharpDeploymentTierSetupException(settings.TierName));
                        }
                        return false;
                    }

                    // validate module information
                    var result = true;
                    var tierModuleInfoText = existing.Stack?.GetModuleVersionText();
                    if(tierModuleInfoText == null) {
                        if(!optional && result) {
                            LogError($"Could not find LambdaSharp tier information for {stackName}");
                        }
                        result = false;
                    }

                    // read deployment S3 bucket name
                    var tierModuleDeploymentBucketArnParts = GetStackOutput("DeploymentBucket")?.Split(':');
                    if((tierModuleDeploymentBucketArnParts == null) && requireBucketName) {
                        if(!optional && result) {
                            LogError("could not find 'DeploymentBucket' output value for deployment tier settings", new LambdaSharpDeploymentTierOutOfDateException(settings.TierName));
                        }
                        result = false;
                    }
                    if(tierModuleDeploymentBucketArnParts != null) {
                        if((tierModuleDeploymentBucketArnParts.Length != 6) || (tierModuleDeploymentBucketArnParts[0] != "arn") || (tierModuleDeploymentBucketArnParts[1] != "aws") || (tierModuleDeploymentBucketArnParts[2] != "s3")) {
                            LogError("invalid value 'DeploymentBucket' output value for deployment tier settings", new LambdaSharpDeploymentTierOutOfDateException(settings.TierName));
                            result = false;
                            tierModuleDeploymentBucketArnParts = null;
                        }
                    }
                    var deploymentBucketName = tierModuleDeploymentBucketArnParts?[5];

                    // read logging S3 bucket name
                    var tierModuleLoggingBucketArnParts = GetStackOutput("LoggingBucket")?.Split(':');
                    if(tierModuleLoggingBucketArnParts != null) {
                        if((tierModuleLoggingBucketArnParts.Length != 6) || (tierModuleLoggingBucketArnParts[0] != "arn") || (tierModuleLoggingBucketArnParts[1] != "aws") || (tierModuleLoggingBucketArnParts[2] != "s3")) {
                            LogError("invalid value 'LoggingBucket' output value for deployment tier settings", new LambdaSharpDeploymentTierOutOfDateException(settings.TierName));
                            result = false;
                            tierModuleLoggingBucketArnParts = null;
                        }
                    }
                    var loggingBucketName = tierModuleLoggingBucketArnParts?[5];

                    // do some sanity checks
                    if(
                        !ModuleInfo.TryParse(tierModuleInfoText, out var tierModuleInfo)
                        || (tierModuleInfo.Namespace != "LambdaSharp")
                        || (tierModuleInfo.Name != "Core")
                    ) {
                        LogError("LambdaSharp tier is not configured propertly", new LambdaSharpDeploymentTierSetupException(settings.TierName));
                        result = false;
                    }

                    // check if tier and tool versions are compatible
                    if(
                        !optional
                        && (tierModuleInfo != null)
                        && requireVersionCheck
                        && !VersionInfoCompatibility.IsTierVersionCompatibleWithToolVersion(tierModuleInfo.Version, settings.ToolVersion)
                    ) {
                        var tierToToolVersionComparison = VersionInfoCompatibility.CompareTierVersionToToolVersion(tierModuleInfo.Version, settings.ToolVersion);
                        if(tierToToolVersionComparison < 0) {
                            LogError($"LambdaSharp tier is not up to date (tool: {settings.ToolVersion}, tier: {tierModuleInfo.Version})", new LambdaSharpDeploymentTierOutOfDateException(settings.TierName));
                            result = false;
                        } else if(tierToToolVersionComparison > 0) {

                            // tier is newer; we expect the tier to be backwards compatible by exposing the same resources as before
                        } else {
                            LogError($"LambdaSharp tool is not compatible (tool: {settings.ToolVersion}, tier: {tierModuleInfo.Version})", new LambdaSharpToolOutOfDateException(tierModuleInfo.Version));
                            result = false;
                        }
                    }

                    // read tier mode
                    var coreServicesModeText = GetStackOutput("CoreServices");
                    if(!Enum.TryParse<CoreServices>(coreServicesModeText, true, out var coreServicesMode) && requireCoreServices) {
                        if(!optional && result) {
                            LogError("unable to parse CoreServices output value from stack");
                        }
                        result = false;
                    }

                    // initialize settings
                    settings.DeploymentBucketName = deploymentBucketName;
                    settings.LoggingBucketName = loggingBucketName;
                    settings.TierVersion = tierModuleInfo?.Version;
                    settings.CoreServices = coreServicesMode;

                    // cache deployment tier settings
                    if(allowCaching && Settings.AllowCaching) {
                        Directory.CreateDirectory(Path.GetDirectoryName(cachedDeploymentTierSettings));
                        await File.WriteAllTextAsync(cachedDeploymentTierSettings, JsonConvert.SerializeObject(new CachedDeploymentTierSettingsInfo {
                            DeploymentBucketName = settings.DeploymentBucketName,
                            TierVersion = settings.TierVersion,
                            CoreServices = settings.CoreServices
                        }));
                    }
                    return result;

                    // local functions
                    string GetStackOutput(string key) => existing.Stack?.Outputs.FirstOrDefault(output => output.OutputKey == key)?.OutputValue;
                }
                return true;
            } finally {
                Settings.LogInfoPerformance($"PopulateDeploymentTierSettingsAsync() for '{settings.TierName}'", stopwatch.Elapsed, cached);
            }
        }

        protected void AddStandardCommandOptions(CommandLineApplication cmd) {

            // add --no-ansi command line option
            AddToolOption(
                cmd,
                "--no-ansi",
                "(optional) Disable colored ANSI terminal output",
                CommandOptionType.NoValue,
                option => Settings.UseAnsiConsole = !option.HasValue()
            );

            // add --verbose command line option
            // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
            AddToolOption(
                cmd,
                "--verbose|-V[:<LEVEL>]",
                "(optional) Show verbose output (0=Quiet, 1=Normal, 2=Detailed, 3=Exceptions; Normal if LEVEL is omitted)",
                CommandOptionType.SingleOrNoValue,
                option => TryParseEnumOption(option, Tool.VerboseLevel.Normal, VerboseLevel.Detailed, out Settings.VerboseLevel)
            );

            // add --quiet command line option
            AddToolOption(
                cmd,
                "--quiet",
                "(optional) Don't show banner or execution time",
                CommandOptionType.NoValue,
                option => {
                    Program.Quiet = option.HasValue();
                    if(!Program.Quiet) {

                        // find top level command line application
                        var app = cmd;
                        while(app.Parent != null) {
                            app = app.Parent;
                        }
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    }
                }
            );

            // add --no-beep command line option
            AddToolOption(
                cmd,
                "--no-beep",
                "(optional) Don't emit beep when finished",
                CommandOptionType.NoValue,
                option => {
                    if(option.HasValue()) {
                        Program.BeepThreshold = TimeSpan.MaxValue;
                    }
                }
            );
        }

        protected void AddToolOption(CommandLineApplication cmd, string template, string description, CommandOptionType optionType, Action<CommandOption> action) {
            if(cmd is null) {
                throw new ArgumentNullException(nameof(cmd));
            }
            if(action is null) {
                throw new ArgumentNullException(nameof(action));
            }
            var option = cmd.Option(template, description, optionType);
            if(!_commandOptions.TryGetValue(cmd, out var actions)) {
                actions = new List<Action>();
                _commandOptions[cmd] = actions;
            }
            actions.Add(() => action(option));
        }

        protected void ExecuteCommandActions(CommandLineApplication cmd) {
            if(cmd is null) {
                throw new ArgumentNullException(nameof(cmd));
            }
            if(_commandOptions.TryGetValue(cmd, out var actions)) {
                foreach(var action in actions) {
                    action();
                }
            }
        }
    }
}
