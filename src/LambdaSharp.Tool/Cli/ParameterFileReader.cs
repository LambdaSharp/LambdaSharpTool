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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.KeyManagementService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using LambdaSharp.Tool.Internal;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;

namespace LambdaSharp.Tool.Cli {

    public class ParameterFileReader : AModelProcessor {

        //--- Types ---
        public class ParameterStoreFunctionNodeDeserializer : INodeDeserializer {

            //--- Fields ---
            public readonly Dictionary<string, string> Dictionary = new Dictionary<string, string>();
            public readonly Dictionary<string, string> Encryption = new Dictionary<string, string>();
            private readonly Dictionary<string, JObject> _configFiles = new Dictionary<string, JObject>();
            private readonly string _workingDirectory;

            //--- Constructors ---
            public ParameterStoreFunctionNodeDeserializer(string workingDirectory, LogErrorDelegate logError, Settings settings) {
                _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
                LogError = logError ?? throw new ArgumentNullException(nameof(logError));
                Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            }

            //--- Properties ---
            public bool FindDependencies { get; set; }
            private LogErrorDelegate LogError { get; }
            private Settings Settings { get; }

            //--- Methods ---
            public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(reader.Current is NodeEvent node) {
                    switch(node.Tag) {
                    case "!GetConfig":
                        return GetConfig(node, reader, expectedType, nestedObjectDeserializer, out value);
                    case "!GetEnv":
                        return GetEnv(node, reader, expectedType, nestedObjectDeserializer, out value);
                    case "!GetParam":
                        return GetParam(node, reader, expectedType, nestedObjectDeserializer, out value);
                    case "!Ref":
                        return Ref(node, reader, expectedType, nestedObjectDeserializer, out value);
                    case "!Sub":
                        return Sub(node, reader, expectedType, nestedObjectDeserializer, out value);
                    }
                }
                value = null;
                return false;
            }

            private bool GetConfig(NodeEvent node, IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(node is SequenceStart sequenceStart) {

                    // deserialize single parameter
                    INodeDeserializer nested = new CollectionNodeDeserializer(new DefaultObjectFactory());
                    if(
                        nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)
                        && (value is IList list)
                        && (list.Count == 2)
                        && (list[0] is string sourceFile)
                        && (list[1] is string key)
                    ) {
                        if(!_configFiles.TryGetValue(sourceFile, out var configFile)) {
                            try {
                                configFile = JObject.Parse(File.ReadAllText(Path.Combine(_workingDirectory, sourceFile)));
                                _configFiles[sourceFile] = configFile;
                            } catch(FileNotFoundException) {
                                LogError($"unable to load json config file '{configFile}'", null);
                                value = null;
                                return true;
                            }
                        }
                        try {
                            value = configFile.SelectToken(key)?.Value<string>();
                        } catch(Exception e) {
                            LogError($"!GetConfig has invalid JSON-path expression: '{key}'", e);
                            value = null;
                            return true;
                        }
                        if(value == null) {
                            LogError($"!GetConfig unable to find '{key}' in file '{sourceFile}'", null);
                            return true;
                        }
                        return true;
                    } else {
                        LogError("invalid expression for !GetConfig", null);
                    }
                }
                value = null;
                return false;
            }

            private bool GetEnv(NodeEvent node, IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(node is Scalar scalar) {

                    // deserialize single parameter
                    INodeDeserializer nested = new ScalarNodeDeserializer();
                    if(nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value) && (value is string key)) {
                        value = Environment.GetEnvironmentVariable(key);
                        return true;
                    }
                } else {
                    LogError("invalid expression for !GetEnv", null);
                }
                value = null;
                return false;
            }

            private bool GetParam(NodeEvent node, IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(node is Scalar scalar) {

                    // NOTE: !GetParam parameterKey

                    // deserialize single parameter
                    INodeDeserializer nested = new ScalarNodeDeserializer();
                    if(
                        nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out var tagValue)
                        && (tagValue is string parameterKey)
                    ) {
                        if(Dictionary.TryGetValue(parameterKey, out var parameterValue)) {

                            // substitute expression with parameter value
                            value = parameterValue;
                        } else {

                            // record missing parameter key
                            Dictionary[parameterKey] = null;
                            value = null;
                        }
                        return true;
                    }
                } else if(node is SequenceStart sequenceStart) {

                    // NOTE: !GetParam [ parameterKey, encryptionKey ]

                    // deserialize parameter list
                    INodeDeserializer nested = new CollectionNodeDeserializer(new DefaultObjectFactory());
                    if(
                        nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out var tagValue)
                        && (tagValue is IList list)
                    ) {
                        if(list.Count == 1) {
                            if(list[0] is string parameterKey) {
                                if(Dictionary.TryGetValue(parameterKey, out var parameterValue)) {

                                    // substitute expression with parameter value
                                    value = parameterValue;
                                } else {

                                    // record missing parameter key
                                    Dictionary[parameterKey] = null;
                                    value = null;
                                }
                                return true;
                            } else {
                                LogError("invalid expression for !GetParam [ parameterKey ]", null);
                            }
                        } else if(list.Count == 2) {
                            if(
                                (list[0] is string parameterKey)
                                && (list[1] is string encryptionKey)
                            ) {
                                if(Dictionary.TryGetValue(parameterKey, out var parameterValue)) {

                                    // substitute expression with parameter value
                                    value = parameterValue;
                                } else {

                                    // record missing parameter key
                                    Dictionary[parameterKey] = null;
                                    Encryption[parameterKey] = encryptionKey;
                                    value = null;
                                }
                                return true;
                            } else {
                                LogError("invalid expression for !GetParam [ parameterKey, encryptionKey ]", null);
                            }
                        }
                    } else {
                        LogError("!GetParam must be followed by either a list or a string", null);
                    }
                } else {
                    LogError("invalid expression for !GetParam", null);
                }
                value = null;
                return false;
            }

            private bool Ref(NodeEvent node, IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(node is Scalar scalar) {

                    // deserialize single parameter
                    INodeDeserializer nested = new ScalarNodeDeserializer();
                    if(nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value) && (value is string key)) {
                        value = GetBuiltinVariable(key);
                        if(value == null) {
                            value = "<MISSING>";
                            if(!FindDependencies) {
                                LogError($"missing !Ref variable '{key}'", null);
                            }
                        }
                        return true;
                    }
                } else {
                    LogError("invalid expression for !Ref", null);
                }
                value = null;
                return false;
            }

            private bool Sub(NodeEvent node, IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(node is Scalar scalar) {

                    // NOTE: !Sub formatString

                    // deserialize single parameter
                    INodeDeserializer nested = new ScalarNodeDeserializer();
                    if(
                        nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out var tagValue)
                        && (tagValue is string formatString)
                    ) {
                        return ApplySubExpression(formatString, new Dictionary<string, string>(), out value);
                    }
                } else if(node is SequenceStart sequenceStart) {

                    // NOTE: !Sub [ formatString, arguments ]

                    // deserialize parameter list
                    INodeDeserializer nested = new CollectionNodeDeserializer(new DefaultObjectFactory());
                    if(
                        nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out var tagValue)
                        && (tagValue is IList list)
                        && (list.Count == 2)
                        && (list[0] is string formatString)
                        && (list[1] is IDictionary arguments)
                    ) {
                        return ApplySubExpression(formatString, arguments, out value);
                    } else {
                        LogError("invalid expression for !Sub [ formatString, arguments ]", null);
                    }
                } else {
                    LogError("invalid expression for !Sub", null);
                }
                value = null;
                return false;
            }

            // local functions
            bool ApplySubExpression(string formatString, IDictionary arguments, out object result) {
                result = ModelFunctions.ReplaceSubPattern(formatString, (key, suffix) => {
                    if(suffix != null) {
                        if(!FindDependencies) {
                            LogError($"suffixed !Sub arguments are not supported '{key}{suffix}'", null);
                        }
                        return "<INVALID>";
                    }

                    // check if key is a local argument
                    var value = arguments[key];
                    if(value is string text) {

                        // return value of argument
                        return text;
                    } else if(!arguments.Contains(key)) {

                        // argument was not supplied, read key from builtin or environment variables instead
                        var environmentValue = GetBuiltinVariable(key) ?? Environment.GetEnvironmentVariable(key);
                        if(environmentValue != null) {
                            return environmentValue;
                        }
                        if(!FindDependencies) {
                            LogError($"missing !Sub argument '{key}'", null);
                        }
                        return "<MISSING>";
                    } else if(value == null) {
                        if(!FindDependencies) {
                            LogError($"missing !Sub argument '{key}'", null);
                        }
                        return "<MISSING>";
                    } else {

                        // keep the expression as is during find-dependencies phase
                        if(!FindDependencies) {
                            LogError($"!Sub argument '{key}' must be a string", null);
                        }
                        return "<INVALID>";
                    }
                });
                return true;
            }

            private string GetBuiltinVariable(string key)
                => key switch {
                    "Deployment::BucketName" => Settings.DeploymentBucketName,
                    "Deployment::Tier" => Settings.Tier,
                    "Deployment::TierLowercase" => Settings.Tier.ToLowerInvariant(),
                    "Deployment::TierPrefix" => Settings.TierPrefix,
                    "Deployment::TierPrefixLowercase" => Settings.TierPrefix.ToLowerInvariant(),
                    _ => null
                };
        }

        //--- Constructors ---
        public ParameterFileReader(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public Dictionary<string, string> ReadInputParametersFiles() {
            if(!File.Exists(SourceFilename)) {
                LogError("cannot find parameters file");
                return null;
            }
            switch(Path.GetExtension(SourceFilename).ToLowerInvariant()) {
            case ".yml":
            case ".yaml":
                try {
                    return ReadYamlFile();
                } catch(YamlDotNet.Core.YamlException e) {
                    LogError($"parsing error near {e.Message}", e);
                } catch(Exception e) {
                    LogError(e);
                }
                return null;
            default:
                LogError("incompatible inputs file format");
                return null;
            }
        }

        private Dictionary<string, string> ReadYamlFile() {
            var source = File.ReadAllText(SourceFilename);

            // initialize YAML parser
            var sourceRelativePath = new FileInfo(SourceFilename).Directory.FullName;
            var parameterStoreDeserializer = new ParameterStoreFunctionNodeDeserializer(sourceRelativePath, LogError, Settings);
            var inputs = ParseYamlFile(source, findDependencies: true);

            // check if any parameter store references were found
            if(parameterStoreDeserializer.Dictionary.Any()) {
                var allParameterKeys = parameterStoreDeserializer.Dictionary.Keys.ToArray();
                var offset = 0;
                for(var batch = allParameterKeys.Take(10); batch.Any(); batch = allParameterKeys.Skip(offset).Take(10)) {
                    offset += batch.Count();

                    // batch fetch parameters from parameter store
                    var getParameteresResponse = Settings.SsmClient.GetParametersAsync(new GetParametersRequest {
                        Names = batch.ToList(),
                        WithDecryption = true
                    }).Result;

                    // add resolved values
                    foreach(var parameter in getParameteresResponse.Parameters) {

                        // check if retrieved parameter needs to be encrypted
                        if(parameterStoreDeserializer.Encryption.TryGetValue(parameter.Name, out var encryptionKey) && (encryptionKey != null)) {

                            // automatically prefix with "alias/" if the key is not an arn and doesn't have the "alias/" prefix
                            if(!encryptionKey.StartsWith("arn:") && !encryptionKey.StartsWith("alias/", StringComparison.Ordinal)) {
                                encryptionKey = "alias/" + encryptionKey;
                            }

                            // re-encrypt value using the tier's default secret key
                            var encryptedResult = Settings.KmsClient.EncryptAsync(new EncryptRequest {
                                KeyId = encryptionKey,
                                Plaintext = new MemoryStream(Encoding.UTF8.GetBytes(parameter.Value))
                            }).Result;
                            parameterStoreDeserializer.Dictionary[parameter.Name] = Convert.ToBase64String(encryptedResult.CiphertextBlob.ToArray());
                        } else if(parameter.Type != ParameterType.SecureString) {

                            // store value in plaintext
                            parameterStoreDeserializer.Dictionary[parameter.Name] = parameter.Value;
                        } else {
                            LogError($"parameter '{parameter.Name}' is a SecureString; either provide an encryption key to re-encrypt the value or null to store as plaintext");
                            parameterStoreDeserializer.Dictionary[parameter.Name] = "<BAD>";
                        }
                    }
                }

                // check if any value failed to resolve
                foreach(var parameter in parameterStoreDeserializer.Dictionary.Where(kv => kv.Value == null)) {
                    LogError($"!GetParam unable to find '{parameter.Key}' in parameter store");
                }
            }

            // reparse document after parameter references were resolved (this pass also logs previously suppressed errors)
            inputs = ParseYamlFile(source, findDependencies: false);

            // resolve 'alias/' key names to key ARNs
            if(inputs.TryGetValue("Secrets", out var keys)) {
                if(keys is string key) {
                    inputs["Secrets"] = key.Split(',').Select(item => ConvertAliasToKeyArn(item.Trim())).ToList();
                } else if(keys is IList<object> list) {
                    inputs["Secrets"] = list.Select(item => ConvertAliasToKeyArn(item as string)).ToList();
                }

                // assume key name is an alias and resolve it to its ARN
                string ConvertAliasToKeyArn(string keyId) {
                    if(keyId == null) {
                        return null;
                    }
                    if(keyId.StartsWith("arn:")) {
                        return keyId;
                    }
                    if(keyId.StartsWith("alias/", StringComparison.Ordinal)) {
                        try {
                            return Settings.KmsClient.DescribeKeyAsync(keyId).GetAwaiter().GetResult().KeyMetadata.Arn;
                        } catch(Exception e) {
                            LogError($"failed to resolve key alias: {keyId}", e);
                            return null;
                        }
                    }
                    try {
                        return Settings.KmsClient.DescribeKeyAsync($"alias/{keyId}").GetAwaiter().GetResult().KeyMetadata.Arn;
                    } catch(Exception e) {
                        LogError($"failed to resolve key alias: {keyId}", e);
                        return null;
                    }
                }
            }

            // create final dictionary of input values
            var result = new Dictionary<string, string>();
            foreach(var input in inputs) {
                var key = input.Key.Replace("::", "");
                switch(input.Value) {
                case string text:
                    result.Add(key, text);
                    break;
                case IEnumerable values when values.Cast<object>().All(value => value is string):
                    result.Add(key, string.Join(",", values.OfType<string>()));
                    break;
                case null:
                    LogError($"parameter '{input.Key}' is null");
                    break;
                default:
                    LogError($"parameter '{input.Key}' has an invalid value (type: {input.Value?.GetType().ToString() ?? "<null>"})");
                    break;
                }
            }
            return result;

            // local functions
            Dictionary<string, object> ParseYamlFile(string text, bool findDependencies) {
                parameterStoreDeserializer.FindDependencies = findDependencies;
                return new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .WithNodeDeserializer(parameterStoreDeserializer)
                    .WithTagMapping("!GetConfig", typeof(CloudFormationListFunction))
                    .WithTagMapping("!GetEnv", typeof(CloudFormationListFunction))
                    .WithTagMapping("!GetParam", typeof(CloudFormationListFunction))
                    .WithTagMapping("!Ref", typeof(CloudFormationListFunction))
                    .WithTagMapping("!Sub", typeof(CloudFormationListFunction))
                    .Build()
                    .Deserialize<Dictionary<string, object>>(text);
            }
        }
    }
}
