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
using Amazon.CloudFormation.Model;
using LambdaSharp.Tool.Internal;

namespace LambdaSharp.Tool.Cli.Tier {

    internal class TierManager : ASettingsBase {

        //--- Constructors ---
        public TierManager(Settings settings) : base(settings) { }

        //--- Properties ---
        public string TierName => Settings.TierName;

        //--- Methods ---
        public async Task<IEnumerable<TierModuleDetails>> GetModuleDetailsAsync(bool includeCoreModule = false) {
            var tierPrefix = Settings.TierPrefix;
            var stacks = (await ListAllStacks()).Where(stack => {
                var lambdaSharpTier = stack.Outputs.FirstOrDefault(output => output.OutputKey == "LambdaSharpTier");

                // if the stack doesn't have the 'LambdaSharpTier' output value, we need a different heuristic
                if(lambdaSharpTier == null) {

                    // NOTE (2019-09-19, bjorg): for legacy stacks without the 'LambdaSharpTier' output value, we check
                    //  for the presence of the 'Secrets' and 'DeploymentBucketName' parameters to determine if the stack
                    //  was deployed by the LambdaSharp.Tool
                    if(
                        stack.Parameters.Any(parameter => parameter.ParameterKey == "Secrets")
                        && stack.Parameters.Any(parameter => parameter.ParameterKey == "DeploymentBucketName")
                    ) {

                        // check if the stack is prefixed with the deployment tier name
                        return (tierPrefix.Length > 0) && stack.StackName.StartsWith(tierPrefix, StringComparison.Ordinal);
                    }
                    return false;
                }

                // NOTE (2019-06-18, bjorg): empty string output values are returned as `null` values; so we need to first detect
                //  if an output key exists and then default to empty string when it is null to properly compare it.
                return (lambdaSharpTier.OutputValue ?? "") == Settings.Tier;
            });

            // convert stacks to module details
            return stacks.Select(stack => ConvertToModule(stack, tierPrefix))
                .Where(module => includeCoreModule || !module.ModuleReference.StartsWith("LambdaSharp.Core", StringComparison.Ordinal))
                .OrderBy(module => module.DeploymentDate)
                .ToList();
        }

        public async Task<IEnumerable<TierModuleDetails>> GetDeploymentTierDetailsAsync() {
            var stacks = (await ListAllStacks()).Where(stack => IsCoreModule(stack));

            // convert stacks to module details
            return stacks.Select(stack => {
                var lambdaSharpTier = stack.Outputs.FirstOrDefault(output => output.OutputKey == "LambdaSharpTier");

                // if the stack doesn't have the 'LambdaSharpTier' output value, we need a different heuristic
                string tierPrefix;
                if(lambdaSharpTier == null) {

                    // determine tier prefix from stack name
                    var coreNameIndex = stack.StackName.IndexOf("LambdaSharp-Core");
                    if(coreNameIndex < 0) {
                        tierPrefix = "";
                    } else {
                        tierPrefix = stack.StackName.Substring(0, coreNameIndex);
                    }
                } else if(lambdaSharpTier.OutputValue != null) {
                    tierPrefix = lambdaSharpTier.OutputValue + "-";
                } else {
                    tierPrefix = "";
                }
                return ConvertToModule(stack, tierPrefix);
            })
                .OrderBy(module => module.DeploymentDate)
                .ToList();
        }

        public void ShowModuleDetails(
            IEnumerable<TierModuleDetails> moduleDetails,
            params (string ColumnTitle, Func<TierModuleDetails, string> GetColumnValue)[] columns
        ) {
            if(!moduleDetails.Any()) {
                return;
            }
            var columnsWithWidth = columns.Select(
                (column, index) => (
                    ColumnTitle: column.ColumnTitle,
                    GetColumnValue: column.GetColumnValue,
                    ColumnWidth: Math.Max(column.ColumnTitle.Length, moduleDetails.Max(module => column.GetColumnValue(module)?.Length ?? 0)) + ((index != (columns.Length - 1)) ? 4 : 0)
                )
            ).ToList();

            // compute the width of every column by getting the max string length of the column name and all possible values for the column
            Console.Write(Settings.HighContrastColor);
            foreach(var column in columnsWithWidth) {
                Console.Write(column.ColumnTitle.PadRight(column.ColumnWidth));
            }
            Console.Write(Settings.ResetColor);
            Console.WriteLine();
            foreach(var summary in moduleDetails) {
                foreach(var column in columnsWithWidth) {
                    Console.Write(column.GetColumnValue(summary).PadRight(column.ColumnWidth));
                }
                Console.WriteLine();
            }
        }

        private async Task<IEnumerable<Stack>> ListAllStacks() {
            var result = new List<Stack>();
            var request = new DescribeStacksRequest();
            do {
                var response = await Settings.CfnClient.DescribeStacksAsync(request);
                result.AddRange(response.Stacks);
                request.NextToken = response.NextToken;
            } while(request.NextToken != null);
            return result;
        }

        private TierModuleDetails ConvertToModule(Stack stack, string tierPrefix) {
            string coreServices;
            if(IsCoreModule(stack)) {
                coreServices = stack.Outputs.FirstOrDefault(output => output.OutputKey == "CoreServices")?.OutputValue;

                // NOTE (2020-06-25, bjorg): for legacy stacks without the 'CoreServices' output value, we check
                //  for the presence of the 'LoggingStream' parameters to determine if the stack has
                //  core services enabled.
                if(coreServices == null) {
                    coreServices = (stack.Outputs.FirstOrDefault(output => output.OutputKey == "LoggingStream")?.OutputValue != null)
                        ? "Enabled"
                        : "Disabled";
                }
            } else {
                coreServices = stack.Parameters
                    .FirstOrDefault(parameter => parameter.ParameterKey == "LambdaSharpCoreServices")
                    ?.ParameterValue;
            }
            return new TierModuleDetails {
                ModuleDeploymentName = stack.StackName.Substring(tierPrefix.Length),
                StackStatus = stack.StackStatus.ToString(),
                DeploymentDate = (stack.LastUpdatedTime > stack.CreationTime) ? stack.LastUpdatedTime : stack.CreationTime,
                Stack = stack,
                ModuleReference = GetShortModuleReference(stack),
                CoreServices = coreServices,
                HasDefaultSecretKeyParameter = stack.Parameters.Any(parameter => parameter.ParameterKey == "LambdaSharpCoreDefaultSecretKey"),
                DeploymentBucketArn = stack.Outputs.FirstOrDefault(output => output.OutputKey == "DeploymentBucket")?.OutputValue,
                DeploymentTierName = (tierPrefix.Length > 0)
                    ? tierPrefix.Substring(0, tierPrefix.Length - 1)
                    : "<DEFAULT>"
            };
        }

        private string GetShortModuleReference(Stack stack) {
            string moduleReference = stack.GetModuleVersionText() ?? "";
            if(moduleReference.EndsWith("@" + Settings.DeploymentBucketName, StringComparison.Ordinal)) {
                return moduleReference.Substring(0, moduleReference.Length - Settings.DeploymentBucketName.Length - 1);
            }
            return moduleReference;
        }

        private bool IsCoreModule(Stack stack) => GetShortModuleReference(stack).StartsWith("LambdaSharp.Core:", StringComparison.Ordinal);
    }
}
