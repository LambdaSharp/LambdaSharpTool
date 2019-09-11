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
using Amazon.CloudFormation.Model;
using LambdaSharp.Tool.Internal;

namespace LambdaSharp.Tool.Cli.Tier {

    internal class TierManager : ASettingsBase {

        //--- Constructors ---
        public TierManager(Settings settings) : base(settings) { }

        //--- Methods ---
        public async Task<IEnumerable<TierModuleDetails>> GetModuleDetailsAsync() {
            var prefix = Settings.TierPrefix;
            var stacks = new List<Stack>();
            var request = new DescribeStacksRequest();
            do {
                var response = await Settings.CfnClient.DescribeStacksAsync(request);
                stacks.AddRange(response.Stacks.Where(summary => {

                    // NOTE (2019-06-18, bjorg): empty string output values are returned as `null` values; so we need to first detect
                    //  if an output key exists and then default to empty string when it is null to properly compare it.
                    var lambdaSharpTier = summary.Outputs.FirstOrDefault(output => output.OutputKey == "LambdaSharpTier");
                    if(lambdaSharpTier == null) {
                        return (prefix.Length > 0) && summary.StackName.StartsWith(prefix, StringComparison.Ordinal);
                    }
                    return (lambdaSharpTier.OutputValue ?? "") == Settings.Tier;
                }));
                request.NextToken = response.NextToken;
            } while(request.NextToken != null);

            // convert stacks to module details
            return stacks.Select(stack => new TierModuleDetails {
                StackName = stack.StackName,
                ModuleDeploymentName = stack.StackName.Substring(prefix.Length),
                StackStatus = stack.StackStatus.ToString(),
                DeploymentDate = (stack.LastUpdatedTime > stack.CreationTime) ? stack.LastUpdatedTime : stack.CreationTime,
                Stack = stack,
                ModuleReference = GetShortModuleReference(stack.GetModuleVersionText() ?? ""),
                CoreServices = stack.Parameters
                    .FirstOrDefault(parameter => parameter.ParameterKey == "LambdaSharpCoreServices")
                    ?.ParameterValue,
                HasDefaultSecretKeyParameter = stack.Parameters.Any(parameter => parameter.ParameterKey == "LambdaSharpCoreDefaultSecretKey"),
                IsRoot = stack.RootId == null
            })
                .Where(module => !module.ModuleReference.StartsWith("LambdaSharp.Core", StringComparison.Ordinal))
                .OrderBy(module => module.DeploymentDate)
                .ToList();

            // local functions
            string GetShortModuleReference(string moduleReference) {
                if(moduleReference.EndsWith("@" + Settings.DeploymentBucketName, StringComparison.Ordinal)) {
                    return moduleReference.Substring(0, moduleReference.Length - Settings.DeploymentBucketName.Length - 1);
                }
                return moduleReference;
            }
        }

        public void ShowModuleDetails(IEnumerable<TierModuleDetails> moduleDetails, params (string ColumnTitle, Func<TierModuleDetails, string> GetColumnValue)[] columns) {
            if(moduleDetails.Any()) {
                var columnsWithWidth = columns.Select((column, index) => (ColumnTitle: column.ColumnTitle, GetColumnValue: column.GetColumnValue, ColumnWidth: Math.Max(column.ColumnTitle.Length, moduleDetails.Max(module => column.GetColumnValue(module)?.Length ?? 0)) + ((index != (columns.Length - 1)) ? 4 : 0)));
                Console.WriteLine();
                Console.WriteLine($"Found {moduleDetails.Count():N0} modules for deployment tier '{Settings.TierName}'");
                Console.WriteLine();

                // compute the width of every column by getting the max string length of the column name and all possible values for the column
                if(Settings.UseAnsiConsole) {
                    Console.Write(AnsiTerminal.BrightWhite);
                }
                foreach(var column in columnsWithWidth) {
                    Console.Write(column.ColumnTitle.PadRight(column.ColumnWidth));
                }
                if(Settings.UseAnsiConsole) {
                    Console.Write(AnsiTerminal.Reset);
                }
                Console.WriteLine();
                foreach(var summary in moduleDetails) {
                    foreach(var column in columnsWithWidth) {
                        Console.Write(column.GetColumnValue(summary).PadRight(column.ColumnWidth));
                    }
                    Console.WriteLine();
                }
            } else {
                Console.WriteLine();
                Console.WriteLine($"Found no modules for deployment tier '{Settings.TierName}'");
            }
        }
    }
}
