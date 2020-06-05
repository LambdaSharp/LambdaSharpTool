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

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Compiler;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Model;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class StructureAnalyzer : ASyntaxAnalyzer {

        //--- Class Fields ---
        private static readonly HashSet<string> _cloudFormationParameterTypes;
        private static readonly string _decryptSecretFunctionCode;
        private static Regex SecretArnRegex = new Regex(@"^arn:aws:kms:[a-z\-]+-\d:\d{12}:key\/[a-fA-F0-9\-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex SecretAliasRegex = new Regex("^[0-9a-zA-Z/_\\-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex AlphanumericRegex = new Regex("^[0-9a-zA-Z]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        public override bool VisitStart(UsingModuleDeclaration node) {

            // TODO: load resource types from module
            return true;
        }

        public override bool VisitStart(PseudoParameterDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref(node.FullName, resolved: true);
            return true;
        }

        public override bool VisitStart(ImportDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // extract optional export name from module reference
            var export = node.ItemName.Value;
            var module = node.Module.Value;
            var moduleParts = module.Split("::", 2);
            if(moduleParts.Length == 2) {
                module = moduleParts[0];
                export = moduleParts[1];
            }
            var import = $"{module}::{export}";

            // validate module name
            if(ModuleInfo.TryParse(node.Module.Value, out var moduleInfo)) {
                if(moduleInfo.Version != null) {
                    _builder.Log(Error.ImportModuleAttributeCannotHaveVersion, node.Module);
                }
                if(moduleInfo.Origin != null) {
                    _builder.Log(Error.ImportModuleAttributeCannotHaveOrigin, node.Module);
                }
            } else {
                _builder.Log(Error.ImportModuleAttributeIsInvalid, node.Module);
            }

            // validate import type
            if(node.Type == null) {
                node.Type = Fn.Literal("String");
            } else if(node.Type?.Value != "Secret") {

                // TODO: validate the import type
                if(node.EncryptionContext != null) {
                    _builder.Log(Error.EncryptionContextAttributeRequiresSecretType, node);
                }
            }

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // check if an import parameter for this reference exists already
            var importParameterName = module.ToIdentifier() + export.ToIdentifier();
            var foundDeclaration = _builder.ItemDeclarations.FirstOrDefault(item => item.FullName == importParameterName);
            if(foundDeclaration != null) {
                if(foundDeclaration is ParameterDeclaration existingParameterDeclaration) {

                    // NOTE (2020-02-27, bjorg): if an import declaration already exists for this value, it must be identical in every way; such duplicates
                    //  are allowed, because it is not possible to know ahead of time if a value was already imported
                    if(existingParameterDeclaration.Import?.Value != import) {
                        _builder.Log(Error.ImportDuplicateWithDifferentBinding(importParameterName), foundDeclaration);
                    } else if(existingParameterDeclaration.Type.Value != node.Type.Value) {
                        _builder.Log(Error.ImportDuplicateWithDifferentType(importParameterName), foundDeclaration);
                    }
                } else {
                    _builder.Log(Error.DuplicateName(importParameterName), foundDeclaration);
                }
                node.ReferenceExpression = Fn.Ref(foundDeclaration.FullName);
            } else {

                // add import declaration as a module parameter
                var importParameterDeclaration = AddDeclaration(node.ParentModuleDeclaration, new ParameterDeclaration(Fn.Literal(importParameterName)) {
                    Type = Fn.Literal(node.Type.Value),
                    Description = Fn.Literal($"Cross-module reference for {module}::{export}"),
                    EncryptionContext = node.EncryptionContext,

                    // set default settings for import parameters
                    AllowedPattern = Fn.Literal("^.+$"),
                    ConstraintDescription = Fn.Literal("must either be a cross-module reference or a non-empty value"),
                    Section = Fn.Literal($"{module} Imports"),
                    Label = Fn.Literal(export),
                    Import = Fn.Literal($"{module}::{export}"),
                    DiscardIfNotReachable = true
                });
                node.ReferenceExpression = Fn.Ref(importParameterDeclaration.FullName);
            }

            // add optional grants
            if(node.Allow != null) {
                AddGrant(
                    name: node.FullName,
                    awsType: node.Type.Value,
                    reference: node.ReferenceExpression,
                    allow: node.Allow,
                    condition: null
                );
            }
            return true;
        }

        public override bool VisitStart(VariableDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // validate Value attribute
            if(node.Type?.Value == "Secret") {
                if((node.Value is ListExpression) || (node.Value is ObjectExpression)) {
                    _builder.Log(Error.SecretTypeMustBeLiteralOrExpression, node.Value);
                }
            } else if(node.EncryptionContext != null) {
                _builder.Log(Error.EncryptionContextAttributeRequiresSecretType, node);
            }

            // set declaration expression
            AExpression declarationExpression;
            if(node.EncryptionContext != null) {
                declarationExpression = Fn.Join(
                    "|",
                    new AExpression[] {
                        node.Value
                    }.Union(
                        node.EncryptionContext.Select(kv => Fn.Literal($"{kv.Key}={kv.Value}"))
                    ).ToArray()
                );
            } else {
                declarationExpression = (node.Value is ListExpression values)
                    ? Fn.Join(",", values)
                    : node.Value;
            }
            node.ReferenceExpression = declarationExpression;


            // // check if value must be decrypted
            // if(result.HasSecretType) {
            //     var decoder = AddResource(
            //         parent: result,
            //         name: "Plaintext",
            //         description: null,
            //         scope: null,
            //         resource: CreateDecryptSecretResourceFor(result),
            //         resourceExportAttribute: null,
            //         dependsOn: null,
            //         condition: null,
            //         pragmas: null
            //     );
            //     decoder.Reference = FnGetAtt(decoder.ResourceName, "Plaintext");
            //     decoder.DiscardIfNotReachable = true;
            // }

            // // add optional grants
            // if(allow != null) {
            //     AddGrant(result.LogicalId, type, value, allow, condition: null);
            // }
            return true;
        }

        public override bool VisitStart(GroupDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref("AWS::NoValue");
            return true;
        }

        public override bool VisitStart(ResourceDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref(node.FullName, resolved: true);

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // check if declaration is a resource reference
            if(node.Value != null) {

                // references should resolved to the resource reference
                node.ReferenceExpression = node.Value;

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
                    foreach(var arnValue in listExpression) {
                        ValidateARN(arnValue);

                        // add resource permissions per ARN
                        if(node.Allow != null) {
                            AddGrant(
                                name: node.FullName,
                                awsType: node.Type.Value,
                                reference: arnValue,
                                allow: node.Allow,
                                condition: null
                            );
                        }
                    }

                    // default type to 'List'
                    if(node.Type == null) {

                        // TODO: what's the best type here?
                        node.Type = Fn.Literal("List");
                    }
                } else {
                    ValidateARN(node.Value);

                    // add resource permissions per ARN
                    if(node.Allow != null) {
                        AddGrant(
                            name: node.FullName,
                            awsType: node.Type.Value,
                            reference: node.Value,
                            allow: node.Allow,
                            condition: null
                        );
                    }

                    // default type to 'String'
                    if(node.Type == null) {
                        node.Type = Fn.Literal("String");
                    }
                }
            } else if(node.Type != null) {

                // ensure Properties property is set to an empty object expression when null
                if(node.Properties == null) {
                    node.Properties = new ObjectExpression();
                }

                // check if type is AWS resource type or a LambdaSharp custom resource type
                if(_builder.CloudformationSpec.IsAwsType(node.Type.Value)) {

                    // validate resource properties for native CloudFormation resource type
                    if(node.HasTypeValidation) {
                        ValidateProperties(node.Type.Value, node.Properties);
                    }
                } else if(_builder.TryGetCustomResourceType(node.Type.Value, out var resourceType)) {

                    // validate resource properties for LambdaSharp custom resource type
                    if(node.HasTypeValidation) {
                        ValidateProperties(resourceType, node.Properties);
                    }
                } else {
                    _builder.Log(Error.ResourceUnknownType(node.Type.Value), node.Type);
                }

                // check if resource is conditional
                if((node.If != null) && !(node.If is ConditionExpression)) {

                    // creation condition as sub-declaration
                    AddDeclaration(node, new ConditionDeclaration(Fn.Literal("Condition")) {
                        Value = node.If
                    });
                }

                // add resource permissions
                if(node.Allow != null) {
                    AddGrant(
                        name: node.FullName,
                        awsType: node.Type.Value,
                        reference: node.ReferenceExpression,
                        allow: node.Allow,
                        condition: null
                    );
                }
            } else {

                // CloudFormation resource must have a type
                _builder.Log(Error.TypeAttributeMissing, node);
            }
            return true;

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

        public override bool VisitStart(NestedModuleDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref("AWS::NoValue");

            // TODO: validate the parameters and output values from the module
            return true;
        }

        public override bool VisitStart(PackageDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

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
            var variable = AddDeclaration(node, new VariableDeclaration(Fn.Literal("PackageName")) {
                Type = Fn.Literal("String"),
                Value = Fn.Literal($"{node.LogicalId}-DRYRUN.zip")
            });

            // set declaration expression
            node.ReferenceExpression = GetModuleArtifactExpression($"${{{variable.FullName}}}");
            return true;
        }

        public override bool VisitStart(ConditionDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = new ConditionExpression {
                ReferenceName = Fn.Literal(node.FullName)
            };
            return true;
        }

        public override bool VisitStart(MappingDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref("AWS::NoValue");

            // check if object expression is valid (must have first- and second-level keys)
            if(node.Value.Any()) {
                var topLevelKeys = new HashSet<string>();
                var secondLevelKeys = new HashSet<string>();

                // check that all first-level keys have object expressions
                foreach(var topLevelEntry in node.Value) {

                    // validate top-level key
                    if(!AlphanumericRegex.IsMatch(topLevelEntry.Key.Value)) {
                        _builder.Log(Error.MappingKeyMustBeAlphanumeric, topLevelEntry.Key);
                    }
                    if(!topLevelKeys.Add(topLevelEntry.Key.Value)) {
                        _builder.Log(Error.MappingDuplicateKey, topLevelEntry.Key);
                    }

                    // validate top-level value
                    if(topLevelEntry.Value is ObjectExpression secondLevelObjectExpression) {
                        if(secondLevelObjectExpression.Any()) {
                            secondLevelKeys.Clear();

                            // check that all second-level keys have literal expressions
                            foreach(var secondLevelEntry in secondLevelObjectExpression) {

                                // validate top-level key
                                if(!AlphanumericRegex.IsMatch(secondLevelEntry.Key.Value)) {
                                    _builder.Log(Error.MappingKeyMustBeAlphanumeric, secondLevelEntry.Key);
                                }
                                if(!secondLevelKeys.Add(secondLevelEntry.Key.Value)) {
                                    _builder.Log(Error.MappingDuplicateKey, secondLevelEntry.Key);
                                }

                                // validate second-level value
                                if(!IsListOrLiteral(secondLevelEntry.Value)) {
                                    _builder.Log(Error.MappingExpectedListOrLiteral, secondLevelEntry.Value);
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
            return true;

            // local functions
            bool IsListOrLiteral(AExpression value)
                => (value is LiteralExpression)
                    || ((value is ListExpression listExpression) && listExpression.All(item => IsListOrLiteral(item)));
        }

        public override bool VisitStart(ResourceTypeDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref("AWS::NoValue");
            return true;
        }

        public override bool VisitStart(MacroDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);

            // set declaration expression
            node.ReferenceExpression = Fn.Ref("AWS::NoValue");

            // check if a root macros collection needs to be created
            if(!_builder.TryGetItemDeclaration("Macros", out var macrosItem)) {
                macrosItem = AddDeclaration(node.ParentModuleDeclaration, new GroupDeclaration(Fn.Literal("Macros")) {
                    Description = Fn.Literal("Macro definitions")
                });
            }

            // add macro resource
            AddDeclaration(macrosItem, new ResourceDeclaration(node.ItemName) {
                Type = Fn.Literal("AWS::CloudFormation::Macro"),
                Description = node.Description,
                Properties = new ObjectExpression {
                    ["Name"] = Fn.Sub($"${{DeploymentPrefix}}{node.ItemName}"),
                    ["Description"] = node.Description,
                    ["FunctionName"] = Fn.Ref(node.Handler.Value)
                }
            });
            return true;
        }

        public override bool VisitStart(AndConditionExpression node) {
            AssertIsConditionExpression(node.LeftValue);
            AssertIsConditionExpression(node.RightValue);
            return true;
        }

        public override bool VisitStart(Base64FunctionExpression node) => true;

        public override bool VisitStart(CidrFunctionExpression node) {
            AssertIsValueExpression(node.IpBlock);
            AssertIsValueExpression(node.Count);
            AssertIsValueExpression(node.CidrBits);
            return true;
        }

        public override bool VisitStart(ConditionExpression node) => true;

        public override bool VisitStart(EqualsConditionExpression node) {
            AssertIsValueExpression(node.LeftValue);
            AssertIsValueExpression(node.RightValue);
            return true;
        }

        public override bool VisitStart(FindInMapFunctionExpression node) {
            AssertIsValueExpression(node.TopLevelKey);
            AssertIsValueExpression(node.SecondLevelKey);
            return true;
        }

        public override bool VisitStart(GetAttFunctionExpression node) {
            AssertIsValueExpression(node.AttributeName);

            // TODO: validate the attribute exists on !GetAtt on the given resource type (unless type checking is disabled for this declration)
            return true;
        }

        public override bool VisitStart(GetAZsFunctionExpression node) {
            AssertIsValueExpression(node.Region);
            return true;
        }

        public override bool VisitStart(IfFunctionExpression node) {

            // NOTE (2019-11-22, bjorg): we allow literal expression, in addition to condition expression, for the !If condition
            if(node.Condition is LiteralExpression literalExpression) {

                // lift literal expression into a !Condition expression
                node.Condition = new ConditionExpression {
                    SourceLocation = literalExpression.SourceLocation,
                    ReferenceName = literalExpression
                };
            } else if((node.Condition is AConditionExpression) && !(node.Condition is ConditionExpression)) {

                // convert inline conditional expression into a reference to a conditional declaration
                string conditionName = null;
                for(var i = 0; ; ++i) {
                    conditionName = $"IfExpr{i}";
                    if(!_builder.TryGetItemDeclaration($"{node.ParentItemDeclaration.FullName}::{conditionName}", out var _)) {
                        break;
                    }
                }
                var condition = AddDeclaration(node.ParentItemDeclaration, new ConditionDeclaration(Fn.Literal(conditionName)) {
                    Value = node.Condition
                });
                node.Condition = new ConditionExpression {
                    SourceLocation = node.Condition.SourceLocation,
                    ReferenceName = Fn.Literal(condition.FullName)
                };
            }
            AssertIsConditionExpression(node.Condition);
            AssertIsValueExpression(node.IfTrue);
            AssertIsValueExpression(node.IfFalse);
            return true;
        }

        public override bool VisitStart(ImportValueFunctionExpression node) {
            AssertIsValueExpression(node.SharedValueToImport);
            return true;
        }

        public override bool VisitStart(JoinFunctionExpression node) {
            AssertIsLiteralString(node.Delimiter);
            AssertIsValueExpression(node.Values);
            return true;
        }

        public override bool VisitStart(ListExpression node) {
            foreach(var item in node) {
                AssertIsValueExpression(item);
            }
            return true;
        }

        public override bool VisitStart(LiteralExpression node) => true;

        public override bool VisitStart(NotConditionExpression node) {
            AssertIsConditionExpression(node.Value);
            return true;
        }

        public override bool VisitStart(ObjectExpression node) {
            foreach(var kv in node) {
                AssertIsValueExpression(kv.Value);
            }
            return true;
        }

        public override bool VisitStart(OrConditionExpression node) {
            AssertIsConditionExpression(node.LeftValue);
            AssertIsConditionExpression(node.RightValue);
            return true;
        }

        public override bool VisitStart(ReferenceFunctionExpression node) => true;

        public override bool VisitStart(SelectFunctionExpression node) {
            AssertIsValueExpression(node.Index);
            AssertIsValueExpression(node.Values);
            return true;
        }

        public override bool VisitStart(SplitFunctionExpression node) {
            AssertIsLiteralString(node.Delimiter);
            AssertIsValueExpression(node.SourceString);
            return true;
        }

        public override bool VisitStart(SubFunctionExpression node) => true;

        public override bool VisitStart(TransformFunctionExpression node) => true;

        private void ValidateAllowAttribute(ADeclaration node, LiteralExpression type, SyntaxNodeCollection<LiteralExpression> allow) {
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

        private bool IsValidCloudFormationParameterType(string type) => _cloudFormationParameterTypes.Contains(type);

        private bool IsValidCloudFormationResourceType(string awsType) => _builder.CloudformationSpec.IsAwsType(awsType);

        private T AddDeclaration<T>(AItemDeclaration parent, T declaration) where T : AItemDeclaration {
            parent.Declarations.Add(declaration);
            return declaration;
        }

        private T AddDeclaration<T>(ModuleDeclaration parent, T declaration) where T : AItemDeclaration {
            parent.Items.Add(declaration);
            return declaration;
        }

        private void AddGrant(string name, string awsType, AExpression reference, SyntaxNodeCollection<LiteralExpression> allow, AExpression condition)
            => _builder.AddGrant(name, awsType, reference, allow, condition);

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
            if(moduleDeclaration.HasLambdaSharpDependencies) {
                condition = Fn.Condition("UseCoreServices");
                variable = Fn.If("UseCoreServices", Fn.Ref($"LambdaSharp::{name}"), Fn.Ref("AWS::NoValue"));
                return true;
            }
            variable = null;
            condition = null;
            return false;
        }

        private static AExpression GetModuleArtifactExpression(string filename)
            => Fn.Sub($"{ModuleInfo.MODULE_ORIGIN_PLACEHOLDER}/${{Module::Namespace}}/${{Module::Name}}/.artifacts/{filename}");

        private void ValidateProperties(string awsType, ObjectExpression properties) {
            if(_builder.CloudformationSpec.ResourceTypes.TryGetValue(awsType, out var resource)) {
                ValidateProperties(resource, properties);
            }

            // local functions
            void ValidateProperties(ResourceType currentResource, ObjectExpression currentProperties) {

                // 'Fn::Transform' can add arbitrary properties at deployment time, so we can't validate the properties at compile time
                if(currentProperties.ContainsKey("Fn::Transform")) {
                    _builder.Log(Warning.ResourceContainsTransformAndCannotBeValidated, currentProperties);
                } else {

                    // check that all required properties are defined
                    foreach(var property in currentResource.Properties.Where(kv => kv.Value.Required)) {
                        if(!currentProperties.ContainsKey(property.Key)) {
                            _builder.Log(Error.ResourceMissingProperty(property.Key), currentProperties);
                        }
                    }
                }

                // check that all defined properties exist
                foreach(var currentProperty in currentProperties) {
                    if(currentResource.Properties.TryGetValue(currentProperty.Key.Value, out var propertyType)) {
                        switch(propertyType.Type) {
                        case "List": {
                                switch(currentProperty.Value) {
                                case AFunctionExpression _:

                                    // TODO (2019-01-25, bjorg): validate the return type of the function is a list
                                    break;
                                case ListExpression listExpression:
                                    if(propertyType.ItemType != null) {
                                        if(!_builder.CloudformationSpec.TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResourceType)) {
                                            throw new ShouldNeverHappenException($"unable to find property type for: {awsType}.{propertyType.ItemType}");
                                        }

                                        // validate all items in list are objects that match the nested resource type
                                        for(var i = 0; i < listExpression.Count; ++i) {
                                            var item = listExpression[i];
                                            if(item is ObjectExpression objectExpressionItem) {
                                                ValidateProperties(nestedResourceType, objectExpressionItem);
                                            } else {
                                                _builder.Log(Error.ResourcePropertyExpectedMap($"[{i}]"), item);
                                            }
                                        }
                                    } else {

                                        // TODO (2018-12-06, bjorg): validate list items using the primitive type
                                    }
                                    break;
                                default:
                                    _builder.Log(Error.ResourcePropertyExpectedList(currentProperty.Key.Value), currentProperty.Value);
                                    break;
                                }
                            }
                            break;
                        case "Map": {
                                switch(currentProperty.Value) {
                                case AFunctionExpression _:

                                    // TODO (2019-01-25, bjorg): validate the return type of the function is a map
                                    break;
                                case ObjectExpression objectExpression:
                                    if(propertyType.ItemType != null) {
                                        if(!_builder.CloudformationSpec.TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResourceType)) {
                                            throw new ShouldNeverHappenException($"unable to find property type for: {awsType}.{propertyType.ItemType}");
                                        }

                                        // validate all values in map are objects that match the nested resource type
                                        foreach(var kv in objectExpression) {
                                            var item = kv.Value;
                                            if(item is ObjectExpression objectExpressionItem) {
                                                ValidateProperties(nestedResourceType, objectExpressionItem);
                                            } else {
                                                _builder.Log(Error.ResourcePropertyExpectedMap(kv.Key.Value), item);
                                            }
                                        }
                                    } else {

                                        // TODO (2018-12-06, bjorg): validate map entries using the primitive type
                                    }
                                    break;
                                default:
                                    _builder.Log(Error.ResourcePropertyExpectedMap(currentProperty.Key.Value), currentProperty.Value);
                                    break;
                                }
                            }
                            break;
                        case null:

                            // TODO (2018-12-06, bjorg): validate property value with the primitive type
                            break;
                        default: {
                                switch(currentProperty.Value) {
                                case AFunctionExpression _:

                                    // TODO (2019-01-25, bjorg): validate the return type of the function is a map
                                    break;
                                case ObjectExpression objectExpression:
                                    if(!_builder.CloudformationSpec.TryGetPropertyItemType(awsType, propertyType.ItemType, out var nestedResourceType)) {
                                        throw new ShouldNeverHappenException($"unable to find property type for: {awsType}.{propertyType.ItemType}");
                                    }
                                    ValidateProperties(nestedResourceType, objectExpression);
                                    break;
                                default:
                                    _builder.Log(Error.ResourcePropertyExpectedMap(currentProperty.Key.Value), currentProperty.Value);
                                    break;
                                }
                            }
                            break;
                        }
                    } else {
                        _builder.Log(Error.ResourceUnknownProperty(currentProperty.Key.Value, awsType), currentProperty.Key);
                    }
                }
            }
        }

        private void ValidateProperties(ModuleManifestResourceType resourceType, ObjectExpression properties) {

            // TODO (2020-02-19, bjorg): add support for nested custom resource types checks
            foreach(var kv in properties) {
                var key = kv.Key.Value;
                if(
                    (key != "ServiceToken")
                    && (key != "ResourceType")
                    && !resourceType.Properties.Any(field => field.Name == key)
                ) {
                    _builder.Log(Error.ResourceUnknownProperty(key, resourceType.Type));
                }
            }
        }

        private void AssertIsConditionExpression(AExpression expression) {
            switch(expression) {
            case AConditionExpression _:

                // nothing to do
                break;
            case AValueExpression _:
            case AFunctionExpression _:
                _builder.Log(Error.ExpectedConditionExpression, expression);
                break;
            default:
                _builder.Log(Error.UnrecognizedExpressionType(expression), expression);
                break;
            }
        }

        private void AssertIsValueExpression(AExpression expression) {
            switch(expression) {
            case AConditionExpression _:
                _builder.Log(Error.ExpectedConditionExpression, expression);
                break;
            case AValueExpression _:
            case AFunctionExpression _:

                // nothing to do
                break;
            default:
                _builder.Log(Error.UnrecognizedExpressionType(expression), expression);
                break;
            }
        }

        private void AssertIsLiteralString(AExpression expression) {
            if(!(expression is LiteralExpression literalExpression) || literalExpression.Type != LiteralType.String) {
                _builder.Log(Error.ExpectedLiteralStringExpression, expression);
            }
        }
    }
}
