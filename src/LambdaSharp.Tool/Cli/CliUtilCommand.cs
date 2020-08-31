/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Amazon.ApiGatewayV2;
using Amazon.ApiGatewayV2.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;
using JsonDiffPatch;
using LambdaSharp.Tool.Internal;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Generation;

namespace LambdaSharp.Tool.Cli {

    public class CliUtilCommand : ACliCommand {

        //--- Types ---
        private class ProcessTargetInvocationException : Exception {

            //--- Constructors ---
            public ProcessTargetInvocationException(string message) : base(message) { }
        }

        private class KinesisFailedLogRecord {

            //--- Properties ---
            public int attemptsMade { get; set; }
            public long arrivalTimestamp { get; set; }
            public string errorCode { get; set; }
            public string errorMessage { get; set; }
            public long attemptEndingTimestamp { get; set; }
            public string rawData { get; set; }
            public string lambdaArn { get; set; }
        }

        //--- Class Fields ---
        private static HttpClient _httpClient = new HttpClient();
        private static readonly char[] SEPARATORS = new[] { '\r', '\n' };

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("util", cmd => {
                cmd.HelpOption();
                cmd.Description = "Miscellaneous AWS utilities";

                // delete orphaned logs sub-command
                cmd.Command("delete-orphan-logs", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Delete orphaned Lambda and API Gateway CloudWatch logs";
                    var dryRunOption = subCmd.Option("--dryrun", "(optional) Check which logs to delete without deleting them", CommandOptionType.NoValue);
                    var awsProfileOption = subCmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
                    var awsRegionOption = subCmd.Option("--aws-region <NAME>", "(optional) Use a specific AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);

                    // run command
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        await DeleteOrphanLogsAsync(
                            dryRunOption.HasValue(),
                            awsProfileOption.Value(),
                            awsRegionOption.Value()
                        );
                    });
                });

                // download cloudformation specification sub-command
                cmd.Command("download-cloudformation-spec", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Download CloudFormation JSON specification";

                    // run command
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);

                        // determine destination folder
                        var lambdaSharpFolder = System.Environment.GetEnvironmentVariable("LAMBDASHARP");
                        string destinationZipLocation;
                        string destinationJsonLocation;
                        if(lambdaSharpFolder == null) {
                            destinationZipLocation = null;
                            destinationJsonLocation = Settings.CloudFormationResourceSpecificationCacheFilePath;
                        } else {
                            destinationZipLocation = Path.Combine(lambdaSharpFolder, "src", "LambdaSharp.Tool", "Resources", "CloudFormationResourceSpecification.json.gz");
                            destinationJsonLocation = Path.Combine(lambdaSharpFolder, "src", "CloudFormationResourceSpecification.json");
                        }

                        // run command
                        await RefreshCloudFormationSpecAsync(
                            "https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
                            destinationZipLocation,
                            destinationJsonLocation
                        );
                    });
                });

                // create JSON schema definition for API Gateway methods
                cmd.Command("create-invoke-methods-schema", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create JSON schemas for API Gateway invoke methods";
                    var directoryOption = subCmd.Option("--directory|-d", "Directory where .NET assemblies are located", CommandOptionType.SingleValue);
                    var methodOption = subCmd.Option("--method|-m", "Name of a method to analyze", CommandOptionType.MultipleValue);
                    var defaultNamespaceOption = subCmd.Option("--default-namespace|-ns", "(optional) Default namespace for resolving class names", CommandOptionType.SingleValue);
                    var outputFileOption = subCmd.Option("--out|-o", "(optional) Output schema file location (default: console out)", CommandOptionType.SingleValue);

                    // run command
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);

                        // validate options
                        if(directoryOption.Value() == null) {
                            LogError("missing --directory option");
                            return;
                        }
                        if(!methodOption.Values.Any()) {
                            LogError("missing --method option(s)");
                            return;
                        }
                        await CreateInvocationTargetSchemasAsync(
                            directoryOption.Value(),
                            defaultNamespaceOption.Value(),
                            methodOption.Values,
                            outputFileOption.Value()
                        );
                    });
                });

                // list lambda sub-command
                cmd.Command("list-lambdas", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "List all Lambda functions by CloudFormation stack";
                    var awsProfileOption = subCmd.Option("--aws-profile|-P <NAME>", "(optional) Use a specific AWS profile from the AWS credentials file", CommandOptionType.SingleValue);
                    var awsRegionOption = subCmd.Option("--aws-region <NAME>", "(optional) Use a specific AWS region (default: read from AWS profile)", CommandOptionType.SingleValue);

                    // run command
                    AddStandardCommandOptions(subCmd);
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        await ListLambdasAsync(
                            awsProfileOption.Value(),
                            awsRegionOption.Value()
                        );
                    });
                });

                // list modules
                cmd.Command("list-modules", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "List all modules at origin";
                    var bucketOption = subCmd.Option("--bucket <BUCKETNAME>", "(optional) List modules from this S3 bucket (default: match argument)", CommandOptionType.SingleValue);
                    var originOption = subCmd.Option("--origin <ORIGIN>", "(optional) List modules from this origin (default: match argument)", CommandOptionType.SingleValue);
                    var preReleaseOption = subCmd.Option("--include-prerelease", "(optional) Show pre-releases versions (default: omit pre-release versions)", CommandOptionType.NoValue);
                    var originArgument = subCmd.Argument("<ORIGIN>", "origin S3 bucket name", multipleValues: false);
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    AddStandardCommandOptions(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return;
                        }
                        if(
                            !originOption.HasValue()
                            && !bucketOption.HasValue()
                            && string.IsNullOrEmpty(originArgument.Value)
                        ) {
                            Program.ShowHelp = true;
                            Console.WriteLine(subCmd.GetHelpText());
                            return;
                        }
                        await ListModulesAsync(
                            settings,
                            originArgument.Value,
                            bucketOption.Value(),
                            originOption.Value(),
                            preReleaseOption.HasValue()
                        );
                    });
                });

                // validate assembly as Lambda function
                cmd.Command("validate-assembly", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Validate Lambda assembly";
                    var directoryOption = subCmd.Option("--directory|-d", "Directory where .NET assemblies are located", CommandOptionType.SingleValue);
                    var entryPointOption = subCmd.Option("--entry-point|-e", "Name of entry-point method to analyze", CommandOptionType.SingleValue);
                    var outputFileOption = subCmd.Option("--out|-o", "(optional) Output schema file location (default: console out)", CommandOptionType.SingleValue);
                    AddStandardCommandOptions(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);

                        // validate options
                        if(directoryOption.Value() == null) {
                            LogError("missing --directory option");
                            return;
                        }
                        if(!entryPointOption.Values.Any()) {
                            LogError("missing --entry-point option(s)");
                            return;
                        }
                        ValidateAssembly(
                            directoryOption.Value(),
                            entryPointOption.Value(),
                            outputFileOption.Value()
                        );
                    });
                });

                // show failed Kinesis logs
                cmd.Command("show-kinesis-failed-logs", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Show Failed Kinesis Firehose Logging Entries";
                    var keyPrefixOption = subCmd.Option("--key-prefix|-k <PREFIX>", "(optional) S3 key prefix where the failed logging records are stored (default: logging-failed/processing-failed/)", CommandOptionType.SingleValue);
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    AddStandardCommandOptions(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return;
                        }
                        var keyPrefix = keyPrefixOption.HasValue() ? keyPrefixOption.Value() : "logging-failed/processing-failed/";
                        await ShowFailedKinesisProcessingLogs(settings, keyPrefix);
                    });
                });

                // process parameters file and show result
                cmd.Command("show-parameters", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Show Processed YAML Parameters File";

                    // command options
                    var filePathArgument = subCmd.Argument("<FILEPATH>", "Path for YAML parameters file");
                    var initSettingsCallback = CreateSettingsInitializer(subCmd);
                    AddStandardCommandOptions(subCmd);

                    // run command
                    subCmd.OnExecute(async () => {
                        ExecuteCommandActions(subCmd);
                        if(string.IsNullOrEmpty(filePathArgument.Value)) {
                            Program.ShowHelp = true;
                            Console.WriteLine(subCmd.GetHelpText());
                            return;
                        }
                        var settings = await initSettingsCallback();
                        if(settings == null) {
                            return;
                        }
                        await PopulateDeploymentTierSettingsAsync(settings);
                        var parameters = new ParameterFileReader(settings, filePathArgument.Value).ReadInputParametersFiles();
                        if(parameters?.Any() ?? false) {
                            Console.WriteLine();
                            foreach(var kv in parameters.OrderBy(kv => kv.Key)) {
                                Console.WriteLine($"{kv.Key}: {Settings.OutputColor}{kv.Value}{Settings.ResetColor}");
                            }
                        }
                    });
                });

                // show help text if no sub-command is provided
                cmd.OnExecute(() => {
                    Program.ShowHelp = true;
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
            if(destinationJsonLocation != null) {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationJsonLocation));
                var cloudformationJson = json.ToString(Formatting.Indented);
                if(File.Exists(destinationJsonLocation) && ((await File.ReadAllTextAsync(destinationJsonLocation)).ToMD5Hash() == cloudformationJson.ToMD5Hash())) {

                    // not changes, nothing else to do
                    return;
                }
                await File.WriteAllTextAsync(destinationJsonLocation, cloudformationJson);
            }

            // save compressed file
            if(destinationZipLocation != null) {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationZipLocation));
                using(var fileStream = File.OpenWrite(destinationZipLocation))
                using(var compressionStream = new GZipStream(fileStream, CompressionLevel.Optimal))
                using(var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(text))) {
                    await memoryStream.CopyToAsync(compressionStream);
                }
                var info = new FileInfo(destinationZipLocation);
                Console.WriteLine($"Stored compressed spec file {destinationZipLocation}");
                Console.WriteLine($"Compressed file size: {info.Length:N0}");

                // write timestamp when the spec was updated (helps with determining if the embedded or downloaded spec is newer)
                await File.WriteAllTextAsync($"{destinationZipLocation}.timestamp", DateTime.UtcNow.ToString("u"));
            }

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

        public async Task DeleteOrphanLogsAsync(bool dryRun, string awsProfile, string awsRegion) {
            Console.WriteLine();

            // initialize AWS profile
            await InitializeAwsProfile(awsProfile, awsRegion: awsRegion, allowCaching: true);
            var logsClient = new AmazonCloudWatchLogsClient(AWSConfigs.RegionEndpoint);

            // delete orphaned logs
            var totalLogGroups = 0;
            var activeLogGroups = 0;
            var orphanedLogGroups = 0;
            var skippedLogGroups = 0;
            await DeleteOrphanLambdaLogsAsync();
            await DeleteOrphanApiGatewayLogs();
            await DeleteOrphanApiGatewayV2Logs();
            if((orphanedLogGroups > 0) || (skippedLogGroups > 0)) {
                Console.WriteLine();
            }
            Console.WriteLine($"Found {totalLogGroups:N0} log groups. Active {activeLogGroups:N0}. Orphaned {orphanedLogGroups:N0}. Skipped {skippedLogGroups:N0}.");

            // local functions
            async Task DeleteOrphanLambdaLogsAsync() {

                // list all lambda functions
                var lambdaClient = new AmazonLambdaClient(AWSConfigs.RegionEndpoint);
                var request = new ListFunctionsRequest { };
                var lambdaLogGroupNames = new HashSet<string>();
                do {
                    var response = await lambdaClient.ListFunctionsAsync(request);
                    foreach(var function in response.Functions) {
                        lambdaLogGroupNames.Add($"/aws/lambda/{function.FunctionName}");
                    }
                    request.Marker = response.NextMarker;
                } while(request.Marker != null);

                // list all log groups for lambda functions
                await DeleteOrphanCloudWatchLogs(
                    "/aws/lambda/",
                    logGroupName => lambdaLogGroupNames.Contains(logGroupName),
                    logGroupName => Regex.IsMatch(logGroupName, @"^\/aws\/lambda\/[a-zA-Z0-9\-_]+$")
                );
            }

            async Task DeleteOrphanApiGatewayLogs() {

                // list all API Gateway V1 instances
                var apiGatewayClient = new AmazonAPIGatewayClient(AWSConfigs.RegionEndpoint);
                var request = new GetRestApisRequest { };
                var apiGatewayGroupNames = new List<string>();
                do {
                    var response = await apiGatewayClient.GetRestApisAsync(request);
                    apiGatewayGroupNames.AddRange(response.Items.Select(item => $"API-Gateway-Execution-Logs_{item.Id}/"));
                    request.Position = response.Position;
                } while(request.Position != null);

                // list all log groups for API Gateway instances
                await DeleteOrphanCloudWatchLogs(
                    "API-Gateway-Execution-Logs_",
                    logGroupName => apiGatewayGroupNames.Any(apiGatewayGroupName => logGroupName.StartsWith(apiGatewayGroupName, StringComparison.Ordinal)),
                    logGroupName => Regex.IsMatch(logGroupName, @"^API-Gateway-Execution-Logs_[a-zA-Z0-9]+/.+$")
                );
            }

            async Task DeleteOrphanApiGatewayV2Logs() {

                // list all API Gateway V2 instances
                var apiGatewayV2Client = new AmazonApiGatewayV2Client(AWSConfigs.RegionEndpoint);
                var request = new GetApisRequest { };
                var apiGatewayGroupNames = new List<string>();
                do {
                    var response = await apiGatewayV2Client.GetApisAsync(request);
                    apiGatewayGroupNames.AddRange(response.Items.Select(item => $"/aws/apigateway/{item.ApiId}/"));
                    request.NextToken = response.NextToken;
                } while(request.NextToken != null);

                // list all log groups for API Gateway instances
                await DeleteOrphanCloudWatchLogs(
                    "/aws/apigateway/",
                    logGroupName => (logGroupName == "/aws/apigateway/welcome") || apiGatewayGroupNames.Any(apiGatewayGroupName => logGroupName.StartsWith(apiGatewayGroupName, StringComparison.Ordinal)),
                    logGroupName => Regex.IsMatch(logGroupName, @"^/aws/apigateway/[a-zA-Z0-9]+/.+$")
                );
            }

            async Task DeleteOrphanCloudWatchLogs(string logGroupPrefix, Func<string, bool> isActiveLogGroup, Func<string, bool> isValidLogGroup) {
                var describeLogGroupsRequest = new DescribeLogGroupsRequest {
                    LogGroupNamePrefix = logGroupPrefix
                };
                do {
                    var describeLogGroupsResponse = await logsClient.DescribeLogGroupsAsync(describeLogGroupsRequest);
                    totalLogGroups += describeLogGroupsResponse.LogGroups.Count;
                    foreach(var logGroup in describeLogGroupsResponse.LogGroups) {
                        if(isActiveLogGroup(logGroup.LogGroupName)) {

                            // nothing to do
                            ++activeLogGroups;
                        } else if(isValidLogGroup(logGroup.LogGroupName)) {

                            // attempt to delete log group
                            if(dryRun) {
                                Console.WriteLine($"* deleted '{logGroup.LogGroupName}' (skipped)");
                                ++orphanedLogGroups;
                            } else {
                                try {
                                    await logsClient.DeleteLogGroupAsync(new DeleteLogGroupRequest {
                                        LogGroupName = logGroup.LogGroupName
                                    });
                                    Console.WriteLine($"* deleted '{logGroup.LogGroupName}'");
                                    ++orphanedLogGroups;
                                } catch {
                                    LogError($"could not delete '{logGroup.LogGroupName}'");
                                    ++skippedLogGroups;
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
            }
        }

        public async Task CreateInvocationTargetSchemasAsync(
            string directory,
            string rootNamespace,
            IEnumerable<string> methodReferences,
            string outputFile
        ) {
            const string ASYNC_SUFFIX = "Async";
            var schemas = new Dictionary<string, InvocationTargetDefinition>();

            // create a list of nested namespaces from the root namespace
            var namespaces = new List<string>();
            if(!string.IsNullOrEmpty(rootNamespace)) {
                var parts = rootNamespace.Split(".");
                for(var i = 0; i < parts.Length; ++i) {
                    namespaces.Add(string.Join(".", parts.Take(i + 1)) + ".");
                }
            }
            namespaces.Add("");
            namespaces.Reverse();

            // enumerate type methods
            Console.WriteLine($"Inspecting method invocation targets in {directory}");
            foreach(var methodReference in methodReferences.Distinct()) {
                InvocationTargetDefinition entryPoint = null;
                try {

                    // extract class and method names from method reference
                    if(!StringEx.TryParseAssemblyClassMethodReference(methodReference, out var assemblyName, out var typeName, out var methodName)) {
                        throw new ProcessTargetInvocationException($"method reference '{methodReference}' is not well formed");
                    }

                    // load assembly
                    Assembly assembly;
                    var assemblyFilepath = Path.Combine(directory, assemblyName + ".dll");
                    try {
                        assembly = Assembly.LoadFrom(assemblyFilepath);
                    } catch(FileNotFoundException) {
                        throw new ProcessTargetInvocationException($"could not find assembly '{assemblyFilepath}'");
                    } catch(Exception e) {
                        throw new ProcessTargetInvocationException($"error loading assembly '{assemblyFilepath}': {e.Message}");
                    }

                    // find type in assembly
                    var type = namespaces.Select(ns => assembly.GetType(ns + typeName)).Where(t => t != null).FirstOrDefault();
                    if(type == null) {
                        throw new ProcessTargetInvocationException($"could not find type for '{methodReference}' in assembly '{assembly.FullName}'");
                    }

                    // find method, optionally with 'Async' suffix
                    var method = type.GetMethod(methodName);
                    if((method == null) && !methodName.EndsWith(ASYNC_SUFFIX, StringComparison.Ordinal)) {
                        methodName += ASYNC_SUFFIX;
                        method = type.GetMethod(methodName);
                    }
                    if(method == null) {
                        throw new ProcessTargetInvocationException($"could not find method '{methodName}' in type '{type.FullName}'");
                    }
                    var resolvedMethodReference = $"{assemblyName}::{type.FullName}::{method.Name}";
                    var operationName = methodName.EndsWith(ASYNC_SUFFIX, StringComparison.Ordinal)
                        ? methodName.Substring(0, methodName.Length - ASYNC_SUFFIX.Length)
                        : methodName;

                    // process method parameters
                    ParameterInfo requestParameter = null;
                    ParameterInfo proxyRequestParameter = null;
                    var uriParameters = new List<KeyValuePair<string, bool>>();
                    var parameters = method.GetParameters();
                    foreach(var parameter in parameters) {

                        // check if [FromUri] or [FromBody] attributes are present
                        var customAttributes = parameter.GetCustomAttributes(true);
                        var hasFromUriAttribute = customAttributes.Any(attribute => attribute.GetType().FullName == "LambdaSharp.ApiGateway.FromUriAttribute");
                        var hasFromBodyAttribute = customAttributes.Any(attribute => attribute.GetType().FullName == "LambdaSharp.ApiGateway.FromBodyAttribute");
                        if(hasFromUriAttribute && hasFromBodyAttribute) {
                            throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameter '{parameter.Name}' cannot have both [FromUri] and [FromBody] attributes");
                        }

                        // check if parameter is a proxy request
                        var isProxyRequest = parameter.ParameterType.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest";
                        if(isProxyRequest) {
                            if(hasFromUriAttribute || hasFromBodyAttribute) {
                                throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameter '{parameter.Name}' of type 'APIGatewayProxyRequest' cannot have [FromUri] or [FromBody] attribute");
                            }
                            if(proxyRequestParameter != null) {
                                throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameters '{requestParameter.Name}' and '{parameter.Name}' conflict on proxy request");
                            }
                            proxyRequestParameter = parameter;
                            continue;
                        }

                        // check if parameter needs to deserialized from URI or BODY
                        var isSimpleType = parameter.ParameterType.IsValueType || (parameter.ParameterType == typeof(string));
                        if((isSimpleType && !hasFromBodyAttribute) || hasFromUriAttribute) {

                            // check if parameter is read from URI string directly or if its members are read from the URI string
                            if(isSimpleType) {

                                // parameter is required only if it does not have an optional value and is not nullable
                                uriParameters.Add(new KeyValuePair<string, bool>(parameter.Name, !parameter.IsOptional && (Nullable.GetUnderlyingType(parameter.ParameterType) == null) && (parameter.ParameterType.IsValueType || parameter.ParameterType == typeof(string))));
                            } else {
                                var queryParameterType = parameter.ParameterType;

                                // add complex-type properties
                                foreach(var property in queryParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                                    var name = property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? property.Name;
                                    var required = (
                                            (Nullable.GetUnderlyingType(property.PropertyType) == null)
                                            && (property.PropertyType.IsValueType || (property.PropertyType == typeof(string)))
                                            && (property.GetCustomAttribute<JsonPropertyAttribute>()?.Required != Required.Default)
                                            && (property.GetCustomAttribute<JsonPropertyAttribute>()?.Required != Required.DisallowNull)
                                        )
                                        || (property.GetCustomAttribute<JsonRequiredAttribute>() != null)
                                        || (property.GetCustomAttribute<JsonPropertyAttribute>()?.Required == Required.Always)
                                        || (property.GetCustomAttribute<JsonPropertyAttribute>()?.Required == Required.AllowNull);
                                    uriParameters.Add(new KeyValuePair<string, bool>(name, required));
                                }

                                // add complex-type fields
                                foreach(var field in queryParameterType.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                                    var name = field.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? field.Name;
                                    var required = (
                                            (Nullable.GetUnderlyingType(field.FieldType) == null)
                                            && (field.FieldType.IsValueType || (field.FieldType == typeof(string)))
                                            && (field.GetCustomAttribute<JsonPropertyAttribute>()?.Required != Required.Default)
                                            && (field.GetCustomAttribute<JsonPropertyAttribute>()?.Required != Required.DisallowNull)
                                        )
                                        || (field.GetCustomAttribute<JsonRequiredAttribute>() != null)
                                        || (field.GetCustomAttribute<JsonPropertyAttribute>()?.Required == Required.Always)
                                        || (field.GetCustomAttribute<JsonPropertyAttribute>()?.Required == Required.AllowNull);
                                    uriParameters.Add(new KeyValuePair<string, bool>(name, required));
                                }
                            }
                        } else {
                            if(requestParameter != null) {
                                throw new ProcessTargetInvocationException($"{resolvedMethodReference} parameters '{requestParameter.Name}' and '{parameter.Name}' conflict on request body");
                            }
                            requestParameter = parameter;
                        }
                    }

                    // check if no specific request parameter was present, but the method also takes a proxy request
                    if((requestParameter == null) && (proxyRequestParameter != null)) {
                        requestParameter = proxyRequestParameter;
                    }

                    // process method request type
                    var requestSchemaAndContentType = await AddSchema(methodReference, $"for parameter '{requestParameter?.Name}'", requestParameter?.ParameterType);

                    // process method response type
                    var responseType = (method.ReturnType.IsGenericType) && (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                        ? method.ReturnType.GetGenericArguments()[0]
                        : method.ReturnType;
                    var responseSchemaAndContentType = await AddSchema(method.Name, "as return value", responseType);
                    entryPoint = new InvocationTargetDefinition {
                        Assembly = assemblyName,
                        Type = type.FullName,
                        Method = methodName,
                        OperationName = operationName,
                        RequestContentType = requestSchemaAndContentType?.Item2,
                        RequestSchema = requestSchemaAndContentType?.Item1,
                        RequestSchemaName = requestParameter?.ParameterType.FullName,
                        UriParameters = uriParameters.Any() ? new Dictionary<string, bool>(uriParameters) : null,
                        ResponseContentType = responseSchemaAndContentType?.Item2,
                        ResponseSchema = responseSchemaAndContentType?.Item1,
                        ResponseSchemaName = responseType?.FullName
                    };

                    // write result
                    Console.WriteLine($"... {resolvedMethodReference}({string.Join(", ", uriParameters.Select(kv => kv.Key))}) {entryPoint.GetRequestSchemaName()} -> {entryPoint.GetResponseSchemaName()}");
                } catch(ProcessTargetInvocationException e) {
                    entryPoint = new InvocationTargetDefinition {
                        Error = e.Message
                    };
                } catch(Exception e) {
                    entryPoint = new InvocationTargetDefinition {
                        Error = $"internal error: {e.Message}",
                        StackTrace = e.StackTrace
                    };
                }
                if(entryPoint != null) {
                    schemas.Add(methodReference, entryPoint);
                } else {
                    schemas.Add(methodReference, new InvocationTargetDefinition {
                        Error = "internal error: missing target definition"
                    });
                }
            }

            // create json document
            try {
                var output = JsonConvert.SerializeObject(schemas, Formatting.Indented);
                if(outputFile != null) {
                    File.WriteAllText(outputFile, output);
                } else {
                    Console.WriteLine(output);
                }
            } catch(Exception e) {
                LogError("unable to write schema", e);
            }

            // local functions
            async Task<Tuple<JToken, string>> AddSchema(string methodReference, string parameterName, Type messageType) {

                // check if there is no request type
                if(messageType == null) {
                    return Tuple.Create(JToken.FromObject("Void"), (string)null);
                }

                // check if there is no response type
                if(
                    (messageType == typeof(void))
                    || (messageType == typeof(Task))
                ) {
                    return Tuple.Create(JToken.FromObject("Void"), (string)null);
                }

                // check if request/response type is not supported
                if(
                    (messageType == typeof(string))
                    || messageType.IsValueType
                ) {
                    throw new ProcessTargetInvocationException($"{methodReference} has unsupported type {parameterName}");
                }

                // check if request/response type is inside 'Task<T>'
                if(messageType.IsGenericType && messageType.GetGenericTypeDefinition() == typeof(Task<>)) {
                    messageType = messageType.GetGenericArguments()[0];
                }

                // check if request/response has an open-ended schema
                if(
                    (messageType == typeof(object))
                    || (messageType == typeof(JObject))
                ) {
                    return Tuple.Create(JToken.FromObject("Object"), "application/json");
                }

                // check if request/response is not a proxy request/response
                if(
                    (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest")
                    && (messageType.FullName != "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse")
                ) {
                    var schema = await JsonSchema4.FromTypeAsync(messageType, new JsonSchemaGeneratorSettings {
                        FlattenInheritanceHierarchy = true,

#pragma warning disable CS0618
                        // we prefer enums to be handled as strings (NOTE: trying to set this in SerializerSettings causes an NRE in JsonSchema4FromTypeAsync call)
                        DefaultEnumHandling = EnumHandling.String
#pragma warning restore CS0618
                    });

                    // NOTE (2019-04-03, bjorg): we need to allow additional properties, because Swagger doesn't support: "additionalProperties": false
                    schema.AllowAdditionalProperties = true;
                    foreach(var definition in schema.Definitions) {
                        definition.Value.AllowAdditionalProperties = true;
                    }

                    // NOTE (2019-08-16, bjorg): don't emit "x-enumNames" as it is not supported by API Gateway
                    foreach(var definition in schema.Definitions) {
                        definition.Value.EnumerationNames = null;
                    }

                    // return JSON schema document
                    return Tuple.Create((JToken)JObject.Parse(schema.ToJson()), "application/json");
                }
                return Tuple.Create(JToken.FromObject("Proxy"), (string)null);
            }
        }

        public async Task ListLambdasAsync(string awsProfile, string awsRegion) {
            Console.WriteLine();

            // initialize AWS profile
            await InitializeAwsProfile(awsProfile, awsRegion: awsRegion, allowCaching: true);
            var cfnClient = new AmazonCloudFormationClient(AWSConfigs.RegionEndpoint);
            var lambdaClient = new AmazonLambdaClient(AWSConfigs.RegionEndpoint);
            var logsClient = new AmazonCloudWatchLogsClient(AWSConfigs.RegionEndpoint);

            // fetch all Lambda functions on account
            var globalFunctions = (await ListLambdasAsync())
                .ToDictionary(function => function.FunctionName, function => function);

            // fetch all stacks on account
            var stacks = await ListStacksAsync();
            Console.WriteLine($"Analyzing {stacks.Count():N0} CloudFormation stacks and {globalFunctions.Count():N0} Lambda functions");

            // fetch most recent CloudWatch log stream for each Lambda function
            var logStreamsTask = Task.Run(async () => (await Task.WhenAll(globalFunctions.Select(async kv => {
                try {
                    var response = await logsClient.DescribeLogStreamsAsync(new DescribeLogStreamsRequest {
                        Descending = true,
                        LogGroupName = $"/aws/lambda/{kv.Value.FunctionName}",
                        OrderBy = OrderBy.LastEventTime,
                        Limit = 1
                    });
                    return (Name: kv.Value.FunctionName, Streams: response.LogStreams.FirstOrDefault());
                } catch {

                    // log group doesn't exist
                    return (Name: kv.Value.FunctionName, Streams: null);
                }
            }))).ToDictionary(tuple => tuple.Name, tuple => tuple.Streams));

            // fetch all functions belonging to a CloudFormation stack
            var stacksWithFunctionsTask = Task.Run(async () => stacks.Zip(
                    await Task.WhenAll(stacks.Select(stack => ListStackFunctionsAsync(stack.StackId))),
                    (stack, stackFunctions) => (Stack: stack, Functions: stackFunctions)
                ).ToList()
            );

            // wait for both fetch operations to finish
            await Task.WhenAll(logStreamsTask, stacksWithFunctionsTask);
            var logStreams = logStreamsTask.GetAwaiter().GetResult();
            var stacksWithFunctions = stacksWithFunctionsTask.GetAwaiter().GetResult();

            // remove all the functions that were discovered inside a stack from the orphaned list of functions
            foreach(var function in stacksWithFunctions.SelectMany(stackWithFunctions => stackWithFunctions.Functions)) {
                globalFunctions.Remove(function.Configuration.FunctionName);
            }

            // compute the max width for the function name (use logical ID if it belongs to the stack)
            var maxFunctionNameWidth = stacksWithFunctions
                .SelectMany(stackWithFunctions => stackWithFunctions.Functions)
                .Select(function => function.Name.Length)
                .Union(globalFunctions.Values.Select(function => function.FunctionName.Length))
                .Append(0)
                .Max();

            // compute max width for the function runtime name
            var maxRuntimeWidth = stacksWithFunctions
                .SelectMany(stackWithFunctions => stackWithFunctions.Functions)
                .Select(stackFunction => stackFunction.Configuration.Runtime.ToString().Length)
                .Union(globalFunctions.Values.Select(function => function.Runtime.ToString().Length))
                .Append(0)
                .Max();

            // print Lambda functions belonging to stacks
            var showAsteriskExplanation = false;
            foreach(var stackWithFunctions in stacksWithFunctions
                .Where(stackWithFunction => stackWithFunction.Functions.Any())
                .OrderBy(stackWithFunction => stackWithFunction.Stack.StackName)
            ) {
                Console.WriteLine();

                // check if CloudFormation stack was deployed by LambdaSharp
                var moduleInfoOutput = stackWithFunctions.Stack.Outputs
                    .FirstOrDefault(output => (output.OutputKey == "ModuleInfo") || (output.OutputKey == "Module"))
                    ?.OutputValue;
                var lambdaSharpToolOutput = stackWithFunctions.Stack.Outputs
                    .FirstOrDefault(output => output.OutputKey == "LambdaSharpTool")
                    ?.OutputValue;

                // NOTE (2020-05-06, bjorg): pre-0.6, the module information was emitted as two output values
                var moduleNameOutput = stackWithFunctions.Stack.Outputs
                    .FirstOrDefault(output => output.OutputKey == "ModuleName")
                    ?.OutputValue;
                var moduleVersionOutput = stackWithFunctions.Stack.Outputs
                    .FirstOrDefault(output => output.OutputKey == "ModuleVersion")
                    ?.OutputValue;

                // show CloudFormation stack name and optionally LambdaSharp module information
                Console.Write($"{Settings.OutputColor}{stackWithFunctions.Stack.StackName}{Settings.ResetColor}");
                if(ModuleInfo.TryParse(moduleInfoOutput, out var moduleInfo)) {
                    Console.Write($" ({Settings.InfoColor}{moduleInfo.FullName}:{moduleInfo.Version}{Settings.ResetColor}) [lash {lambdaSharpToolOutput ?? "pre-0.6.1"}]");
                } else if((moduleNameOutput != null) && (moduleVersionOutput != null)) {
                    Console.Write($" ({Settings.InfoColor}{moduleNameOutput}:{moduleVersionOutput}{Settings.ResetColor}) [lash pre-0.6]");
                }
                Console.WriteLine(":");

                foreach(var function in stackWithFunctions.Functions.OrderBy(function => function.Name)) {
                    PrintFunction(function.Name, function.Configuration);
                }
            }

            // print orphan Lambda functions
            if(globalFunctions.Any()) {
                Console.WriteLine();
                Console.WriteLine("ORPHANS:");
                foreach(var function in globalFunctions.Values.OrderBy(function => function.FunctionName)) {
                    PrintFunction(function.FunctionName, function);
                }
            }

            // show optional (*) explanation if it was printed
            if(showAsteriskExplanation) {
                Console.WriteLine();
                Console.WriteLine("(*) Showing Lambda last-modified date, because last event timestamp in CloudWatch log stream is not available");
            }

            // local functions
            async Task<IEnumerable<FunctionConfiguration>> ListLambdasAsync() {
                var result = new List<FunctionConfiguration>();
                var request = new ListFunctionsRequest();
                do {
                    var response = await lambdaClient.ListFunctionsAsync(request);
                    result.AddRange(response.Functions);
                    request.Marker = response.NextMarker;
                } while(request.Marker != null);
                return result;
            }

            async Task<IEnumerable<Stack>> ListStacksAsync() {
                var result = new List<Stack>();
                var request = new DescribeStacksRequest();
                do {
                    var response = await cfnClient.DescribeStacksAsync(request);
                    result.AddRange(response.Stacks);
                    request.NextToken = response.NextToken;
                } while(request.NextToken != null);
                return result;
            }

            async Task<IEnumerable<(string Name, FunctionConfiguration Configuration)>> ListStackFunctionsAsync(string stackName) {
                var result = new List<(string, FunctionConfiguration)>();
                var request = new ListStackResourcesRequest {
                    StackName = stackName
                };
                do {
                    var attempts = 0;
                again:
                    try {
                        var response = await cfnClient.ListStackResourcesAsync(request);
                        result.AddRange(
                            response.StackResourceSummaries
                                .Where(resourceSummary => resourceSummary.ResourceType == "AWS::Lambda::Function")
                                .Select(summary => {
                                    globalFunctions.TryGetValue(summary.PhysicalResourceId, out var configuration);
                                    return (Name: summary.LogicalResourceId, Configuration: configuration);
                                })
                                .Where(tuple => tuple.Configuration != null)
                        );
                        request.NextToken = response.NextToken;
                    } catch(AmazonCloudFormationException e) when(
                        (e.Message == "Rate exceeded")
                        && (++attempts < 30)
                    ) {
                        await Task.Delay(TimeSpan.FromSeconds(attempts));
                        goto again;
                    }
                } while(request.NextToken != null);
                return result;
            }

            void PrintFunction(string name, FunctionConfiguration function) {
                Console.Write("    ");
                Console.Write(name);
                Console.Write("".PadRight(maxFunctionNameWidth - name.Length + 4));
                Console.Write(function.Runtime);
                Console.Write("".PadRight(maxRuntimeWidth - function.Runtime.ToString().Length + 4));
                if(
                    !logStreams.TryGetValue(function.FunctionName, out var logStream)
                    || (logStream?.LastEventTimestamp == null)
                ) {
                    Console.Write(DateTimeOffset.Parse(function.LastModified).ToString("yyyy-MM-dd"));
                    Console.Write("(*)");
                    showAsteriskExplanation = true;
                } else {
                    Console.Write(logStream.LastEventTimestamp.ToString("yyyy-MM-dd"));
                }
                Console.WriteLine();
            }
        }

        public void ValidateAssembly(string directory, string methodReference, string outputFile) {
            try {
                if(!StringEx.TryParseAssemblyClassMethodReference(methodReference, out var assemblyName, out var typeName, out var methodName)) {
                    throw new ProcessTargetInvocationException($"method reference '{methodReference}' is not well formed");
                }
                var output = Program.Quiet ? null : Console.Out;

                // load assembly
                Assembly assembly;
                var assemblyFilepath = Path.Combine(directory, assemblyName + ".dll");
                try {
                    assembly = Assembly.LoadFrom(assemblyFilepath);
                } catch(FileNotFoundException) {
                    throw new ProcessTargetInvocationException($"could not find assembly '{assemblyFilepath}'");
                } catch(Exception e) {
                    throw new ProcessTargetInvocationException($"error loading assembly '{assemblyFilepath}': {e.Message}");
                }

                // check for Lambda serialization assembly attribute
                var lambdaSerializationAssemblyAttribute = assembly
                    .GetCustomAttributes(typeof(Amazon.Lambda.Core.LambdaSerializerAttribute), false)
                    .OfType<Amazon.Lambda.Core.LambdaSerializerAttribute>()
                    .FirstOrDefault();
                if(lambdaSerializationAssemblyAttribute != null) {
                    switch(lambdaSerializationAssemblyAttribute.SerializerType.FullName) {
                    case "LambdaSharp.Serialization.LambdaJsonSerializer":
                    case "Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer":
                    case "Amazon.Lambda.Serialization.Json.JsonSerializer":
                        throw new ProcessTargetInvocationException($"remove Lambda serializer attribute: [assembly: LambdaSerializer(typeof(...))]");
                    default:
                        var current = lambdaSerializationAssemblyAttribute.SerializerType;
                        while(current.BaseType != null) {
                            current = current.BaseType;
                            var name = current.FullName;
                            if(name == "LambdaSharp.Serialization.LambdaJsonSerializer") {

                                // custom serializer derives from LambdaSharp serializer, which is okay
                                output?.WriteLine("=> Custom Lambda serializer is derived from LambdaSharp.Serialization.LambdaJsonSerializer");
                                break;
                            } else if((name == "Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer") || (name == "Amazon.Lambda.Serialization.Json.JsonSerializer")) {
                                throw new ProcessTargetInvocationException($"custom Lambda serializer must derive from LambdaSharp.Serialization.LambdaJsonSerializer");
                            }
                        }
                        break;
                    }
                } else {
                    output?.WriteLine("=> Default Lambda serializer used");
                }

                // load Lambda function class
                var type = assembly.GetType(typeName);
                if(type == null) {
                    throw new ProcessTargetInvocationException($"could not find type '{typeName}'");
                }

                // find the Lambda entry point method
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                if(method == null) {
                    throw new ProcessTargetInvocationException($"could not find method '{methodName}' in class '{typeName}'");
                }
                output?.WriteLine("=> Entry-point class and method are valid");
            } catch(ProcessTargetInvocationException e) {
                LogError(e.Message);
            }
        }

        private async Task ShowFailedKinesisProcessingLogs(Settings settings, string keyPrefix) {

            // populate information about the deployment tier
            await PopulateDeploymentTierSettingsAsync(settings);
            if(settings.LoggingBucketName == null) {
                LogError("Deployment tier does not a have logging bucket");
                return;
            }

            // list all objects under the specified key
            var request = new ListObjectsV2Request {
                BucketName = settings.LoggingBucketName,
                Prefix = keyPrefix,
                MaxKeys = 100
            };
            do {
                var response = await settings.S3Client.ListObjectsV2Async(request);
                foreach(var s3Object in response.S3Objects) {
                    Console.WriteLine($"{Settings.OutputColor}Key:{Settings.ResetColor}: {s3Object.Key}");
                    try {
                        var records = await GetS3ObjectContentsAsync(s3Object.Key);
                        foreach(var record in records) {
                            Console.WriteLine($"{Settings.InfoColor}ArrivalTime:{Settings.ResetColor} {DateTimeOffset.FromUnixTimeMilliseconds(record.arrivalTimestamp)}");
                            Console.WriteLine($"{Settings.InfoColor}AttemptEndingTimestamp:{Settings.ResetColor} {DateTimeOffset.FromUnixTimeMilliseconds(record.attemptEndingTimestamp)}");
                            Console.WriteLine($"{Settings.InfoColor}AttemptsMade:{Settings.ResetColor} {record.attemptsMade}");
                            Console.WriteLine($"{Settings.InfoColor}ErrorCode:{Settings.ResetColor} {record.errorCode}");
                            Console.WriteLine($"{Settings.InfoColor}ErrorMessage:{Settings.ResetColor} {record.errorMessage}");
                            Console.WriteLine($"{Settings.InfoColor}Lambda ARN:{Settings.ResetColor} {record.lambdaArn}");
                            var entries = await DecodeBase64GzipDataAsync(record.rawData);
                            Console.WriteLine($"{Settings.InfoColor}Entries:{Settings.ResetColor} {JObject.Parse(entries).ToString(Formatting.Indented)}");
                        }
                    } catch(Exception e) {
                        LogError($"unable to read records for {s3Object.Key}", e);
                    }
                    Console.WriteLine();
                }
                request.ContinuationToken = response.NextContinuationToken;
            } while(request.ContinuationToken != null);

            // local functions
            async Task<IEnumerable<KinesisFailedLogRecord>> GetS3ObjectContentsAsync(string key) {
                try {
                    var response = await settings.S3Client.GetObjectAsync(new GetObjectRequest {
                        BucketName = settings.LoggingBucketName,
                        Key = key
                    });
                    using(var decompressionStream = new GZipStream(response.ResponseStream, CompressionMode.Decompress))
                    using(var destinationStream = new MemoryStream()) {
                        await decompressionStream.CopyToAsync(destinationStream);
                        return Encoding.UTF8.GetString(destinationStream.ToArray())
                            .Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries)
                            .Select(json => JsonConvert.DeserializeObject<KinesisFailedLogRecord>(json.Trim()));
                    }
                } catch(AmazonS3Exception) {
                    return null;
                }
            }

            async Task<string> DecodeBase64GzipDataAsync(string data) {
                using(var sourceStream = new MemoryStream(Convert.FromBase64String(data)))
                using(var decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                using(var destinationStram = new MemoryStream()) {
                    await decompressionStream.CopyToAsync(destinationStram);
                    return Encoding.UTF8.GetString(destinationStram.ToArray());
                }
            }
        }

        public async Task ListModulesAsync(Settings settings, string lookupValue, string bucketName, string origin, bool includePreRelease) {
            Console.WriteLine();

            // check if lookup value is a module reference
            if(ModuleInfo.TryParse(lookupValue, out var moduleInfo)) {
                bucketName ??= moduleInfo.Origin;
                origin ??= moduleInfo.Origin;
            } else {

                // use lookup value a default for bucket and origin unless explicitly provided
                bucketName ??= lookupValue;
                origin ??= lookupValue;
            }

            // list all modules at bucket
            var moduleLocations = await new ModelManifestLoader(settings, "cmd-line").ListManifestsAsync(bucketName, origin, includePreRelease);
            if(moduleLocations.Any()) {
                foreach(var moduleGroup in moduleLocations
                    .Where(moduleLocation => (moduleInfo == null) || (moduleLocation.ModuleInfo.FullName == moduleInfo.FullName))
                    .GroupBy(moduleLocation => moduleLocation.ModuleInfo.FullName)
                    .OrderBy(group => group.Key)
                ) {
                    var versions = moduleGroup.Select(moduleLocation => moduleLocation.ModuleInfo.Version).ToList();
                    versions.Sort(new VersionInfoComparer());
                    Console.WriteLine($"{moduleGroup.Key}: {string.Join(", ", versions.Select(version => $"{Settings.InfoColor}{version}{Settings.ResetColor}"))}");
                }
            } else {
                Console.WriteLine("no modules found");
            }
        }
    }
}
