/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Linq;
using System.Text.RegularExpressions;
using MindTouch.LambdaSharp.Tool.Model.AST;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelImportProcessor : AModelProcessor {

        //--- Constants ---
        private const string IMPORT_PATTERN = "^/?[a-zA-Z][a-zA-Z0-9_]*(/[a-zA-Z][a-zA-Z0-9_]*)*/?$";

        //--- Fields ---
        private ImportResolver _importer;

        //--- Constructors ---
        public ModelImportProcessor(Settings settings) : base(settings) {
            _importer = new ImportResolver(settings.SsmClient);
        }

        //--- Methods ---
        public void Process(ModuleNode module) {

            // find all parameters with an `Import` field
            AtLocation("Parameters", () => FindAllParameterImports());

            // resolve all imported values
            _importer.BatchResolveImports();

            // replace all parameters with an `Import` field
            AtLocation("Parameters", () => ReplaceAllParameterImports());

            // check if any imports were not found
            foreach(var missing in _importer.MissingImports) {
                AddError($"import parameter '{missing}' not found");
            }
            return;

            // local functions
            void FindAllParameterImports(IEnumerable<ParameterNode> @params = null) {
                var paramIndex = 0;
                foreach(var param in @params ?? module.Parameters) {
                    ++paramIndex;
                    var paramName = param.Name ?? $"#{paramIndex}";
                    AtLocation(paramName, () => {
                        if(param.Import != null) {
                            AtLocation("Import", () => {
                                if(!Regex.IsMatch(param.Import, IMPORT_PATTERN)) {
                                    AddError("import value is invalid");
                                    return;
                                }

                                // check if import requires a deployment tier prefix
                                if(!param.Import.StartsWith("/")) {
                                    param.Import = $"/{Settings.Tier}/" + param.Import;
                                }
                                _importer.Add(param.Import);
                            });
                        }

                        // check if we need to import a custom resource handler topic
                        var resourceType = param?.Resource?.Type;
                        if((resourceType != null) && !resourceType.StartsWith("AWS::")) {
                            AtLocation("Resource", () => {
                                AtLocation("Type", () => {

                                    // confirm the custom resource has a `ServiceToken` specified or imports one
                                    if(resourceType.StartsWith("Custom::") || (resourceType == "AWS::CloudFormation::CustomResource")) {
                                        if(param.Resource.ImportServiceToken != null) {
                                            _importer.Add(param.Resource.ImportServiceToken);
                                        } else {
                                            AtLocation("Properties", () => {
                                                if(param.Resource.Properties?.ContainsKey("ServiceToken") != true) {
                                                    AddError("missing ServiceToken in custom resource properties");
                                                }
                                            });
                                        }
                                        return;
                                    }

                                    // parse resource name as `{MODULE}::{TYPE}` pattern to import the custom resource topic name
                                    var customResourceHandlerAndType = resourceType.Split("::");
                                    if(customResourceHandlerAndType.Length != 2) {
                                        AddError("custom resource type must have format {MODULE}::{TYPE}");
                                        return;
                                    }
                                    if(!Regex.IsMatch(customResourceHandlerAndType[0], CLOUDFORMATION_ID_PATTERN)) {
                                        AddError($"custom resource prefix must be alphanumeric: {customResourceHandlerAndType[0]}");
                                        return;
                                    }
                                    if(!Regex.IsMatch(customResourceHandlerAndType[1], CLOUDFORMATION_ID_PATTERN)) {
                                        AddError($"custom resource suffix must be alphanumeric: {customResourceHandlerAndType[1]}");
                                        return;
                                    }
                                    param.Resource.Type = "Custom::" + param.Resource.Type.Replace("::", "");

                                    // check if custom resource needs a service token to be retrieved
                                    if(!(param.Resource.Properties?.ContainsKey("ServiceToken") ?? false)) {
                                        var importServiceToken = $"/{Settings.Tier}"
                                            + $"/{customResourceHandlerAndType[0]}"
                                            + $"/{customResourceHandlerAndType[1]}CustomResourceTopic";
                                        param.Resource.ImportServiceToken = importServiceToken;
                                        _importer.Add(importServiceToken);
                                    }
                                });
                            });
                        }

                        // check if we need to recurse into nested parameters
                        if(param.Parameters != null) {
                            AtLocation("Parameters", () => {
                                FindAllParameterImports(param.Parameters);
                            });
                        }
                    });
                }
            }
            // local functions
            void ReplaceAllParameterImports(IList<ParameterNode> @params = null) {
                var parameterCollection = @params ?? module.Parameters;
                for(var i = 0; i < parameterCollection.Count; ++i) {
                    var parameter = parameterCollection[i];
                    var parameterName = parameter.Name ?? $"#{i + 1}";
                    AtLocation(parameterName, () => {

                        var resource = parameter.Resource;
                        if(resource != null) {
                            AtLocation("Resource", () => {
                                if(resource.ImportServiceToken != null) {
                                    if(!_importer.TryGetValue(resource.ImportServiceToken, out string importedValue)) {
                                        AddError("unable to find custom resource handler topic");
                                    } else {

                                        // add resolved `ServiceToken` to custom resource
                                        if(resource.Properties == null) {
                                            resource.Properties = new Dictionary<string, object>();
                                        }
                                        resource.Properties["ServiceToken"] = importedValue;
                                    }
                                }
                            });
                        }

                        // replace nested parameters
                        if(parameter.Parameters != null) {
                            ReplaceAllParameterImports(parameter.Parameters);
                        }

                        // replace current parameter
                        if(parameter.Import != null) {

                            // check if import is a parameter hierarchy
                            if(parameter.Import.EndsWith("/", StringComparison.Ordinal)) {
                                var imports = AtLocation("Import", () => {
                                    _importer.TryGetValue(parameter.Import, out IEnumerable<ResolvedImport> found);
                                    return found;
                                }, null);
                                if(imports?.Any() == true) {
                                    parameterCollection[i] = ConvertImportedParameter(
                                        parameter.Import.Substring(0, parameter.Import.Length - 1),
                                        new ParameterNode(parameter)
                                    );
                                } else {
                                    AddError($"could not find import");
                                }

                                // local functions
                                ParameterNode ConvertImportedParameter(string path, ParameterNode node) {
                                    var current = imports.FirstOrDefault(import => import.Key == path);
                                    SetImportedParameterNode(path, node, current);

                                    // find nested, imported values
                                    var subImports = imports.Where(import => import.Key.StartsWith(path + "/", StringComparison.Ordinal)).ToArray();
                                    if(subImports.Any()) {
                                        node.Parameters = subImports
                                            .ToLookup(import => import.Key.Substring(path.Length + 1).Split('/', 2)[0])
                                            .Select(child => ConvertImportedParameter(
                                                path + "/" + child.Key,
                                                new ParameterNode {
                                                    Name = child.Key
                                                }
                                            ))
                                            .ToArray();
                                    }
                                    return node;
                                }
                            } else {
                                var import = AtLocation("Import", () => {
                                    _importer.TryGetValue(parameter.Import, out ResolvedImport found);
                                    return found;
                                }, null);
                                if(import != null) {

                                    // check the imported parameter store type
                                    parameterCollection[i] = new ParameterNode(parameter);
                                    SetImportedParameterNode(parameter.Import, parameterCollection[i], import);
                                } else {
                                    AddError($"import key not found '{parameter.Import}'");
                                }
                            }
                        }
                    });
                }
            }
        }

        private void SetImportedParameterNode(string path, ParameterNode node, ResolvedImport import) {
            switch(import?.Type) {
            case "String":
                node.Value = import.Value;
                break;
            case "StringList":
                node.Values = import.Value.Split(',');
                break;
            case "SecureString":
                node.Secret = import.Value;
                node.EncryptionContext = new Dictionary<string, string> {
                    ["PARAMETER_ARN"] = $"arn:aws:ssm:{Settings.AwsRegion}:{Settings.AwsAccountId}:parameter{import.Key}"
                };
                break;
            case null:

                // set empty string on non-existing imported nodes
                node.Value = "";
                break;
            default:
                AddError($"unrecognized import type '{import?.Type}' for import key '{path}'");
                node.Value = "<NOT SET>";
                break;
            }
        }
    }
}