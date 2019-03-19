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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using JsonDiffPatch;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Generation;

namespace LambdaSharp.Tool.Cli {

    public class CliUtilCommand : ACliCommand {

        //--- Class Fields ---
        private static HttpClient _httpClient = new HttpClient();

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
                        await DeleteOrphanLambdaLogsAsync(dryRunOption.HasValue());
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
                            LogError("LAMBDASHARP environment variable is not defined");
                            return;
                        }
                        var destinationZipLocation = Path.Combine(lambdaSharpFolder, "src/LambdaSharp.Tool/Resources/CloudFormationResourceSpecification.json.gz");
                        var destinationJsonLocation = Path.Combine(lambdaSharpFolder, "Docs/CloudFormationResourceSpecification.json");

                        // run command
                        await RefreshCloudFormationSpecAsync(
                            "https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
                            destinationZipLocation,
                            destinationJsonLocation
                        );
                    });
                });

                cmd.Command("create-invoke-methods-schema", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create JSON request/response schema for API Gateway invoke methods";
                    var outputFileOption = subCmd.Option("--out|-o", "", CommandOptionType.SingleValue);
                    var assemblyArgument = subCmd.Argument("assembly", "File-path to .NET assembly");
                    var classArgument = subCmd.Argument("class", "Full class name");
                    var methodsArgument = subCmd.Argument("method", "Invoke method name(s)", multipleValues: true);
                    subCmd.OnExecute(async () => {
                        Console.WriteLine($"{app.FullName} - {subCmd.Description}");
                        await CreateInvokeMethodSchemasAsync(assemblyArgument.Value, classArgument.Value, methodsArgument.Values, outputFileOption.Value());
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        public async Task RefreshCloudFormationSpecAsync(
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
            var json = JObject.Parse(text);

            // apply patches
            var jsonPatchesUris = new[] {
                "https://raw.githubusercontent.com/aws-cloudformation/cfn-python-lint/master/src/cfnlint/data/ExtendedSpecs/all/01_spec_patch.json",
//                "https://raw.githubusercontent.com/aws-cloudformation/cfn-python-lint/master/src/cfnlint/data/ExtendedSpecs/all/02_parameter_types.json",
//                "https://raw.githubusercontent.com/aws-cloudformation/cfn-python-lint/master/src/cfnlint/data/ExtendedSpecs/all/03_value_types.json",
//                "https://raw.githubusercontent.com/aws-cloudformation/cfn-python-lint/master/src/cfnlint/data/ExtendedSpecs/all/04_property_values.json",
            };
            foreach(var jsonPatchUri in jsonPatchesUris) {

                // fetch patch document from URI
                var httpResponse = await _httpClient.SendAsync(new HttpRequestMessage {
                    RequestUri = new Uri(jsonPatchUri),
                    Method = HttpMethod.Get
                });
                if(!httpResponse.IsSuccessStatusCode) {
                    LogError($"unable to fetch '{jsonPatchUri}'");
                    continue;
                }
                var patch = PatchDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

                // apply each patch operation individually
                JToken token = json;
                var patcher = new JsonPatcher();
                foreach(var patchOperation in patch.Operations) {
                    try {
                        token = patcher.ApplyOperation(patchOperation, token);
                    } catch {
                        LogWarn($"unable to apply patch operation to '{patchOperation.Path}'");
                    }
                }
                json = (JObject)token;
            }

            // strip all "Documentation" fields to reduce document size
            Console.WriteLine($"Original size: {text.Length:N0}");
            json.SelectTokens("$..UpdateType").ToList().ForEach(property => property.Parent.Remove());
            json.SelectTokens("$.PropertyTypes..Documentation").ToList().ForEach(property => property.Parent.Remove());
            json.SelectTokens("$.ResourceTypes.*.*..Documentation").ToList().ForEach(property => property.Parent.Remove());
            json = OrderFields(json);
            text = json.ToString(Formatting.None);
            Console.WriteLine($"Stripped size: {text.Length:N0}");
            File.WriteAllText(destinationJsonLocation, json.ToString(Formatting.Indented));

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

        public async Task DeleteOrphanLambdaLogsAsync(bool dryRun) {
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
                                LogError($"could not delete '{logGroup.LogGroupName}'");
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

        public async Task CreateInvokeMethodSchemasAsync(
            string assemblyName,
            string className,
            IEnumerable<string> methodNames,
            string outputFile
        ) {
            var schemas = new JObject();

            // load assembly
            Assembly assembly;
            try {
                assembly = Assembly.LoadFrom(assemblyName);
            } catch(FileNotFoundException) {
                LogError("could not find assembly");
                return;
            } catch(Exception e) {
                LogError("error loading assembly", e);
                return;
            }

            // find type in assembly
            var type = assembly.GetType(className);
            if(type == null) {
                LogError("could not find type");
                return;
            }
            Console.WriteLine($"Type: {type.FullName}");

            // enumerate type methods
            foreach(var method in methodNames.Select(methodName => type.GetMethod(methodName))) {
                try {
                    var schema = new JObject();
                    schemas.Add(method.Name, schema);

                    // process method request type
                    var requestType = method.GetParameters()
                        .FirstOrDefault(p => p.Name == "request")
                        ?.ParameterType;
                    var requestSchema = await AddSchema(requestType);
                    schema.Add("Request", requestSchema);

                    // process method response type
                    var responseType = (method.ReturnType.IsGenericType) && (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                        ? method.ReturnType.GetGenericArguments()[0]
                        : method.ReturnType;
                    var responseSchema = await AddSchema(responseType);
                    schema.Add("Response", responseSchema);

                    // write result
                    Console.WriteLine($"=> {method.Name}: {((requestSchema != null) ? requestType.Name : "(none)")} -> {((responseSchema != null) ? responseType.Name : "(none)")}");
                } catch(Exception e) {
                    LogError($"error processing method '{method.Name}'", e);
                    return;
                }
            }

            // create json document
            try {
                var output = JsonConvert.SerializeObject(schemas, Formatting.Indented);
                if(outputFile != null) {
                    File.WriteAllText(outputFile, output);
                } else {
                    Console.WriteLine(outputFile);
                }
            } catch(Exception e) {
                LogError("unable to write schema", e);
            }

            // local functions
            async Task<JObject> AddSchema(Type messageType) {
                if(
                    (messageType != null)
                    && !messageType.IsValueType
                    && (messageType != typeof(string))
                    && (messageType != typeof(void))
                    && (messageType != typeof(Task))
                    && (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest")
                    && (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse")
                ) {
                    return JObject.Parse((await JsonSchema4.FromTypeAsync(messageType)).ToJson());
                }
                return null;
            }
        }
    }
}
