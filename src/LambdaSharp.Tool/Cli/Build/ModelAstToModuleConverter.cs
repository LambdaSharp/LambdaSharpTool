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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using LambdaSharp.Tool.Model.AST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelAstToModuleConverter : AModelProcessor {

        //--- Constants ---
        private const string SECRET_ALIAS_PATTERN = "[0-9a-zA-Z/_\\-]+";

        //--- Fields ---
        private ModuleBuilder _builder;

        //--- Constructors ---
        public ModelAstToModuleConverter(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public ModuleBuilder Convert(ModuleNode module) {

            // convert module definition
            try {

                // ensure version is present
                VersionInfo version;
                if(module.Version == null) {
                    version = VersionInfo.Parse("1.0-DEV");
                } else if(!VersionInfo.TryParse(module.Version, out version)) {
                    LogError("'Version' expected to have format: Major.Minor[.Build[.Revision]]");
                    version = VersionInfo.Parse("0.0");
                }

                // ensure namespace is present
                if(!module.Module.TryParseModuleFullName(out string moduleNamespace, out var moduleName)) {
                    LogError("'Module' attribute must have format 'Namespace.Name'");
                }

                // initialize module
                _builder = new ModuleBuilder(Settings, SourceFilename, new Module {
                    Namespace = moduleNamespace,
                    Name = moduleName,
                    Version = version,
                    Description = module.Description
                });

                // convert collections
                ForEach("Pragmas", module.Pragmas, ConvertPragma);
                ForEach("Secrets", module.Secrets, ConvertSecret);
                ForEach("Using", module.Using, ConvertUsing);
                ForEach("Items", module.Items, ConvertItem);
                return _builder;
            } catch(Exception e) {
                LogError(e);
                return null;
            }
        }

        private void ConvertPragma(int index, object pragma) {
            AtLocation($"{index}", () => _builder.AddPragma(pragma));
        }

        private void ConvertSecret(int index, string secret) {
            AtLocation($"{index}", () => {
                if(string.IsNullOrEmpty(secret)) {
                    LogError($"secret has no value");
                } else if(secret.Equals("aws/ssm", StringComparison.OrdinalIgnoreCase)) {
                    LogError($"cannot grant permission to decrypt with aws/ssm");
                } else if(secret.StartsWith("arn:")) {
                    if(!Regex.IsMatch(secret, $"arn:aws:kms:{Settings.AwsRegion}:{Settings.AwsAccountId}:key/[a-fA-F0-9\\-]+")) {
                        LogError("secret key must be a valid ARN for the current region and account ID");
                    }
                } else if(!Regex.IsMatch(secret, SECRET_ALIAS_PATTERN)) {
                    LogError("secret key must be a valid alias");
                }
                _builder.AddSecret(secret);
            });
        }

        private void ConvertUsing(int index, ModuleDependencyNode dependency) {
            AtLocation($"{index}", () => {
                if(!ModuleInfo.TryParse(dependency.Module, out var moduleInfo)) {
                    LogError("invalid module reference format");
                    return;
                }
                _builder.AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Shared).Wait();
            });
        }

        private AFunctionSource ConvertFunctionSource(ModuleItemNode function, int index, FunctionSourceNode source) {
            var type = DeterminNodeType("source", index, source, FunctionSourceNode.FieldCombinations, new[] {
                "Api",
                "Schedule",
                "S3",
                "SlackCommand",
                "Topic",
                "Sqs",
                "Alexa",
                "DynamoDB",
                "Kinesis",
                "WebSocket"
            });
            switch(type) {
            case "Api":
                return AtLocation("Api", () => {

                    // extract http method from route
                    var api = source.Api.Trim();
                    var pathSeparatorIndex = api.IndexOfAny(new[] { ':', ' ' });
                    if(pathSeparatorIndex < 0) {
                        LogError("invalid api format");
                        return new RestApiSource {
                            HttpMethod = "ANY",
                            Path = new string[0],
                            Integration = ApiGatewaySourceIntegration.RequestResponse
                        };
                    }
                    var method = api.Substring(0, pathSeparatorIndex).ToUpperInvariant();
                    if(method == "*") {
                        method = "ANY";
                    }
                    var path = api.Substring(pathSeparatorIndex + 1).TrimStart().Split('/', StringSplitOptions.RemoveEmptyEntries);

                    // parse integration into a valid enum
                    var integration = AtLocation("Integration", () => Enum.Parse<ApiGatewaySourceIntegration>(source.Integration ?? "RequestResponse", ignoreCase: true));
                    return new RestApiSource {
                        HttpMethod = method,
                        Path = path,
                        Integration = integration,
                        OperationName = source.OperationName,
                        ApiKeyRequired = source.ApiKeyRequired,
                        Invoke = source.Invoke
                    };
                });
            case "Schedule":
                return AtLocation("Schedule", () => new ScheduleSource {
                    Expression = source.Schedule,
                    Name = source.Name
                });
            case "S3":
                return AtLocation("S3", () => new S3Source {
                    Bucket = source.S3,
                    Events = source.Events ?? new List<string> {

                        // default S3 events to listen to
                        "s3:ObjectCreated:*"
                    },
                    Prefix = source.Prefix,
                    Suffix = source.Suffix
                });
            case "SlackCommand":
                return AtLocation("SlackCommand", () => new RestApiSource {
                    HttpMethod = "POST",
                    Path = source.SlackCommand.Split('/', StringSplitOptions.RemoveEmptyEntries),
                    Integration = ApiGatewaySourceIntegration.SlackCommand,
                    OperationName = source.OperationName
                });
            case "Topic":
                return AtLocation("Topic", () => new TopicSource {
                    TopicName = source.Topic,
                    Filters = source.Filters
                });
            case "Sqs":
                return AtLocation("Sqs", () => new SqsSource {
                    Queue = source.Sqs,
                    BatchSize = source.BatchSize ?? 10
                });
            case "Alexa":
                return AtLocation("Alexa", () => new AlexaSource {
                    EventSourceToken = source.Alexa
                });
            case "DynamoDB":
                return AtLocation("DynamoDB", () => new DynamoDBSource {
                    DynamoDB = source.DynamoDB,
                    BatchSize = source.BatchSize ?? 100,
                    StartingPosition = source.StartingPosition ?? "LATEST"
                });
            case "Kinesis":
                return AtLocation("Kinesis", () => new KinesisSource {
                    Kinesis = source.Kinesis,
                    BatchSize = source.BatchSize ?? 100,
                    StartingPosition = source.StartingPosition ?? "LATEST"
                });
            case "WebSocket":
                return AtLocation("WebSocket", () => new WebSocketSource {
                    RouteKey = source.WebSocket.Trim(),
                    OperationName = source.OperationName,
                    ApiKeyRequired = source.ApiKeyRequired,
                    Invoke = source.Invoke
                });
            }
            return null;
        }

        private void ConvertItem(int index, ModuleItemNode node)
            => ConvertItem(null, index, node, new[] {
                "Condition",
                "Function",
                "Group",
                "Import",
                "Macro",
                "Mapping",
                "Nested",
                "Package",
                "Parameter",
                "Resource",
                "ResourceType",
                "Variable"
            });

        private void ConvertItem(AModuleItem parent, int index, ModuleItemNode node, IEnumerable<string> expectedTypes) {
            var type = DeterminNodeType("item", index, node, ModuleItemNode.FieldCombinations, expectedTypes);
            switch(type) {
            case "Parameter":
                AtLocation(node.Parameter, () => {

                    // validation
                    if(node.Properties != null) {
                        Validate(node.Type != null, "'Type' attribute is required");
                    }
                    Validate((node.Allow == null) || (node.Type == "AWS") || ResourceMapping.IsCloudFormationType(node.Type), "'Allow' attribute can only be used with AWS resource types");
                    Validate(parent == null, "'Parameter' cannot be nested");

                    // create input parameter item
                    _builder.AddParameter(
                        name: node.Parameter,
                        section: node.Section,
                        label: node.Label,
                        description: node.Description,
                        type: node.Type ?? "String",
                        scope: ConvertScope(node.Scope),
                        noEcho: node.NoEcho,
                        defaultValue: node.Default,
                        constraintDescription: node.ConstraintDescription,
                        allowedPattern: node.AllowedPattern,
                        allowedValues: node.AllowedValues,
                        maxLength: node.MaxLength,
                        maxValue: node.MaxValue,
                        minLength: node.MinLength,
                        minValue: node.MinValue,
                        allow: node.Allow,
                        properties: ParseToDictionary("Properties", node.Properties),
                        arnAttribute: node.DefaultAttribute,
                        encryptionContext: node.EncryptionContext,
                        pragmas: node.Pragmas
                    );
                });
                break;
            case "Import":
                AtLocation(node.Import, () => {

                    // validation
                    Validate((node.Allow == null) || (node.Type == "AWS") || ResourceMapping.IsCloudFormationType(node.Type), "'Allow' attribute can only be used with AWS resource types");
                    Validate(node.Module != null, "missing 'Module' attribute");

                    // create input parameter item
                    _builder.AddImport(
                        parent: parent,
                        name: node.Import,
                        description: node.Description,
                        type: node.Type ?? "String",
                        scope: ConvertScope(node.Scope),
                        allow: node.Allow,
                        module: node.Module ?? "Bad.Module",
                        encryptionContext: node.EncryptionContext
                    );
                });
                break;
            case "Variable":
                AtLocation(node.Variable, () => {

                    // validation
                    Validate(node.Value != null, "missing 'Value' attribute");
                    Validate((node.EncryptionContext == null) || (node.Type == "Secret"), "item must have Type 'Secret' to use 'EncryptionContext' section");
                    Validate((node.Type != "Secret") || !(node.Value is IList<object>), "item with type 'Secret' cannot have a list of values");

                    // create variable item
                    _builder.AddVariable(
                        parent: parent,
                        name: node.Variable,
                        description: node.Description,
                        type: node.Type ?? "String",
                        scope: ConvertScope(node.Scope),
                        value: node.Value ?? "",
                        allow: null,
                        encryptionContext: node.EncryptionContext
                    );
                });
                break;
            case "Group":
                AtLocation(node.Group, () => {

                    // create namespace item
                    var result = _builder.AddVariable(
                        parent: parent,
                        name: node.Group,
                        description: node.Description,
                        type: "String",
                        scope: null,
                        value: "",
                        allow: null,
                        encryptionContext: null
                    );

                    // recurse
                    ConvertItems(result, expectedTypes);
                });
                break;
            case "Resource":
                AtLocation(node.Resource, () => {
                    if(node.Value != null) {

                        // validation
                        Validate((node.Allow == null) || (node.Type == null) || ResourceMapping.IsCloudFormationType(node.Type), "'Allow' attribute can only be used with AWS resource types");
                        Validate(node.If == null, "'If' attribute cannot be used with a referenced resource");
                        Validate(node.Properties == null, "'Properties' section cannot be used with a referenced resource");
                        if(node.Value is IList<object> values) {
                            foreach(var arn in values) {
                                ValidateARN(arn);
                            }
                        } else {
                            ValidateARN(node.Value);
                        }

                        // create variable item
                        _builder.AddVariable(
                            parent: parent,
                            name: node.Resource,
                            description: node.Description,
                            type: node.Type ?? "String",
                            scope: ConvertScope(node.Scope),
                            value: node.Value,
                            allow: node.Allow,
                            encryptionContext: node.EncryptionContext
                        );
                    } else {

                        // validation
                        Validate(node.Type != null, "missing 'Type' attribute");
                        Validate((node.Allow == null) || ResourceMapping.IsCloudFormationType(node.Type ?? ""), "'Allow' attribute can only be used with AWS resource types");

                        // create resource item
                        _builder.AddResource(
                            parent: parent,
                            name: node.Resource,
                            description: node.Description,
                            type: node.Type ?? "AWS",
                            scope: ConvertScope(node.Scope),
                            allow: node.Allow,
                            properties: ParseToDictionary("Properties", node.Properties),
                            dependsOn: ConvertToStringList(node.DependsOn),
                            arnAttribute: node.DefaultAttribute,
                            condition: node.If,
                            pragmas: node.Pragmas
                        );
                    }
                });
                break;
            case "Nested":
                AtLocation(node.Nested, () => {

                    // validation
                    if(node.Module == null) {
                        LogError("missing 'Module' attribute");
                    } else if(!ModuleInfo.TryParse(node.Module, out var moduleInfo)) {
                        LogError("invalid value for 'Module' attribute");
                    } else {

                        // create nested module item
                        _builder.AddNestedModule(
                            parent: parent,
                            name: node.Nested,
                            description: node.Description,
                            moduleInfo: moduleInfo,
                            scope: ConvertScope(node.Scope),
                            dependsOn: node.DependsOn,
                            parameters: node.Parameters
                        );
                    }
                });
                break;
            case "Package":

                // package resource
                AtLocation(node.Package, () => {

                    // discover files to package
                    var files = new List<KeyValuePair<string, string>>();
                    if(node.Files != null) {
                        string folder;
                        string filePattern;
                        SearchOption searchOption;
                        var packageFiles = Path.Combine(Settings.WorkingDirectory, node.Files);
                        if((packageFiles.EndsWith("/", StringComparison.Ordinal) || Directory.Exists(packageFiles))) {
                            folder = Path.GetFullPath(packageFiles);
                            filePattern = "*";
                            searchOption = SearchOption.AllDirectories;
                        } else {
                            folder = Path.GetDirectoryName(packageFiles);
                            filePattern = Path.GetFileName(packageFiles);
                            searchOption = SearchOption.TopDirectoryOnly;
                        }
                        if(Directory.Exists(folder)) {
                            foreach(var filePath in Directory.GetFiles(folder, filePattern, searchOption)) {
                                var relativeFilePathName = Path.GetRelativePath(folder, filePath);
                                files.Add(new KeyValuePair<string, string>(relativeFilePathName, filePath));
                            }
                            files = files.OrderBy(file => file.Key).ToList();
                        } else {
                            LogError($"cannot find folder '{Path.GetRelativePath(Settings.WorkingDirectory, folder)}'");
                        }
                    } else {
                        LogError("missing 'Files' attribute");
                    }

                    // create package resource item
                    _builder.AddPackage(
                        parent: parent,
                        name: node.Package,
                        description: node.Description,
                        scope: ConvertScope(node.Scope),
                        files: files
                    );
                });
                break;
            case "Function":
                AtLocation(node.Function, () => {

                    // validation
                    Validate(node.Memory != null, "missing 'Memory' attribute");
                    Validate(int.TryParse(node.Memory, out _), "invalid 'Memory' value");
                    Validate(node.Timeout != null, "missing 'Timeout' attribute");
                    Validate(int.TryParse(node.Timeout, out _), "invalid 'Timeout' value");
                    ValidateFunctionSource(node.Sources ?? new FunctionSourceNode[0]);

                    // determine function type
                    var project = node.Project;
                    var language = node.Language;
                    var runtime = node.Runtime;
                    var handler = node.Handler;
                    DetermineFunctionType(node.Function, ref project, ref language, ref runtime, ref handler);

                    // create function item
                    var sources = AtLocation("Sources", () => node.Sources
                        ?.Select((source, eventIndex) => ConvertFunctionSource(node, eventIndex, source))
                        .Where(evt => evt != null)
                        .ToList()
                    );
                    _builder.AddFunction(
                        parent: parent,
                        name: node.Function,
                        description: node.Description,
                        scope: ConvertScope(node.Scope),
                        project: project,
                        language: language,
                        environment: node.Environment,
                        sources: sources,
                        condition: node.If,
                        pragmas: node.Pragmas,
                        timeout: node.Timeout,
                        runtime: runtime,
                        memory: node.Memory,
                        handler: handler,
                        properties: ParseToDictionary("Properties", node.Properties)
                    );
                });
                break;
            case "Condition":
                AtLocation(node.Condition, () => {
                    AtLocation("Value", () => {
                        Validate(node.Value != null, "missing 'Value' attribute");
                    });
                    _builder.AddCondition(
                        parent: parent,
                        name: node.Condition,
                        description: node.Description,
                        value: node.Value
                    );
                });
                break;
            case "Mapping":
                AtLocation(node.Mapping, () => {
                    IDictionary<string, IDictionary<string, string>> topLevelResults = new Dictionary<string, IDictionary<string, string>>();
                    if(node.Value is IDictionary topLevelDictionary) {
                        AtLocation("Value", () => {
                            Validate(topLevelDictionary.Count > 0, "missing top-level mappings");

                            // iterate over top-level dictionary
                            foreach(DictionaryEntry topLevel in topLevelDictionary) {
                                AtLocation((string)topLevel.Key, () => {
                                    var secondLevelResults = new Dictionary<string, string>();
                                    topLevelResults[(string)topLevel.Key] = secondLevelResults;

                                    // convert top-level entry
                                    if(topLevel.Value is IDictionary secondLevelDictionary) {
                                        Validate(secondLevelDictionary.Count > 0, "missing second-level mappings");

                                        // iterate over second-level dictionary
                                        foreach(DictionaryEntry secondLevel in secondLevelDictionary) {
                                            AtLocation((string)secondLevel.Key, () => {

                                                // convert second-level entry
                                                if(secondLevel.Value is string secondLevelValue) {
                                                    secondLevelResults[(string)secondLevel.Key] = secondLevelValue;
                                                } else {
                                                    LogError("invalid value");
                                                }
                                            });
                                        }
                                    } else {
                                        LogError("invalid value");
                                    }
                                });
                            }
                        });
                    } else if(node.Value != null) {
                        LogError("invalid value for 'Value' attribute");
                    } else {
                        LogError("missing 'Value' attribute");
                    }
                    _builder.AddMapping(
                        parent: parent,
                        name: node.Mapping,
                        description: node.Description,
                        value: topLevelResults
                    );
                });
                break;
            case "ResourceType":
                Validate(node.Handler != null, "missing 'Handler' attribute");
                AtLocation(node.ResourceType, () => {

                    // read properties
                    List<ModuleManifestResourceProperty> properties = null;
                    if(node.Properties != null) {
                        AtLocation("Properties", () => {
                            properties = ParseTo<List<ModuleManifestResourceProperty>>(node.Properties);

                            // validate fields
                            Validate((properties?.Count() ?? 0) > 0, "empty or invalid 'Properties' section");
                        });
                    } else {
                        LogError("missing 'Properties' section");
                    }

                    // read attributes
                    List<ModuleManifestResourceProperty> attributes = null;
                    if(node.Attributes != null) {
                        AtLocation("Attributes", () => {
                            attributes = ParseTo<List<ModuleManifestResourceProperty>>(node.Attributes);

                            // validate fields
                            Validate((attributes?.Count() ?? 0) > 0, "empty or invalid 'Attributes' section");
                        });
                    } else {
                        LogError("missing 'Attributes' section");
                    }

                    // create resource type
                    _builder.AddResourceType(
                        node.ResourceType,
                        node.Description,
                        node.Handler,
                        properties,
                        attributes
                    );
                });
                break;
            case "Macro":
                Validate(node.Handler != null, "missing 'Handler' attribute");
                AtLocation(node.Macro, () => _builder.AddMacro(node.Macro, node.Description, node.Handler));
                break;
            }

            // local functions
            void ConvertItems(AModuleItem result, IEnumerable<string> nestedExpectedTypes) {
                ForEach("Items", node.Items, (i, p) => ConvertItem(result, i, p, nestedExpectedTypes));
            }

            void ValidateARN(object arn) {
                if((arn is string text) && !text.StartsWith("arn:") && (text != "*")) {
                    LogError($"resource name must be a valid ARN or wildcard: {arn}");
                }
            }
        }

        private void ValidateFunctionSource(IEnumerable<FunctionSourceNode> sources) {
            var index = 0;
            foreach(var source in sources) {
                ++index;
                AtLocation($"{index}", () => {
                    if(source.Api != null) {

                        // TODO (2018-11-10, bjorg): validate REST API expression
                    } else if(source.Schedule != null) {

                        // TODO (2018-06-27, bjorg): add cron/rate expression validation
                    } else if(source.S3 != null) {

                        // TODO (2018-06-27, bjorg): add events, prefix, suffix validation
                    } else if(source.SlackCommand != null) {

                        // TODO (2018-11-10, bjorg): validate REST API expression
                    } else if(source.Topic != null) {

                        // nothing to validate
                    } else if(source.Sqs != null) {

                        // validate settings
                        AtLocation("BatchSize", () => {
                            if(source.BatchSize is string batchSizeText) {
                                if(!int.TryParse(batchSizeText, out var batchSize) || (batchSize < 1) || (batchSize > 10)) {
                                    LogError($"invalid BatchSize value: {source.BatchSize}");
                                }
                            }
                        });
                    } else if(source.Alexa != null) {

                        // TODO (2018-11-10, bjorg): validate Alexa Skill ID
                    } else if(source.DynamoDB != null) {

                        // validate settings
                        AtLocation("BatchSize", () => {
                            if(source.BatchSize is string batchSizeText) {
                                if(!int.TryParse(batchSizeText, out var batchSize) || (batchSize < 1) || (batchSize > 100)) {
                                    LogError($"invalid BatchSize value: {source.BatchSize}");
                                }
                            }
                        });
                        AtLocation("StartingPosition", () => {
                            if(source.StartingPosition is string) {
                                switch(source.StartingPosition) {
                                case "TRIM_HORIZON":
                                case "LATEST":
                                case null:
                                    break;
                                default:
                                    LogError($"invalid StartingPosition value: {source.StartingPosition}");
                                    break;
                                }
                            }
                        });
                    } else if(source.Kinesis != null) {

                        // validate settings
                        AtLocation("BatchSize", () => {
                            if(source.BatchSize is string batchSizeText) {
                                if(!int.TryParse(batchSizeText, out var batchSize) || (batchSize < 1) || (batchSize > 100)) {
                                    LogError($"invalid BatchSize value: {source.BatchSize}");
                                }
                            }
                        });
                        AtLocation("StartingPosition", () => {
                            if(source.StartingPosition is string) {
                                switch(source.StartingPosition) {
                                case "TRIM_HORIZON":
                                case "LATEST":
                                case null:
                                    break;
                                default:
                                    LogError($"invalid StartingPosition value: {source.StartingPosition}");
                                    break;
                                }
                            }
                        });
                    } else if(source.WebSocket != null) {

                        // TODO (2019-03-13, bjorg): validate WebSocket route expression
                    } else {
                        LogError("unknown source type");
                    }
                });
            }
        }

        private string DeterminNodeType(
            string itemName,
            int index,
            object instance,
            Dictionary<string, IEnumerable<string>> typeChecks,
            IEnumerable<string> expectedTypes
        ) {
            var instanceLookup = JObject.FromObject(instance);
            return AtLocation($"{index}", () => {

                // find all declaration fields with a non-null value; use alphabetical order for consistency
                var matches = typeChecks
                    .OrderBy(kv => kv.Key)
                    .Where(kv => IsFieldSet(kv.Key))
                    .Select(kv => new {
                        ItemType = kv.Key,
                        ValidFields = kv.Value
                    })
                    .ToArray();
                switch(matches.Length) {
                case 0:
                    LogError($"unknown {itemName} type");
                    return null;
                case 1:

                    // good to go
                    break;
                default:
                    LogError($"ambiguous {itemName} type: {string.Join(", ", matches.Select(kv => kv.ItemType))}");
                    return null;
                }

                // validate match
                var match = matches.First();
                var invalidFields = typeChecks

                    // collect all field names
                    .SelectMany(kv => kv.Value)
                    .Distinct()

                    // only keep names that are not defined for the matched type
                    .Where(field => !match.ValidFields.Contains(field))

                    // check if the field is set on the instance
                    .Where(field => IsFieldSet(field))
                    .OrderBy(field => field)
                    .ToArray();
                if(invalidFields.Any()) {
                    LogError($"'{string.Join(", ", invalidFields)}' cannot be used with '{match.ItemType}'");
                }

                // check if the matched item was expected
                if(!expectedTypes.Contains(match.ItemType)) {
                    LogError($"unexpected node type: {match.ItemType}");
                    return null;
                }
                return match.ItemType;
            });

            // local functions
            bool IsFieldSet(string field)
                => instanceLookup.TryGetValue(field, out var token) && (token.Type != JTokenType.Null);
        }

        private void DetermineFunctionType(
            string functionName,
            ref string project,
            ref string language,
            ref string runtime,
            ref string handler
        ) {
            if(project == null) {

                // identify folder for function
                var folderName = new[] {
                    functionName,
                    $"{_builder.Name}.{functionName}"
                }.FirstOrDefault(name => Directory.Exists(Path.Combine(Settings.WorkingDirectory, name)));
                if(folderName == null) {
                    LogError($"could not locate function directory");
                    return;
                }

                // determine the function project
                project = project ?? new [] {
                    Path.Combine(Settings.WorkingDirectory, folderName, $"{folderName}.csproj"),
                    Path.Combine(Settings.WorkingDirectory, folderName, "index.js"),
                    Path.Combine(Settings.WorkingDirectory, folderName, "build.sbt")
                }.FirstOrDefault(path => File.Exists(path));
            } else if(Path.GetExtension(project) == ".csproj") {
                project = Path.Combine(Settings.WorkingDirectory, project);
            } else if(Path.GetExtension(project) == ".js") {
                project = Path.Combine(Settings.WorkingDirectory, project);
            } else if (Path.GetExtension(project) == ".sbt") {
                project = Path.Combine(Settings.WorkingDirectory, project);
            } else if(Directory.Exists(Path.Combine(Settings.WorkingDirectory, project))) {

                // determine the function project
                project = new [] {
                    Path.Combine(Settings.WorkingDirectory, project, $"{project}.csproj"),
                    Path.Combine(Settings.WorkingDirectory, project, "index.js"),
                    Path.Combine(Settings.WorkingDirectory, project, "build.sbt")
                }.FirstOrDefault(path => File.Exists(path));
            }
            if((project == null) || !File.Exists(project)) {
                LogError("could not locate the function project");
                return;
            }
            switch(Path.GetExtension((string)project).ToLowerInvariant()) {
            case ".csproj":
                DetermineDotNetFunctionProperties(functionName, project, ref language, ref runtime, ref handler);
                break;
            case ".js":
                DetermineJavascriptFunctionProperties(functionName, project, ref language, ref runtime, ref handler);
                break;
            case ".sbt":
                ScalaPackager.DetermineFunctionProperties(functionName, project, ref language, ref runtime, ref handler);
                break;
            default:
                LogError("could not determine the function language");
                return;
            }
        }

        private void DetermineDotNetFunctionProperties(
            string functionName,
            string project,
            ref string language,
            ref string runtime,
            ref string handler
        ) {
            language = "csharp";

            // check if the handler/runtime were provided or if they need to be extracted from the project file
            var csproj = XDocument.Load(project);
            var mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");

            // compile function project
            var projectName = mainPropertyGroup?.Element("AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(project);

            // check if we need to parse the <TargetFramework> element to determine the lambda runtime
            var targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
            if(runtime == null) {
                switch(targetFramework) {
                case "netcoreapp1.0":
                    runtime = "dotnetcore1.0";
                    break;
                case "netcoreapp2.0":
                    runtime = "dotnetcore2.0";
                    break;
                case "netcoreapp2.1":
                    runtime = "dotnetcore2.1";
                    break;
                default:
                    LogError($"could not determine runtime from target framework: {targetFramework}; specify 'Runtime' attribute explicitly");
                    break;
                }
            }

            // check if we need to read the project file <RootNamespace> element to determine the handler name
            if(handler == null) {
                var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
                if(rootNamespace != null) {
                    handler = $"{projectName}::{rootNamespace}.Function::FunctionHandlerAsync";
                } else {
                    LogError("could not auto-determine handler; either add 'Handler' attribute or <RootNamespace> to project file");
                }
            }
        }

        private void DetermineJavascriptFunctionProperties(
            string functionName,
            string project,
            ref string language,
            ref string runtime,
            ref string handler
        ) {
            language = "javascript";
            runtime = runtime ?? "nodejs8.10";
            handler = handler ?? "index.handler";
        }

        private IList<string> ConvertScope(object scope) {
            if(scope == null) {
                return new string[0];
            }
            return AtLocation("Scope", () => {
                return (scope == null)
                    ? new List<string>()
                    : ConvertToStringList(scope);
            });
        }

        private T ParseTo<T>(object value) {
            if(value == null) {
                return default;
            }
            try {
                return JToken.FromObject(value, new JsonSerializer {
                    NullValueHandling = NullValueHandling.Ignore
                }).ToObject<T>();
            } catch {
                return default;
            }
        }

        private IDictionary<string, object> ParseToDictionary(string location, object value) {
            Dictionary<string, object> result = null;
            if(value != null) {
                result = new Dictionary<string, object>();
                AtLocation(location, () => {
                    if(value is IDictionary dictionary) {
                        foreach(DictionaryEntry entry in dictionary) {
                            result.Add((string)entry.Key, entry.Value);
                        }
                    } else {
                        LogError("invalid map");
                    }
                });
            }
            return result;
        }
    }
}