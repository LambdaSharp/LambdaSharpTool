/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ParameterAndImportDeclarationProcessor : ASyntaxProcessor {

        //--- Constants ---
        private const int MAX_PARAMETER_DESCRIPTION_LENGTH = 4_000;

        // TODO: validate max parameter length
        private const int MAX_PARAMETER_VALUE_LENGTH = 4_000;

        //--- Class Fields ---
        #region Errors/Warnings

        // Structure
        private static readonly Error ParameterDeclarationCannotBeNested = new Error(0, "Parameter declaration cannot be nested in a Group");

        // Property: Type
        private static readonly ErrorFunc UnknownType = parameter => new Error(0, $"unknown parameter type '{parameter}'");
        private static readonly Warning AssumingStringType = new Warning(0, "missing 'Type' attribute, assuming type 'String'");

        // Property: MinLength, MaxLength
        private static readonly Error MinLengthAttributeRequiresStringType = new Error(0, "'MinLength' attribute can only be used with 'String' type");
        private static readonly Error MinLengthMustBeAnInteger = new Error(0, "'MinLength' must be an integer");
        private static readonly Error MinLengthMustBeNonNegative = new Error(0, $"'MinLength' must be greater or equal than 0");
        private static readonly Error MinLengthTooLarge = new Error(0, $"'MinLength' cannot exceed' {MAX_PARAMETER_VALUE_LENGTH:N0}");
        private static readonly Error MinMaxLengthInvalidRange = new Error(0, "'MinLength' must be less or equal to 'MaxLength'");
        private static readonly Error MaxLengthAttributeRequiresStringType = new Error(0, "'MaxLength' attribute can only be used with 'String' type");
        private static readonly Error MaxLengthMustBeAnInteger = new Error(0, "'MaxLength' must be an integer");
        private static readonly Error MaxLengthMustBePositive = new Error(0, "'MaxLength' must be greater than 0");
        private static readonly Error MaxLengthTooLarge = new Error(0, $"'MaxLength' cannot exceed' {MAX_PARAMETER_VALUE_LENGTH:N0}");

        // Property: MinValue, MaxValue
        private static readonly Error MinValueAttributeRequiresNumberType = new Error(0, "'MinValue' attribute can only be used with 'Number' type");
        private static readonly Error MinValueMustBeAnInteger = new Error(0, "'MinValue' must be an integer");
        private static readonly Error MaxValueAttributeRequiresNumberType = new Error(0, "'MaxValue' attribute can only be used with 'Number' type");
        private static readonly Error MinMaxValueInvalidRange = new Error(0, "'MinValue' must be less or equal to 'MaxValue'");
        private static readonly Error MaxValueMustBeAnInteger = new Error(0, "'MaxValue' must be an integer");

        // Property: AllowedPattern, ConstraintDescription
        private static readonly Error AllowedPatternAttributeRequiresStringType = new Error(0, "'AllowedPattern' attribute can only be used with 'String' type");
        private static readonly Error AllowedPatternAttributeInvalid = new Error(0, "'AllowedPattern' attribute must be a valid regular expression");
        private static readonly Error ConstraintDescriptionAttributeRequiresStringType = new Error(0, "'ConstraintDescription' attribute can only be used with 'String' type");
        private static readonly Error ConstraintDescriptionAttributeRequiresAllowedPatternAttribute = new Error(0, "'ConstraintDescription' attribute requires 'AllowedPattern' attribute to be set");

        // Property: Description
        private static readonly Error DescriptionAttributeExceedsSizeLimit = new Error(0, $"'Description' attribute cannot exceed {MAX_PARAMETER_DESCRIPTION_LENGTH:N0} characters");

        // Property: EncryptionContext
        private static readonly Error EncryptionContextAttributeRequiresSecretType = new Error(0, "'EncryptionContext' attribute can only be used with 'Secret' type");
        private static readonly Error EncryptionContextExpectedLiteralStringExpression = new Error(0, "'EncryptionContext' expected literal string expression");

        // Property: Allow, Import, Properties
        private static readonly Error AllowAttributeRequiresCloudFormationType = new Error(0, "'Allow' attribute can only be used with a CloudFormation type");
        private static readonly Error ParameterAttributeImportExpectedLiteral = new Error(0, "'Import' attribute can only be used with a value parameter type");
        private static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error(0, "'Properties' attribute can only be used with a CloudFormation type");
        #endregion

        private static readonly HashSet<string> _cloudFormationParameterTypes = new HashSet<string> {

            // Literal Types
            "String",
            "Number",
            "List<Number>",
            "CommaDelimitedList",

            // Parameter Store Keys & Values
            "AWS::SSM::Parameter::Name",
            "AWS::SSM::Parameter::Value<String>",
            "AWS::SSM::Parameter::Value<List<String>>",
            "AWS::SSM::Parameter::Value<CommaDelimitedList>",

            // Validated Resource Types
            "AWS::EC2::AvailabilityZone::Name",
            "AWS::EC2::Image::Id",
            "AWS::EC2::Instance::Id",
            "AWS::EC2::KeyPair::KeyName",
            "AWS::EC2::SecurityGroup::GroupName",
            "AWS::EC2::SecurityGroup::Id",
            "AWS::EC2::Subnet::Id",
            "AWS::EC2::Volume::Id",
            "AWS::EC2::VPC::Id",
            "AWS::Route53::HostedZone::Id",

            // List of Validated Resource Types
            "List<AWS::EC2::AvailabilityZone::Name>",
            "List<AWS::EC2::Image::Id>",
            "List<AWS::EC2::Instance::Id>",
            "List<AWS::EC2::KeyPair::KeyName>",
            "List<AWS::EC2::SecurityGroup::GroupName>",
            "List<AWS::EC2::SecurityGroup::Id>",
            "List<AWS::EC2::Subnet::Id>",
            "List<AWS::EC2::Volume::Id>",
            "List<AWS::EC2::VPC::Id>",
            "List<AWS::Route53::HostedZone::Id>",

            // Validated Resource Types from Parameter Store
            "AWS::SSM::Parameter::Value<AWS::EC2::AvailabilityZone::Name>",
            "AWS::SSM::Parameter::Value<AWS::EC2::Image::Id>",
            "AWS::SSM::Parameter::Value<AWS::EC2::Instance::Id>",
            "AWS::SSM::Parameter::Value<AWS::EC2::KeyPair::KeyName>",
            "AWS::SSM::Parameter::Value<AWS::EC2::SecurityGroup::GroupName>",
            "AWS::SSM::Parameter::Value<AWS::EC2::SecurityGroup::Id>",
            "AWS::SSM::Parameter::Value<AWS::EC2::Subnet::Id>",
            "AWS::SSM::Parameter::Value<AWS::EC2::Volume::Id>",
            "AWS::SSM::Parameter::Value<AWS::EC2::VPC::Id>",
            "AWS::SSM::Parameter::Value<AWS::Route53::HostedZone::Id>",

            // Validated List of Resource Types from Parameter Store
            "AWS::SSM::Parameter::Value<List<AWS::EC2::AvailabilityZone::Name>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::Image::Id>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::Instance::Id>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::KeyPair::KeyName>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::SecurityGroup::GroupName>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::SecurityGroup::Id>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::Subnet::Id>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::Volume::Id>",
            "AWS::SSM::Parameter::Value<List<AWS::EC2::VPC::Id>",
            "AWS::SSM::Parameter::Value<List<AWS::Route53::HostedZone::Id>"
        };

        //--- Class Methods ---
        private static bool IsCloudFormationParameterType(string type) => _cloudFormationParameterTypes.Contains(type);

        //--- Constructors ---
        public ParameterAndImportDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.Inspect(node => {
                switch(node) {
                case ParameterDeclaration parameterDeclaration:
                    ValidateStructure(parameterDeclaration);
                    ValidateParemeterType(parameterDeclaration);
                    break;
                case ImportDeclaration importDeclaration:
                    ValidateStructure(importDeclaration);
                    ValidateImportType(importDeclaration);
                    break;
                }
            });
        }

        private void ValidateStructure(AItemDeclaration node) {

            // ensure parameter declaration is a child of the module declaration (nesting is not allowed)
            if(!(node.GetParents().OfType<ADeclaration>().FirstOrDefault() is ModuleDeclaration)) {
                Logger.Log(ParameterDeclarationCannotBeNested, node);
            }
        }

        private void ValidateParemeterType(ParameterDeclaration node) {

            // assume 'String' type when 'Type' attribute is omitted
            if(node.Type == null) {
                Logger.Log(AssumingStringType, node);
                node.Type = Fn.Literal("String", node.SourceLocation);
            }

            // default 'Section' attribute value is "Module Settings" when omitted
            if(node.Section == null) {
                node.Section = Fn.Literal("Module Settings");
            }

            // the 'Description' attribute cannot exceed 4,000 characters
            if((node.Description != null) && (node.Description.Value.Length > MAX_PARAMETER_DESCRIPTION_LENGTH)) {
                Logger.Log(DescriptionAttributeExceedsSizeLimit, node.Description);
            }

            // only the 'Number' type can have the 'MinValue' and 'MaxValue' attributes
            if(node.Type.Value == "Number") {
                if((node.MinValue != null) && !int.TryParse(node.MinValue.Value, out var _)) {
                    Logger.Log(MinValueMustBeAnInteger, node.MinValue);
                }
                if((node.MaxValue != null) && !int.TryParse(node.MaxValue.Value, out var _)) {
                    Logger.Log(MaxValueMustBeAnInteger, node.MaxValue);
                }
                if(
                    (node.MinValue != null)
                    && (node.MaxValue != null)
                    && int.TryParse(node.MinValue.Value, out var minValueRange)
                    && int.TryParse(node.MaxValue.Value, out var maxValueRange)
                    && (maxValueRange < minValueRange)
                ) {
                    Logger.Log(MinMaxValueInvalidRange, node.MinValue);
                }

            } else {

                // ensure Number parameter options are not used
                if(node.MinValue != null) {
                    Logger.Log(MinValueAttributeRequiresNumberType, node.MinValue);
                }
                if(node.MaxValue != null) {
                    Logger.Log(MaxValueAttributeRequiresNumberType, node.MaxValue);
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
                        Logger.Log(AllowedPatternAttributeInvalid, node.AllowedPattern);
                    }
                } else if(node.ConstraintDescription != null) {

                    // the 'ConstraintDescription' attribute is only valid in conjunction with the 'AllowedPattern' attribute
                    Logger.Log(ConstraintDescriptionAttributeRequiresAllowedPatternAttribute, node.ConstraintDescription);
                }
                if(node.MinLength != null) {
                    if(!int.TryParse(node.MinLength.Value, out var minLength)) {
                        Logger.Log(MinLengthMustBeAnInteger, node.MinLength);
                    } else if(minLength < 0) {
                        Logger.Log(MinLengthMustBeNonNegative, node.MinLength);
                    } else if(minLength > MAX_PARAMETER_VALUE_LENGTH) {
                        Logger.Log(MinLengthTooLarge, node.MinLength);
                    }
                }
                if(node.MaxLength != null) {
                    if(!int.TryParse(node.MaxLength.Value, out var maxLength)) {
                        Logger.Log(MaxLengthMustBeAnInteger, node.MaxLength);
                    } else if(maxLength <= 0) {
                        Logger.Log(MaxLengthMustBePositive, node.MaxLength);
                    } else if(maxLength > MAX_PARAMETER_VALUE_LENGTH) {
                        Logger.Log(MaxLengthTooLarge, node.MaxLength);
                    }
                }
                if(
                    (node.MinLength != null)
                    && (node.MaxLength != null)
                    && int.TryParse(node.MinLength.Value, out var minLengthRange)
                    && int.TryParse(node.MaxLength.Value, out var maxLengthRange)
                    && (maxLengthRange < minLengthRange)
                ) {
                    Logger.Log(MinMaxLengthInvalidRange, node.MinLength);
                }
            } else {

                // ensure String parameter options are not used
                if(node.AllowedPattern != null) {
                    Logger.Log(AllowedPatternAttributeRequiresStringType, node.AllowedPattern);
                }
                if(node.ConstraintDescription != null) {
                    Logger.Log(ConstraintDescriptionAttributeRequiresStringType, node.ConstraintDescription);
                }
                if(node.MinLength != null) {
                    Logger.Log(MinLengthAttributeRequiresStringType, node.MinLength);
                }
                if(node.MaxLength != null) {
                    Logger.Log(MaxLengthAttributeRequiresStringType, node.MaxLength);
                }
            }

            // only the 'Secret' type can have the 'EncryptionContext' attribute
            if(node.Type.Value == "Secret") {
                ValidateEncryptContext(node.EncryptionContext);

                // TODO: add resource for decrypting secret
            } else {

                // ensure Secret parameter options are not used
                if(node.EncryptionContext != null) {
                    Logger.Log(EncryptionContextAttributeRequiresSecretType, node.EncryptionContext);
                }
            }

            // only CloudFormation resource types can have 'Properties' or 'Allow' attributes
            if(IsCloudFormationParameterType(node.Type.Value)) {

                // ensure CloudFormation resource type parameter options are not used
                if(node.Properties != null) {
                    Logger.Log(PropertiesAttributeRequiresCloudFormationType, node.Properties);
                }
                if(node.Allow != null) {
                    Logger.Log(AllowAttributeRequiresCloudFormationType, node.Allow);
                }
            } else if(Provider.TryGetResourceType(node.Type.Value, out var _)) {

                // only value parameters can have 'Import' attribute
                if(node.Import != null) {
                    Logger.Log(ParameterAttributeImportExpectedLiteral, node.Import);
                }
            } else {
                Logger.Log(UnknownType(node.Type.Value), node.Type);
            }
        }

        private void ValidateImportType(ImportDeclaration node) {

            // assume 'String' type when 'Type' attribute is omitted
            if(node.Type == null) {
                Logger.Log(AssumingStringType, node);
                node.Type = Fn.Literal("String", node.SourceLocation);
            }

            // only the 'Secret' type can have the 'EncryptionContext' attribute
            if(node.Type.Value == "Secret") {
                ValidateEncryptContext(node.EncryptionContext);

                // TODO: add resource for decrypting secret
            } else {

                // ensure Secret parameter options are not used
                if(node.EncryptionContext != null) {
                    Logger.Log(EncryptionContextAttributeRequiresSecretType, node.EncryptionContext);
                }

                // validate the import type
                if(
                    (node.Type.Value != "String")
                    && (node.Type.Value != "Number")
                    && !Provider.TryGetResourceType(node.Type.Value, out var _)
                ) {
                    Logger.Log(UnknownType(node.Type.Value), node.Type);
                }
            }
        }

        private void ValidateEncryptContext(ObjectExpression? encryptionContext) {

            // all 'EncryptionContext' values must be literal values
            if(encryptionContext != null) {

                // all expressions must be literals for the EncryptionContext
                foreach(var kv in encryptionContext) {
                    if(!(kv.Value is LiteralExpression)) {
                        Logger.Log(EncryptionContextExpectedLiteralStringExpression, kv.Value);
                    }
                }
            }
        }
    }
}