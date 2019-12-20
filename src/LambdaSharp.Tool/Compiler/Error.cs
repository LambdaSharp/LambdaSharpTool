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
using System.Linq;

namespace LambdaSharp.Tool.Compiler {
    using ErrorFunc = Func<string, Error>;

    public struct Warning : IBuildReportEntry {

        //--- Constants ---
        public static readonly Func<string, Warning> ReferenceIsUnreachable = parameter => new Warning(0, $"'{parameter}' is defined but never used");

        //--- Constructors ---
        public Warning(int code, string message) {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        //--- Properties ---
        public int Code { get; private set; }
        public string Message { get; private set; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Warning;
    }

    public struct Error : IBuildReportEntry {

        //--- Constants ---
        #region *** Internal Errors ***
        public static readonly ErrorFunc MissingParserDefinition = parameter => new Error(0, $"no parser defined for type '{parameter}'");
        #endregion

        #region *** Parsing Errors ***
        public static readonly Error ExpectedExpression = new Error(0, "expected a map, list, or literal value");
        public static readonly Error ExpectedListExpression = new Error(0, "expected a list");
        public static readonly Error ExpectedLiteralValue = new Error(0, "expected a literal value");
        public static readonly Error ExpectedMapExpression = new Error(0, "expected a map");
        public static readonly ErrorFunc UnrecognizedModuleItem = parameter => new Error(0, $"unrecognized item '{parameter}'");
        public static readonly ErrorFunc DuplicateKey = parameter => new Error(0, $"duplicate key '{parameter}'");
        public static readonly ErrorFunc UnexpectedKey = parameter => new Error(0, $"unexpected key '{parameter}'");
        public static readonly Func<IEnumerable<string>, Error> RequiredKeysMissing = parameter => new Error(0, $"missing required keys {string.Join(", ", parameter.OrderBy(key => key))}");
        public static readonly ErrorFunc UnknownFunctionTag = parameter => new Error(0, $"unknown function '{parameter}'");

        // TODO: unroll errors for each function (!Cidr, !Condition, !If, etc.); this makes it easier to track what errors are reported by each function
        public static readonly ErrorFunc FunctionExpectsOneParameter = parameter => new Error(0, $"{parameter} expects 1 parameter");
        public static readonly ErrorFunc FunctionExpectsTwoParameters = parameter => new Error(0, $"{parameter} expects 2 parameters");
        public static readonly ErrorFunc FunctionExpectsThreeParameters = parameter => new Error(0, $"{parameter} expects 3 parameters");
        public static readonly ErrorFunc SubFunctionParametersCannotUseAttributeNotation = parameter => new Error(0, $"cannot use attribute notation on local parameter '{parameter}'");

        // TODO: replace with error that shows what is expected
        public static readonly ErrorFunc FunctionInvalidParameter = parameter => new Error(0, $"invalid parameter for {parameter} function");

        public static readonly ErrorFunc FunctionExpectsLiteralFirstParameter = parameter => new Error(0, $"{parameter} first parameter must be a literal value");
        public static readonly ErrorFunc FunctionExpectsMapSecondParameter = parameter => new Error(0, $"{parameter} second parameter must be a map");
        public static readonly Error TransformFunctionMissingName = new Error(0, "!Transform function requires 'Name' key");
        public static readonly Error TransformFunctionExpectsLiteralNameParameter = new Error(0, "!Transform function requires 'Name' key to be a literal value");
        public static readonly Error TransformFunctionExpectsMapParametersParameter = new Error(0, "!Transform function requires 'Parameters' key to be a map");
        #endregion

        #region *** !GetAtt Validation ***
        public static readonly Error GetAttCannotBeUsedInAConditionDeclaration = new Error(0, "condition cannot use !GetAtt function");
        #endregion

        #region *** Identifier Validation ***
        public static readonly Error NameIsReservedAws = new Error(0, "'AWS' is a reserved name");
        public static readonly Error NameMustBeAlphanumeric = new Error(0, "name must be alphanumeric");
        #endregion

        #region *** Structural Validation ***
        public static readonly Error ParameterDeclarationCannotBeNested = new Error(0, "Parameter declaration cannot be nested in a Group");

        // TODO: these is an internal error; should it be an exception instead?
        public static readonly Func<object, Error> UnrecognizedExpressionType = parameter => new Error(0, $"unrecognized expression: {parameter?.GetType().Name ?? "<null>"}");
        #endregion

        #region *** Mapping Validation ***
        public static readonly Error MappingDeclarationTopLevelIsMissing = new Error(0, "Mapping declaration is missing top-level mappings");
        public static readonly Error MappingDeclarationSecondLevelIsMissing = new Error(0, "Mapping declaration is missing second-level mappings");
        public static readonly Error MappingKeyMustBeAlphanumeric = new Error(0, "key must be alphanumeric");
        public static readonly Error MappingDuplicateKey = new Error(0, "duplicate key");
        public static readonly Error MappingExpectedListOrLiteral = new Error(0, "expected list expression or literal value");
        #endregion

        #region *** Resource Type Validation ***
        public static readonly Error ResourceTypeInvalidFormat = new Error(0, "the expected format for the resource type name is: Prefix::Suffix");
        public static readonly ErrorFunc ResourceTypeReservedPrefix = parameter => new Error(0, $"'{parameter}' is a reserved resource type prefix");
        public static readonly Error ResourceTypeAttributesAttributeInvalid = new Error(0, "'Attributes' attribute cannot be empty");
        public static readonly Error ResourceTypePropertiesAttributeInvalid = new Error(0, "'Properties' attribute cannot be empty");
        public static readonly ErrorFunc ResourceTypePropertyDuplicateName = parameter => new Error(0, $"duplicate property name '{parameter}'");
        public static readonly ErrorFunc ResourceTypeAttributeDuplicateName = parameter => new Error(0, $"duplicate attribute name '{parameter}'");
        #endregion

        // TODO: assign to specific declaration type
        #region *** Attribute Validation ***
        public static readonly Error AllowAttributeRequiresCloudFormationType = new Error(0, "'Allow' attribute can only be used with a CloudFormation type");
        public static readonly Error AllowAttributeRequiresTypeAttribute = new Error(0, "'Allow' attribute requires 'Type' attribute to be set");
        public static readonly Error AllowedPatternAttributeInvalid = new Error(0, "'AllowedPattern' attribute must be a regular expression");
        public static readonly Error AllowedPatternAttributeRequiresStringType = new Error(0, "'AllowedPattern' attribute can only be used with 'String' type");
        public static readonly Error ConstraintDescriptionAttributeRequiresAllowedPatternAttribute = new Error(0, "'ConstraintDescription' attribute requires 'AllowedPattern' attribute to be set");
        public static readonly Error ConstraintDescriptionAttributeRequiresStringType = new Error(0, "'ConstraintDescription' attribute can only be used with 'String' type");
        public static readonly Error DescriptionAttributeExceedsSizeLimit = new Error(0, "'Description' attribute cannot exceed 4,000 characters");
        public static readonly Error EncryptionContextAttributeRequiresSecretType = new Error(0, "'EncryptionContext' attribute can only be used with 'Secret' type");
        public static readonly Error FilesAttributeInvalid = new Error(0, "'Files' attribute must refer to an existing file or folder");
        public static readonly Error HandlerAttributeMissing = new Error(0, "'Handler' attribute is required");
        public static readonly Error HandlerAttributeIsRequiredForScalaFunction = new Error(0, "'Handler' attribute is required for Scala functions");
        public static readonly Error IfAttributeRequiresCloudFormationType = new Error(0, "'If' attribute can only be used with a CloudFormation type");
        public static readonly Error LanguageAttributeInvalid = new Error(0, "'Language' attribute must be a support project language");
        public static readonly Error LanguageAttributeMissing = new Error(0, "'Language' attribute is required");
        public static readonly Error MaxLengthAttributeRequiresStringType = new Error(0, "'MaxLength' attribute can only be used with 'String' type");
        public static readonly Error MaxValueAttributeRequiresNumberType = new Error(0, "'MaxValue' attribute can only be used with 'Number' type");
        public static readonly Error MemoryAttributeInvalid = new Error(0, "'Memory' attribute must have an integer value");
        public static readonly Error MinLengthAttributeRequiresStringType = new Error(0, "'MinLength' attribute can only be used with 'String' type");
        public static readonly Error MinValueAttributeRequiresNumberType = new Error(0, "'MinValue' attribute can only be used with 'Number' type");
        public static readonly Error ModuleAttributeInvalid = new Error(0, "'Module' attribute must be a module reference");
        public static readonly Error ModuleNameAttributeInvalid = new Error(0, "'Module' attribute must have format 'Namespace.Name'");
        public static readonly Error ProjectAttributeInvalid = new Error(0, "'Project' attribute project must refer a supported project file or folder");
        public static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error(0, "'Properties' attribute can only be used with a CloudFormation type");
        public static readonly Error ResourceValueAttributeInvalid = new Error(0, "'Value' attribute must be a valid ARN or wildcard");
        public static readonly Error RuntimeAttributeMissing = new Error(0, "'Runtime' attribute is required");
        public static readonly Error TimeoutAttributeInvalid = new Error(0, "'Timeout' attribute must have an integer value");
        public static readonly Error TypeAttributeInvalid = new Error(0, "'Type' attribute must be CloudFormation parameter type");
        public static readonly Error TypeAttributeMissing = new Error(0, "'Type' attribute is required");
        public static readonly Error VersionAttributeInvalid = new Error(0, "the expected format for the 'Version' attribute is: Major.Minor[.Patch]");
        #endregion

        #region *** Reference Validation ***
        public static readonly ErrorFunc ReferenceMustBeResourceOrParameterOrVariable = parameter => new Error(0, $"{parameter} must be a resource, parameter, or variable");
        public static readonly ErrorFunc ReferenceMustBeParameter = parameter => new Error(0, $"{parameter} must be a parameter");
        public static readonly ErrorFunc ReferenceMustBeResourceInstance = parameter => new Error(0, $"{parameter} must be a CloudFormation stack resource");
        public static readonly ErrorFunc ReferenceWithCircularDependency = parameter => new Error(0, $"circular dependency on {parameter}");
        public static readonly ErrorFunc ReferenceMustBeFunction = parameter => new Error(0, $"{parameter} must be a function");
        public static readonly ErrorFunc ReferenceDoesNotExist = parameter => new Error(0, $"undefined reference to {parameter}");
        public static readonly ErrorFunc ReferenceCannotBeSelf = parameter => new Error(0, $"self-dependency on {parameter}");
        #endregion

        // TODO: keep reviewing errors
        public static readonly Error ValueMustBeAnInteger = new Error(0, "value must be an integer");
        public static readonly Error DuplicateName = new Error(0, "duplicate name");
        public static readonly Error CannotGrantPermissionToDecryptParameterStore = new Error(0, "cannot grant permission to decrypt with aws/ssm");
        public static readonly Error SecretKeyMustBeValidARN = new Error(0, "secret key must be a valid ARN");
        public static readonly Error SecreteKeyMustBeValidAlias = new Error(0, "secret key must be a valid alias");
        public static readonly Error SecretTypeMustBeLiteralOrExpression = new Error(0, "variable with type 'Secret' must be a literal value or expression");
        public static readonly Error ExpectedLiteralValueOrListOfLiteralValues = new Error(0, "expected literal value or list of literal values");
        public static readonly Error UnsupportedVersionOfDotNetCore = new Error(0, "this version of .NET Core is no longer supported for Lambda functions");
        public static readonly Error UnknownVersionOfDotNetCore = new Error(0, "could not determine runtime from target framework; specify 'Runtime' attribute explicitly");
        public static readonly Error FailedToAutoDetectHandlerInDotNetFunctionProject = new Error(0, "could not auto-determine handler; either add 'Handler' attribute or <RootNamespace> to project file");
        public static readonly ErrorFunc NameMustBeACloudFormationResource = parameter => new Error(0, $"identifier {parameter} must refer to a CloudFormation resource");
        public static readonly ErrorFunc IdentifierReferesToInvalidDeclarationType = parameter => new Error(0, $"identifier {parameter} cannot refer to this declaration type");
        public static readonly ErrorFunc IdentifierMustReferToAConditionDeclaration = parameter => new Error(0, $"identifier {parameter} must refer to a Condition");
        public static readonly ErrorFunc IdentifierMustReferToAMappingDeclaration = parameter => new Error(0, $"identifier {parameter} must refer to a Mapping");
        public static readonly Error HandlerMustBeAFunctionOrSnsTopic = new Error(0, "Handler must reference a Function or AWS::SNS::Topic resource declaration");
        public static readonly Error HandlerMustBeAFunction = new Error(0, "Handler must reference a Function declaration");
        public static readonly Error ExpectedConditionExpression = new Error(0, "expected a condition expression");
        public static readonly Error ExpectedLiteralStringExpression = new Error(0, "expected literal string expression");
        public static readonly Error ApiEventSourceInvalidApiFormat = new Error(0, "malformed REST API declaration");
        public static readonly Error ApiEventSourceUnsupportedIntegrationType = new Error(0, "unsupported integration type");
        public static readonly ErrorFunc ApiEventSourceInvalidGreedyParameterMustBeLast = parameter => new Error(0, $"the {parameter} parameter must be the last segment in the path");
        public static readonly Error S3EventSourceEventListCannotBeEmpty = new Error(0, "'Events' attribute cannot be an empty list");
        public static readonly ErrorFunc S3EventSourceUnrecognizedEventType = parameter => new Error(0, $"'{parameter}' is not a recognized S3 event type");
        public static readonly Error SlackCommandEventSourceInvalidRestPath = new Error(0, "REST API path for SlackCommand can not contain parameters");
        public static readonly Error SqsEventSourceInvalidBatchSize = new Error(0, "'BatchSize' must be an integer value between 1 and 10");
        public static readonly Error DynamoDBEventSourceInvalidBatchSize = new Error(0, "'BatchSize' must be an integer value between 1 and 1,000");
        public static readonly Error DynamoDBEventSourceInvalidStartingPosition = new Error(0, "'StartingPosition' must be either LATEST or TRIM_HORIZON");
        public static readonly Error DynamoDBEventSourceInvalidMaximumBatchingWindowInSeconds = new Error(0, "'MaximumBatchingWindowInSeconds' must be an integer value between 0 and 300");
        public static readonly Error KinesisEventSourceInvalidBatchSize = new Error(0, "'BatchSize' must be an integer value between 1 and 10,000");
        public static readonly Error KinesisEventSourceInvalidStartingPosition = new Error(0, "'StartingPosition' must be one of AT_TIMESTAMP, LATEST, or TRIM_HORIZON");
        public static readonly Error KinesisEventSourceInvalidMaximumBatchingWindowInSeconds = new Error(0, "'MaximumBatchingWindowInSeconds' must be an integer value between 0 and 300");
        public static readonly Error WebSocketEventSourceInvalidPredefinedRoute = new Error(0, "WebSocket route starting with $ must be one of $connect, $disconnect, or $default");
        public static readonly Error WebSocketEventSourceInvalidAuthorizationType = new Error(0, "'AuthorizationType' must be either AWS_IAM or CUSTOM");
        public static readonly Error WebSocketEventSourceInvalidAuthorizationTypeForCustomAuthorizer = new Error(0, "'AuthorizationType' must be CUSTOM");
        public static readonly Error WebSocketEventSourceInvalidAuthorizationConfigurationForRoute = new Error(0, "'AuthorizationType' can only be used on $connect WebSocket route");
        public static readonly Error FunctionPropertiesEnvironmentMustBeMap = new Error(0, "Properties.Environment must be a map");
        public static readonly Error FunctionPropertiesEnvironmentVariablesMustBeMap = new Error(0, "Properties.Environment.Variables must be a map");

        //--- Constructors ---
        public Error(int code, string message) {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        //--- Properties ---
        public int Code { get; private set; }
        public string Message { get; private set; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Error;
    }
}
