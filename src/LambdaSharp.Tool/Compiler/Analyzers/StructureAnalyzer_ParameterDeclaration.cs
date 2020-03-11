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

using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Tool.Compiler.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class StructureAnalyzer {

        //--- Methods ---
        public override bool VisitStart(ParameterDeclaration node) {

            // register item declaration
            _builder.RegisterItemDeclaration(node);
            node.ReferenceExpression = FnRef(node.FullName, resolved: true);

            /*
             * VALIDATION
             */

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // ensure parameter declaration is a child of the module declaration (nesting is not allowed)
            if(!(node.Parents.OfType<ADeclaration>().FirstOrDefault() is ModuleDeclaration)) {
                _builder.Log(Error.ParameterDeclarationCannotBeNested, node);
            }

            // default 'Type' attribute value is 'String' when omitted
            if(node.Type == null) {
                node.Type = Literal("String");
            }

            // default 'Section' attribute value is "Module Settings" when omitted
            if(node.Section == null) {
                node.Section = Literal("Module Settings");
            }

            // the 'Description' attribute cannot exceed 4,000 characters
            if((node.Description != null) && (node.Description.Value.Length > 4_000)) {
                _builder.Log(Error.DescriptionAttributeExceedsSizeLimit, node.Description);
            }

            // only the 'Number' type can have the 'MinValue' and 'MaxValue' attributes
            if(node.Type.Value == "Number") {
                if((node.MinValue != null) && !int.TryParse(node.MinValue.Value, out var _)) {
                    _builder.Log(Error.ValueMustBeAnInteger, node.MinValue);
                }
                if((node.MaxValue != null) && !int.TryParse(node.MaxValue.Value, out var _)) {
                    _builder.Log(Error.ValueMustBeAnInteger, node.MaxValue);
                }
            } else {
                if(node.MinValue != null) {
                    _builder.Log(Error.MinValueAttributeRequiresNumberType, node.MinValue);
                }
                if(node.MaxValue != null) {
                    _builder.Log(Error.MaxValueAttributeRequiresNumberType, node.MaxValue);
                }
            }

            // only the 'String' type can have the 'AllowedPattern', 'MinLength', and 'MaxLength' attributes
            if(node.Type.Value == "String") {

                // the 'AllowedPattern' attribute must be a valid regex expression
                if(node.AllowedPattern != null) {

                    // check if 'AllowedPattern' is a valid regular expression
                    try {
                        new Regex(node.AllowedPattern.Value);
                    } catch {
                        _builder.Log(Error.AllowedPatternAttributeInvalid, node.AllowedPattern);
                    }
                } else if(node.ConstraintDescription != null) {
                    // the 'ConstraintDescription' attribute is only valid in conjunction with the 'AllowedPattern' attribute
                    _builder.Log(Error.ConstraintDescriptionAttributeRequiresAllowedPatternAttribute, node.ConstraintDescription);
                }
                if((node.MinLength != null) && !int.TryParse(node.MinLength.Value, out var _)) {
                    _builder.Log(Error.ValueMustBeAnInteger, node.MinLength);
                }
                if((node.MaxLength != null) && !int.TryParse(node.MaxLength.Value, out var _)) {
                    _builder.Log(Error.ValueMustBeAnInteger, node.MaxLength);
                }
            } else {
                if(node.AllowedPattern != null) {
                    _builder.Log(Error.AllowedPatternAttributeRequiresStringType, node.AllowedPattern);
                }
                if(node.ConstraintDescription != null) {
                    _builder.Log(Error.ConstraintDescriptionAttributeRequiresStringType, node.ConstraintDescription);
                }
                if(node.MinLength != null) {
                    _builder.Log(Error.MinLengthAttributeRequiresStringType, node.MinLength);
                }
                if(node.MaxLength != null) {
                    _builder.Log(Error.MaxLengthAttributeRequiresStringType, node.MaxLength);
                }
            }

            // only the 'Secret' type can have the 'EncryptionContext' attribute
            if(node.Type.Value == "Secret") {

                // all 'EncryptionContext' values must be literal values
                if(node.EncryptionContext != null) {
                    foreach(var kv in node.EncryptionContext) {
                        if(!(kv.Value is LiteralExpression)) {
                            _builder.Log(Error.ExpectedLiteralStringExpression, kv.Value);
                        }
                    }
                }
            } else {
                if(node.EncryptionContext != null) {
                    _builder.Log(Error.EncryptionContextAttributeRequiresSecretType, node.Properties);
                }
            }

            // only on-demand-resource parameter types can have 'Properties' or 'Allow' attributes
            if(IsValidCloudFormationParameterType(node.Type.Value) || (node.Type.Value == "Secret")) {
                if(node.Properties != null) {
                    _builder.Log(Error.PropertiesAttributeRequiresCloudFormationType, node.Properties);
                }
                if(node.Allow != null) {
                    _builder.Log(Error.AllowAttributeRequiresCloudFormationType, node.Properties);
                }
            } else {
                ValidateAllowAttribute(node, node.Type, node.Allow);

                // only value parameters can have 'Import' attribute
                if(node.Import != null) {

                    // TODO: move to Error.cs
                    _builder.Log(new Error(0, "'Import' attribute can only be used with a value parameter type"), node.Properties);
                }
            }

            /*
             * EXPRESSION CREATION
             */

            // create 'Import' expression
            if(node.Import != null) {

                // extract export name from module reference
                var moduleParts = node.Import.Value.Split("::", 2);
                var module = moduleParts[0];
                string export;
                if(moduleParts.Length == 2) {
                    export = moduleParts[1];
                } else {
                    export = "MISSING";

                    // TODO: move to Error.cs
                    _builder.Log(new Error(0, "invalid 'Import' attribute"), node.Import);
                }
                var importDefaultValue = $"${module.Replace(".", "-")}::{export}";

                // validate module name
                if(ModuleInfo.TryParse(module, out var moduleInfo)) {
                    if(moduleInfo.Version != null) {

                        // TODO: move to Error.cs
                        _builder.Log(new Error(0, "'Import' attribute cannot have a version"), node.Import);
                    }
                    if(moduleInfo.Origin != null) {

                        // TODO: move to Error.cs
                        _builder.Log(new Error(0, "'Import' attribute cannot have an origin"), node.Import);
                    }
                } else {

                    // TODO: move to Error.cs
                    _builder.Log(new Error(0, "invalid 'Import' attribute"), node.Import);
                }
                if(node.Default != null) {

                    // TODO: move to Error.cs
                    _builder.Log(new Error(0, "cannot use 'Default' attribute with 'Import'"), node.Import);
                } else {
                    node.Default = Literal(importDefaultValue);
                }

                // add condition for distinguishing import bindings from literal values
                var conditionDeclaration = AddDeclaration(node, new ConditionDeclaration(Literal("IsImported")) {
                    Value = FnAnd(
                        FnNot(FnEquals(FnRef(node.FullName, resolved: true), Literal(""))),
                        FnEquals(FnSelect("0", FnSplit("$", FnRef(node.FullName))), Literal(""))
                    )
                });

                // use condition to determine where the imported value should come from
                node.ReferenceExpression = FnIf(
                    conditionDeclaration.FullName,
                    FnImportValue(FnSub("${DeploymentPrefix}${Import}", new ObjectExpression {
                        ["Import"] = FnSelect("1", FnSplit("$", node.ReferenceExpression))
                    })),
                    node.ReferenceExpression
                );
            }

            // create resource for decrypting parameter value
            if(node.Type.Value == "Secret") {

                // NOTE (2019-10-30, bjorg): for a 'Secret' type parameter, we need to create a
                //  resource to decrypt the parameter into a plaintext value.
                var expression = node.ReferenceExpression;
                if(node.EncryptionContext != null) {
                    expression = FnJoin(
                        "|",
                        new AExpression[] {
                            node.ReferenceExpression
                        }.Union(
                            node.EncryptionContext.Select(kv => Literal($"{kv.Key}={kv.Value}"))
                        ).ToArray()
                    );
                }
                var decryptResourceDeclaration = AddDeclaration(node, new ResourceDeclaration(Literal("Plaintext")) {
                    Type = Literal("Module::DecryptSecret"),
                    Properties = new ObjectExpression {
                        ["ServiceToken"] = FnGetAtt("Module::DecryptSecretFunction", "Arn"),
                        ["Ciphertext"] = expression
                    },
                    DiscardIfNotReachable = true
                });
                node.ReferenceExpression = FnGetAtt(decryptResourceDeclaration.FullName, "Plaintext");
            } else if(IsValidCloudFormationParameterType(node.Type.Value)) {

                // nothing to do
            } else if(IsValidCloudFormationResourceType(node.Type.Value)) {

                // check if the 'Properties' attribute is set, which indicates a resource must be created when the parameter is omitted
                if(node.Properties != null) {

                    // add condition for creating the source
                    var conditionDeclaration = AddDeclaration(node, new ConditionDeclaration(Literal("IsBlank")) {
                        Value = FnEquals(FnRef(node.FullName, resolved: true), Literal(""))
                    });

                    // add conditional resource
                    var resourceDeclaration = AddDeclaration(node, new ResourceDeclaration(Literal("Resource")) {
                        Type = Literal(node.Type.Value),

                        // TODO: should the data-structure be cloned?
                        Properties = node.Properties,

                        // TODO: set 'arnAttribute' for resource (default attribute to return when referencing the resource),

                        If = FnCondition(conditionDeclaration.FullName),

                        // TODO: should the data-structure be cloned?
                        Pragmas = node.Pragmas
                    });

                    // update the reference expression for the parameter
                    node.ReferenceExpression = FnIf(conditionDeclaration.FullName, _builder.GetExportReference(resourceDeclaration), node.ReferenceExpression);
                }
                if(node.Allow != null) {

                    // request input parameter or conditional managed resource grants
                    AddGrant(
                        name: node.FullName,
                        awsType: node.Type.Value,
                        reference: node.ReferenceExpression,
                        allow: node.Allow,
                        condition: null
                    );
                }
            } else {
                _builder.Log(Error.ResourceTypeAttributeTypeIsInvalid, node.Type);
            }
            return true;
        }
    }
}