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

using LambdaSharp.CloudFormation.Reporting;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal static class Errors {

        //--- Class Methods ---
        public static IReportEntry NameMustBeAlphanumeric(SourceLocation location) => ReportEntry.Error("name must be alphanumeric", location);
        public static IReportEntry CannotUseReservedName(string name, SourceLocation location) => ReportEntry.Error($"cannot use reserved name '{name}'", location);
        public static IReportEntry ExpectedStringValue(SourceLocation location) => ReportEntry.Error("must be a string value", location);
        public static IReportEntry TemplateDescriptionTooLong(int maxDescriptionLength, SourceLocation location) => ReportEntry.Error($"template description cannot exceed {maxDescriptionLength:N0} bytes", location);
        public static IReportEntry TemplateVersionIsNotValid(SourceLocation location) => ReportEntry.Error("template version is not valid", location);
        public static IReportEntry TemplateResourcesSectionMissing(SourceLocation location) => ReportEntry.Error("template is missing the resources section", location);
        public static IReportEntry TemplateResourcesTooShort(SourceLocation location) => ReportEntry.Error("template resources section must contain at least one resource", location);
        public static IReportEntry DeclarationMissingName(SourceLocation location) => ReportEntry.Error("name missing for declaration", location);
        public static IReportEntry DeclarationNameMustBeString(SourceLocation location) => ReportEntry.Error("declaration name must be a string", location);

        #region Resource Declaration
        public static IReportEntry ResourceMissingType(SourceLocation location) => ReportEntry.Error("type missing for resource", location);
        public static IReportEntry ResourceTypeExpectedString(SourceLocation location) => ReportEntry.Error("resource type must be a string", location);
        #endregion

        #region Condition Declaration
        public static IReportEntry ConditionMissingValue(SourceLocation location) => ReportEntry.Error("value missing for condition", location);
        #endregion

        #region Output Declaration
        public static IReportEntry OutputMissingValue(SourceLocation location) => ReportEntry.Error("value missing for output", location);
        public static IReportEntry OutputExportMissingName(SourceLocation location) => ReportEntry.Error("export name missing for output", location);
        #endregion

        #region Mapping Declaration
        public static IReportEntry MappingMissingValue(SourceLocation location) => ReportEntry.Error("value missing for mapping", location);
        public static IReportEntry MappingLevel1KeyMissing(SourceLocation location) => ReportEntry.Error("level 1 key missing for mapping", location);
        public static IReportEntry MappingLevel1ValueMissing(SourceLocation location) => ReportEntry.Error("level 1 value missing for mapping", location);
        public static IReportEntry MappingLevel1ValueExpectedMap(SourceLocation location) => ReportEntry.Error("level 1 value must be a map", location);
        public static IReportEntry MappingLevel2KeyMissing(SourceLocation location) => ReportEntry.Error("level 2 key missing for mapping", location);
        public static IReportEntry MappingLevel2ValueExpectedLiteral(SourceLocation location) => ReportEntry.Error("level 2 value must be a literal", location);
        #endregion

        #region Parameter Declaration
        public static IReportEntry ParameterMissingType(SourceLocation location) => ReportEntry.Error("'Type' missing for parameter", location);
        public static IReportEntry ParameterTypeExpectedString(SourceLocation location) => ReportEntry.Error("'Type' must be a string", location);

        // Property: MinLength, MaxLength
        public static IReportEntry MinLengthAttributeRequiresStringType(SourceLocation location) => ReportEntry.Error("'MinLength' attribute can only be used with 'String' type", location);
        public static IReportEntry MinLengthMustBeAnInteger(SourceLocation location) => ReportEntry.Error("'MinLength' must be an integer", location);
        public static IReportEntry MinLengthMustBeNonNegative(SourceLocation location) => ReportEntry.Error($"'MinLength' must be greater or equal than 0", location);
        public static IReportEntry MinLengthTooLarge(int maxParameterValueLength, SourceLocation location) => ReportEntry.Error($"'MinLength' cannot exceed {maxParameterValueLength:N0}", location);
        public static IReportEntry MinMaxLengthInvalidRange(SourceLocation location) => ReportEntry.Error("'MinLength' must be less or equal to 'MaxLength'", location);
        public static IReportEntry MaxLengthAttributeRequiresStringType(SourceLocation location) => ReportEntry.Error("'MaxLength' attribute can only be used with 'String' type", location);
        public static IReportEntry MaxLengthMustBeAnInteger(SourceLocation location) => ReportEntry.Error("'MaxLength' must be an integer", location);
        public static IReportEntry MaxLengthMustBePositive(SourceLocation location) => ReportEntry.Error("'MaxLength' must be greater than 0", location);
        public static IReportEntry MaxLengthTooLarge(int maxParameterValueLength, SourceLocation location) => ReportEntry.Error($"'MaxLength' cannot exceed {maxParameterValueLength:N0}", location);

        // Property: MinValue, MaxValue
        public static IReportEntry MinValueAttributeRequiresNumberType(SourceLocation location) => ReportEntry.Error("'MinValue' attribute can only be used with 'Number' type", location);
        public static IReportEntry MinValueMustBeAnInteger(SourceLocation location) => ReportEntry.Error("'MinValue' must be an integer", location);
        public static IReportEntry MaxValueAttributeRequiresNumberType(SourceLocation location) => ReportEntry.Error("'MaxValue' attribute can only be used with 'Number' type", location);
        public static IReportEntry MinMaxValueInvalidRange(SourceLocation location) => ReportEntry.Error("'MinValue' must be less or equal to 'MaxValue'", location);
        public static IReportEntry MaxValueMustBeAnInteger(SourceLocation location) => ReportEntry.Error("'MaxValue' must be an integer", location);

        // Property: AllowedPattern, ConstraintDescription
        public static IReportEntry AllowedPatternAttributeRequiresStringType(SourceLocation location) => ReportEntry.Error("'AllowedPattern' attribute can only be used with 'String' type", location);
        public static IReportEntry AllowedPatternAttributeInvalid(SourceLocation location) => ReportEntry.Error("'AllowedPattern' attribute must be a valid regular expression", location);
        public static IReportEntry ConstraintDescriptionAttributeRequiresStringType(SourceLocation location) => ReportEntry.Error("'ConstraintDescription' attribute can only be used with 'String' type", location);
        public static IReportEntry ConstraintDescriptionAttributeRequiresAllowedPatternAttribute(SourceLocation location) => ReportEntry.Error("'ConstraintDescription' attribute requires 'AllowedPattern' attribute to be set", location);
        #endregion
    }
}