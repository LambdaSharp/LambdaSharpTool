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
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class StructureAnalyzer : ASyntaxAnalyzer {

        //--- Class Fields ---
        private static readonly HashSet<string> _cloudFormationParameterTypes;
        private static readonly string _decryptSecretFunctionCode;
        private static Regex SecretArnRegex = new Regex(@"^arn:aws:kms:[a-z\-]+-\d:\d{12}:key\/[a-fA-F0-9\-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex SecretAliasRegex = new Regex("^[0-9a-zA-Z/_\\-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Constructor ---
        static StructureAnalyzer() {

            // load source code for inline secret decryption function
            _decryptSecretFunctionCode = typeof(StructureAnalyzer).Assembly.ReadManifestResource("Resources/DecryptSecretFunction.js");

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
        public StructureAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, UsingDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                _builder.Log(Error.ModuleAttributeInvalid, node.Module);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // add module reference as a shared dependency
                _builder.AddSharedDependency(node, moduleInfo);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ImportDeclaration node) {

            // validate attributes
            ValidateExpressionIsLiteralOrListOfLiteral(node.Scope);
            ValidateAllowAttribute(node, node.Type, node.Allow);
        }

        public override void VisitStart(ASyntaxNode parent, VariableDeclaration node) {

            // validate Value attribute
            if(node.Type?.Value == "Secret") {
                if((node.Value is ListExpression) || (node.Value is ObjectExpression)) {
                    _builder.Log(Error.SecretTypeMustBeLiteralOrExpression, node.Value);
                }
            } else if(node.EncryptionContext != null) {
                _builder.Log(Error.EncryptionContextAttributeRequiresSecretType, node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, GroupDeclaration node) { }

        public override void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {

            // validate attributes
            ValidateExpressionIsLiteralOrListOfLiteral(node.Scope);
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // check if declaration is a resource reference
            if(node.Value != null) {

                // referenced resource cannot be conditional
                if(node.If != null) {
                    _builder.Log(Error.IfAttributeRequiresCloudFormationType, node.If);
                }

                // referenced resource cannot have properties
                if(node.Properties != null) {
                    _builder.Log(Error.PropertiesAttributeRequiresCloudFormationType, node.Properties);
                }

                // validate Value attribute
                if(node.Value is ListExpression listExpression) {
                    foreach(var arnValue in listExpression.Items) {
                        ValidateARN(arnValue);
                    }
                } else {
                    ValidateARN(node.Value);
                }

                // default type to 'String'
                if(node.Type == null) {
                    node.Type = Literal("String");
                }
            } else {

                // CloudFormation resource must have a type
                if(node.Type == null) {
                    _builder.Log(Error.TypeAttributeMissing, node);
                }

                // check if resource is conditional
                if((node.If != null) && !(node.If is ConditionExpression)) {

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

                // TODO: validate properties

                // add resource permissions
                if(node.Allow != null) {
                    AddGrant(
                        name: node.LogicalId,
                        awsType: node.Type.Value,
                        reference: node.ReferenceExpression,
                        allow: node.Allow,
                        condition: null
                    );
                }
            }

            // local functions
            void ValidateARN(AExpression arn) {
                if(
                    !(arn is LiteralExpression literalExpression)
                    || (
                        !literalExpression.Value.StartsWith("arn:", StringComparison.Ordinal)
                        && (literalExpression.Value != "*")
                    )
                ) {
                    _builder.Log(Error.ResourceValueAttributeInvalid, arn);
                }
            }
        }

        public override void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                _builder.Log(Error.ModuleAttributeInvalid, node.Module);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // add module reference as a shared dependency
                _builder.AddNestedDependency(node, moduleInfo);

                // NOTE: we cannot validate the parameters and output values from the module until the
                //  nested dependency has been resolved.
            }
        }

        public override void VisitStart(ASyntaxNode parent, PackageDeclaration node) {

            // validate attributes
            ValidateExpressionIsLiteralOrListOfLiteral(node.Scope);

            // 'Files' reference is relative to YAML file it originated from
            var workingDirectory = Path.GetDirectoryName(node.SourceLocation.FilePath);
            var absolutePath = Path.Combine(workingDirectory, node.Files.Value);

            // determine if 'Files' is a file or a folder
            if(File.Exists(absolutePath)) {

                // TODO: add support for using a single item that has no key
                node.ResolvedFiles.Add(new KeyValuePair<string, string>("", absolutePath));
            } else if(Directory.Exists(absolutePath)) {

                // add all files from folder
                foreach(var filePath in Directory.GetFiles(absolutePath, "*", SearchOption.AllDirectories)) {
                    var relativeFilePathName = Path.GetRelativePath(absolutePath, filePath);
                    node.ResolvedFiles.Add(new KeyValuePair<string, string>(relativeFilePathName, filePath));
                }
                node.ResolvedFiles = node.ResolvedFiles.OrderBy(kv => kv.Key).ToList();
            } else {
                _builder.Log(Error.FilesAttributeInvalid, node.Files);
            }

            // add variable to resolve package location
            var variable = AddDeclaration(node, new VariableDeclaration {
                Variable = Literal("PackageName"),
                Type = Literal("String"),
                Value = Literal($"{node.LogicalId}-DRYRUN.zip")
            });

            // update 'Package' reference
            node.ReferenceExpression = GetModuleArtifactExpression($"${{{variable.FullName}}}");
        }

        public override void VisitStart(ASyntaxNode parent, ConditionDeclaration node) { }

        public override void VisitStart(ASyntaxNode parent, MappingDeclaration node) {

            // check if object expression is valid (must have first- and second-level keys)
            if(node.Value.Items.Any()) {

                // check that all first-level keys have object expressions
                foreach(var topLevelEntry in node.Value.Items) {
                    if(topLevelEntry.Value is ObjectExpression secondLevelObjectExpression) {
                        if(secondLevelObjectExpression.Items.Any()) {

                            // check that all second-level keys have literal expressions
                            foreach(var secondLevelEntry in secondLevelObjectExpression.Items) {
                                if(!(secondLevelEntry.Value is LiteralExpression)) {
                                    _builder.Log(Error.ValueMustBeAnInteger, secondLevelEntry);
                                }
                            }
                        } else {
                            _builder.Log(Error.MappingDeclarationSecondLevelIsMissing, secondLevelObjectExpression);
                        }
                    } else {
                        _builder.Log(Error.ExpectedMapExpression, topLevelEntry.Value);
                    }
                }
            } else {
                _builder.Log(Error.MappingDeclarationTopLevelIsMissing, node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {

            // NOTE (2019-11-05, bjorg): processing happens in VisitEnd() after the property and attribute nodes have been processed
        }

        public override void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node) {

            // TODO: register custom resource so that it is available do the module (maybe in VisitEnd?)

            // // TODO (2018-09-20, bjorg): add custom resource name validation
            // if(_customResourceTypes.Any(existing => existing.Type == resourceType)) {
            //     LogError($"Resource type '{resourceType}' is already defined.");
            // }

            // // add resource type definition
            // AddItem(new ResourceTypeItem(resourceType, description, handler));
            // _customResourceTypes.Add(new ModuleManifestResourceType {
            //     Type = resourceType,
            //     Description = description,
            //     Properties = properties ?? Enumerable.Empty<ModuleManifestResourceProperty>(),
            //     Attributes = attributes ?? Enumerable.Empty<ModuleManifestResourceProperty>()
            // });

            // TODO: better rules for parsing CloudFormation types
            //  - https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-resource-specification-format.html

            // ensure unique property names
            var names = new HashSet<string>();
            if(node.Properties.Any()) {
                foreach(var property in node.Properties) {
                    if(!names.Add(property.Name.Value)) {
                        _builder.Log(Error.DuplicateName, property.Name);
                    }
                }
            } else {
                _builder.Log(Error.ResourceTypePropertiesAttributeInvalid, node);
            }

            // ensure unique attribute names
            names.Clear();
            if(node.Attributes.Any()) {
                foreach(var attribute in node.Attributes) {
                    if(!names.Add(attribute.Name.Value)) {
                        _builder.Log(Error.DuplicateName, attribute.Name);
                    }
                }
            } else {
                _builder.Log(Error.ResourceTypeAttributesAttributeInvalid, node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) {
            if(!_builder.IsValidCloudFormationName(node.Name.Value)) {
                _builder.Log(Error.NameMustBeAlphanumeric, node);
            }
            if(node.Type == null) {

                // default Type is String when omitted
                node.Type = new LiteralExpression {
                    Value = "String"
                };
            } else if(!IsValidCloudFormationType(node.Type.Value)) {
                _builder.Log(Error.TypeAttributeInvalid, node.Type);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) {
            if(!_builder.IsValidCloudFormationName(node.Name.Value)) {
                _builder.Log(Error.NameMustBeAlphanumeric, node);
            }
            if(node.Type == null) {

                // default Type is String when omitted
                node.Type = new LiteralExpression {
                    Value = "String"
                };
            } else if(!IsValidCloudFormationType(node.Type.Value)) {
                _builder.Log(Error.TypeAttributeInvalid, node.Type);
            }
        }

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) {

            // check if a root macros collection needs to be created
            if(!_builder.TryGetItemDeclaration("Macros", out var macrosItem)) {
                macrosItem = AddDeclaration(node.Parents.OfType<ModuleDeclaration>().First(), new GroupDeclaration {
                    Group = Literal("Macros"),
                    Description = Literal("Macro definitions")
                });
            }

            // add macro resource
            AddDeclaration(macrosItem, new ResourceDeclaration {
                Resource = Literal(node.Macro.Value),
                Type = Literal("AWS::CloudFormation::Macro"),
                Description = node.Description,
                Properties = new ObjectExpression {
                    ["Name"] = FnSub($"${{DeploymentPrefix}}{node.Macro.Value}"),
                    ["Description"] = node.Description,
                    ["FunctionName"] = FnRef(node.Handler.Value)
                }
            });
        }

        private void ValidateAllowAttribute(ADeclaration node, LiteralExpression type, AExpression allow) {
            ValidateExpressionIsLiteralOrListOfLiteral(allow);
            if(allow != null) {
                if(type == null) {
                    _builder.Log(Error.AllowAttributeRequiresTypeAttribute, node);
                } else if(type?.Value == "AWS") {

                    // nothing to do; any 'Allow' expression is legal
                } else if(!IsValidCloudFormationResourceType(type.Value)) {
                    _builder.Log(Error.AllowAttributeRequiresCloudFormationType, node);
                } else {

                    // TODO: ResourceMapping.IsCloudFormationType(node.Type?.Value), "'Allow' attribute can only be used with AWS resource types"
                }
            }
        }

        private void ValidateExpressionIsNumber(ASyntaxNode parent, AExpression expression, Error errorMessage) {
            if((expression is LiteralExpression literal) && !int.TryParse(literal.Value, out _)) {
                _builder.Log(errorMessage, expression);
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

        private T AddDeclaration<T>(ModuleDeclaration parent, T declaration) where T : AItemDeclaration {
            parent.Items.Add(declaration);
            declaration.Visit(parent, new SyntaxHierarchyAnalyzer(_builder));
            return declaration;
        }

        private void AddGrant(string name, string awsType, AExpression reference, AExpression allow, AExpression condition) {

            // TODO: validate AWS type

            var allowList = new List<string>();
            if(allow is LiteralExpression allowLiteralExpression) {
                allowList.Add(allowLiteralExpression.Value);
            } else if(allow is ListExpression allowListExpression) {
                foreach(var allowListItem in allowListExpression.OfType<LiteralExpression>()) {
                    allowList.Add(allowListItem.Value);
                }
            } else {

                // TODO: could also be !Ref or !Split, etc...
            }
            if(!allowList.Any()) {

                // nothing to do
                return;
            }

            // TODO: always validate as well
            // ValidateAllowAttribute(node, node.Type, node.Allow);

            // TODO:
            throw new NotImplementedException();
        }

        private bool TryGetLabeledPragma(ModuleDeclaration moduleDeclaration, string key, out AExpression value) {
            foreach(var objectPragma in moduleDeclaration.Pragmas.OfType<ObjectExpression>()) {
                if(objectPragma.TryGetValue(key, out value)) {
                    return true;
                }
            }
            value = null;
            return false;
        }

        private bool TryGetOverride(ModuleDeclaration moduleDeclaration, string key, out AExpression expression) {
            if(
                TryGetLabeledPragma(moduleDeclaration, "Overrides", out var value)
                && (value is ObjectExpression map)
                && map.TryGetValue(key, out expression)
            ) {
                return true;
            }
            expression = null;
            return false;
        }

        private bool TryGetVariable(ModuleDeclaration moduleDeclaration, string name, out AExpression variable, out AExpression condition) {
            if(TryGetOverride(moduleDeclaration, $"Module::{name}", out variable)) {
                condition = null;
                return true;
            }
            if(HasLambdaSharpDependencies(moduleDeclaration)) {
                condition = FnCondition("UseCoreServices");
                variable = FnIf("UseCoreServices", FnRef($"LambdaSharp::{name}"), FnRef("AWS::NoValue"));
                return true;
            }
            variable = null;
            condition = null;
            return false;
        }

        private static AExpression GetModuleArtifactExpression(string filename)
            => FnSub($"{ModuleInfo.MODULE_ORIGIN_PLACEHOLDER}/${{Module::Namespace}}/${{Module::Name}}/.artifacts/{filename}");

        private bool HasPragma(ModuleDeclaration moduleDeclaration, string pragma)
            =>  moduleDeclaration.Pragmas.Items.Any(value => (value is LiteralExpression literalExpression) && (literalExpression.Value == pragma));

        private bool HasPragma(FunctionDeclaration functionDeclaration, string pragma)
            =>  functionDeclaration.Pragmas.Items.Any(value => (value is LiteralExpression literalExpression) && (literalExpression.Value == pragma));

        private bool HasLambdaSharpDependencies(ModuleDeclaration moduleDeclaration) => !HasPragma(moduleDeclaration, "no-lambdasharp-dependencies");
        private bool HasModuleRegistration(ModuleDeclaration moduleDeclaration) => !HasPragma(moduleDeclaration, "no-module-registration");
        private bool HasFunctionRegistration(FunctionDeclaration functionDeclaration) => !HasPragma(functionDeclaration, "no-function-registration");
        private bool HasDeadLetterQueue(FunctionDeclaration functionDeclaration) => !HasPragma(functionDeclaration, "no-dead-letter-queue");

        private void ValidateExpressionIsLiteralOrListOfLiteral(AExpression expression) {

            // missing and literal expressions are safe
            if((expression == null) || (expression is LiteralExpression)) {
                return;
            }

            // list expressions must be further validated
            if(expression is ListExpression listExpression) {

                // make sure every item in the list is a literal expression
                foreach(var item in listExpression) {
                    if(!(item is LiteralExpression)) {
                        _builder.Log(Error.ExpectedLiteralValue, item);
                    }
                }
            } else {
                _builder.Log(Error.ExpectedLiteralValueOrListOfLiteralValues, expression);
            }
        }
    }
}
