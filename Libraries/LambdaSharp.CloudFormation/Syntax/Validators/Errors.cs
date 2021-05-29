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
        public static IReportEntry TemplateDescriptionTooLong(SourceLocation location) => ReportEntry.Error("template description cannot exceed 1,024 bytes", location);
        public static IReportEntry TemplateVersionIsNotValid(SourceLocation location) => ReportEntry.Error("template version is not valid", location);
        public static IReportEntry TemplateResourcesSectionMissing(SourceLocation location) => ReportEntry.Error("template is missing the resources section", location);
        public static IReportEntry TemplateResourcesTooShort(SourceLocation location) => ReportEntry.Error("template resources section must contain at least one resource", location);
        public static IReportEntry DeclarationMissingName(SourceLocation location) => ReportEntry.Error("name missing for declaration", location);
        public static IReportEntry DeclarationNameMustBeString(SourceLocation location) => ReportEntry.Error("declaration name must be a string value", location);
    }
}