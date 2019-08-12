/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using LambdaSharp.Tool.Model;
using LambdaSharp.Tool.Internal;

namespace LambdaSharp.Tool.Cli.Deploy {
    using CloudFormationStack = Amazon.CloudFormation.Model.Stack;
    using CloudFormationParameter = Amazon.CloudFormation.Model.Parameter;

    public class ModelUpdater : AModelProcessor {

        //--- Class Fields ---
        private static HashSet<string> _protectedResourceTypes = new HashSet<string> {
            "AWS::ApiGateway::RestApi",
            "AWS::ApiGatewayV2::Api",
            "AWS::AppSync::GraphQLApi",
            "AWS::DynamoDB::Table",
            "AWS::EC2::Instance",
            "AWS::EMR::Cluster",
            "AWS::Kinesis::Stream",
            "AWS::KinesisFirehose::DeliveryStream",
            "AWS::KMS::Key",
            "AWS::Neptune::DBCluster",
            "AWS::Neptune::DBInstance",
            "AWS::RDS::DBInstance",
            "AWS::Redshift::Cluster",
            "AWS::S3::Bucket"
        };

        //--- Constructors ---
        public ModelUpdater(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public async Task<bool> DeployChangeSetAsync(
            ModuleManifest manifest,
            ModuleNameMappings nameMappings,
            ModuleLocation moduleLocation,
            string stackName,
            bool allowDataLoss,
            bool protectStack,
            List<CloudFormationParameter> parameters
        ) {
            var now = DateTime.UtcNow;

            // check if cloudformation stack already exists and is in a final state
            Console.WriteLine();
            Console.WriteLine($"Deploying stack: {stackName} [{moduleLocation.ModuleInfo}]");
            var mostRecentStackEventId = await Settings.CfnClient.GetMostRecentStackEventIdAsync(stackName);

            // validate template (must have been copied to deployment bucket at this stage)
            if(moduleLocation.SourceBucketName != Settings.DeploymentBucketName) {
                LogError($"module source must match the deployment tier S3 bucket (EXPECTED: {Settings.DeploymentBucketName}, FOUND: {moduleLocation.SourceBucketName})");
                return false;
            }
            ValidateTemplateResponse validation;
            try {
                validation = await Settings.CfnClient.ValidateTemplateAsync(new ValidateTemplateRequest  {
                    TemplateURL = moduleLocation.ModuleTemplateUrl
                });
            } catch(AmazonCloudFormationException e) {
                LogError($"{e.Message} (url: {moduleLocation.ModuleTemplateUrl})");
                return false;
            }

            // create change-set
            var success = false;
            var changeSetName = $"{moduleLocation.ModuleInfo.FullName.Replace(".", "-")}-{now:yyyy-MM-dd-hh-mm-ss}";
            var updateOrCreate = (mostRecentStackEventId != null) ? "update" : "create";
            var capabilities = validation.Capabilities.Any()
                ? "[" + string.Join(", ", validation.Capabilities) + "]"
                : "";
            Console.WriteLine($"=> Stack {updateOrCreate} initiated for {stackName} {capabilities}");
            var response = await Settings.CfnClient.CreateChangeSetAsync(new CreateChangeSetRequest {
                Capabilities = validation.Capabilities,
                ChangeSetName = changeSetName,
                ChangeSetType = (mostRecentStackEventId != null) ? ChangeSetType.UPDATE : ChangeSetType.CREATE,
                Description = $"Stack {updateOrCreate} {moduleLocation.ModuleInfo.FullName} (v{moduleLocation.ModuleInfo.Version})",
                Parameters = new List<CloudFormationParameter>(parameters) {
                    new CloudFormationParameter {
                        ParameterKey = "DeploymentPrefix",
                        ParameterValue = Settings.TierPrefix
                    },
                    new CloudFormationParameter {
                        ParameterKey = "DeploymentPrefixLowercase",
                        ParameterValue = Settings.TierPrefix.ToLowerInvariant()
                    },
                    new CloudFormationParameter {
                        ParameterKey = "DeploymentBucketName",
                        ParameterValue = Settings.DeploymentBucketName
                    }
                },
                StackName = stackName,
                TemplateURL = moduleLocation.ModuleTemplateUrl,
                Tags = Settings.GetCloudFormationStackTags(moduleLocation.ModuleInfo.FullName, stackName)
            });
            try {
                var changes = await WaitForChangeSetAsync(response.Id);
                if(changes == null) {
                    return false;
                }
                if(!changes.Any()) {
                    Console.WriteLine("=> No stack update required");
                    return true;
                }

                //  changes
                if(!allowDataLoss) {
                    var lossyChanges = DetectLossyChanges(changes);
                    if(lossyChanges.Any()) {
                        LogError("one or more resources could be replaced or deleted; use --allow-data-loss to proceed");
                        Console.WriteLine("=> WARNING: detected potential replacement and data-loss in the following resources");
                        foreach(var lossy in lossyChanges) {
                            Console.WriteLine($"{(lossy.ResourceChange.Replacement == Replacement.True ? "ALWAYS" : "CONDITIONAL"),-12} {lossy.ResourceChange.ResourceType,-55} {TranslateLogicalIdToFullName(lossy.ResourceChange.LogicalResourceId)}");
                        }
                        return false;
                    }
                }

                // execute change-set
                await Settings.CfnClient.ExecuteChangeSetAsync(new ExecuteChangeSetRequest {
                    ChangeSetName = changeSetName,
                    StackName = stackName
                });
                var outcome = await Settings.CfnClient.TrackStackUpdateAsync(stackName, response.StackId, mostRecentStackEventId, nameMappings, LogError);
                if(outcome.Success) {
                    Console.WriteLine($"=> Stack {updateOrCreate} finished");
                    ShowStackResult(outcome.Stack);
                    success = true;
                } else {
                    Console.WriteLine($"=> Stack {updateOrCreate} FAILED");
                }

                // optionally enable stack protection
                if(success) {

                    // on success, protect the stack if requested
                    if(protectStack) {
                        await Settings.CfnClient.UpdateTerminationProtectionAsync(new UpdateTerminationProtectionRequest {
                            EnableTerminationProtection = protectStack,
                            StackName = stackName
                        });
                    }
                } else if(mostRecentStackEventId == null) {

                    // delete a new stack that failed to create
                    try {
                        await Settings.CfnClient.DeleteStackAsync(new DeleteStackRequest {
                            StackName = stackName
                        });
                     } catch { }
                }
                return success;
            } finally {
                try {
                    await Settings.CfnClient.DeleteChangeSetAsync(new DeleteChangeSetRequest {
                        ChangeSetName = response.Id
                    });
                } catch { }
            }

            // local function
            string TranslateLogicalIdToFullName(string logicalId) {
                var fullName = logicalId;
                nameMappings?.ResourceNameMappings.TryGetValue(logicalId, out fullName);
                return fullName ?? logicalId;
            }
        }

        private void ShowStackResult(CloudFormationStack stack) {
            var outputs = stack.Outputs;
            if(outputs.Any()) {
                Console.WriteLine("Stack output values:");
                foreach(var output in outputs.OrderBy(output => output.OutputKey)) {
                    var line = $"=> {output.OutputKey}";
                    if(!string.IsNullOrEmpty(output.Description)) {
                        line += $": {output.Description}";
                    }
                    line += $" = {output.OutputValue}";
                    Console.WriteLine(line);
                }
            }
        }

        private async Task<List<Change>> WaitForChangeSetAsync(string changeSetId) {

            // wait until change-set if available
            var changeSetRequest = new DescribeChangeSetRequest {
                ChangeSetName = changeSetId
            };
            var changes = new List<Change>();
            while(true) {
                await Task.Delay(TimeSpan.FromSeconds(3));
                var changeSetResponse = await Settings.CfnClient.DescribeChangeSetAsync(changeSetRequest);
                if(changeSetResponse.Status == ChangeSetStatus.CREATE_PENDING) {

                    // wait until the change-set is CREATE_COMPLETE
                    continue;
                }
                if(changeSetResponse.Status == ChangeSetStatus.CREATE_IN_PROGRESS) {

                    // wait until the change-set is CREATE_COMPLETE
                    continue;
                }
                if(changeSetResponse.Status == ChangeSetStatus.CREATE_COMPLETE) {
                    changes.AddRange(changeSetResponse.Changes);
                    if(changeSetResponse.NextToken != null) {
                        changeSetRequest.NextToken = changeSetResponse.NextToken;
                        continue;
                    }
                    return changes;
                }
                if(changeSetResponse.Status == ChangeSetStatus.FAILED) {
                    if(changeSetResponse.StatusReason.StartsWith("The submitted information didn't contain changes.", StringComparison.Ordinal)) {
                        return new List<Change>();
                    }
                    LogError($"change-set failed: {changeSetResponse.StatusReason}");
                    return null;
                }
                LogError($"unexpected change-set status: {changeSetResponse.ExecutionStatus}");
                return null;
            }
        }

        private IEnumerable<Change> DetectLossyChanges(IEnumerable<Change> changes) {
            return changes
                .Where(change => change.Type == ChangeType.Resource)
                .Where(change =>
                    _protectedResourceTypes.Contains(change.ResourceChange.ResourceType)
                    && (
                        (change.ResourceChange.Action == ChangeAction.Remove)
                        || (
                            (change.ResourceChange.Action == ChangeAction.Modify)
                            && (change.ResourceChange.Replacement != Replacement.False)
                        )
                    )
                ).ToArray();
        }
    }
}