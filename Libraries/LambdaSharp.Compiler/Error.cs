/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using LambdaSharp.Modules.Metadata;

namespace LambdaSharp.Compiler {
    using ErrorFunc = Func<string, Error>;
    using ErrorFunc2 = Func<string, string, Error>;
    using ErrorFunc3 = Func<string, string, string, Error>;
    using ErrorFunc5 = Func<string, string, string, string, string, Error>;
    using WarningFunc = Func<string, Warning>;
    using WarningFunc2 = Func<string, string, Warning>;

    public readonly struct Debug : IBuildReportEntry, IEquatable<Debug> {

        //--- Operators ---
        public static bool operator == (Debug left, Debug right) => left.Equals(right);
        public static bool operator != (Debug left, Debug right) => !left.Equals(right);

        //--- Constructors ---
        public Debug(string message) => Message = message ?? throw new ArgumentNullException(nameof(message));

        //--- Properties ---
        public int Code => 0;
        public string Message { get; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Debug;

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is Debug other) && Equals(other);

        public override int GetHashCode()
            => Message.GetHashCode() ^ Severity.GetHashCode();

        public bool Equals(Debug other)
            => Message == other.Message;
    }

    public readonly struct Timing : IBuildReportEntry, IEquatable<Timing> {

        //--- Operators ---
        public static bool operator == (Timing left, Timing right) => left.Equals(right);
        public static bool operator != (Timing left, Timing right) => !left.Equals(right);

        //--- Constructors ---
        public Timing(string description, TimeSpan duration, bool? cached)
            => (Description, Duration, Cached) = (description ?? throw new ArgumentNullException(nameof(description)), duration, cached);

        //--- Properties ---
        public int Code => 0;
        public string Message => $"{Description} [duration={Duration.TotalSeconds:N2}s{(Cached.HasValue ? $", cached={Cached.Value.ToString().ToLowerInvariant()}" : "")}]";
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Timing;
        public string Description { get; }
        public TimeSpan Duration { get; }
        public bool? Cached { get; }

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is Timing other) && Equals(other);

        public override int GetHashCode()
            => Message.GetHashCode() ^ Severity.GetHashCode() ^ Description.GetHashCode() ^ Duration.GetHashCode() ^ Cached.GetHashCode();

        public bool Equals(Timing other)
            => (Message == other.Message)
                && (Description == other.Description)
                && (Duration == other.Duration)
                && (Cached == other.Cached);
    }

    public readonly struct Verbose : IBuildReportEntry, IEquatable<Verbose> {

        //--- Operators ---
        public static bool operator == (Verbose left, Verbose right) => left.Equals(right);
        public static bool operator != (Verbose left, Verbose right) => !left.Equals(right);

        //--- Constructors ---
        public Verbose(string message) => Message = message ?? throw new ArgumentNullException(nameof(message));

        //--- Properties ---
        public int Code => 0;
        public string Message { get; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Verbose;

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is Verbose other) && Equals(other);

        public override int GetHashCode()
            => Message.GetHashCode() ^ Severity.GetHashCode();

        public bool Equals(Verbose other)
            => Message == other.Message;
    }

    public readonly struct Info : IBuildReportEntry, IEquatable<Info> {

        //--- Operators ---
        public static bool operator == (Info left, Info right) => left.Equals(right);
        public static bool operator != (Info left, Info right) => !left.Equals(right);

        //--- Constructors ---
        public Info(string message) => Message = message ?? throw new ArgumentNullException(nameof(message));

        //--- Properties ---
        public int Code => 0;
        public string Message { get; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Info;

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is Info other) && Equals(other);

        public override int GetHashCode()
            => Code.GetHashCode() ^ Message.GetHashCode() ^ Severity.GetHashCode();

        public bool Equals(Info other)
            => Message == other.Message;
    }

    public readonly struct Warning : IBuildReportEntry, IEquatable<Warning> {

        //--- Constants ---
        #region *** Reference Validation ***
        public static readonly WarningFunc ReferenceIsUnreachable = parameter => new Warning(0, $"'{parameter}' is defined but never used");
        #endregion

        #region *** Manifest Loader ***
        public static readonly WarningFunc ManifestLoaderCouldNotFindBucket = parameter => new Warning(0, $"could not find '{parameter}' bucket");
        public static readonly WarningFunc ManifestLoaderCouldNotDetectBucketRegion = parameter => new Warning(0, $"could not detect region for '{parameter}' bucket");
        public static readonly WarningFunc ManifestLoaderCouldNotRetrieveModuleVersion = parameter => new Warning(0, $"unable to retrieve module version from CloudFormation stack '{parameter}'");
        #endregion

        #region *** Resource Type Validation ***
        public static readonly WarningFunc2 ResourceTypeAmbiguousTypeReference = (p1, p2) => new Warning(0, $"ambiguous resource type '{p1}' [{p2}]");
        #endregion

        // TODO: keep reviewing warnings
        public static readonly Warning UnableToValidateDependency = new Warning(0, "unable to validate dependency");

        //--- Operators ---
        public static bool operator == (Warning left, Warning right) => left.Equals(right);
        public static bool operator != (Warning left, Warning right) => !left.Equals(right);

        //--- Constructors ---
        public Warning(int code, string message) {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        //--- Properties ---
        public int Code { get; }
        public string Message { get; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Warning;

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is Warning other) && Equals(other);

        public override int GetHashCode()
            => Code.GetHashCode() ^ Message.GetHashCode() ^ Severity.GetHashCode();

        public bool Equals(Warning other)
            => (Code == other.Code) && (Message == other.Message);
    }

    public readonly struct Error : IBuildReportEntry, IEquatable<Error> {

        // TODO: consider having a string as error ID (e.g. "CS1001")

        //--- Constants ---
        #region *** Internal Errors ***
        public static readonly ErrorFunc MissingParserDefinition = parameter => new Error($"no parser defined for type '{parameter}'");
        #endregion

        #region *** Parsing Errors ***
        public static readonly Error ExpectedExpression = new Error("expected a map, list, or literal value");
        public static readonly Error ExpectedListExpression = new Error("expected a list");
        public static readonly Error ExpectedLiteralValue = new Error("expected a literal value");
        public static readonly Error ExpectedMapExpression = new Error("expected a map");
        public static readonly ErrorFunc UnrecognizedModuleItem = parameter => new Error($"unrecognized item '{parameter}'");
        public static readonly ErrorFunc DuplicateKey = parameter => new Error($"duplicate key '{parameter}'");
        public static readonly ErrorFunc UnexpectedKey = parameter => new Error($"unexpected key '{parameter}'");
        public static readonly Func<IEnumerable<string>, Error> RequiredKeysMissing = parameter => new Error($"missing required keys {string.Join(", ", parameter.OrderBy(key => key))}");
        public static readonly ErrorFunc UnknownFunctionTag = parameter => new Error($"unknown function '{parameter}'");

        // TODO: unroll errors for each function (!Cidr, !Condition, !If, etc.); this makes it easier to track what errors are reported by each function
        public static readonly ErrorFunc FunctionExpectsOneParameter = parameter => new Error($"{parameter} expects 1 parameter");
        public static readonly ErrorFunc FunctionExpectsTwoParameters = parameter => new Error($"{parameter} expects 2 parameters");
        public static readonly ErrorFunc FunctionExpectsTwoTo10Parameters = parameter => new Error($"{parameter} expects 2 to 10 parameters");
        public static readonly ErrorFunc FunctionExpectsThreeParameters = parameter => new Error($"{parameter} expects 3 parameters");
        public static readonly ErrorFunc SubFunctionParametersCannotUseAttributeNotation = parameter => new Error($"cannot use attribute notation on local parameter '{parameter}'");

        // TODO: replace with error that shows what is expected
        public static readonly ErrorFunc FunctionInvalidParameter = parameter => new Error($"invalid parameter for {parameter} function");

        public static readonly ErrorFunc FunctionExpectsLiteralFirstParameter = parameter => new Error($"{parameter} first parameter must be a literal value");
        public static readonly ErrorFunc FunctionExpectsMapSecondParameter = parameter => new Error($"{parameter} second parameter must be a map");
        public static readonly Error TransformFunctionMissingName = new Error("!Transform function requires 'Name' key");
        public static readonly Error TransformFunctionExpectsLiteralNameParameter = new Error("!Transform function requires 'Name' key to be a literal value");
        public static readonly Error TransformFunctionExpectsMapParametersParameter = new Error("!Transform function requires 'Parameters' key to be a map");
        #endregion

        #region *** Identifier Validation ***
        #endregion

        #region *** Structural Validation ***

        // TODO: these is an internal error; should it be an exception instead?
        public static readonly Func<object, Error> UnrecognizedExpressionType = parameter => new Error($"unrecognized expression: {parameter?.GetType().Name ?? "<null>"}");
        #endregion

        #region *** Mapping Declaration Validation ***
        public static readonly Error MappingDeclarationTopLevelIsMissing = new Error("Mapping declaration is missing top-level mappings");
        public static readonly Error MappingDeclarationSecondLevelIsMissing = new Error("Mapping declaration is missing second-level mappings");
        public static readonly Error MappingKeyMustBeAlphanumeric = new Error("key must be alphanumeric");
        public static readonly Error MappingDuplicateKey = new Error("duplicate key");
        public static readonly Error MappingExpectedListOrLiteral = new Error("expected list expression or literal value");
        #endregion

        #region *** Parameter Declaration Validation ***
        public static readonly Error ParameterImportAttributeIsInvalid = new Error("invalid 'Import' attribute");
        public static readonly Error ParameterImportAttributeCannotHaveVersion = new Error("'Import' attribute cannot have a version");
        public static readonly Error ParameterImportAttributeCannotHaveOrigin = new Error("'Import' attribute cannot have an origin");
        public static readonly Error ParameterDefaultAttributeCannotUseWithImportAttribute = new Error("cannot use 'Default' attribute with 'Import'");
        #endregion

        #region *** Import Declaration Validation ***
        public static readonly Error ImportModuleAttributeIsInvalid = new Error("invalid 'Module' attribute");
        public static readonly Error ImportModuleAttributeCannotHaveVersion = new Error("'Module' attribute cannot have a version");
        public static readonly Error ImportModuleAttributeCannotHaveOrigin = new Error("'Module' attribute cannot have an origin");
        public static readonly ErrorFunc ImportDuplicateWithDifferentBinding = parameter => new Error($"import declaration '{parameter}' is already defined with a different binding");
        public static readonly ErrorFunc ImportDuplicateWithDifferentType = parameter => new Error($"import declaration '{parameter}' is already defined with a different type");
        #endregion

        #region *** Resource Type Declaration Validation ***
        public static readonly Error ResourceTypeNameInvalidFormat = new Error("the expected format for the resource type name is: Prefix::Suffix");
        public static readonly ErrorFunc ResourceTypeNameReservedPrefix = parameter => new Error($"'{parameter}' is a reserved resource type prefix");
        public static readonly Error ResourceTypePropertyNameMustBeAlphanumeric = new Error("name must be alphanumeric");
        public static readonly ErrorFunc ResourceTypePropertyDuplicateName = parameter => new Error($"duplicate property name '{parameter}'");
        public static readonly Error ResourceTypePropertyTypeIsInvalid = new Error("'Type' must be CloudFormation parameter type");
        public static readonly Error ResourceTypePropertyRequiredMustBeBool = new Error("'Required' must have a boolean value");
        public static readonly Error ResourceTypeAttributeNameMustBeAlphanumeric = new Error("name must be alphanumeric");
        public static readonly ErrorFunc ResourceTypeAttributeDuplicateName = parameter => new Error($"duplicate attribute name '{parameter}'");
        public static readonly Error ResourceTypeAttributeTypeIsInvalid = new Error("'Type' must be CloudFormation parameter type");
        public static readonly Error ResourceTypeAttributesAttributeIsInvalid = new Error("'Attributes' attribute cannot be empty");
        public static readonly Error ResourceTypePropertiesAttributeIsInvalid = new Error("'Properties' attribute cannot be empty");
        #endregion

        #region *** Resource Declaration Validation ***
        public static readonly ErrorFunc2 ResourceUnknownProperty = (p1, p2) => new Error($"unrecognized property '{p1}' on resource type {p2}");

        #endregion

        // TODO: assign to specific declaration type
        #region *** Attribute Validation ***
        public static readonly Error AllowAttributeRequiresCloudFormationType = new Error("'Allow' attribute can only be used with a CloudFormation type");
        public static readonly Error AllowAttributeRequiresTypeAttribute = new Error("'Allow' attribute requires 'Type' attribute to be set");
        public static readonly Error EncryptionContextAttributeRequiresSecretType = new Error("'EncryptionContext' attribute can only be used with 'Secret' type");
        public static readonly Error HandlerAttributeMissing = new Error("'Handler' attribute is required");
        public static readonly Error HandlerAttributeIsRequiredForScalaFunction = new Error("'Handler' attribute is required for Scala functions");
        public static readonly Error IfAttributeRequiresCloudFormationType = new Error("'If' attribute can only be used with a CloudFormation type");
        public static readonly Error LanguageAttributeInvalid = new Error("'Language' attribute must be a support project language");
        public static readonly Error LanguageAttributeMissing = new Error("'Language' attribute is required");
        public static readonly Error MemoryAttributeInvalid = new Error("'Memory' attribute must have an integer value");
        public static readonly Error ModuleNameAttributeInvalid = new Error("'Module' attribute must have format 'Namespace.Name'");
        public static readonly Error ProjectAttributeInvalid = new Error("'Project' attribute project must refer a supported project file or folder");
        public static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error("'Properties' attribute can only be used with a CloudFormation type");
        public static readonly Error ResourceValueAttributeInvalid = new Error("'Value' attribute must be a valid ARN or wildcard");
        public static readonly Error RuntimeAttributeMissing = new Error("'Runtime' attribute is required");
        public static readonly Error TimeoutAttributeInvalid = new Error("'Timeout' attribute must have an integer value");
        public static readonly Error VersionAttributeInvalid = new Error("the expected format for the 'Version' attribute is: Major.Minor[.Patch]");
        #endregion

        #region *** Reference Validation ***
        public static readonly ErrorFunc ReferenceMustBeFunction = parameter => new Error($"{parameter} must be a function");
        public static readonly ErrorFunc ReferenceDoesNotExist = parameter => new Error($"undefined reference to {parameter}");
        public static readonly ErrorFunc ReferenceCannotBeSelf = parameter => new Error($"self-dependency on {parameter}");
        #endregion

        #region *** Manifest Loader ***
        public static readonly ErrorFunc ManifestLoaderInvalidTemplate = parameter => new Error($"invalid CloudFormation template: {parameter}");
        public static readonly ErrorFunc ManifestLoaderIncompatibleManifestVersion = parameter => new Error($"Incompatible LambdaSharp manifest version (found: {parameter}, expected: {ModuleManifest.CurrentVersion})");
        public static readonly Error ManifestLoaderMissingNameMappings = new Error("CloudFormation file does not contain LambdaSharp name mappings");
        public static readonly ErrorFunc ManifestLoaderCouldNotLoadTemplate = parameter => new Error($"could not load CloudFormation template for {parameter}");
        public static readonly ErrorFunc ManifestLoaderCouldNotFindModule = parameter => new Error($"could not find module '{parameter}'");
        #endregion

        #region *** CloudFormation Specification ***
        public static readonly Error CloudFormationSpecInvalidRegion = new Error("region is not valid");
        public static readonly Error CloudFormationSpecInvalidVersion = new Error("version is not valid");
        public static readonly Error CloudFormationSpecNotFound = new Error("unable to find a matching CloudFormation resource specification");
        #endregion

        #region *** WebSocket Event Source ***
        public static readonly Error WebSocketEventSourceInvalidPredefinedRoute = new Error("WebSocket route starting with $ must be one of $connect, $disconnect, or $default");
        public static readonly Error WebSocketEventSourceInvalidAuthorizationType = new Error("'AuthorizationType' must be either AWS_IAM or CUSTOM");
        public static readonly Error WebSocketEventSourceInvalidAuthorizationTypeForCustomAuthorizer = new Error("'AuthorizationType' must be CUSTOM");
        public static readonly Error WebSocketEventSourceInvalidAuthorizationConfigurationForRoute = new Error("'AuthorizationType' can only be used on $connect WebSocket route");
        public static readonly Error WebSocketApiKeyRequiredExpectedBoolean = new Error("'ApiKeyRequired' must be a boolean value");
        #endregion

        #region *** REST API Event Source ***
        public static readonly Error RestApiEventSourceInvalidApiFormat = new Error("malformed REST API declaration");
        public static readonly Error RestApiEventSourceUnsupportedIntegrationType = new Error("unsupported REST API integration type");
        public static readonly ErrorFunc RestApiEventSourceGreedyParameterMustBeLast = parameter => new Error($"the '{parameter}' parameter must be the last segment in the path");
        #endregion

        #region *** S3 Event Source ***
        public static readonly Error S3EventSourceEventListCannotBeEmpty = new Error("'Events' attribute cannot be an empty list");
        public static readonly ErrorFunc S3EventSourceUnrecognizedEventType = parameter => new Error($"'{parameter}' is not a recognized S3 event type");
        #endregion

        #region *** Slack Event Source ***
        public static readonly Error SlackCommandEventSourceInvalidRestPath = new Error("REST API path for SlackCommand can not contain parameters");
        #endregion

        #region *** SQS Event Source ***
        public static readonly Error SqsEventSourceInvalidBatchSize = new Error("'BatchSize' must be an integer value between 1 and 10");
        #endregion

        #region *** DynamoDB Stream Event Source ***
        public static readonly Error DynamoDBStreamEventSourceInvalidBatchSize = new Error("'BatchSize' must be an integer value between 1 and 1,000");
        public static readonly Error DynamoDBStreamEventSourceInvalidStartingPosition = new Error("'StartingPosition' must be either LATEST or TRIM_HORIZON");
        public static readonly Error DynamoDBStreamEventSourceInvalidMaximumBatchingWindowInSeconds = new Error("'MaximumBatchingWindowInSeconds' must be an integer value between 0 and 300");
        #endregion

        #region *** Kinesis Stream Event Source ***
        public static readonly Error KinesisStreamEventSourceInvalidBatchSize = new Error("'BatchSize' must be an integer value between 1 and 10,000");
        public static readonly Error KinesisStreamEventSourceInvalidStartingPosition = new Error("'StartingPosition' must be one of AT_TIMESTAMP, LATEST, or TRIM_HORIZON");
        public static readonly Error KinesisStreamEventSourceInvalidMaximumBatchingWindowInSeconds = new Error("'MaximumBatchingWindowInSeconds' must be an integer value between 0 and 300");
        #endregion


        // TODO: keep reviewing errors
        public static readonly ErrorFunc CircularDependencyDetected = parameter => new Error($"circular dependency {parameter}");
        public static readonly Error SecretTypeMustBeLiteralOrExpression = new Error("variable with type 'Secret' must be a literal value or expression");
        public static readonly Error UnsupportedVersionOfDotNetCore = new Error("this version of .NET Core is no longer supported for Lambda functions");
        public static readonly Error UnknownVersionOfDotNetCore = new Error("could not determine runtime from target framework; specify 'Runtime' attribute explicitly");
        public static readonly Error FailedToAutoDetectHandlerInDotNetFunctionProject = new Error("could not auto-determine handler; either add 'Handler' attribute or <RootNamespace> to project file");
        public static readonly Error HandlerMustBeAFunctionOrSnsTopic = new Error("Handler must reference a Function or AWS::SNS::Topic resource declaration");
        public static readonly Error HandlerMustBeAFunction = new Error("Handler must reference a Function declaration");
        public static readonly Error ExpectedConditionExpression = new Error("expected a condition expression");
        public static readonly Error ExpectedLiteralStringExpression = new Error("expected literal string expression");
        public static readonly Error FunctionPropertiesEnvironmentMustBeMap = new Error("Properties.Environment must be a map");
        public static readonly Error FunctionPropertiesEnvironmentVariablesMustBeMap = new Error("Properties.Environment.Variables must be a map");
        public static readonly ErrorFunc UnsupportedDependencyType = parameter => new Error($"unsupported depency type '{parameter}'");

        //--- Operators ---
        public static bool operator == (Error left, Error right) => left.Equals(right);
        public static bool operator != (Error left, Error right) => !left.Equals(right);

        //--- Constructors ---
        public Error(string message) : this(0, message) { }

        public Error(int code, string message) {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        //--- Properties ---
        public int Code { get; }
        public string Message { get; }
        public BuildReportEntrySeverity Severity => BuildReportEntrySeverity.Error;

        //--- Methods ---
        public override bool Equals(object? obj)
            => (obj is Error other) && Equals(other);

        public override int GetHashCode()
            => Code.GetHashCode() ^ Message.GetHashCode() ^ Severity.GetHashCode();

        public bool Equals(Error other)
            => (Code == other.Code) && (Message == other.Message);
    }
}
