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

    public class DeclarationsVisitor : ASyntaxVisitor {

        //--- Class Fields ---

        private static readonly HashSet<string> _cloudFormationParameterTypes;

        //--- Class Constructor ---
        static DeclarationsVisitor() {

            // create list of natively supported CloudFormation types
            _cloudFormationParameterTypes = new HashSet<string> {
                "String",
                "Number",
                "List<Number>",
                "CommaDelimitedList",
                "AWS::SSM::Parameter::Name",
                "AWS::SSM::Parameter::Value<String>",
                "AWS::SSM::Parameter::Value<List<String>>",
                "AWS::SSM::Parameter::Value<CommaDelimitedList>"
            };
            foreach(var awsType in new[] {
                "AWS::EC2::AvailabilityZone::Name",
                "AWS::EC2::Image::Id",
                "AWS::EC2::Instance::Id",
                "AWS::EC2::KeyPair::KeyName",
                "AWS::EC2::SecurityGroup::GroupName",
                "AWS::EC2::SecurityGroup::Id",
                "AWS::EC2::Subnet::Id",
                "AWS::EC2::Volume::Id",
                "AWS::EC2::VPC::Id",
                "AWS::Route53::HostedZone::Id"
            }) {

                // add vanilla type
                _cloudFormationParameterTypes.Add(awsType);

                // add list of type
                _cloudFormationParameterTypes.Add($"List<{awsType}>");

                // add parameter store reference of type
                _cloudFormationParameterTypes.Add($"AWS::SSM::Parameter::Value<{awsType}>");

                // add parameter store reference of list of type
                _cloudFormationParameterTypes.Add($"AWS::SSM::Parameter::Value<List<{awsType}>>");
            }
        }

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
        public DeclarationsVisitor(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

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
                    node.Module.Value = moduleInfo.ToString();
                }

                // add module reference as a shared dependency
                _builder.AddSharedDependency(moduleInfo);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ParameterDeclaration node) {

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // ensure parameter declaration is a child of the module declaration (nesting is not allowed)
            if(!(node.Parents.OfType<ADeclaration>() is ModuleDeclaration)) {
                _builder.LogError($"parameter declaration cannot be nested", node.SourceLocation);
            }

            // default 'Type' attribute value is 'String' when omitted
            if(node.Type == null) {
                node.Type = new LiteralExpression {
                    Parent = node,
                    SourceLocation = node.SourceLocation,
                    Value = "String"
                };
            }

            // default 'Section' attribute value is "Module Settings" when omitted
            if(node.Section == null) {
                node.Section = new LiteralExpression {
                    Parent = node,
                    SourceLocation = node.SourceLocation,
                    Value = "Module Settings"
                };
            }

            // the 'Description' attribute cannot exceed 4,000 characters
            if((node.Description != null) && (node.Description.Value.Length > 4_000)) {
                _builder.LogError("the Description attribute exceeds 4,000 characters", node.Description.SourceLocation);
            }

            // only the 'Number' type can have the 'MinValue' and 'MaxValue' attributes
            if(node.Type.Value == "Number") {
                if(node.MinValue != null) {

                    // TODO: validate the value is a number
                    throw new NotImplementedException();
                }
                if(node.MaxValue != null) {

                    // TODO: validate the value is a number
                    throw new NotImplementedException();
                }
            } else {
                if(node.MinValue != null) {
                    _builder.LogError($"MinValue attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
                if(node.MaxValue != null) {
                    _builder.LogError($"MaxValue attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
            }

            // only the 'String' type can have the 'AllowedPattern', 'MinLength', and 'MaxLength' attributes
            if(node.Type.Value == "String") {

                // the 'AllowedPattern' attribute must be a valid regex expression
                if(node.AllowedPattern != null) {
                    try {
                        new Regex(node.AllowedPattern.Value);
                    } catch(ArgumentException) {
                        _builder.LogError($"AllowedPattern must be a valid regex expression", node.Properties.SourceLocation);
                    }
                } else {

                    // the 'ConstraintDescription' attribute is invalid when the 'ALlowedPattern' attribute is omitted
                    if(node.ConstraintDescription != null) {
                        _builder.LogError($"ConstraintDescription attribute can only be used in conjunction with the AllowedPattern attribute", node.Properties.SourceLocation);
                    }
                }
            } else {
                if(node.AllowedPattern != null) {
                    _builder.LogError($"AllowedPattern attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
                if(node.MinLength != null) {
                    _builder.LogError($"MinLength attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
                if(node.MaxLength != null) {
                    _builder.LogError($"MaxLength attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
            }

            // only the 'Secret' type can have the 'EncryptionContext' attribute
            if(node.Type.Value == "Secret") {

                // NOTE (2019-10-30, bjorg): for a 'Secret' type parameter, we need to create a new resource
                //  that is used to decrypt the parameter into a plaintext value.

                // TODO:
                // var decoder = AddResource(
                //     parent: result,
                //     name: "Plaintext",
                //     description: null,
                //     scope: null,
                //     resource: CreateDecryptSecretResourceFor(result),
                //     resourceExportAttribute: null,
                //     dependsOn: null,
                //     condition: null,
                //     pragmas: null
                // );
                // decoder.Reference = FnGetAtt(decoder.ResourceName, "Plaintext");
                // decoder.DiscardIfNotReachable = true;

                throw new NotImplementedException();
            } else {
                if(node.EncryptionContext != null) {
                    _builder.LogError($"EncryptionContext attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
            }

            // validate other attributes based on parameter type
            if(IsValidCloudFormationParameterType(node.Type.Value)) {

                // value types cannot have Properties/Allow attribute
                if(node.Properties != null) {
                    _builder.LogError($"Properties attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
                if(node.Allow != null) {
                    _builder.LogError($"Allow attribute cannot be used with this parameter type", node.Properties.SourceLocation);
                }
            } else if(IsValidCloudFormationResourceType(node.Type.Value)) {

                // check if the 'Properties' attribute is set, which indicates a resource must be created when the parameter is omitted
                if(node.Properties != null) {

                    // add condition for creating the source
                    var condition = AddDeclaration(node, new ConditionDeclaration {
                        Condition = new LiteralExpression {
                            Value = "IsBlank"
                        },
                        Value = new EqualsConditionExpression {
                            LeftValue = new ConditionReferenceExpression {
                                ReferenceName = node.FullName
                            },
                            RightValue = new ConditionLiteralExpression {
                                Value = ""
                            }
                        }
                    });

                    // add conditional resource
                    var resource = AddDeclaration(node, new ResourceDeclaration {
                        Resource = new LiteralExpression {
                            Value = "Resource"
                        },
                        Type = new LiteralExpression {
                            Value = node.Type.Value
                        },

                        // TODO: should the data-structure be cloned?
                        Properties = node.Properties,

                        // TODO: set 'arnAttribute' for resource (default attribute to return when referencing the resource),

                        If = new ConditionLiteralExpression {
                            Value = condition.FullName
                        },

                        // TODO: should the data-structure be cloned?
                        Pragmas = node.Pragmas
                    });

                    // update the reference expression for the parameter
                    _builder.GetProperties(node.FullName).ReferenceExpression = new IfFunctionExpression {
                        Condition = new ConditionLiteralExpression {
                            Value = condition.FullName
                        },
                        IfTrue = _builder.GetExportReference(resource),
                        IfFalse = new ReferenceFunctionExpression {
                            ReferenceName = node.FullName
                        }
                    };

                    // TODO: validate resource properties

                    // TODO: grant resource permissions

                    // // request input parameter or conditional managed resource grants
                    // AddGrant(instance.LogicalId, type, result.Reference, allow, condition: null);

                    throw new NotImplementedException();
                }
                if(node.Allow != null) {

                    // TODO: validate attributes
                    ValidateAllowAttribute(node, node.Type, node.Allow);

                    // TODO add grants
                    throw new NotImplementedException();
                }
            } else {
                _builder.LogError($"unsupported type", node.Type.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ImportDeclaration node) {

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);
        }

        public override void VisitStart(ASyntaxNode parent, VariableDeclaration node) {

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

        public override void VisitStart(ASyntaxNode parent, GroupDeclaration node) { }

        public override void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {

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
                if((node.If != null) && !(node.If is ConditionLiteralExpression)) {

                    // creation condition as sub-declaration
                    AddDeclaration(node, new ConditionDeclaration {
                        Condition = new LiteralExpression {
                            SourceLocation = node.If.SourceLocation,
                            Value = "Condition"
                        },
                        Description = null,
                        Value = node.If
                    });
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

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                _builder.LogError($"invalid 'Module' attribute value", node.Module.SourceLocation);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                    node.Module.Value = moduleInfo.ToString();
                }

                // add module reference as a shared dependency
                _builder.AddNestedDependency(moduleInfo);

                // NOTE: we cannot validate the parameters and output values from the module until the
                //  nested dependency has been resolved.
            }
        }

        public override void VisitStart(ASyntaxNode parent, PackageDeclaration node) {

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

        public override void VisitStart(ASyntaxNode parent, ConditionDeclaration node) { }

        public override void VisitStart(ASyntaxNode parent, MappingDeclaration node) {

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
            if(!_builder.IsValidCloudFormationName(node.Name.Value)) {
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
            if(!_builder.IsValidCloudFormationName(node.Name.Value)) {
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

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) { }

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

            // CloudFormation primitive types
            case "String":
            case "Long":
            case "Integer":
            case "Double":
            case "Boolean":
            case "Timestamp":
                return true;

            // LambdaSharp primitive types
            case "Secret":
                return true;
            default:
                return false;
            }
        }

        private bool IsValidCloudFormationParameterType(string type) => _cloudFormationParameterTypes.Contains(type);

        // TODO: check AWS type
        private bool IsValidCloudFormationResourceType(string type) => throw new NotImplementedException();

        private T AddDeclaration<T>(AItemDeclaration parent, T declaration) where T : AItemDeclaration {
            parent.AddDeclaration(declaration, new AItemDeclaration.DoNotCallThisDirectly());
            declaration.Visit(parent, new SyntaxHierarchyAnalyzer(_builder));
            return declaration;
        }
    }
}
