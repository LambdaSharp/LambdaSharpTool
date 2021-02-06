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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.APIGateway.Model;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Model;
using LambdaSharp.Build;
using LambdaSharp.Build.CSharp;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {

    public class CliInfoCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("info", cmd => {
                cmd.HelpOption();
                cmd.Description = "Show LambdaSharp information";

                // info options
                var showSensitiveInformationOption = cmd.Option("--show-sensitive", "(optional) Show sensitive information", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                AddStandardCommandOptions(cmd);
                cmd.OnExecute(async () => {
                    ExecuteCommandActions(cmd);
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    await Info(
                        settings,
                        new GitTool(BuildEventsConfig).GetGitShaValue(Directory.GetCurrentDirectory(), showWarningOnFailure: false),
                        new GitTool(BuildEventsConfig).GetGitBranch(Directory.GetCurrentDirectory(), showWarningOnFailure: false),
                        showSensitiveInformationOption.HasValue()
                    );
                });
            });
        }

        public async Task Info(
            Settings settings,
            string gitSha,
            string gitBranch,
            bool showSensitive
        ) {
            await PopulateDeploymentTierSettingsAsync(settings, optional: true);
            var apiGatewayAccount = await DetermineMissingApiGatewayRolePolicies(settings);
            var lambdaAccount = await GetLambdaAccountDetails(settings);

            // show LambdaSharp settings
            Console.WriteLine($"LambdaSharp Deployment Tier");
            Console.WriteLine($"    Name: {settings.TierName}");
            Console.WriteLine($"    Version: {settings.TierVersion?.ToString() ?? "<NOT SET>"}");
            Console.WriteLine($"    Core Services: {((settings.CoreServices != CoreServices.Undefined) ? settings.CoreServices.ToString() : "<NOT SET>")}");
            Console.WriteLine($"    Deployment S3 Bucket: {settings.DeploymentBucketName ?? "<NOT SET>"}");
            Console.WriteLine($"    Logging S3 Bucket: {settings.LoggingBucketName ?? "<NOT SET>"}");
            if(apiGatewayAccount.Arn == null) {
                Console.WriteLine($"    API Gateway Role: <NOT SET>");
            } else if(!apiGatewayAccount.MissingPolicies.Any()) {
                Console.WriteLine($"    API Gateway Role: {ConcealAwsAccountId(apiGatewayAccount.Arn)}");
            } else {
                Console.WriteLine($"    API Gateway Role: <INCOMPLETE> {ConcealAwsAccountId(apiGatewayAccount.Arn)}");
                LogWarn($"API Gateway role is incomplete (missing: {string.Join(", ", apiGatewayAccount.MissingPolicies)})");
            }
            Console.WriteLine($"Git");
            Console.WriteLine($"    Branch: {gitBranch ?? "<NOT SET>"}");
            Console.WriteLine($"    SHA: {gitSha ?? "<NOT SET>"}");
            Console.WriteLine($"AWS");
            Console.WriteLine($"    Region: {settings.AwsRegion ?? "<NOT SET>"}");
            Console.WriteLine($"    Account Id: {ConcealAwsAccountId(settings.AwsAccountId ?? "<NOT SET>")}");
            if(lambdaAccount != null) {
                const long gigabyte = 1024L * 1024L * 1024L;
                Console.WriteLine($"    Lambda Storage: {lambdaAccount.AccountUsage.TotalCodeSize / (float)gigabyte:G2}GB of {lambdaAccount.AccountLimit.TotalCodeSize / (float)gigabyte:G2}GB ({lambdaAccount.AccountUsage.TotalCodeSize / (float)lambdaAccount.AccountLimit.TotalCodeSize:P2})");
                Console.WriteLine($"    Lambda Reserved Executions: {lambdaAccount.AccountLimit.ConcurrentExecutions - lambdaAccount.AccountLimit.UnreservedConcurrentExecutions:N0} of {lambdaAccount.AccountLimit.ConcurrentExecutions:N0} ({(lambdaAccount.AccountLimit.ConcurrentExecutions - lambdaAccount.AccountLimit.UnreservedConcurrentExecutions) / (float)lambdaAccount.AccountLimit.ConcurrentExecutions:P2})");
            } else {
                Console.WriteLine($"    Lambda Storage: N/A");
                Console.WriteLine($"    Lambda Reserved Executions: N/A");
            }
            Console.WriteLine($"Tools");
            Console.WriteLine($"    .NET Core CLI Version: {new DotNetTool(BuildEventsConfig).GetDotNetVersion() ?? "<NOT FOUND>"}");
            Console.WriteLine($"    Git CLI Version: {new GitTool(BuildEventsConfig).GetGitVersion() ?? "<NOT FOUND>"}");
            Console.WriteLine($"    Amazon.Lambda.Tools: {new AmazonLambdaTool(BuildEventsConfig).GetAmazonLambdaToolVersion() ?? "<NOT FOUND>"}");
            Console.WriteLine($"    ReadyToRun Compilation: {(Settings.IsAmazonLinux2() ? "Yes" : "No")}");

            // local functions
            string ConcealAwsAccountId(string text) {
                if(showSensitive || (settings.AwsAccountId == null)) {
                    return text;
                }
                return text.Replace(settings.AwsAccountId, new string('*', settings.AwsAccountId.Length));
            }
        }

        private async Task<(string Arn, IEnumerable<string> MissingPolicies)> DetermineMissingApiGatewayRolePolicies(Settings settings) {
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

        private async Task<GetAccountSettingsResponse> GetLambdaAccountDetails(Settings settings) {
            if(settings.LambdaClient == null) {
                return null;
            }
            try {
                var account = await settings.LambdaClient.GetAccountSettingsAsync(new GetAccountSettingsRequest { });
                return account;
            } catch(Exception) {
                return null;
            }
        }
    }
}
