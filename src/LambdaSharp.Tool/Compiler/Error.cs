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

namespace LambdaSharp.Tool.Compiler {

    public struct Error {

        //--- Class Fields ---

        #region *** Identifier Validation ***
        public static readonly Error NameIsReservedAws = new Error(1000, "'AWS' is a reserved name");
        public static readonly Error NameMustBeAlphanumeric = new Error(0, "name must be alphanumeric");
        #endregion

        #region *** Structural Validation ***
        public static readonly Error ParameterDeclarationCannotBeNested = new Error(0, "Parameter declaration cannot be nested in a Group");
        public static readonly Error MappingDeclarationTopLevelIsMissing = new Error(0, "Mapping declaration is missing top-level mappings");
        public static readonly Error MappingDeclarationSecondLevelIsMissing = new Error(0, "Mapping declaration is missing second-level mappings");
        #endregion

        #region *** Attribute Validation ***
        public static readonly Error DescriptionAttributeExceedsSizeLimit = new Error(0, "'Description' attribute cannot exceed 4,000 characters");
        public static readonly Error EncryptionContextAttributeRequiresSecretType = new Error(0, "'EncryptionContext' attribute can only be used with 'Secret' type");
        public static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error(0, "'Properties' attribute can only be used with a CloudFormation type");
        public static readonly Error AllowAttributeRequiresCloudFormationType = new Error(0, "'Allow' attribute can only be used with a CloudFormation type");
        public static readonly Error MinValueAttributeRequiresNumberType = new Error(0, "'MinValue' attribute can only be used with 'Number' type");
        public static readonly Error MaxValueAttributeRequiresNumberType = new Error(0, "'MaxValue' attribute can only be used with 'Number' type");
        public static readonly Error AllowedPatternAttributeRequiresStringType = new Error(0, "'AllowedPattern' attribute can only be used with 'String' type");
        public static readonly Error AllowedPatternAttributeIsInvalid = new Error(0, "'AllowedPattern' attribute must be a regular expression");
        public static readonly Error ConstraintDescriptionAttributeRequiresAllowedPatternAttribute = new Error(0, "'ConstraintDescription' attribute can only be used in conjunction with the 'AllowedPattern' attribute");
        public static readonly Error ConstraintDescriptionAttributeRequiresStringType = new Error(0, "'ConstraintDescription' attribute can only be used with 'String' type");
        public static readonly Error MinLengthAttributeRequiresStringType = new Error(0, "'MinLength' attribute can only be used with 'String' type");
        public static readonly Error MaxLengthAttributeRequiresStringType = new Error(0, "'MaxLength' attribute can only be used with 'String' type");
        public static readonly Error TypeAttributeMissing = new Error(0, "'Type' attribute is required");
        public static readonly Error IfAttributeRequiresCloudFormationType = new Error(0, "'If' attribute can only be used with a CloudFormation type");
        public static readonly Error ModuleAttributeIsInvalid = new Error(0, "'Module' attribute must be a module reference");
        public static readonly Error FilesAttributeIsInvalid = new Error(0, "'Files' attribute must refer to an existing file or folder");
        public static readonly Error ResourceValueAttributeIsInvalid = new Error(0, "'Value' attribute must be a valid ARN or wildcard");
        public static readonly Error TypeAttributeIsInvalid = new Error(0, "'Type' attribute must be CloudFormation parameter type");
        public static readonly Error ResourceTypePropertiesAttributeIsInvalid = new Error(0, "'Properties' attribute cannot be empty");
        public static readonly Error ResourceTypeAttributesAttributeIsInvalid = new Error(0, "'Attributes' attribute cannot be empty");
        public static readonly Error VersionAttributeIsInvalid = new Error(0, "'Version' attribute expected to have format: Major.Minor[.Patch]");
        public static readonly Error ModuleNameAttributeIsInvalid = new Error(0, "'Module' attribute must have format 'Namespace.Name'");
        #endregion

        // TODO: keep reviewing errors
        public static readonly Error ValueMustBeAnInteger = new Error(0, "value must be an integer");
        public static readonly Error ExpectedObjectExpression = new Error(0, "expected an object expressions");
        public static readonly Error ExpectedLiteralValue = new Error(0, "expected literal value");
        public static readonly Error DuplicateName = new Error(0, "duplicate name");
        public static readonly Error CannotGrantPermissionToDecryptParameterStore = new Error(0, "cannot grant permission to decrypt with aws/ssm");
        public static readonly Error SecretKeyMustBeValidARN = new Error(0, "secret key must be a valid ARN");
        public static readonly Error SecreteKeyMustBeValidAlias = new Error(0, "secret key must be a valid alias");
        public static readonly Error SecretTypeMustBeLiteralOrExpression = new Error(0, "variable with type 'Secret' must be a literal value or expression");
        public static readonly Error AllowAttributeRequiresTypeAttribute = new Error(0, "'Allow' attribute requires 'Type' attribute");
        public static readonly Error AllowAttributeRequiresAwsType = new Error(0, "'Allow' attribute can only be used with AWS resource types");
        public static readonly Error ExpectedLiteralValueOrListOfLiteralValues = new Error(0, "expected literal value or list of literal values");
        public static readonly Error MissingRuntimeAttribute = new Error(0, "missing 'Runtime' attribute");
        public static readonly Error MissingHandlerAttribute = new Error(0, "missing 'Handler' attribute");
        public static readonly Error MissingLanguageAttribute = new Error(0, "missing 'Language' attribute");
        public static readonly Error InvalidMemoryAttributeValue = new Error(0, "invalid 'Memory' value");
        public static readonly Error InvalidTimeoutAttributeValue = new Error(0, "invalid 'Timeout' value");
        public static readonly Error UnsupportedFunctionProjectAttribute = new Error(0, "function project file could not be found or is not supported");
        public static readonly Error InvalidLanguageAttributeValue = new Error(0, "invalid 'Language' value");
        public static readonly Error UnsupportedVersionOfDotNetCore = new Error(0, "this version of .NET Core is no longer supported for Lambda functions");
        public static readonly Error UnknownVersionOfDotNetCore = new Error(0, "could not determine runtime from target framework; specify 'Runtime' attribute explicitly");
        public static readonly Error FailedToAutoDetectHandlerInDotNetFunctionProject = new Error(0, "could not auto-determine handler; either add 'Handler' attribute or <RootNamespace> to project file");
        public static readonly Error HandlerAttributeIsRequiredForScalaFunction = new Error(0, "Handler attribute is required for Scala functions");

        public static readonly Func<string, Error> NameIsNotAResource = parameter => new Error(0, $"{parameter} is not a resource");
        public static readonly Func<string, Error> NameMustBeACloudFormationResource = parameter => new Error(0, $"identifier {parameter} must refer to a CloudFormation resource");
        public static readonly Func<string, Error> UnknownIdentifier = parameter => new Error(0, $"unknown identifier {parameter}");
        public static readonly Func<string, Error> IdentifierReferesToInvalidDeclarationType = parameter => new Error(0, $"identifier {parameter} cannot refer to this declaration type");
        public static readonly Func<string, Error> IdentifierMustReferToACondition = parameter => new Error(0, $"identifier {parameter} must refer to a Condition");
        public static readonly Error HandlerMustBeAFunctionOrSnsTopic = new Error(0, "Handler must reference a Function or AWS::SNS::Topic resource declaration");
        public static readonly Error HandlerMustBeAFunction = new Error(0, "Handler must reference a Function declaration");
        public static readonly Error ExpectedConditionExpression = new Error(2000, "expected a condition expression");

        //--- Fields ---
        public readonly int Code;
        public readonly string Message;

        //--- Constructors ---
        public Error(int code, string message) {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
