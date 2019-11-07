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
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public partial class DeclarationsVisitor {

        //--- Methods ---
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
                    _builder.LogError($"MinValue attribute cannot be used with this parameter type", node.MinValue.SourceLocation);
                }
                if(node.MaxValue != null) {
                    _builder.LogError($"MaxValue attribute cannot be used with this parameter type", node.MaxValue.SourceLocation);
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
                        _builder.LogError($"AllowedPattern must be a valid regex expression", node.AllowedPattern.SourceLocation);
                    }
                } else if(node.ConstraintDescription != null) {
                    // the 'ConstraintDescription' attribute is only valid in conjunction with the 'AllowedPattern' attribute
                    _builder.LogError($"ConstraintDescription attribute can only be used in conjunction with the AllowedPattern attribute", node.ConstraintDescription.SourceLocation);
                }
                if(node.MinLength != null) {

                    // TODO: validate the value is a number
                    throw new NotImplementedException();
                }
                if(node.MaxLength != null) {

                    // TODO: validate the value is a number
                    throw new NotImplementedException();
                }
            } else {
                if(node.AllowedPattern != null) {
                    _builder.LogError($"AllowedPattern attribute cannot be used with this parameter type", node.AllowedPattern.SourceLocation);
                }
                if(node.ConstraintDescription != null) {
                    _builder.LogError($"ConstraintDescription attribute cannot be used with this parameter type", node.ConstraintDescription.SourceLocation);
                }
                if(node.MinLength != null) {
                    _builder.LogError($"MinLength attribute cannot be used with this parameter type", node.MinLength.SourceLocation);
                }
                if(node.MaxLength != null) {
                    _builder.LogError($"MaxLength attribute cannot be used with this parameter type", node.MaxLength.SourceLocation);
                }
            }

            // only the 'Secret' type can have the 'EncryptionContext' attribute
            if(node.Type.Value == "Secret") {

                // NOTE (2019-10-30, bjorg): for a 'Secret' type parameter, we need to create a new resource
                //  that is used to decrypt the parameter into a plaintext value.

                var decoder = AddDeclaration(node, new ResourceDeclaration {
                    Resource = Literal("Plaintext"),
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
                        Condition = Literal("IsBlank"),
                        Value = FnEquals(FnRef(node.FullName), Literal(""))
                    });

                    // add conditional resource
                    var resource = AddDeclaration(node, new ResourceDeclaration {
                        Resource = Literal("Resource"),
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
                        name: node.Parameter.Value,
                        awsType: node.Type.Value,
                        reference: node.ReferenceExpression,
                        allow: node.Allow.Tags,
                        condition: null
                    );
                }
            } else {
                _builder.LogError($"unsupported type", node.Type.SourceLocation);
            }
        }
    }
}