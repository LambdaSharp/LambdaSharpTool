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
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Tool.Parser.Syntax;

namespace LambdaSharp.Tool.Parser.Analyzers {

    public class DeclarationsAnalyzer : ASyntaxVisitor {

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
        public DeclarationsAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

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
                _builder.LogError($"invalid Module attribute value", node.Module.SourceLocation);
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
            _builder.AddItemDeclaration(parent, node, node.Parameter.Value);

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
            _builder.AddItemDeclaration(parent, node, node.Import.Value);

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);
        }

        public override void VisitStart(ASyntaxNode parent, VariableDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Variable.Value);

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

        public override void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Resource.Value);

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
                    _builder.LogError($"resource reference must be a valid ARN or wildcard", arn.SourceLocation);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Nested.Value);

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                _builder.LogError($"invalid Module attribute value", node.Module.SourceLocation);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // TODO: load nested attribute for parameter/output-attributes validation
            }
        }

        public override void VisitStart(ASyntaxNode parent, GroupDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Group.Value);
        }

        public override void VisitStart(ASyntaxNode parent, ConditionDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Condition.Value);
        }

        public override void VisitStart(ASyntaxNode parent, PackageDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Package.Value);
        }

        public override void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Function.Value);
        }

        public override void VisitStart(ASyntaxNode parent, MappingDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Mapping.Value);
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.ResourceType.Value);
        }

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) {

            // register item declaration
            _builder.AddItemDeclaration(parent, node, node.Macro.Value);
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
    }
}
