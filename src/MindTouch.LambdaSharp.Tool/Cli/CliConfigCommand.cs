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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliConfigCommand : ACliCommand {

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

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("config", cmd => {
                cmd.HelpOption();
                cmd.Description = "Configure LambdaSharp CLI";

                // tool options
                var moduleS3BucketNameOption = cmd.Option("--module-s3-bucket-name <NAME>", "(optional) Existing S3 bucket name for module deployments", CommandOptionType.SingleValue);
                var cloudFormationNotificationsTopicArnOption = cmd.Option("--cloudformation-notifications-topic <ARN>", "(optional) Existing SNS topic ARN for CloudFormation notifications ", CommandOptionType.SingleValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var forceUpdateOption = cmd.Option("--force-update", "(optional) Force CLI profile update", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd, requireDeploymentTier: false);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    await Config(
                        settings,
                        moduleS3BucketNameOption.Value(),
                        cloudFormationNotificationsTopicArnOption.Value(),
                        protectStackOption.HasValue(),
                        forceUpdateOption.HasValue(),
                        new AmazonCloudFormationClient(),
                        new AmazonSimpleSystemsManagementClient()
                    );
                });
            });
        }

        private async Task Config(
            Settings settings,
            string moduleS3BucketName,
            string cloudFormationNotificationsTopicArn,
            bool protectStack,
            bool forceUpdate,
            IAmazonCloudFormation cfClient,
            IAmazonSimpleSystemsManagement ssmClient
        ) {
            var template = ReadResource("LambdaSharpToolConfig.yml", new Dictionary<string, string> {
                ["VERSION"] = Version.ToString()
            });

            // try to read CLI settings
            var lambdaSharpToolPath = $"/LambdaSharpTool/{settings.ToolProfile}/";
            var lambdaSharpToolSettings = await ssmClient.GetAllParametersByPathAsync(lambdaSharpToolPath);
            if(!lambdaSharpToolSettings.Any()) {
                Console.WriteLine($"Configuring a new profile for LambdaSharp CLI");

                // prompt for missing values
                if(settings.ToolProfileExplicitlyProvided) {
                    Console.WriteLine($"Creating CLI profile: {settings.ToolProfile}");
                } else {
                    settings.ToolProfile = Prompt.GetString("CLI profile name:", settings.ToolProfile);
                }
                if(moduleS3BucketName == "") {
                    Console.WriteLine($"Creating new S3 bucket");
                } else if(moduleS3BucketName != null) {
                    Console.WriteLine($"Using existing S3 bucket name: {moduleS3BucketName}");
                } else {
                    moduleS3BucketName = Prompt.GetString("Existing S3 bucket name for module deployments (blank value creates new bucket):") ?? "";
                }
                if(cloudFormationNotificationsTopicArn == "") {
                    Console.WriteLine($"Creating new SNS topic for CloudFormation notifications");
                } else if(cloudFormationNotificationsTopicArn != null) {
                    Console.WriteLine($"SNS topic ARN for CloudFormation notifications: {cloudFormationNotificationsTopicArn}");
                } else {
                    cloudFormationNotificationsTopicArn = Prompt.GetString("Existing SNS topic ARN for CloudFormation notifications (empty value creates new topic):") ?? "";
                }

                // create lambdasharp CLI resources stack
                var stackName = $"LambdaSharpTool-{settings.ToolProfile}";
                Console.WriteLine($"=> Stack creation initiated for {stackName}");
                var request = new CreateStackRequest {
                    StackName = stackName,
                    Capabilities = new List<string> {
                        "CAPABILITY_NAMED_IAM"
                    },
                    OnFailure = OnFailure.DELETE,
                    Parameters = new List<Parameter> {
                        new Parameter {
                            ParameterKey = "DeploymentBucketName",
                            ParameterValue = moduleS3BucketName
                        },
                        new Parameter {
                            ParameterKey = "DeploymentNotificationTopicArn",
                            ParameterValue = cloudFormationNotificationsTopicArn
                        },
                        new Parameter {
                            ParameterKey = "LambdaSharpToolVersion",
                            ParameterValue = Version.ToString()
                        },
                        new Parameter {
                            ParameterKey = "LambdaSharpToolProfile",
                            ParameterValue = settings.ToolProfile
                        }
                    },
                    EnableTerminationProtection = protectStack,
                    TemplateBody = template
                };
                var response = await cfClient.CreateStackAsync(request);
                var outcome = await cfClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId: null);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack creation finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                } else {
                    Console.WriteLine($"=> Stack creation FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                }
            } else {

                // check if exiting profile needs to be upgraded
                if(!forceUpdate) {
                    if(!VersionInfo.TryParse(GetToolSetting("Version"), out VersionInfo existingVersion)) {
                        AddError("unable to parse existing version; use --force-update to proceed anyway");
                        return;
                    }
                    if(existingVersion < Version) {
                        Console.WriteLine($"LambdaSharp CLI configuration appears to be out of date: (v{existingVersion})");
                        var upgrade = Prompt.GetYesNo("Do you want to upgrade?", false);
                        if(!upgrade) {
                            return;
                        }
                    } else if(!existingVersion.IsCompatibleWith(Version)) {
                        Console.WriteLine();
                        Console.WriteLine($"WARNING: LambdaSharp CLI is not compatible with v{existingVersion}; use --force-update to proceed anyway");
                        return;
                    }
                }
                try {
                    var stackName = $"LambdaSharpTool-{settings.ToolProfile}";
                    Console.WriteLine($"=> Stack update initiated for {stackName}");
                    var mostRecentStackEventId = await cfClient.GetMostRecentStackEventIdAsync(stackName);
                    var request = new UpdateStackRequest {
                        StackName = stackName,
                        Capabilities = new List<string> {
                            "CAPABILITY_NAMED_IAM"
                        },
                        Parameters = new List<Parameter> {
                            new Parameter {
                                ParameterKey = "DeploymentBucketName",
                                UsePreviousValue = true
                            },
                            new Parameter {
                                ParameterKey = "DeploymentNotificationTopicArn",
                                UsePreviousValue = true
                            },
                            new Parameter {
                                ParameterKey = "LambdaSharpToolVersion",
                                ParameterValue = Version.ToString()
                            },
                            new Parameter {
                                ParameterKey = "LambdaSharpToolProfile",
                                UsePreviousValue = true
                            }
                        },
                        TemplateBody = template
                    };
                    var response = await cfClient.UpdateStackAsync(request);
                    var outcome = await cfClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId);
                    if(outcome.Success) {
                        Console.WriteLine($"=> Stack update finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    } else {
                        Console.WriteLine($"=> Stack update FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    }
                } catch(AmazonCloudFormationException e) when(e.Message == "No updates are to be performed.") {

                    // this error is thrown when no required updates where found
                    Console.WriteLine($"=> No stack update required (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                }
            }

            // local functions
            string GetToolSetting(string name) {
                lambdaSharpToolSettings.TryGetValue(lambdaSharpToolPath + name, out KeyValuePair<string, string> kv);
                return kv.Value;
            }
        }
    }
}
