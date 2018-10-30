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
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3.Transfer;
using MindTouch.LambdaSharp.Tool.Model;
using MindTouch.LambdaSharp.Tool.Internal;
using Newtonsoft.Json;
using Amazon.S3.Model;

namespace MindTouch.LambdaSharp.Tool {
    using CloudFormationStack = Amazon.CloudFormation.Model.Stack;
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class ModelUpdater : AModelProcessor {

        //--- Constructors ---
        public ModelUpdater(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public async Task<bool> DeployAsync(
            ModuleManifest manifest,
            string altModuleName,
            bool allowDataLoss,
            bool protectStack,
            Dictionary<string, string> inputs,
            bool forceDeploy
        ) {
            var stackName = $"{Settings.Tier}-{altModuleName ?? manifest.ModuleName}";

            // check if cloudformation stack already exists and is in a final state
            var mostRecentStackEventId = await Settings.CfClient.GetMostRecentStackEventIdAsync(stackName);

            // check version of previously deployed module
            if(!forceDeploy) {
                try {
                    var describe = await Settings.CfClient.DescribeStacksAsync(new DescribeStacksRequest {
                        StackName = stackName
                    });
                    var deployedOutputs = describe.Stacks.FirstOrDefault()?.Outputs;
                    if(deployedOutputs != null) {
                        var deployedName = deployedOutputs.FirstOrDefault(output => output.OutputKey == "ModuleName")?.OutputValue;
                        var deployedVersionText = deployedOutputs.FirstOrDefault(output => output.OutputKey == "ModuleVersion")?.OutputValue;
                        if(deployedName == null) {
                            AddError("unable to determine the deployed module name; use --force-deploy to proceed anyway");
                            return false;
                        }
                        if(deployedName != manifest.ModuleName) {
                            AddError($"deployed module name ({deployedName}) does not match {manifest.ModuleName}; use --force-deploy to proceed anyway");
                            return false;
                        }
                        if(
                            (deployedVersionText == null)
                            || !VersionInfo.TryParse(deployedVersionText, out VersionInfo deployedVersion)
                        ) {
                            AddError("unable to determine the deployed module version; use --force-deploy to proceed anyway");
                            return false;
                        }
                        if(deployedVersion > VersionInfo.Parse(manifest.ModuleVersion)) {
                            AddError($"deployed module version (v{deployedVersionText}) is newer than v{manifest.ModuleVersion}; use --force-deploy to proceed anyway");
                            return false;
                        }
                    }
                } catch(AmazonCloudFormationException) {

                    // stack doesn't exist
                }
            }
            Console.WriteLine($"Deploying stack: {stackName} [{manifest.ModuleName}]");

            // set optional notification topics for cloudformation operations
            var notificationArns =  new List<string>();
            if(Settings.DeploymentNotificationsTopicArn != null) {
                notificationArns.Add(Settings.DeploymentNotificationsTopicArn);
            }

            // default stack policy denies all updates
            var stackPolicyBody =
@"{
    ""Statement"": [{
        ""Effect"": ""Allow"",
        ""Action"": ""Update:*"",
        ""Principal"": ""*"",
        ""Resource"": ""*""
    }, {
        ""Effect"": ""Deny"",
        ""Action"": [
            ""Update:Replace"",
            ""Update:Delete""
        ],
        ""Principal"": ""*"",
        ""Resource"": ""*"",
        ""Condition"": {
            ""StringEquals"": {
                ""ResourceType"": [
                    ""AWS::ApiGateway::RestApi"",
                    ""AWS::AppSync::GraphQLApi"",
                    ""AWS::DynamoDB::Table"",
                    ""AWS::EC2::Instance"",
                    ""AWS::EMR::Cluster"",
                    ""AWS::Kinesis::Stream"",
                    ""AWS::KinesisFirehose::DeliveryStream"",
                    ""AWS::KMS::Key"",
                    ""AWS::Neptune::DBCluster"",
                    ""AWS::Neptune::DBInstance"",
                    ""AWS::RDS::DBInstance"",
                    ""AWS::Redshift::Cluster"",
                    ""AWS::S3::Bucket""
                ]
            }
        }
    }]
}";
            var stackDuringUpdatePolicyBody =
@"{
    ""Statement"": [{
        ""Effect"": ""Allow"",
        ""Action"": ""Update:*"",
        ""Principal"": ""*"",
        ""Resource"": ""*""
    }]
}";
            // initialize template parameters
            var parameters = new List<CloudFormationParameter> {
                new CloudFormationParameter {
                    ParameterKey = "DeploymentPrefix",
                    ParameterValue = string.IsNullOrEmpty(Settings.Tier) ? "" : Settings.Tier + "-"
                },
                new CloudFormationParameter {
                    ParameterKey = "DeploymentPrefixLowercase",
                    ParameterValue = string.IsNullOrEmpty(Settings.Tier) ? "" : Settings.Tier.ToLowerInvariant() + "-"
                },
                new CloudFormationParameter {
                    ParameterKey = "DeploymentBucketName",
                    ParameterValue = Settings.DeploymentBucketName ?? ""
                }
            };
            foreach(var input in inputs) {
                parameters.Add(new CloudFormationParameter {
                    ParameterKey = input.Key,
                    ParameterValue = input.Value
                });
            }

            // create/update cloudformation stack
            var success = false;
            var templateUrl = $"https://{Settings.DeploymentBucketName}.s3.amazonaws.com/{manifest.Template}";
            if(mostRecentStackEventId != null) {
                try {
                    Console.WriteLine($"=> Stack update initiated for {stackName}");
                    var request = new UpdateStackRequest {
                        StackName = stackName,
                        Capabilities = new List<string> {
                            "CAPABILITY_NAMED_IAM"
                        },
                        NotificationARNs = notificationArns,
                        Parameters = parameters,
                        StackPolicyBody = stackPolicyBody,
                        StackPolicyDuringUpdateBody = allowDataLoss ? stackDuringUpdatePolicyBody : null,
                        TemplateURL = templateUrl
                    };
                    var response = await Settings.CfClient.UpdateStackAsync(request);
                    var outcome = await Settings.CfClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId);
                    if(outcome.Success) {
                        Console.WriteLine($"=> Stack update finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                        ShowStackResult(outcome.Stack);
                        success = true;
                    } else {
                        Console.WriteLine($"=> Stack update FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    }
                } catch(AmazonCloudFormationException e) when(e.Message == "No updates are to be performed.") {

                    // this error is thrown when no required updates where found
                    Console.WriteLine($"=> No stack update required (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    success = true;
                }
            } else {
                Console.WriteLine($"=> Stack creation initiated for {stackName}");
                var request = new CreateStackRequest {
                    StackName = stackName,
                    Capabilities = new List<string> {
                        "CAPABILITY_NAMED_IAM"
                    },
                    OnFailure = OnFailure.DELETE,
                    NotificationARNs = notificationArns,
                    Parameters = parameters,
                    StackPolicyBody = stackPolicyBody,
                    EnableTerminationProtection = protectStack,
                    TemplateURL = templateUrl
                };
                var response = await Settings.CfClient.CreateStackAsync(request);
                var outcome = await Settings.CfClient.TrackStackUpdateAsync(stackName, mostRecentStackEventId);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack creation finished (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                    ShowStackResult(outcome.Stack);
                    success = true;
                } else {
                    Console.WriteLine($"=> Stack creation FAILED (finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                }
            }
            return success;

            // local function
            void ShowStackResult(CloudFormationStack stack) {
                var outputs = stack.Outputs;
                if(outputs.Any()) {
                    Console.WriteLine("Stack output values:");
                    foreach(var output in outputs.OrderBy(output => output.OutputKey)) {
                        Console.WriteLine($"=> {output.Description ?? output.OutputKey}: {output.OutputValue}");
                    }
                }
            }
        }
    }
}