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
        public ModelUpdater(Settings settings) : base(settings) { }

        //--- Methods ---
        public async Task<bool> DeployAsync(
            Module module,
            string altModuleName,
            string templateFile,
            bool allowDataLoss,
            bool protectStack,
            Dictionary<string, string> inputs
        ) {
            var stackName = $"{Settings.Tier}-{altModuleName ?? module.Name}";
            if(altModuleName != null) {

                // TODO (2018-10-09, bjorg): check if modules has any custom resources; if it does, fail, because it makes
                //  no sense to allow multiple instantiations of a module with custom resources since they are global
            }
            Console.WriteLine($"Deploying stack: {stackName}");

            // check if cloudformation stack already exists
            var mostRecentStackEventId = await Settings.CfClient.GetMostRecentStackEventIdAsync(stackName);

            // set optional notification topics for cloudformation operations
            var notificationArns =  new List<string>();
            if(Settings.DeploymentNotificationsTopicArn != null) {
                notificationArns.Add(Settings.DeploymentNotificationsTopicArn);
            }

            // upload cloudformation template
            string templateUrl = null;
            if(Settings.DeploymentBucketName != null) {
                var template = File.ReadAllText(templateFile);
                var minifiedTemplate = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(template), Formatting.None);
                var templateS3Key = $"{Settings.DeploymentBucketPath}{module.Name}/cloudformation-v{module.Version}-{minifiedTemplate.ToMD5Hash()}.json";
                templateUrl = $"https://{Settings.DeploymentBucketName}.s3.amazonaws.com/{templateS3Key}";
                Console.WriteLine($"=> Uploading CloudFormation template: s3://{Settings.DeploymentBucketName}/{templateS3Key}");
                await Settings.S3Client.PutObjectAsync(new PutObjectRequest {
                    BucketName = Settings.DeploymentBucketName,
                    ContentBody = minifiedTemplate,
                    ContentType = "application/json",
                    Key = templateS3Key,
                });
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
                    ParameterKey = "Tier",
                    ParameterValue = Settings.Tier
                },
                new CloudFormationParameter {
                    ParameterKey = "TierLowercase",
                    ParameterValue = Settings.Tier.ToLowerInvariant()
                },
                new CloudFormationParameter {
                    ParameterKey = "DeploymentBucketName",
                    ParameterValue = Settings.DeploymentBucketName ?? ""
                },
                new CloudFormationParameter {
                    ParameterKey = "DeploymentBucketPath",
                    ParameterValue = Settings.DeploymentBucketPath ?? ""
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
                        TemplateURL = templateUrl,
                        TemplateBody = (templateUrl == null) ? File.ReadAllText(templateFile) : null
                    };
                    var response = await Settings.CfClient.UpdateStackAsync(request);
                    var outcome = await Settings.CfClient.TrackStackUpdateAsync(response.StackId, mostRecentStackEventId);
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
                    TemplateURL = templateUrl,
                    TemplateBody = (templateUrl == null) ? File.ReadAllText(templateFile) : null
                };
                var response = await Settings.CfClient.CreateStackAsync(request);
                var outcome = await Settings.CfClient.TrackStackUpdateAsync(response.StackId, mostRecentStackEventId);
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
                    foreach(var output in outputs) {
                        Console.WriteLine($"=> {output.Description}: {output.OutputValue}");
                    }
                }
            }
        }
    }
}