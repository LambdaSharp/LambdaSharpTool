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
            public ParameterStoreFunctionNodeDeserializer(string workingDirectory, LogErrorDelegate logError) {
                _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
                LogError = logError ?? throw new ArgumentNullException(nameof(logError));
            }

            //--- Properties ---
            private LogErrorDelegate LogError { get; }

            //--- Methods ---
            public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
                if(reader.Current is NodeEvent node) {
                    switch(node.Tag) {
                    case "!GetConfig": {
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
                        }
                        break;
                    case "!GetEnv": {
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
                        }
                        break;
                    case "!GetParam": {
                            if(node is Scalar scalar) {

                                // deserialize single parameter
                                INodeDeserializer nested = new ScalarNodeDeserializer();
                                if(nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value) && (value is string parameterKey)) {
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

                                // TODO: this needs to be tested

                                // deserialize single parameter
                                INodeDeserializer nested = new CollectionNodeDeserializer(new DefaultObjectFactory());
                                if(
                                    nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)
                                    && (value is IList list)
                                    && (list.Count == 2)
                                    && (list[0] is string parameterKey)
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
                                    LogError("invalid expression for !GetConfig", null);
                                }
                            } else {
                                LogError("invalid expression for !GetParam", null);
                            }
                        }
                        break;
                    }
                }
                value = null;
                return false;
            }
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
                    LogError($"parsing error near {e.Message}");
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
            var parameterStoreDeserializer = new ParameterStoreFunctionNodeDeserializer(sourceRelativePath, LogError);
            var inputs = ParseYamlFile(source);

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

                // reparse document after parameter references were resolved
                inputs = ParseYamlFile(source);
            }

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
                            return Settings.KmsClient.DescribeKeyAsync(keyId).Result.KeyMetadata.Arn;
                        } catch(Exception e) {
                            LogError($"failed to resolve key alias: {keyId}", e);
                            return null;
                        }
                    }
                    try {
                        return Settings.KmsClient.DescribeKeyAsync($"alias/{keyId}").Result.KeyMetadata.Arn;
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
            Dictionary<string, object> ParseYamlFile(string text)
                => new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .WithNodeDeserializer(parameterStoreDeserializer)
                    .WithTagMapping("!GetConfig", typeof(CloudFormationListFunction))
                    .WithTagMapping("!GetEnv", typeof(CloudFormationListFunction))
                    .WithTagMapping("!GetParam", typeof(CloudFormationListFunction))
                    .Build()
                    .Deserialize<Dictionary<string, object>>(text);
        }
    }
}
