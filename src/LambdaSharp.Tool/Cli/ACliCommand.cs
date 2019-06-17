/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SimpleSystemsManagement;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {

    public class AwsAccountInfo {

        //--- Properties ---
        public string Region { get; set; }
        public string AccountId { get; set; }
        public string UserArn { get; set; }
    }

    public abstract class ACliCommand : CliBase {

        //--- Class Fields ---
        private static string PromptColor = AnsiTerminal.Cyan;

        //--- Class Methods ---
        public static CommandOption AddTierOption(CommandLineApplication cmd)
            => cmd.Option("--tier|-T <NAME>", "(optional) Name of deployment tier (default: LAMBDASHARP_TIER environment variable)", CommandOptionType.SingleValue);

        public static string ReadResource(string resourceName, IDictionary<string, string> substitutions = null) {
            var result = typeof(ACliCommand).Assembly.ReadManifestResource($"LambdaSharp.Tool.Resources.{resourceName}");
            if(substitutions != null) {
                foreach(var kv in substitutions) {
                    result = result.Replace($"%%{kv.Key}%%", kv.Value);
                }
            }
            return result;
        }

        public static string PromptString(string message, string defaultValue = null) {
            var prompt = "";
            if(Settings.UseAnsiConsole) {
                prompt += PromptColor;
            }
            prompt += $"|=> {message}:";
            if(!string.IsNullOrEmpty(defaultValue)) {
                prompt += $" [{defaultValue}]";
            }
            if(Settings.UseAnsiConsole) {
                prompt += AnsiTerminal.Reset;
            }
            prompt += " ";
            Console.Write(prompt);
            SetCursorVisible(true);
            var result = Console.ReadLine();
            SetCursorVisible(false);
            return string.IsNullOrEmpty(result)
                ? defaultValue
                : result;

            // local functions
            void SetCursorVisible(bool visible) {
                try {
                    Console.CursorVisible = visible;
                } catch { }
            }
        }

        public static void PromptText(string message) {
            if(Settings.UseAnsiConsole) {
                Console.WriteLine($"{PromptColor}*** {message} ***{AnsiTerminal.Reset}");
            } else {
                Console.WriteLine($"*** {message} ***");
            }
        }

        public static string PromptChoice(string message, IList<string> choices) {
            if(Settings.UseAnsiConsole) {
                Console.WriteLine($"{PromptColor}{message} (multiple choice){AnsiTerminal.Reset}");
                var choiceCount = choices.Count;
                for(var i = 0; i < choiceCount; ++i) {
                    Console.WriteLine($"{PromptColor}{i + 1}. {choices[i]}{AnsiTerminal.Reset}");
                }
                while(true) {
                    var enteredValue = PromptString("Enter a choice");
                    if(int.TryParse(enteredValue, out var choice) && (choice >= 1) && (choice <= choiceCount)) {
                        return choices[choice - 1];
                    }
                }
            } else {
                Console.WriteLine($"{message} (multiple choice)");
                var choiceCount = choices.Count;
                for(var i = 0; i < choiceCount; ++i) {
                    Console.WriteLine($"{i + 1}. {choices[i]}");
                }
                while(true) {
                    var enteredValue = PromptString($"Enter a choice (1-{choiceCount})");
                    if(int.TryParse(enteredValue, out var choice) && (choice >= 1) && (choice <= choiceCount)) {
                        return choices[choice - 1];
                    }
                }
            }
        }

        //--- Methods ---
        protected async Task<AwsAccountInfo> InitializeAwsProfile(string awsProfile, string awsAccountId = null, string awsRegion = null, string awsUserArn = null) {

            // initialize AWS profile
            if(awsProfile != null) {

                // select an alternate AWS profile by setting the AWS_PROFILE environment variable
                Environment.SetEnvironmentVariable("AWS_PROFILE", awsProfile);
                Environment.SetEnvironmentVariable("AWS_DEFAULT_PROFILE", awsProfile);
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
                } catch(Exception e) {
                    LogError("unable to determine the AWS Account Id and Region", e);
                    return null;
                }
            }

            // set AWS region for library and spawned processes
            AWSConfigs.AWSRegion = awsRegion;
            Environment.SetEnvironmentVariable("AWS_REGION", awsRegion);
            Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", awsRegion);
            return new AwsAccountInfo {
                Region = awsRegion,
                AccountId = awsAccountId,
                UserArn = awsUserArn
            };
        }

        protected Func<Task<Settings>> CreateSettingsInitializer(
            CommandLineApplication cmd,
            bool requireAwsProfile = true,
            bool requireDeploymentTier = true
        ) {
            CommandOption tierOption = null;
            CommandOption awsProfileOption = null;

            // add misc options
            if(requireDeploymentTier) {
                tierOption = AddTierOption(cmd);
            }
            if(requireAwsProfile) {
                awsProfileOption = cmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
            }
            var verboseLevelOption = cmd.Option("--verbose|-V[:<LEVEL>]", "(optional) Show verbose output (0=Quiet, 1=Normal, 2=Detailed, 3=Exceptions; Normal if LEVEL is omitted)", CommandOptionType.SingleOrNoValue);
            var noAnsiOutputOption = cmd.Option("--no-ansi", "Disable colored ANSI terminal output", CommandOptionType.NoValue);

            // add hidden testing options
            var awsRegionOption = cmd.Option("--aws-region <NAME>", "(test only) Override AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
            var awsAccountIdOption = cmd.Option("--aws-account-id <VALUE>", "(test only) Override AWS account Id (default: read from AWS profile)", CommandOptionType.SingleValue);
            var awsUserArnOption = cmd.Option("--aws-user-arn <ARN>", "(test only) Override AWS user ARN (default: read from AWS profile)", CommandOptionType.SingleValue);
            var toolVersionOption = cmd.Option("--cli-version <VALUE>", "(test only) LambdaSharp CLI version for profile", CommandOptionType.SingleValue);
            var deploymentBucketNameOption = cmd.Option("--deployment-bucket-name <NAME>", "(test only) S3 Bucket name used to deploy modules (default: read from LambdaSharp CLI configuration)", CommandOptionType.SingleValue);
            var deploymentNotificationTopicOption = cmd.Option("--deployment-notifications-topic <ARN>", "(test only) SNS Topic for CloudFormation deployment notifications (default: read from LambdaSharp CLI configuration)", CommandOptionType.SingleValue);
            var moduleBucketNamesOption = cmd.Option("--module-bucket-names <NAMES>", "(test only) Comma-separated list of S3 Bucket names used to find modules (default: read from LambdaSharp CLI configuration)", CommandOptionType.SingleValue);
            var tierVersionOption = cmd.Option("--tier-version <VERSION>", "(test only) LambdaSharp tier version (default: read from deployment tier)", CommandOptionType.SingleValue);
            awsAccountIdOption.ShowInHelpText = false;
            awsRegionOption.ShowInHelpText = false;
            toolVersionOption.ShowInHelpText = false;
            deploymentBucketNameOption.ShowInHelpText = false;
            deploymentNotificationTopicOption.ShowInHelpText = false;
            moduleBucketNamesOption.ShowInHelpText = false;
            tierVersionOption.ShowInHelpText = false;
            return async () => {

                // check if ANSI console output needs to be disabled
                if(noAnsiOutputOption.HasValue()) {
                    Settings.UseAnsiConsole = false;
                }

                // initialize logging level
                if(!TryParseEnumOption(verboseLevelOption, Tool.VerboseLevel.Normal, VerboseLevel.Detailed, out Settings.VerboseLevel)) {

                    // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by 'TryParseEnumOption'
                    return null;
                }

                // initialize deployment tier
                string tier = null;
                if(requireDeploymentTier) {
                    tier = tierOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_TIER");
                    if(string.IsNullOrEmpty(tier)) {

                        // TODO: allow empty tier name
                        LogError("missing deployment tier name");
                    }
                }

                // initialize AWS profile
                try {
                    AwsAccountInfo awsAccount = null;
                    IAmazonSimpleSystemsManagement ssmClient = null;
                    IAmazonCloudFormation cfnClient = null;
                    IAmazonKeyManagementService kmsClient = null;
                    IAmazonS3 s3Client = null;
                    IAmazonAPIGateway apiGatewayClient = null;
                    IAmazonIdentityManagementService iamClient = null;
                    if(requireAwsProfile) {
                        awsAccount = await InitializeAwsProfile(
                            awsProfileOption.Value(),
                            awsAccountIdOption.Value(),
                            awsRegionOption.Value(),
                            awsUserArnOption.Value()
                        );

                        // create AWS clients
                        ssmClient = new AmazonSimpleSystemsManagementClient();
                        cfnClient = new AmazonCloudFormationClient();
                        kmsClient = new AmazonKeyManagementServiceClient();
                        s3Client = new AmazonS3Client();
                        apiGatewayClient = new AmazonAPIGatewayClient();
                        iamClient = new AmazonIdentityManagementServiceClient();
                    }
                    if(HasErrors) {
                        return null;
                    }

                    // initialize LambdaSharp deployment values
                    var tierVersion = tierVersionOption.Value();
                    var deploymentBucketName = deploymentBucketNameOption.Value();
                    var deploymentNotificationTopic = deploymentNotificationTopicOption.Value();
                    var moduleBucketNames = moduleBucketNamesOption.Value()?.Split(',');

                    // create a settings instance for each module filename
                    return new Settings {
                        ToolVersion = Version,
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
                        IamClient = iamClient
                    };
                } catch(AmazonClientException e) when(e.Message == "No RegionEndpoint or ServiceURL configured") {
                    LogError("AWS profile configuration is missing a region specifier");
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
            LogError($"value for {option.LongName} must be one of {string.Join(", ", pairs)}");
            result = defaultvalue;
            return false;
        }

        protected async Task<bool> PopulateRuntimeSettingsAsync(Settings settings, bool optional = false) {
            if(
                (settings.DeploymentBucketName == null)
                || (settings.TierVersion == null)
            ) {
                if(settings.Tier == null) {
                    if(!optional) {
                        LogError($"must provide a tier name with --tier option");
                        return false;
                    }
                    return true;
                }

                // attempt to find an existing core module
                var stackName = $"{settings.TierPrefix}LambdaSharp-Core";
                Stack stack = null;
                try {
                    var describe = await settings.CfnClient.DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = stackName
                    });

                    // make sure the stack is in a stable state (not updating and not failed)
                    stack = describe.Stacks.FirstOrDefault();
                    switch(stack?.StackStatus) {
                    case null:
                    case "CREATE_COMPLETE":
                    case "ROLLBACK_COMPLETE":
                    case "UPDATE_COMPLETE":
                    case "UPDATE_ROLLBACK_COMPLETE":

                        // we're good to go
                        break;
                    default:
                        LogError($"{stackName} is not in a valid state; module deployment must be complete and successful (status: {stack?.StackStatus})");
                        return false;
                    }
                } catch(AmazonCloudFormationException) {

                    // stack not found; nothing to do
                }

                // validate module information
                var tierModuleInfoText = stack?.Outputs.FirstOrDefault(output => output.OutputKey == "Module")?.OutputValue;
                if(tierModuleInfoText == null) {
                    if(!optional) {
                        LogError($"Could not find LambdaSharp tier information for {stackName}");
                    }
                    return false;
                }
                if(
                    !ModuleInfo.TryParse(tierModuleInfoText, out var tierModuleInfo)
                    || (tierModuleInfo.Owner != "LambdaSharp")
                    || (tierModuleInfo.Name != "Core")
                ) {
                    LogError("LambdaSharp tier is not configured propertly", new LambdaSharpDeploymentTierSetupException(settings.Tier));
                    return false;
                }
                settings.TierVersion = tierModuleInfo.Version;

                // check if tier and tool versions are compatible
                if(!optional) {
                    var tierToToolVersionComparison = tierModuleInfo.Version.CompareToVersion(settings.ToolVersion);
                    if(tierToToolVersionComparison == 0) {

                        // versions are identical; nothing to do
                    } else if(tierToToolVersionComparison < 0) {
                        LogError($"LambdaSharp tier is not up to date (tool: {settings.ToolVersion}, tier: {tierModuleInfo.Version})", new LambdaSharpDeploymentTierSetupException(settings.Tier));
                        return false;
                    } else if(tierToToolVersionComparison > 0) {

                        // tier is newer; we expect the tier to be backwards compatible by exposing the same resources as before
                    } else {
                        LogError($"LambdaSharp tool is not compatible (tool: {settings.ToolVersion}, tier: {tierModuleInfo.Version})", new LambdaSharpToolOutOfDateException(tierModuleInfo.Version));
                        return false;
                    }
                }

                // read deployment S3 bucket name
                var tierModuleBucketArnParts = GetStackOutput("DeploymentBucket")?.Split(':');
                if(tierModuleBucketArnParts == null) {
                    LogError("could not find 'DeploymentBucket' output value");
                    return false;
                }
                if((tierModuleBucketArnParts.Length != 6) || (tierModuleBucketArnParts[0] != "arn") || (tierModuleBucketArnParts[1] != "aws") || (tierModuleBucketArnParts[2] != "s3")) {
                    LogError("invalid value for 'DeploymentBucket' output value");
                    return false;
                }
                settings.DeploymentBucketName = tierModuleBucketArnParts[5];

                // read tier mode
                var coreServicesModeText = GetStackOutput("CoreServices");
                if(!Enum.TryParse<CoreServices>(coreServicesModeText, true, out var coreServicesMode)) {
                    LogError("unable to parse CoreServices output value from stack");
                    return false;
                }
                settings.CoreServices = coreServicesMode;

                // local functions
                string GetStackOutput(string key) => stack.Outputs.FirstOrDefault(output => output.OutputKey == key)?.OutputValue;
            }
            return true;
        }

        protected string GetGitShaValue(string workingDirectory, bool showWarningOnFailure = true) {

            // read the gitSha using 'git' directly
            var process = new Process {
                StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "HEAD" })) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                }
            };

            // attempt to get git-sha value
            string gitSha = null;
            try {
                process.Start();
                gitSha = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if(process.ExitCode != 0) {
                    if(showWarningOnFailure) {
                        LogWarn($"unable to get git-sha 'git rev-parse HEAD' failed with exit code = {process.ExitCode}");
                    }
                    gitSha = null;
                }
            } catch {
                if(showWarningOnFailure) {
                    LogWarn("git is not installed; skipping git-sha detection");
                }
            }

            // check if folder contains uncommitted/untracked changes
            process = new Process {
                StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "status", "--porcelain" })) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                }
            };

            // attempt to get git status
            try {
                process.Start();
                var dirty = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if(process.ExitCode != 0) {
                    if(showWarningOnFailure) {
                        LogWarn($"unable to get git status 'git status --porcelain' failed with exit code = {process.ExitCode}");
                    }
                }

                // check if any changes were detected
                if(!string.IsNullOrEmpty(dirty)) {
                    gitSha = "DIRTY-" + gitSha;
                }
            } catch {
                if(showWarningOnFailure) {
                    LogWarn("git is not installed; skipping git status detection");
                }
            }
            return gitSha;
        }

        protected string GetGitBranch(string workingDirectory, bool showWarningOnFailure = true) {

            // read the gitSha using 'git' directly
            var process = new Process {
                StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "--abbrev-ref", "HEAD" })) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                }
            };
            string gitBranch = null;
            try {
                process.Start();
                gitBranch = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if(process.ExitCode != 0) {
                    if(showWarningOnFailure) {
                        LogWarn($"unable to get git branch 'git rev-parse --abbrev-ref HEAD' failed with exit code = {process.ExitCode}");
                    }
                    gitBranch = null;
                }
            } catch {
                if(showWarningOnFailure) {
                    LogWarn("git is not installed; skipping git branch detection");
                }
            }
            return gitBranch;
        }

        protected async Task<(string Arn, IEnumerable<string> MissingPolicies)> DetermineMissingApiGatewayRolePolicies(Settings settings) {
            if((settings.ApiGatewayClient == null) || (settings.IamClient == null)) {
                return (Arn: null, Enumerable.Empty<string>());
            }

            // inspect API Gateway role
            try {
                var missingPolicies = new List<string> {
                    "AmazonAPIGatewayPushToCloudWatchLogs",
                    "AWSXrayWriteOnlyAccess"
                };

                // retrieve the CloudWatch/X-Ray role from the API Gateway account
                var account = await settings.ApiGatewayClient.GetAccountAsync(new GetAccountRequest());
                if(account.CloudwatchRoleArn != null) {

                    // check if the role has the expected managed policies
                    var attachedPolicies = (await settings.IamClient.ListAttachedRolePoliciesAsync(new ListAttachedRolePoliciesRequest {
                        RoleName = account.CloudwatchRoleArn.Split('/').Last()
                    })).AttachedPolicies;
                    foreach(var attachedPolicy in attachedPolicies) {
                        missingPolicies.Remove(attachedPolicy.PolicyName);
                    }
                }
                return (Arn: account.CloudwatchRoleArn, MissingPolicies: Enumerable.Empty<string>());
            } catch(Exception) {
                return (Arn: null, Enumerable.Empty<string>());
            }
        }

        protected async Task CheckApiGatewayRole(Settings settings) {

                // retrieve the CloudWatch/X-Ray role from the API Gateway account
            Console.WriteLine("=> Checking API Gateway role");
                var account = await settings.ApiGatewayClient.GetAccountAsync(new GetAccountRequest());
            var role = await GetOrCreateRole(account.CloudwatchRoleArn?.Split('/').Last() ?? "LambdaSharp-ApiGatewayRole");

            // check if the role has the expected managed policies; if not, attach them
            var attachedPolicies = (await settings.IamClient.ListAttachedRolePoliciesAsync(new ListAttachedRolePoliciesRequest {
                RoleName = role.RoleName
            })).AttachedPolicies;
            await CheckOrAttachPolicy("arn:aws:iam::aws:policy/service-role/AmazonAPIGatewayPushToCloudWatchLogs");
            await CheckOrAttachPolicy("arn:aws:iam::aws:policy/AWSXrayWriteOnlyAccess");

            // update API Gateway Account role if needed
            if(account.CloudwatchRoleArn == null) {
                Console.WriteLine($"=> Updating API Gateway role to {role.Arn}");
            again:
                try {
                    await settings.ApiGatewayClient.UpdateAccountAsync(new UpdateAccountRequest {
                        PatchOperations = new List<PatchOperation> {
                            new PatchOperation {
                                Op = Op.Replace,
                                Path = "/cloudwatchRoleArn",
                                Value = role.Arn
                            }
                        }
                    });
                } catch(BadRequestException) {
                    Console.WriteLine($"=> Waiting for API Gateway role changes to settle, trying again in 5 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                } catch(TooManyRequestsException) {
                    Console.WriteLine($"=> Update request was throttled, trying again in 2 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    goto again;
                }
            }

            // local functions
            async Task CheckOrAttachPolicy(string managedPolicyArn) {

                // check if managed policy is already attached; it not, attach it
                if(!attachedPolicies.Any(policy => policy.PolicyArn == managedPolicyArn)) {
                    Console.WriteLine($"=> Attaching managed policy to API Gateway role: {managedPolicyArn}");
                    await settings.IamClient.AttachRolePolicyAsync(new AttachRolePolicyRequest {
                        PolicyArn = managedPolicyArn,
                        RoleName = role.RoleName
                    });
                }
            }

            async Task<Role> GetOrCreateRole(string roleName) {
                try {

                    // attempt to resolve the given role by name
                    return (await settings.IamClient.GetRoleAsync(new GetRoleRequest {
                        RoleName = roleName
                    })).Role;
                } catch(NoSuchEntityException) {

                    // IAM role not found, fallthrough to the next step
                }

                // only create the LambdaSharp API Gateway Role when the account has no role
                Console.WriteLine("=> Creating API Gateway role");
                return (await settings.IamClient.CreateRoleAsync(new CreateRoleRequest {
                    RoleName = "LambdaSharp-ApiGatewayRole",
                    Description = "API Gateway Role for CloudWatch Logs and X-Ray Tracing",
                    AssumeRolePolicyDocument = @"{""Version"":""2012-10-17"",""Statement"":[{""Sid"": ""ApiGatewayPrincipal"",""Effect"":""Allow"",""Principal"":{""Service"":""apigateway.amazonaws.com""},""Action"":""sts:AssumeRole""}]}"
                })).Role;
            }
        }
    }
}
