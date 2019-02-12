/*
 * MindTouch λ#
 * Copyright (C) 2006-2018-2019 MindTouch, Inc.
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
using LambdaSharp.Tool.Internal;

namespace LambdaSharp.Tool.Cli {

    public class CliConfigCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("config", cmd => {
                cmd.HelpOption();
                cmd.Description = "Configure LambdaSharp CLI";

                // tool options
                var existingS3BucketNameOption = cmd.Option("--existing-s3-bucket-name <NAME>", "(optional) Existing S3 bucket name for module deployments (blank value creates new bucket)", CommandOptionType.SingleValue);
                var requestedS3BucketNameOption = cmd.Option("--requested-s3-bucket-name <NAME>", "(optional) Requested S3 bucket name for module deployments (blank value assigns automatic name)", CommandOptionType.SingleValue);
                var cloudFormationNotificationsTopicOption = cmd.Option("--cloudformation-notifications-topic <ARN>", "(optional) Existing SNS topic ARN for CloudFormation notifications (blank value creates new topic)", CommandOptionType.SingleValue);
                var protectStackOption = cmd.Option("--protect", "(optional) Enable termination protection for the CloudFormation stack", CommandOptionType.NoValue);
                var forceUpdateOption = cmd.Option("--force-update", "(optional) Force CLI profile update", CommandOptionType.NoValue);
                var initSettingsCallback = CreateSettingsInitializer(cmd, requireDeploymentTier: false);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // check which parameters were provided
                    var parameters = new Dictionary<string, string> {
                        ["LambdaSharpToolVersion"] = Version.ToString(),
                        ["LambdaSharpToolProfile"] = settings.ToolProfile
                    };
                    if(existingS3BucketNameOption.HasValue()) {
                        parameters.Add("DeploymentBucketName", existingS3BucketNameOption.Value());
                    }
                    if(requestedS3BucketNameOption.HasValue()) {
                        parameters.Add("RequestedBucketName", requestedS3BucketNameOption.Value());
                    }
                    if(cloudFormationNotificationsTopicOption.HasValue()) {
                        parameters.Add("DeploymentNotificationTopic", cloudFormationNotificationsTopicOption.Value());
                    }
                    await Config(
                        settings,
                        parameters,
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
            IDictionary<string, string> parameters,
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

                // prompt for CLI profile name
                if(settings.ToolProfileExplicitlyProvided) {
                    Console.WriteLine($"Creating CLI profile: {settings.ToolProfile}");
                } else {

                    // confirm that the implicit profile is the desired profile
                    settings.ToolProfile = Prompt.GetString("|=> CLI profile name:", settings.ToolProfile);
                    parameters["LambdaSharpToolProfile"] = settings.ToolProfile;
                }

                // create lambdasharp CLI resources stack
                var stackName = $"LambdaSharpTool-{settings.ToolProfile}";
                var templateParameters = await PromptMissingTemplateParameters(cfClient, stackName, parameters, template);
                Console.WriteLine($"=> Stack creation initiated for {stackName}");
                if(templateParameters == null) {
                    return;
                }
                var request = new CreateStackRequest {
                    StackName = stackName,
                    Capabilities = new List<string> { },
                    OnFailure = OnFailure.DELETE,
                    Parameters = templateParameters,
                    EnableTerminationProtection = protectStack,
                    TemplateBody = template
                };
                var response = await cfClient.CreateStackAsync(request);
                var outcome = await cfClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId: null);
                if(outcome.Success) {
                    Console.WriteLine("=> Stack creation finished");
                } else {
                    Console.WriteLine("=> Stack creation FAILED");
                }
            } else {
                Console.WriteLine($"Updating CLI profile: {settings.ToolProfile}");

                // check if exiting profile needs to be upgraded
                if(!forceUpdate) {
                    if(!VersionInfo.TryParse(GetToolSetting("Version"), out var existingVersion)) {
                        AddError("unable to parse existing version; use --force-update to proceed anyway");
                        return;
                    }
                    if(existingVersion < Version) {
                        Console.WriteLine($"LambdaSharp CLI configuration appears to be out of date");
                        var upgrade = Prompt.GetYesNo($"|=> Do you want to upgrade LambdaSharp CLI profile for '{settings.ToolProfile}' from v{existingVersion} to v{Version}?", false);
                        if(!upgrade) {
                            return;
                        }
                    } else if(!existingVersion.IsCompatibleWith(Version)) {
                        AddError($"LambdaSharp CLI is not compatible with v{existingVersion}; use --force-update to proceed anyway");
                        return;
                    }
                }
                try {

                    // update lambdasharp CLI resources stack
                    var stackName = $"LambdaSharpTool-{settings.ToolProfile}";
                    var templateParameters = await PromptMissingTemplateParameters(cfClient, stackName, parameters, template);
                    Console.WriteLine($"=> Stack update initiated for {stackName}");
                    if(templateParameters == null) {
                        return;
                    }
                    var request = new UpdateStackRequest {
                        StackName = stackName,
                        Capabilities = new List<string> { },
                        Parameters = templateParameters,
                        TemplateBody = template
                    };
                    var mostRecentStackEventId = await cfClient.GetMostRecentStackEventIdAsync(stackName);
                    var response = await cfClient.UpdateStackAsync(request);
                    var outcome = await cfClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId);
                    if(outcome.Success) {
                        Console.WriteLine("=> Stack update finished");
                    } else {
                        Console.WriteLine("=> Stack update FAILED");
                    }
                } catch(AmazonCloudFormationException e) when(e.Message == "No updates are to be performed.") {

                    // this error is thrown when no required updates where found
                    Console.WriteLine("=> No stack update required");
                }
            }

            // local functions
            string GetToolSetting(string name) {
                lambdaSharpToolSettings.TryGetValue(lambdaSharpToolPath + name, out var kv);
                return kv.Value;
            }
        }

        public async Task<List<Parameter>> PromptMissingTemplateParameters(
            IAmazonCloudFormation cfClient,
            string stackName,
            IDictionary<string, string> providedParameters,
            string templateBody
        ) {

            // get summary of new template
            GetTemplateSummaryResponse templateSummary;
            try {
                templateSummary = await cfClient.GetTemplateSummaryAsync(new GetTemplateSummaryRequest {
                    TemplateBody = templateBody
                });
            } catch(AmazonCloudFormationException e) {
                AddError(e.Message);
                return null;
            }

            // find configuration for existing stack
            Stack existing = null;
            if(stackName != null) {
                try {
                    existing = (await cfClient.DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = stackName
                    })).Stacks.First();
                } catch(AmazonCloudFormationException) { }
            }
            var result = new List<Parameter>();
            var missingParameters = new List<ParameterDeclaration>();
            foreach(var templateParameter in templateSummary.Parameters) {
                if(providedParameters.TryGetValue(templateParameter.ParameterKey, out var providedValue)) {

                    // use the provided parameter value
                    result.Add(new Parameter {
                        ParameterKey = templateParameter.ParameterKey,
                        ParameterValue = providedValue
                    });
                } else if(existing?.Parameters.Any(existingParam => existingParam.ParameterKey == templateParameter.ParameterKey) == true) {

                    // re-use the existing parameter value
                    result.Add(new Parameter {
                        ParameterKey = templateParameter.ParameterKey,
                        UsePreviousValue = true
                    });
                } else {

                    // add parameter to missing parameters
                    missingParameters.Add(templateParameter);
                }
            }

            // ask user for missing values
            if(missingParameters.Any()) {
                Console.WriteLine();
                Console.WriteLine($"Configuring {templateSummary.Description} Parameters");
                foreach(var missingParameter in missingParameters) {
                    var enteredValue = Prompt.GetString($"|=> {missingParameter.Description ?? missingParameter.ParameterKey}:", missingParameter.DefaultValue) ?? "";
                    result.Add(new Parameter {
                        ParameterKey = missingParameter.ParameterKey,
                        ParameterValue = enteredValue
                    });
                }
                Console.WriteLine();
            }

            // report any parameters that were provided, but are not needed
            foreach(var providedParameter in providedParameters) {
                if(!templateSummary.Parameters.Any(expectedParam => expectedParam.ParameterKey == providedParameter.Key)) {
                    AddError($"unexpected module parameter '{providedParameter.Key}'");
                }
            }
            if(HasErrors) {
                return null;
            }

            // return the collected paramaters
            return result;
        }
    }
}
