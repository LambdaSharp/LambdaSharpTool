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

        protected Func<Task<Settings>> CreateSettingsInitializer(CommandLineApplication cmd, bool requireAwsProfile = true) {

            // add misc options
            var awsProfileOption = cmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
            var toolProfileOption = cmd.Option("--tool-profile|-T <NAME>", "(optional) Use a specific LambdaSharp tool profile (default: Default)", CommandOptionType.SingleValue);
            var verboseLevelOption = cmd.Option("--verbose|-V:<LEVEL>", "(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)", CommandOptionType.SingleOrNoValue);

            // add hidden testing options
            var awsAccountIdOption = cmd.Option("--aws-account-id <VALUE>", "(test only) Override AWS account Id (default: read from AWS profile)", CommandOptionType.SingleValue);
            var awsRegionOption = cmd.Option("--aws-region <NAME>", "(test only) Override AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);
            var toolVersionOption = cmd.Option("--tool-version <VALUE>", "(test only) LambdaSharp tool version for profile (default: read from LambdaSharp tool configuration)", CommandOptionType.SingleValue);
            var deploymentBucketNameOption = cmd.Option("--deployment-bucket-name <NAME>", "(test only) S3 Bucket name used to deploy modules (default: read from LambdaSharp tool configuration)", CommandOptionType.SingleValue);
            var deploymentBucketPathOption = cmd.Option("--deployment-bucket-path <NAME>", "(test only) S3 Bucket path used to deploy modules (default: read from LambdaSharp tool configuration)", CommandOptionType.SingleValue);
            var deploymentNotificationTopicArnOption = cmd.Option("--deployment-notifications-topic-arn <ARN>", "(test only) SNS Topic for CloudFormation deployment notifications (default: read from LambdaSharp tool configuration)", CommandOptionType.SingleValue);
            var environmentVersionOption = cmd.Option("--environment-version <VERSION>", "(test only) LambdaSharp environment version for deployment tier (default: read from LambdaSharp environment configuration)", CommandOptionType.SingleValue);
            awsAccountIdOption.ShowInHelpText = false;
            awsRegionOption.ShowInHelpText = false;
            toolVersionOption.ShowInHelpText = false;
            deploymentBucketNameOption.ShowInHelpText = false;
            deploymentBucketPathOption.ShowInHelpText = false;
            deploymentNotificationTopicArnOption.ShowInHelpText = false;
            environmentVersionOption.ShowInHelpText = false;
            return async () => {

                // initialize logging level
                if(verboseLevelOption.HasValue()) {
                    if(!TryParseEnumOption(verboseLevelOption, VerboseLevel.Detailed, out Settings.VerboseLevel)) {

                        // NOTE (2018-08-04, bjorg): no need to add an error message since it's already added by `TryParseEnumOption`
                        return null;
                    }
                }

                // initialize tool profile
                var toolProfile = toolProfileOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_PROFILE");

                // initialize AWS profile
                (string AccountId, string Region)? awsAccount = null;
                if(requireAwsProfile) {
                    awsAccount = await InitializeAwsProfile(
                        awsProfileOption.Value(),
                        awsAccountIdOption.Value(),
                        awsRegionOption.Value()
                    );
                    if(awsAccount == null) {

                        // NOTE (2018-08-15, bjorg): no need to add an error message since it's already added by `InitializeAwsProfile`
                        return null;
                    }
                }

                // create AWS clients
                var ssmClient = new AmazonSimpleSystemsManagementClient();
                var cfClient = new AmazonCloudFormationClient();
                var kmsClient = new AmazonKeyManagementServiceClient();
                var s3Client = new AmazonS3Client();

                // initialize LambdaSharp deployment values
                var environmentVersion = environmentVersionOption.Value();
                var deploymentBucketName = deploymentBucketNameOption.Value();
                var deploymentBucketPath = deploymentBucketPathOption.Value();
                var deploymentNotificationTopicArn = deploymentNotificationTopicArnOption.Value();

                // create a settings entry for each module filename
                return new Settings {
                    ToolVersion = Version,
                    ToolProfile = toolProfile,
                    EnvironmentVersion = (environmentVersion != null) ? VersionInfo.Parse(environmentVersion) : null,
                    AwsRegion = awsAccount.GetValueOrDefault().Region,
                    AwsAccountId = awsAccount.GetValueOrDefault().AccountId,
                    DeploymentBucketName = deploymentBucketName,
                    DeploymentBucketPath = deploymentBucketPath,
                    DeploymentNotificationsTopicArn = deploymentNotificationTopicArn,
                    ResourceMapping = new ResourceMapping(),
                    SsmClient = ssmClient,
                    CfClient = cfClient,
                    KmsClient = kmsClient,
                    S3Client = s3Client
                };
            };
        }

        protected bool TryParseEnumOption<T>(CommandOption option, T defaultvalue, out T result) where T : struct {
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
                || (settings.DeploymentBucketPath == null)
                || (settings.DeploymentNotificationsTopicArn == null)
            ) {

                // import LambdaSharpTool settings
                if(settings.ToolProfile != null) {
                    var lambdaSharpToolPath = $"/LambdaSharpTool/{settings.ToolProfile}/";
                    var lambdaSharpToolSettings = await settings.SsmClient.GetAllParametersByPathAsync(lambdaSharpToolPath);
                    if(!VersionInfo.TryParse(GetLambdaSharpToolSetting("Version"), out VersionInfo lambdaSharpToolVersion)) {
                        AddError("LambdaSharp tool is not configured propertly", new LambdaSharpToolConfigException(settings.ToolProfile));
                        return;
                    }
                    if(lambdaSharpToolVersion < settings.ToolVersion) {
                        AddError($"LambdaSharp tool configuration is not up-to-date (current: {settings.ToolVersion}, existing: {lambdaSharpToolVersion})", new LambdaSharpToolConfigException(settings.ToolProfile));
                        return;
                    }
                    settings.DeploymentBucketName = settings.DeploymentBucketName ?? GetLambdaSharpToolSetting("DeploymentBucketName");
                    settings.DeploymentBucketPath = settings.DeploymentBucketPath ?? GetLambdaSharpToolSetting("DeploymentBucketPath");
                    settings.DeploymentNotificationsTopicArn = settings.DeploymentNotificationsTopicArn ?? GetLambdaSharpToolSetting("DeploymentNotificationTopicArn");

                    // local functions
                    string GetLambdaSharpToolSetting(string name) {
                        lambdaSharpToolSettings.TryGetValue(lambdaSharpToolPath + name, out KeyValuePair<string, string> kv);
                        return kv.Value;
                    }
                }
            }
        }

        protected async Task PopulateEnvironmentSettingsAsync(Settings settings, string tier) {
            if(settings.EnvironmentVersion == null) {

                // import LambdaSharp settings
                if(tier != null) {
                    var lambdaSharpPath = $"/{tier}/LambdaSharp/";
                    var lambdaSharpSettings = await settings.SsmClient.GetAllParametersByPathAsync(lambdaSharpPath);

                    // resolved values that are not yet set
                    if(settings.EnvironmentVersion == null) {
                        var environmentVersion = GetLambdaSharpSetting("Version");
                        if(environmentVersion != null) {
                            settings.EnvironmentVersion = VersionInfo.Parse(environmentVersion);
                        }
                    }

                    // local functions
                    string GetLambdaSharpSetting(string name) {
                        lambdaSharpSettings.TryGetValue(lambdaSharpPath + name, out KeyValuePair<string, string> kv);
                        return kv.Value;
                    }
                }
            }
        }

        protected string GetGitShaValue(string workingDirectory) {

            // read the gitSha using `git` directly
            var process = new Process {
                StartInfo = new ProcessStartInfo("git", ArgumentEscaper.EscapeAndConcatenate(new[] { "rev-parse", "HEAD" })) {
                    RedirectStandardOutput = true,
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
                    Console.WriteLine($"WARNING: unable to get git-sha `git rev-parse HEAD` failed with exit code = {process.ExitCode}");
                    gitsha = null;
                }
            } catch {
                Console.WriteLine("WARNING: git is not installed; skipping git-sha fingerprint file");
            }
            return gitsha;
        }
    }
}
