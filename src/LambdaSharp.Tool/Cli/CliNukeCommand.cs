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
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3.Model;
using LambdaSharp.Tool.Cli.Tier;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {

    public class CliNukeCommand : ACliCommand {


        //--- Constants ---
        private const int MAX_ITERATIONS = 100;

        //--- Methods --
        public void Register(CommandLineApplication app) {

            // nuke a deployment tier
            app.Command("nuke", cmd => {
                cmd.HelpOption();
                cmd.Description = "Delete a LambdaSharp deployment tier";
                var dryRunOption = cmd.Option("--dryrun", "(optional) Check which CloudFormation stacks to delete without deleting them", CommandOptionType.NoValue);
                var confirmTierOption = cmd.Option("--confirm-tier", "(optional) Confirm deployment tier name to skip confirmation prompts", CommandOptionType.SingleValue);

                // misc options
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                AddStandardCommandOptions(cmd);
                cmd.OnExecute(async () => {
                    ExecuteCommandActions(cmd);

                    // read settings and validate them
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }
                    if(confirmTierOption.HasValue()) {
                        if(confirmTierOption.Value() != settings.Tier) {
                            LogError("deployment tier name does not match");
                            return;
                        }
                    }
                    await NukeAsync(settings, dryRunOption.HasValue(), confirmTierOption.HasValue());
                });
            });
        }

        public async Task NukeAsync(Settings settings, bool dryRun, bool confirmed) {
            if(!await PopulateDeploymentTierSettingsAsync(settings, requireBucketName: false, requireCoreServices: false, requireVersionCheck: false)) {
                return;
            }

            // gather module details
            Console.WriteLine($"=> Inspecting deployment tier {Settings.InfoColor}{settings.TierName}{Settings.ResetColor}");
            var tierManager = new TierManager(settings);

            // enumerate all non-nested (root) stack that in a completed update state
            var moduleDetails = (await tierManager.GetModuleDetailsAsync(includeCoreModule: true))
                .Where(module => module.StackStatus.EndsWith("_COMPLETE") && (module.StackStatus != "DELETE_COMPLETE"))
                .ToList();
            if(!moduleDetails.Any()) {
                Console.WriteLine($"=> Found no modules to delete");
                return;
            }
            var count = moduleDetails.Count(module => module.IsRoot);
            if(count == 1) {
                Console.WriteLine($"=> Found 1 CloudFormation stack to delete");
            } else {
                Console.WriteLine($"=> Found {count:N0} CloudFormation stacks to delete");
            }

            // confirm action with user if not already confirmed and this is not a dry run
            if(!confirmed && !dryRun) {

                // list what is about to be deleted
                Console.WriteLine();
                foreach(var module in moduleDetails
                    .Where(module => module.IsRoot)
                    .OrderBy(module => module.ModuleDeploymentName)
                ) {
                    Console.WriteLine($"  {Settings.InfoColor}{module.ModuleDeploymentName}{Settings.ResetColor}");
                }

                // confirm deployment tier name
                Console.WriteLine();
                if(settings.PromptString("Confirm the deployment tier name to delete") != settings.TierName)  {
                    LogError("deployment tier name does not match");
                    return;
                }

                // confirm action one more time
                if(!settings.PromptYesNo($"Proceed with deleting deployment tier '{settings.TierName}'", defaultAnswer: false)) {
                    Console.WriteLine("=> Canceling deletion of deployment tier");
                    return;
                }
                Console.WriteLine();
            }

            // discover all dependencies
            var dependencies = new Dictionary<string, HashSet<string>>();
            foreach(var module in moduleDetails) {
                var dependents = new HashSet<string>();
                dependencies.Add(module.StackName, dependents);

                // enumerate each exported value
                foreach(var output in module.Stack.Outputs.Where(output => output.ExportName != null)) {

                    // discover which stacks are importing it
                    try {
                        var imports = await settings.CfnClient.ListImportsAsync(new ListImportsRequest {
                            ExportName = output.ExportName
                        });
                        foreach(var import in imports.Imports) {
                            dependents.Add(import);
                        }
                    } catch(AmazonCloudFormationException e) when(e.Message == $"Export '{output.ExportName}' is not imported by any stack.") {

                        // nothing to do
                    }
                }
            }

            // iteratively delete the stacks
            var bucketsToDelete = new List<string>();
            for(var i = 0; moduleDetails.Any() && (i < MAX_ITERATIONS); ++i) {
                foreach(var module in moduleDetails.Where(module => module.IsRoot).ToList()) {
                    var stackName = module.StackName;

                    // skip stacks that still have dependents
                    if(dependencies[stackName].Any()) {
                        Settings.LogInfoVerbose($"... skipping due to active dependencies: {stackName}");
                        continue;
                    }
                    if((module.DeploymentBucketArn != null) && (moduleDetails.Count > 1)) {

                        // don't delete the LambdaSharp.Core stack until all other modules have been deleted
                        continue;
                    }

                    // show progress
                    Console.WriteLine($"=> Deleting {Settings.InfoColor}{stackName}{Settings.ResetColor}");
                    if(!dryRun) {

                        // check if this is the LambdaSharp.Core stack
                        if(module.DeploymentBucketArn != null) {
                            var resources = await settings.CfnClient.GetStackResourcesAsync(module.StackName);

                            // check if this stack created its own deployment bucket
                            var deploymentBucketName = resources.FirstOrDefault(resource => resource.LogicalResourceId == "DeploymentBucketResource")?.PhysicalResourceId;
                            if(deploymentBucketName != null) {
                                await DeleteBucketContentsAsync(settings, "deployment bucket", deploymentBucketName);
                                bucketsToDelete.Add(deploymentBucketName);
                            }

                            // check if this stack created its own logging bucket
                            var loggingBucketName = resources.FirstOrDefault(resource => resource.LogicalResourceId == "LoggingBucketResource")?.PhysicalResourceId;
                            if(loggingBucketName != null) {
                                await DeleteBucketContentsAsync(settings, "logging bucket", loggingBucketName);
                                bucketsToDelete.Add(loggingBucketName);
                            }
                        }

                        // delete stack
                        var mostRecentStackEventId = await settings.CfnClient.GetMostRecentStackEventIdAsync(stackName);
                        var oldNameMappings = await new ModelManifestLoader(settings, "source").GetNameMappingsFromCloudFormationStackAsync(stackName);
                        await settings.CfnClient.DeleteStackAsync(new DeleteStackRequest {
                            StackName = stackName
                        });
                        var outcome = await settings.CfnClient.TrackStackUpdateAsync(
                            stackName,
                            module.Stack.StackId,
                            mostRecentStackEventId,
                            nameMappings: null,
                            oldNameMappings,
                            LogError
                        );

                        // confirm that stack is deleted
                        var stackEventsResponse = await settings.CfnClient.DescribeStackEventsAsync(new DescribeStackEventsRequest {
                            StackName = module.Stack.StackId
                        });
                        var success = (stackEventsResponse.StackEvents.First().ResourceStatus == ResourceStatus.DELETE_COMPLETE);
                        if(!success) {
                            Console.WriteLine("=> Stack delete FAILED");
                            LogError($"unable to delete {stackName}");
                            return;
                        }
                        Console.WriteLine("=> Stack delete finished");
                    }

                    // remove this stack and its children from the list of stacks to delete
                    RecursiveRemoveFromModuleDetails(module);
                }
            }

            // delete any left over buckets, such as the logging bucket which has 'Retain' as deletion policy
            if(bucketsToDelete.Any()) {
                Console.WriteLine();
                foreach(var bucketName in bucketsToDelete) {
                    if(await settings.S3Client.DoesS3BucketExistAsync(bucketName)) {
                        try {
                            Console.WriteLine($"=> Deleting S3 Bucket {Settings.InfoColor}{bucketName}{Settings.ResetColor}");
                            await settings.S3Client.DeleteBucketAsync(bucketName);
                        } catch {
                            LogWarn($"unable to delete S3 bucket: {bucketName}");
                        }
                    }
                }
            }

            // local functions
            void RecursiveRemoveFromModuleDetails(TierModuleDetails moduleToRemove) {

                // remove this stack as a dependent from all dependencies
                foreach(var dependency in dependencies) {
                    dependency.Value.Remove(moduleToRemove.StackName);
                }

                // find all child modules of the module to remove
                foreach(var childModule in moduleDetails.Where(module => module.Stack.RootId == moduleToRemove.Stack.StackId).ToList()) {
                    RecursiveRemoveFromModuleDetails(childModule);
                }

                // remove module from list
                moduleDetails.RemoveAll(moduleDetail => moduleDetail.StackName == moduleToRemove.StackName);
            }
        }

        private async Task DeleteBucketContentsAsync(Settings settings, string bucketDescription, string bucketArnOrName) {

            // extract bucket name from bucket ARN
            var colonIndex = bucketArnOrName.LastIndexOf(':');
            string bucketName;
            if(colonIndex > 0) {
                bucketName = bucketArnOrName.Substring(colonIndex + 1);
            } else {
                bucketName = bucketArnOrName;
            }
            Console.WriteLine($"=> Emptying {bucketDescription} {Settings.InfoColor}{bucketName}{Settings.ResetColor}");

            // enumerate all S3 objects
            var request = new ListObjectsV2Request {
                BucketName = bucketName
            };
            var counter = 0;
            var deletions = new List<Task>();
            do {
                var response = await settings.S3Client.ListObjectsV2Async(request);

                // delete any objects found
                if(response.S3Objects.Any()) {
                    deletions.Add(settings.S3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                        BucketName = bucketName,
                        Objects = response.S3Objects.Select(s3 => new KeyVersion {
                            Key = s3.Key
                        }).ToList(),
                        Quiet = true
                    }));
                    counter += response.S3Objects.Count;
                }

                // continue until no more objects can be fetched
                request.ContinuationToken = response.NextContinuationToken;
            } while(request.ContinuationToken != null);

            // wait for all deletions to complete
            await Task.WhenAll(deletions);
        }
    }
}
