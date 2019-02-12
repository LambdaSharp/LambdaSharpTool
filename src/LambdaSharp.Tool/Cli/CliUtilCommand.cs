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
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli {

    public class CliUtilCommand : ACliCommand {

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("util", cmd => {
                cmd.HelpOption();
                cmd.Description = "Miscellaneous AWS utilities";

                // delete orphaned logs sub-command
                cmd.Command("delete-orphan-lambda-logs", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Delete orphaned Lambda CloudWatch logs";
                    var dryRunOption = subCmd.Option("--dryrun", "(optional) Check which logs to delete without deleting them", CommandOptionType.NoValue);

                    // run command
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        await DeleteOrphanLambdaLogs(dryRunOption.HasValue());
                    });
                });

                // download cloudformation specification sub-command
                cmd.Command("download-cloudformation-spec", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Download CloudFormation JSON specification to LAMBDASHARP development folder";
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");

                        // determine destination folder
                        var lambdaSharpFolder = System.Environment.GetEnvironmentVariable("LAMBDASHARP");
                        if(lambdaSharpFolder == null) {
                            AddError("LAMBDASHARP environment variable is not defined");
                            return;
                        }
                        var destinationZipLocation = Path.Combine(lambdaSharpFolder, "src/LambdaSharp.Tool/Resources/CloudFormationResourceSpecification.json.gz");
                        var destinationJsonLocation = Path.Combine(lambdaSharpFolder, "Docs/CloudFormationResourceSpecification.json");

                        // run command
                        await RefreshCloudFormationSpec(
                            "https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
                            destinationZipLocation,
                            destinationJsonLocation
                        );
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        public async Task RefreshCloudFormationSpec(
            string specifcationUrl,
            string destinationZipLocation,
            string destinationJsonLocation
        ) {
            Console.WriteLine();

            // download json specification
            Console.WriteLine($"Fetching specification from {specifcationUrl}");
            var response = await new HttpClient().GetAsync(specifcationUrl);
            string text;
            using(var decompressionStream = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress))
            using(var decompressedMemoryStream = new MemoryStream()) {
                await decompressionStream.CopyToAsync(decompressedMemoryStream);
                text = Encoding.UTF8.GetString(decompressedMemoryStream.ToArray());
            }

            // strip all "Documentation" fields to reduce document size
            Console.WriteLine($"Original size: {text.Length:N0}");
            var json = JObject.Parse(text);
            json.Descendants()
                .OfType<JProperty>()
                .Where(attr => (attr.Name == "Documentation") || (attr.Name == "UpdateType"))
                .ToList()
                .ForEach(attr => attr.Remove());
            json = OrderFields(json);
            text = json.ToString(Formatting.None);
            Console.WriteLine($"Stripped size: {text.Length:N0}");
            File.WriteAllText(destinationJsonLocation, json.ToString(Formatting.Indented).Replace("\r\n", "\n"));

            // save compressed file
            using(var fileStream = File.OpenWrite(destinationZipLocation)) {
            using(var compressionStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            using(var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                await memoryStream.CopyToAsync(compressionStream);
            }
            var info = new FileInfo(destinationZipLocation);
            Console.WriteLine($"Stored compressed spec file {destinationZipLocation}");
            Console.WriteLine($"Compressed file size: {info.Length:N0}");

            // local functions
            JObject OrderFields(JObject value) {
                var result = new JObject();
                foreach(var property in value.Properties().ToList().OrderBy(property => property.Name)) {
                    result.Add(property.Name, (property.Value is JObject propertyValue)
                        ? OrderFields(propertyValue)
                        : property.Value
                    );
                }
                return result;
            }
        }

        public async Task DeleteOrphanLambdaLogs(bool dryRun) {
            Console.WriteLine();

            // list all lambda functions
            var lambdaClient = new AmazonLambdaClient();
            var listFunctionsRequest = new ListFunctionsRequest { };
            var lambdaLogGroupNames = new HashSet<string>();
            do {
                var listFunctionsResponse = await lambdaClient.ListFunctionsAsync(listFunctionsRequest);
                foreach(var function in listFunctionsResponse.Functions) {
                    lambdaLogGroupNames.Add($"/aws/lambda/{function.FunctionName}");
                }
                listFunctionsRequest.Marker = listFunctionsResponse.NextMarker;
            } while(listFunctionsRequest.Marker != null);

            // list all log groups for lambda functions
            var logsClient = new AmazonCloudWatchLogsClient();
            var describeLogGroupsRequest = new DescribeLogGroupsRequest {
                LogGroupNamePrefix = "/aws/lambda/"
            };
            var totalLogGroups = 0;
            var deletedLogGroups = 0;
            var skippedLogGroups = 0;
            do {
                var describeLogGroupsResponse = await logsClient.DescribeLogGroupsAsync(describeLogGroupsRequest);
                totalLogGroups += describeLogGroupsResponse.LogGroups.Count;
                foreach(var logGroup in describeLogGroupsResponse.LogGroups) {
                    if(lambdaLogGroupNames.Contains(logGroup.LogGroupName)) {

                        // nothing to do
                    } else if(System.Text.RegularExpressions.Regex.IsMatch(logGroup.LogGroupName, @"^\/aws\/lambda\/[a-zA-Z0-9\-_]+$")) {

                        // attempt to delete log group
                        if(dryRun) {
                            Console.WriteLine($"* deleted '{logGroup.LogGroupName}' (skipped)");
                        } else {
                            try {
                                await logsClient.DeleteLogGroupAsync(new DeleteLogGroupRequest {
                                    LogGroupName = logGroup.LogGroupName
                                });
                                Console.WriteLine($"* deleted '{logGroup.LogGroupName}'");
                                ++deletedLogGroups;
                            } catch {
                                AddError($"could not delete '{logGroup.LogGroupName}'");
                            }
                        }
                    } else {

                        // log group has an invalid name structure; skip it
                        Console.WriteLine($"SKIPPED '{logGroup.LogGroupName}'");
                        ++skippedLogGroups;
                    }
                }
                describeLogGroupsRequest.NextToken = describeLogGroupsResponse.NextToken;
            } while(describeLogGroupsRequest.NextToken != null);
            if((deletedLogGroups > 0) || (skippedLogGroups > 0)) {
                Console.WriteLine();
            }
            Console.WriteLine($"Found {totalLogGroups:N0} log groups. Deleted {deletedLogGroups:N0}. Skipped {skippedLogGroups:N0}.");
        }
    }
}
