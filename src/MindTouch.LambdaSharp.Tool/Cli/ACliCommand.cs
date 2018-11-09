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
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public abstract class ACliCommand : CliBase {

        //--- Class Methods ---
        public static CommandOption AddTierOption(CommandLineApplication cmd)
            => cmd.Option("--tier|-T <NAME>", "(optional) Name of deployment tier (default: LAMBDASHARP_TIER environment variable)", CommandOptionType.SingleValue);

        //--- Methods ---
        protected async Task<(string AccountId, string Region)?> InitializeAwsProfile(string awsProfile, string awsAccountId = null, string awsRegion = null) {

            // initialize AWS profile
            if(awsProfile != null) {

                // select an alternate AWS profile by setting the AWS_PROFILE environment variable
                Environment.SetEnvironmentVariable("AWS_PROFILE", awsProfile);
            }

            // determine default AWS region
            if((awsAccountId == null) || (awsRegion == null)) {

                // determine AWS region and account
                try {
                    var stsClient = new AmazonSecurityTokenServiceClient();
                    var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
                    awsAccountId = awsAccountId ?? response.Account;
                    awsRegion = awsRegion ?? stsClient.Config.RegionEndpoint.SystemName ?? "us-east-1";
                } catch(Exception e) {
                    AddError("unable to determine the AWS Account Id and Region", e);
                    return null;
                }
            }

            // set AWS region for library and spawned processes
            AWSConfigs.AWSRegion = awsRegion;
            Environment.SetEnvironmentVariable("AWS_REGION", awsRegion);
            Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", awsRegion);
            return (AccountId: awsAccountId, Region: awsRegion);
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
            var toolProfileOption = cmd.Option("--cli-profile|-CLI <NAME>", "(optional) Use a specific LambdaSharp CLI profile (default: Default)", CommandOptionType.SingleValue);
            if(requireAwsProfile) {
                awsProfileOption = cmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
            }
            var verboseLevelOption = cmd.Option("--verbose|-V:<LEVEL>", "(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)", CommandOptionType.SingleOrNoValue);

            // add hidden testing options
            var awsAccountIdOption = cmd.Option("--aws-account-id <VALUE>", "(test only) Override AWS account Id (default: read from AWS profile)", CommandOptionType.SingleValue);
            var awsRegionOption = cmd.Option("--aws-region <NAME>", "(test only) Override AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
            var toolVersionOption = cmd.Option("--cli-version <VALUE>", "(test only) LambdaSharp CLI version for profile", CommandOptionType.SingleValue);
            var deploymentBucketNameOption = cmd.Option("--deployment-bucket-name <NAME>", "(test only) S3 Bucket name used to deploy modules (default: read from LambdaSharp CLI configuration)", CommandOptionType.SingleValue);
            var deploymentNotificationTopicArnOption = cmd.Option("--deployment-notifications-topic-arn <ARN>", "(test only) SNS Topic for CloudFormation deployment notifications (default: read from LambdaSharp CLI configuration)", CommandOptionType.SingleValue);
            var runtimeVersionOption = cmd.Option("--runtime-version <VERSION>", "(test only) LambdaSharp runtime version (default: read from deployment tier)", CommandOptionType.SingleValue);
            awsAccountIdOption.ShowInHelpText = false;
            awsRegionOption.ShowInHelpText = false;
            toolVersionOption.ShowInHelpText = false;
            deploymentBucketNameOption.ShowInHelpText = false;
            deploymentNotificationTopicArnOption.ShowInHelpText = false;
            runtimeVersionOption.ShowInHelpText = false;
            return async () => {

                // initialize logging level
                if(!TryParseEnumOption(verboseLevelOption, Tool.VerboseLevel.Normal, VerboseLevel.Detailed, out Settings.VerboseLevel)) {

                    // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by `TryParseEnumOption`
                    return null;
                }

                // initialize CLI profile
                var toolProfile = toolProfileOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_PROFILE") ?? "Default";

                // initialize deployment tier
                string tier = null;
                if(requireDeploymentTier) {
                    tier = tierOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_TIER");
                    if(string.IsNullOrEmpty(tier)) {
                        AddError("missing deployment tier name");
                    } else if(tier == "Default") {
                        AddError("deployment tier cannot be 'Default' because it is a reserved name");
                    }
                }

                // initialize AWS profile
                try {
                    (string AccountId, string Region)? awsAccount = null;
                    IAmazonSimpleSystemsManagement ssmClient = null;
                    IAmazonCloudFormation cfClient = null;
                    IAmazonKeyManagementService kmsClient = null;
                    IAmazonS3 s3Client = null;
                    if(requireAwsProfile) {
                        awsAccount = await InitializeAwsProfile(
                            awsProfileOption.Value(),
                            awsAccountIdOption.Value(),
                            awsRegionOption.Value()
                        );

                        // create AWS clients
                        ssmClient = new AmazonSimpleSystemsManagementClient();
                        cfClient = new AmazonCloudFormationClient();
                        kmsClient = new AmazonKeyManagementServiceClient();
                        s3Client = new AmazonS3Client();
                    }
                    if(HasErrors) {
                        return null;
                    }

                    // initialize LambdaSharp deployment values
                    var runtimeVersion = runtimeVersionOption.Value();
                    var deploymentBucketName = deploymentBucketNameOption.Value();
                    var deploymentNotificationTopicArn = deploymentNotificationTopicArnOption.Value();

                    // create a settings entry for each module filename
                    return new Settings {
                        ToolVersion = Version,
                        ToolProfile = toolProfile,
                        ToolProfileExplicitlyProvided = toolProfileOption.HasValue(),
                        RuntimeVersion = (runtimeVersion != null) ? VersionInfo.Parse(runtimeVersion) : null,
                        Tier = tier,
                        AwsRegion = awsAccount.GetValueOrDefault().Region,
                        AwsAccountId = awsAccount.GetValueOrDefault().AccountId,
                        DeploymentBucketName = deploymentBucketName,
                        DeploymentNotificationsTopicArn = deploymentNotificationTopicArn,
                        SsmClient = ssmClient,
                        CfClient = cfClient,
                        KmsClient = kmsClient,
                        S3Client = s3Client
                    };
                } catch(AmazonClientException e) when(e.Message == "No RegionEndpoint or ServiceURL configured") {
                    AddError("AWS profile configuration is missing a region specifier");
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

        protected async Task PopulateToolSettingsAsync(Settings settings) {
            if(
                (settings.DeploymentBucketName == null)
                || (settings.DeploymentNotificationsTopicArn == null)
            ) {

                // import LambdaSharp CLI settings
                if(settings.ToolProfile != null) {
                    var lambdaSharpToolPath = $"/LambdaSharpTool/{settings.ToolProfile}/";
                    var lambdaSharpToolSettings = await settings.SsmClient.GetAllParametersByPathAsync(lambdaSharpToolPath);
                    if(!VersionInfo.TryParse(GetLambdaSharpToolSetting("Version"), out VersionInfo lambdaSharpToolVersion)) {
                        AddError("LambdaSharp CLI is not configured propertly", new LambdaSharpToolConfigException(settings.ToolProfile));
                        return;
                    }
                    if((settings.ToolVersion > lambdaSharpToolVersion) && !settings.ToolVersion.IsCompatibleWith(lambdaSharpToolVersion)) {
                        AddError($"LambdaSharp CLI configuration is not up-to-date (current: {settings.ToolVersion}, existing: {lambdaSharpToolVersion})", new LambdaSharpToolConfigException(settings.ToolProfile));
                        return;
                    }
                    settings.DeploymentBucketName = settings.DeploymentBucketName ?? GetLambdaSharpToolSetting("DeploymentBucketName");
                    settings.DeploymentNotificationsTopicArn = settings.DeploymentNotificationsTopicArn ?? GetLambdaSharpToolSetting("DeploymentNotificationTopicArn");

                    // local functions
                    string GetLambdaSharpToolSetting(string name) {
                        lambdaSharpToolSettings.TryGetValue(lambdaSharpToolPath + name, out KeyValuePair<string, string> kv);
                        return kv.Value;
                    }
                }
            }
        }

        protected async Task PopulateRuntimeSettingsAsync(Settings settings) {
            if((settings.RuntimeVersion == null) && (settings.Tier != null)) {
                try {

                    // check version of base LambadSharp module
                    var describe = await settings.CfClient.DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = $"{settings.Tier}-LambdaSharp"
                    });
                    var deployedOutputs = describe.Stacks.FirstOrDefault()?.Outputs;
                    if(deployedOutputs != null) {
                        var deployedName = deployedOutputs.FirstOrDefault(output => output.OutputKey == "ModuleName")?.OutputValue;
                        var deployedVersionText = deployedOutputs.FirstOrDefault(output => output.OutputKey == "ModuleVersion")?.OutputValue;
                        if(
                            (deployedName == "LambdaSharp")
                            && VersionInfo.TryParse(deployedVersionText, out VersionInfo deployedVersion)
                        ) {
                            settings.RuntimeVersion = deployedVersion;
                            return;
                        }
                    }
                } catch(AmazonCloudFormationException) {

                    // stack doesn't exist
                }
            }
        }

        protected string GetGitShaValue(string workingDirectory, bool showWarningOnFailure = true) {

            // read the gitSha using `git` directly
            var process = new Process {
                StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "HEAD" })) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                }
            };
            string gitsha = null;
            try {
                process.Start();
                gitsha = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if(process.ExitCode != 0) {
                    if(showWarningOnFailure) {
                        Console.WriteLine($"WARNING: unable to get git-sha `git rev-parse HEAD` failed with exit code = {process.ExitCode}");
                    }
                    gitsha = null;
                }
            } catch {
                if(showWarningOnFailure) {
                    Console.WriteLine("WARNING: git is not installed; skipping git-sha fingerprint file");
                }
            }
            return gitsha;
        }
    }
}
