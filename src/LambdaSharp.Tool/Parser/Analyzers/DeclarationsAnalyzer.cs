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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LambdaSharp.Tool.Parser.Syntax;

namespace LambdaSharp.Tool.Parser.Analyzers {

    public class DeclarationsAnalyzer : ASyntaxVisitor {

        //--- Class Fields ---

        private static Regex ValidResourceNameRegex = new Regex("[A-Za-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        public static bool TryParseModuleFullName(string compositeModuleFullName, out string moduleNamespace, out string moduleName) {
            moduleNamespace = "<BAD>";
            moduleName = "<BAD>";
            if(!ModuleInfo.TryParse(compositeModuleFullName, out var moduleInfo)) {
                return false;
            }
            if((moduleInfo.Version != null) || (moduleInfo.Origin != null)) {
                return false;
            }
            moduleNamespace = moduleInfo.Namespace;
            moduleName = moduleInfo.Name;
            return true;
        }

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public DeclarationsAnalyzer(Builder builder) {
            _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));
        }

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, ModuleDeclaration node) {

            // ensure module version is present and valid
            if(node.Version == null) {
                _builder.ModuleVersion = VersionInfo.Parse("1.0-DEV");
            } else if(VersionInfo.TryParse(node.Version.Value, out var version)) {
                _builder.ModuleVersion = version;
            } else {
                _builder.LogError($"'Version' expected to have format: Major.Minor[.Patch]", node.Version.SourceLocation);
                _builder.ModuleVersion = VersionInfo.Parse("1.0-DEV");
            }

            // ensure module has a namespace and name
            if(!TryParseModuleFullName(node.Module.Value, out string moduleNamespace, out var moduleName)) {
                _builder.LogError($"'Module' attribute must have format 'Namespace.Name'", node.Module.SourceLocation);
            }

            // validate secrets
            foreach(var secret in node.Secrets) {
                if(secret.Value.Equals("aws/ssm", StringComparison.OrdinalIgnoreCase)) {
                    _builder.LogError($"cannot grant permission to decrypt with aws/ssm", secret.SourceLocation);

                // TODO:
                // } else if(secret.Value.StartsWith("arn:", StringComparison.Ordinal)) {
                //     if(!Regex.IsMatch(secret, $"arn:aws:kms:{Settings.AwsRegion}:{Settings.AwsAccountId}:key/[a-fA-F0-9\\-]+")) {
                //         LogError("secret key must be a valid ARN for the current region and account ID");
                //     }

                } else if(!Regex.IsMatch(secret.Value, @"[0-9a-zA-Z/_\-]+")) {
                    _builder.LogError($"secret key must be a valid alias", secret.SourceLocation);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, UsingDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                _builder.LogError($"invalid 'Module' attribute value", node.Module.SourceLocation);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // TODO:
                //_builder.AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Shared).Wait();
            }
        }

        public override void VisitStart(ASyntaxNode parent, ParameterDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Parameter.Value);

            // validate attributes
            ValidatePropertiesAttribute(node, node.Type, node.Properties);
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // ensure parameter declration is a child of the module declaration (nesting is not allowed)
            if(!(node.Parents.OfType<ADeclaration>() is ModuleDeclaration)) {
                _builder.LogError($"parameter declaration cannot be nested", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ImportDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Import.Value);

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);
        }

        public override void VisitStart(ASyntaxNode parent, VariableDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Variable.Value);

            // validate EncryptionContext attribute
            if(node.EncryptionContext != null) {
                if(node.Type?.Value != "Secret") {
                    _builder.LogError($"variable must have type 'Secret' to use 'EncryptionContext' attribute", node.SourceLocation);
                }
            }

            // validate Value attribute
            if(node.Type?.Value == "Secret") {
                if((node.Value is ListExpression) || (node.Value is ObjectExpression)) {
                    _builder.LogError($"variable with type 'Secret' must be a literal value or function expression", node.Value.SourceLocation);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, GroupDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Group.Value);
        }

        public override void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Resource.Value);

            // check if declaration is a resource reference
            if(node.Value != null) {

                // validate attributes
                ValidateAllowAttribute(node, node.Type, node.Allow);

                // referenced resource cannot be conditional
                if(node.If != null) {
                    _builder.LogError($"'If' attribute cannot be used with a referenced resource", node.If.SourceLocation);
                }

                // referenced resource cannot have properties
                if(node.Properties != null) {
                    _builder.LogError($"'Properties' section cannot be used with a referenced resource", node.Properties.SourceLocation);
                }

                // validate Value attribute
                if(node.Value is ListExpression listExpression) {
                    foreach(var arnValue in listExpression.Items) {
                        ValidateARN(arnValue);
                    }
                } else {
                    ValidateARN(node.Value);
                }
            } else {

                // CloudFormation resource must have a type
                if(node.Type == null) {
                    _builder.LogError($"missing 'Type' attribute", node.SourceLocation);
                } else {

                    // the Allow attribute is only valid with native CloudFormation types (not custom resources)
                    if((node.Allow != null) && !IsNativeCloudFormationType(node.Type.Value)) {
                        _builder.LogError($"'Allow' attribute can only be used with AWS resource types", node.Type.SourceLocation);
                    }
                }

                // check if resource is conditional
                if(!(node.If is ConditionLiteralExpression)) {

                    // TODO: creation condition as sub-declaration
                }
            }

            // local functions
            void ValidateARN(AValueExpression arn) {
                if(
                    !(arn is LiteralExpression literalExpression)
                    || (
                        !literalExpression.Value.StartsWith("arn:", StringComparison.Ordinal)
                        && (literalExpression.Value != "*")
                    )
                ) {
                    _builder.LogError($"'Value' attribute must be a valid ARN or wildcard", arn.SourceLocation);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Nested.Value);

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                _builder.LogError($"invalid 'Module' attribute value", node.Module.SourceLocation);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // TODO: load nested attribute for parameter/output-attributes validation
            }
        }

        public override void VisitStart(ASyntaxNode parent, PackageDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Package.Value);

            // validate Files attributes
            if(
                !Directory.Exists(node.Files.Value)
                && !Directory.Exists(Path.GetDirectoryName(node.Files.Value))
                && !File.Exists(node.Files.Value)
            ) {
                _builder.LogError($"'Files' attribute must refer to an existing file or folder", node.Files.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Function.Value);

            // validate attributes
            ValidateExpressionIsNumber(node, node.Memory, $"invalid 'Memory' value");
            ValidateExpressionIsNumber(node, node.Timeout, $"invalid 'Timeout' value");

            var functionName = node.Function.Value;
            var workingDirectory = Path.GetDirectoryName(node.SourceLocation.FilePath);
            string project = null;

            // determine project file location and type
            if(node.Project == null) {

                // use function name to determine function project file
                project = DetermineProjectFileLocation(Path.Combine(workingDirectory, functionName));
            } else {

                // check if the project type can be determined by the project file extension
                project = Path.Combine(workingDirectory, node.Project.Value);
                if(File.Exists(project)) {

                    // check if project file extension is supported
                    switch(Path.GetExtension(project).ToLowerInvariant()) {
                    case ".csproj":
                    case ".js":
                    case ".sbt":

                        // known extension for an existing project file; nothing to do
                        break;
                    default:
                        project = null;
                        break;
                    }
                } else {
                    project = DetermineProjectFileLocation(project);
                }
            }
            if(project == null) {
                _builder.LogError($"function project file could not be found or is not supported", node.SourceLocation);
                return;
            }

            // update 'Project' attribute with known project file that exists
            node.Project = new LiteralExpression {
                Parent = node,
                SourceLocation = node.Project?.SourceLocation,
                Value = project
            };

            // fill in missing attributes based on function type
            switch(Path.GetExtension(project).ToLowerInvariant()) {
            case ".csproj":
                DetermineDotNetFunctionProperties();
                break;
            case ".js":
                DetermineJavascriptFunctionProperties();
                break;
            case ".sbt":
                DetermineScalaFunctionProperties();
                break;
            default:
                _builder.LogError($"unsupported language for Lambda function", node.SourceLocation);
                break;
            }

            // check if resource is conditional
            if(!(node.If is ConditionLiteralExpression)) {

                // TODO: creation condition as sub-declaration
            }

            // local function
            string DetermineProjectFileLocation(string folderPath)
                => new[] {
                    Path.Combine(folderPath, $"{new DirectoryInfo(folderPath).Name}.csproj"),
                    Path.Combine(folderPath, "index.js"),
                    Path.Combine(folderPath, "build.sbt")
                }.FirstOrDefault(projectPath => File.Exists(projectPath));

            void DetermineDotNetFunctionProperties() {

                // set the language
                if(node.Language == null) {
                    node.Language = new LiteralExpression {
                        Value = "csharp"
                    };
                }

                // check if the handler/runtime were provided or if they need to be extracted from the project file
                var csproj = XDocument.Load(node.Project.Value);
                var mainPropertyGroup = csproj.Element("Project")?.Element("PropertyGroup");

                // compile function project
                var projectName = mainPropertyGroup?.Element("AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(project);

                // check if we need to parse the <TargetFramework> element to determine the lambda runtime
                var targetFramework = mainPropertyGroup?.Element("TargetFramework").Value;
                if(node.Runtime == null) {
                    switch(targetFramework) {
                    case "netcoreapp1.0":
                        _builder.LogError($".NET Core 1.0 is no longer supported for Lambda functions", node.SourceLocation);
                        break;
                    case "netcoreapp2.0":
                        _builder.LogError($".NET Core 2.0 is no longer supported for Lambda functions", node.SourceLocation);
                        break;
                    case "netcoreapp2.1":
                        node.Runtime = new LiteralExpression {
                            Value = "dotnetcore2.1"
                        };
                        break;
                    default:
                        _builder.LogError($"could not determine runtime from target framework: {targetFramework}; specify 'Runtime' attribute explicitly", node.SourceLocation);
                        break;
                    }
                }

                // check if we need to read the project file <RootNamespace> element to determine the handler name
                if(node.Handler == null) {
                    var rootNamespace = mainPropertyGroup?.Element("RootNamespace")?.Value;
                    if(rootNamespace != null) {
                        node.Handler = new LiteralExpression {
                            Value = $"{projectName}::{rootNamespace}.Function::FunctionHandlerAsync"
                        };
                    } else {
                        _builder.LogError($"could not auto-determine handler; either add 'Handler' attribute or <RootNamespace> to project file", node.SourceLocation);
                    }
                }
            }

            void DetermineJavascriptFunctionProperties() {

                // set the language
                if(node.Language == null) {
                    node.Language = new LiteralExpression {
                        Value = "javascript"
                    };
                }

                // set runtime
                if(node.Runtime == null) {
                    node.Runtime = new LiteralExpression {
                        Value = "nodejs8.10"
                    };
                }

                // set handler
                if(node.Handler == null) {
                    node.Handler = new LiteralExpression {
                        Value = "index.handler"
                    };
                }
            }

            void DetermineScalaFunctionProperties() {

                // set the language
                if(node.Language == null) {
                    node.Language = new LiteralExpression {
                        Value = "scala"
                    };
                }

                // set runtime
                if(node.Runtime == null) {
                    node.Runtime = new LiteralExpression {
                        Value = "java8"
                    };
                }

                // set handler
                if(node.Handler == null) {
                    _builder.LogError($"Handler attribute is required for Scala functions", node.SourceLocation);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, ConditionDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Condition.Value);
        }

        public override void VisitStart(ASyntaxNode parent, MappingDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Mapping.Value);

            // check if object expression is valid (must have first- and second-level keys)
            if(node.Value.Items.Count > 0) {

                // check that all first-level keys have object expressions
                foreach(var topLevelEntry in node.Value.Items) {
                    if(topLevelEntry.Value is ObjectExpression secondLevelObjectExpression) {
                        if(secondLevelObjectExpression.Items.Count > 0) {
                            _builder.LogError($"missing second-level mappings", secondLevelObjectExpression.SourceLocation);
                        } else {

                            // check that all second-level keys have literal expressions
                            foreach(var secondLevelEntry in secondLevelObjectExpression.Items) {
                                if(!(secondLevelEntry.Value is LiteralExpression)) {
                                    _builder.LogError($"value must be a literal", secondLevelEntry.SourceLocation);
                                }
                            }
                        }
                    } else {
                        _builder.LogError($"value must be an object", topLevelEntry.Value.SourceLocation);
                    }
                }
            } else {
                _builder.LogError($"missing top-level mappings", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.ResourceType.Value);
        }

        public override void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node) {

            // TODO: better rules for parsing CloudFormation types
            //  - https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html

            // ensure unique property names
            var names = new HashSet<string>();
            if(node.Properties.Count > 0) {
                foreach(var property in node.Properties) {
                    if(!names.Add(property.Name.Value)) {
                        _builder.LogError("duplicate name", property.Name.SourceLocation);
                    }
                }
            } else {
                _builder.LogError($"empty Properties section", node.SourceLocation);
            }

            // ensure unique attribute names
            names.Clear();
            if(node.Attributes.Count > 0) {
                foreach(var attribute in node.Attributes) {
                    if(!names.Add(attribute.Name.Value)) {
                        _builder.LogError("duplicate name", attribute.Name.SourceLocation);
                    }
                }
            } else {
                _builder.LogError($"empty Attributes section", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) {
            if(!ValidResourceNameRegex.IsMatch(node.Name.Value)) {
                _builder.LogError($"name must be alphanumeric", node.SourceLocation);
            }
            if(node.Type == null) {

                // default Type is String when omitted
                node.Type = new LiteralExpression {
                    Value = "String"
                };
            } else if(!IsValidCloudFormationType(node.Type.Value)) {
                _builder.LogError($"unsupported type", node.Type.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) {
            if(!ValidResourceNameRegex.IsMatch(node.Name.Value)) {
                _builder.LogError($"name must be alphanumeric", node.SourceLocation);
            }
            if(node.Type == null) {

                // default Type is String when omitted
                node.Type = new LiteralExpression {
                    Value = "String"
                };
            } else if(!IsValidCloudFormationType(node.Type.Value)) {
                _builder.LogError($"unsupported type", node.Type.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) {

            // register item declaration
            AddItemDeclaration(parent, node, node.Macro.Value);
        }

        private void AddItemDeclaration(ASyntaxNode parent, ADeclaration declaration, string name) {
            if(!ValidResourceNameRegex.IsMatch(name)) {
                _builder.LogError($"name must be alphanumeric", declaration.SourceLocation);
            }
            _builder.AddItemDeclaration(parent, declaration, name);
        }

        private void ValidateAllowAttribute(ADeclaration node, LiteralExpression type, TagListDeclaration allow) {
            if(allow != null) {
                if(type == null) {
                    _builder.LogError($"'Allow' attribute requires 'Type' attribute", node.SourceLocation);
                } else if(type?.Value == "AWS") {

                    // nothing to do; any 'Allow' expression is legal
                } else {
                    // TODO: ResourceMapping.IsCloudFormationType(node.Type?.Value), "'Allow' attribute can only be used with AWS resource types"
                }
            }
        }

        private void ValidatePropertiesAttribute(ADeclaration node, LiteralExpression type, ObjectExpression properties) {
            if(properties != null) {
                if(type == null) {
                    _builder.LogError($"'Property' attribute requires 'Type' attribute", node.SourceLocation);
                } else {
                    // TODO: check if type is a valid AWS type
                    // TODO: create nested condition declaration
                    // TODO: create nested conditional resource declaration
                }
            }
        }

        private bool IsNativeCloudFormationType(string awsType) {

            // TODO:
            throw new NotImplementedException();
        }

        private void ValidateExpressionIsNumber(ASyntaxNode parent, AValueExpression expression, string errorMessage) {
            if((expression is LiteralExpression literal) && !int.TryParse(literal.Value, out _)) {
                _builder.LogError(errorMessage, expression.SourceLocation);
            }
        }

        private bool IsValidCloudFormationType(string type) {
            switch(type) {
            case "String":
            case "Long":
            case "Integer":
            case "Double":
            case "Boolean":
            case "Timestamp":
                return true;
            default:
                return false;
            }
        }
    }
}
