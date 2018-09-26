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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliConfigCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("config", cmd => {
                cmd.HelpOption();
                cmd.Description = "Setup LambdaSharp environment";
                var toolProfileOption = cmd.Option("--tool-profile", "", CommandOptionType.SingleValue);
                var updateS3BucketNameOption = cmd.Option("--s3-bucket-name", "", CommandOptionType.SingleValue);
                var updateS3KeyPrefixOption = cmd.Option("--s3-key-prefix", "", CommandOptionType.SingleValue);
                var updateSnsCloudFormationTopicArnOption = cmd.Option("--cloudformation-topic", "", CommandOptionType.SingleValue);

                // command options
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    await Setup(
                        toolProfileOption.Value() ?? "Default",
                        updateS3BucketNameOption.Value(),
                        updateS3KeyPrefixOption.Value(),
                        updateSnsCloudFormationTopicArnOption.Value()
                    );
                });
            });
        }

        private async Task Setup(
            string toolProfile,
            string updateS3BucketName,
            string updateS3KeyPrefix,
            string updatesSnsCloudFormationTopicArn
        ) {
            var cfClient = new AmazonCloudFormationClient();
            var ssmClient = new AmazonSimpleSystemsManagementClient();

            var lambdaSharpToolPath = $"/LambdaSharpTool/{toolProfile}/";
            var lambdaSharpToolSettings = await ssmClient.GetAllParametersByPathAsync(lambdaSharpToolPath);

            if(!lambdaSharpToolSettings.Any()) {

                // TODO: create new stack
                throw new NotImplementedException("MISSING: CREATE NEW STACK");
            } else {
                var existingVersion = GetToolSetting("Version");

                // TODO: compare versions

                // show LambdaSharp settings
                var existingStackName = GetToolSetting("StackName");
                var existingStackId = GetToolSetting("StackId");
                var existingBucketName = GetToolSetting("DeploymentBucketName");
                var existingKeyPrefix = GetToolSetting("DeploymentKeyPrefix");
                var existingCloudFormationTopic = GetToolSetting("DeploymentNotificationTopicArn");
                Console.WriteLine($"LambdaSharp Tool Setting for profile '{toolProfile}'");
                Console.WriteLine($"Tool Version: {existingVersion ?? "<NOT SET>"}");
                Console.WriteLine($"CloudFormation Stack Name: {existingStackName ?? "<NOT SET>"}");
                Console.WriteLine($"CloudFormation Stack ID: {existingStackId ?? "<NOT SET>"}");
                Console.WriteLine($"Module Deployment S3 Bucket Name: {existingBucketName ?? "<NOT SET>"}");
                Console.WriteLine($"Module Deployment S3 Key Prefix: {existingKeyPrefix ?? "<NOT SET>"}");
                Console.WriteLine($"CloudFormation Deployment Notifications: {existingCloudFormationTopic ?? "<NOT SET>"}");
            }

            /* Flow
                1) Check if config exists
                    No => goto CreateStack
                    Yes => show settings
                2) Check if config version is newer
                    Yes => can't downgrade
                3) Check if config version is older
                    Yes => ask if upgrade stack and update
                4) Ask if settings should be updated

                CreateStack:
                    1) Ask for tool settings
                    2) Create stack
             */

            // // describe stack
            // var stackParameters = await FetchExistingParameters();
            // var stackProfile = GetExistingParameter("LambdaSharpToolProfile") ?? "Default";
            // var stackVersion = GetExistingParameter("LambdaSharpToolVersion") ?? "none";
            // var stackBucketName = GetExistingParameter("DeploymentBucketName");
            // var stackKeyPrefix = GetExistingParameter("DeploymentKeyPrefix");
            // var stackCloudFormationTopic = GetExistingParameter("DeploymentNotificationTopicArn");

            // Console.WriteLine($"Current LambdaSharp Tool Profile: {stackProfile}");
            // Console.WriteLine($"Current LambdaSharp Tool Version: {stackVersion}");

            // // TODO: re-prompt parameters

            // updateS3BucketName = Prompt.GetString("S3 bucket name to use for module deployments:", stackBucketName);
            // updateS3KeyPrefix = Prompt.GetString("S3 key prefix to use for module deployments:", stackKeyPrefix);
            // updatesSnsCloudFormationTopicArn = Prompt.GetString("SNS topic ARN to use for CloudFormation notifications:", stackCloudFormationTopic);

            // Console.WriteLine();
            // Console.WriteLine($"=> S3 Bucket: {updateS3BucketName}");
            // Console.WriteLine($"=> S3 Key Prefix: {updateS3KeyPrefix}");
            // Console.WriteLine($"=> CF Notifications: {updatesSnsCloudFormationTopicArn}");

            // local functions
            // async Task<Dictionary<string, string>> FetchExistingParameters() {
            //     var result = new Dictionary<string, string>();
            //     try {
            //         var describeStackResponse = await cfClient.DescribeStacksAsync(new DescribeStacksRequest {
            //             StackName = stackName
            //         });
            //         var stack = describeStackResponse.Stacks.First();
            //         foreach(var parameter in stack.Parameters) {
            //             result[parameter.ParameterKey] = parameter.ParameterValue;
            //         }
            //     } catch { }
            //     return result;
            // }

            string GetToolSetting(string name) {
                lambdaSharpToolSettings.TryGetValue(lambdaSharpToolPath + name, out KeyValuePair<string, string> kv);
                return kv.Value;
            }

            // string GetExistingParameter(string key) => stackParameters.TryGetValue(key, out string value) ? value : null;
        }
    }
}
