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
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class StructureAnalyzer {

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, ParameterDeclaration node) {

            // validate attributes
            ValidateAllowAttribute(node, node.Type, node.Allow);

            // ensure parameter declaration is a child of the module declaration (nesting is not allowed)
            if(!(node.Parents.OfType<ADeclaration>() is ModuleDeclaration)) {
                _builder.Log(Error.ParameterDeclarationCannotBeNested, node);
            }

            // default 'Type' attribute value is 'String' when omitted
            if(node.Type == null) {
                node.Type = new LiteralExpression("String") {
                    SourceLocation = node.SourceLocation
                };
            }

            // default 'Section' attribute value is "Module Settings" when omitted
            if(node.Section == null) {
                node.Section = new LiteralExpression("Module Settings") {
                    SourceLocation = node.SourceLocation
                };
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

                // NOTE (2019-10-30, bjorg): for a 'Secret' type parameter, we need to create a new resource
                //  that is used to decrypt the parameter into a plaintext value.

                var decoder = AddDeclaration(node, new ResourceDeclaration(Literal("Plaintext")) {
                    Type = Literal("Module::DecryptSecret"),
                    Properties = new ObjectExpression {
                        ["ServiceToken"] = FnGetAtt("Module::DecryptSecretFunction", "Arn"),
                        ["Ciphertext"] = FnRef(node.FullName)
                    },
                    DiscardIfNotReachable = true
                });
                decoder.ReferenceExpression = FnGetAtt(decoder.FullName, "Plaintext");
            } else {
                if(node.EncryptionContext != null) {
                    _builder.Log(Error.EncryptionContextAttributeRequiresSecretType, node.Properties);
                }
            }

            // validate other attributes based on parameter type
            if(IsValidCloudFormationParameterType(node.Type.Value)) {

                // value types cannot have Properties/Allow attribute
                if(node.Properties != null) {
                    _builder.Log(Error.PropertiesAttributeRequiresCloudFormationType, node.Properties);
                }
                if(node.Allow != null) {
                    _builder.Log(Error.AllowAttributeRequiresCloudFormationType, node.Properties);
                }
            } else if(IsValidCloudFormationResourceType(node.Type.Value)) {

                // check if the 'Properties' attribute is set, which indicates a resource must be created when the parameter is omitted
                if(node.Properties != null) {

                    // add condition for creating the source
                    var condition = AddDeclaration(node, new ConditionDeclaration(Literal("IsBlank")) {
                        Value = FnEquals(FnRef(node.FullName), Literal(""))
                    });

                    // add conditional resource
                    var resource = AddDeclaration(node, new ResourceDeclaration(Literal("Resource")) {
                        Type = Literal(node.Type.Value),

                        // TODO: should the data-structure be cloned?
                        Properties = node.Properties,

                        // TODO: set 'arnAttribute' for resource (default attribute to return when referencing the resource),

                        If = FnCondition(condition.FullName),

                        // TODO: should the data-structure be cloned?
                        Pragmas = node.Pragmas
                    });

                    // update the reference expression for the parameter
                    node.ReferenceExpression = new IfFunctionExpression {
                        Condition = FnCondition(condition.FullName),
                        IfTrue = _builder.GetExportReference(resource),
                        IfFalse = FnRef(node.FullName)
                    };
                }
                if(node.Allow != null) {

                    // validate attributes
                    ValidateAllowAttribute(node, node.Type, node.Allow);

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
                _builder.Log(Error.TypeAttributeInvalid, node.Type);
            }
        }
    }
}