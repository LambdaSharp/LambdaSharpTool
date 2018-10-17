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
                cmd.Description = "Configure LambdaSharp environment";
                var toolProfileOption = cmd.Option("--tool-profile <NAME>", "(optional) Profile name for LambdaSharp tool (default: 'Default')", CommandOptionType.SingleValue);
                var moduleS3BucketNameOption = cmd.Option("--module-s3-bucket-name <>", "(optional) Existing S3 bucket name for module deployments", CommandOptionType.SingleValue);
                var moduleS3BucketPathOption = cmd.Option("--module-s3-bucket-path", "(optional) S3 bucket path for module deployments (default: 'Modules/')", CommandOptionType.SingleValue);
                var cloudFormationNotificationsTopicArnOption = cmd.Option("--cloudformation-notifications-topic", "(optional) Existing SNS topic ARN for CloudFormation notifications ", CommandOptionType.SingleValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var toolProfileArgument = cmd.Argument("<NAME>", "(optional) Profile name for LambdaSharp tool (default: 'Default')");

                // command options
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    await Setup(
                        toolProfileOption.Value() ?? toolProfileArgument.Values.FirstOrDefault(),
                        moduleS3BucketNameOption.Value(),
                        moduleS3BucketPathOption.Value(),
                        cloudFormationNotificationsTopicArnOption.Value(),
                        protectStackOption.HasValue(),
                        new AmazonCloudFormationClient(),
                        new AmazonSimpleSystemsManagementClient()
                    );
                });
            });
        }

        private async Task Setup(
            string toolProfile,
            string moduleS3BucketName,
            string moduleS3BucketPath,
            string cloudFormationNotificationsTopicArn,
            bool protectStack,
            IAmazonCloudFormation cfClient,
            IAmazonSimpleSystemsManagement ssmClient
        ) {
            var template = ReadResource("LambdaSharpToolConfig.yml", new Dictionary<string, string> {
                ["VERSION"] = Version.ToString()
            });

            // try to read tool settings
            var assumeToolProfile = toolProfile ?? Environment.GetEnvironmentVariable("LAMBDASHARP_PROFILE") ?? "Default";
            var lambdaSharpToolPath = $"/LambdaSharpTool/{assumeToolProfile}/";
            var lambdaSharpToolSettings = await ssmClient.GetAllParametersByPathAsync(lambdaSharpToolPath);
            if(!lambdaSharpToolSettings.Any()) {
                Console.WriteLine($"Configuring a new profile for LambdaSharp tool");

                // if no tool profile name is specified, go into interactive mode to request tool configuration parameters
                if(toolProfile == null) {

                    // prompt for values
                    toolProfile = Prompt.GetString("Profile name:", "Default");
                    moduleS3BucketName = Prompt.GetString("Existing S3 bucket name for module deployments (empty value creates new bucket):") ?? "";
                    moduleS3BucketPath = Prompt.GetString("S3 bucket path for module deployments:", "Modules/");
                    cloudFormationNotificationsTopicArn = Prompt.GetString("Existing SNS topic ARN for CloudFormation notifications (empty value creates new bucket):") ?? "";
                } else {

                    // set defaults for missing values
                    moduleS3BucketName = moduleS3BucketName ?? "";
                    moduleS3BucketPath = moduleS3BucketPath ?? "Modules/";
                    cloudFormationNotificationsTopicArn = cloudFormationNotificationsTopicArn ?? "";
                }

                // create lambdasharp tool resources stack
                var stackName = $"LambdaSharpTool-{toolProfile}";
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
                            ParameterKey = "DeploymentBucketPath",
                            ParameterValue = moduleS3BucketPath
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
                            ParameterValue = toolProfile
                        }
                    },
                    EnableTerminationProtection = protectStack,
                    TemplateBody = template
                };
                var response = await cfClient.CreateStackAsync(request);
                var outcome = await cfClient.TrackStackUpdateAsync(response.StackId, mostRecentStackEventId: null);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack creation finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                } else {
                    Console.WriteLine($"=> Stack creation FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                }
            } else {
                toolProfile = assumeToolProfile;
                if(!VersionInfo.TryParse(GetToolSetting("Version"), out VersionInfo existingVersion)) {
                    AddError("unable to parse existing version");
                    return;
                }
                if(existingVersion.CompareTo(Version) == VersionInfoCompare.Older) {

                    // TODO (2018-10-09, bjorg): logic for upgrading lambdasharp tool
                    AddError("upgrading is not yet supported");
                    return;
                }
                if(existingVersion.CompareTo(Version) == VersionInfoCompare.Newer) {
                    Console.WriteLine();
                    Console.WriteLine($"WARNING: LambdaSharp tool configuration is more recent (v{existingVersion})");
                    return;
                }
                try {
                    var stackName = $"LambdaSharpTool-{toolProfile}";
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
                                ParameterKey = "DeploymentBucketPath",
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
                    var outcome = await cfClient.TrackStackUpdateAsync(response.StackId, mostRecentStackEventId);
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
