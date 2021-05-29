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
using LambdaSharp.CloudFormation.Builder;
using LambdaSharp.CloudFormation.Reporting;
using LambdaSharp.CloudFormation.Template;

namespace LambdaSharp.CloudFormation.Validation {

    // TODO:
    //  - can a condition and a resource have the same name?

    public sealed class TemplateValidator {

        //--- Types ---
        private class Context {

            //--- Constructors ---
            public Context(CloudFormationTemplate template) {
                Template = template ?? throw new ArgumentNullException(nameof(template));
            }

            //--- Properties ---
            public CloudFormationTemplate Template { get; }
        }

        //--- Class Fields ---
        private static readonly Func<SourceLocation, IReportEntry> NameMustBeAlphanumericError = location => ReportEntry.Error("name must be alphanumeric", location);
        private static readonly Func<string, SourceLocation, IReportEntry> ReservedNameError = (name, location) => ReportEntry.Error($"'{name}' is a reserved name", location);

        //--- Fields ---
        private readonly IReport _report;

        //--- Constructors ---
        public TemplateValidator(IReport report) {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        //--- Methods ---
        public void Validate(CloudFormationTemplate template) {
            if(template is null) {
                throw new ArgumentNullException(nameof(template));
            }

            var context = new Context(template);

            // validate parameters
            if(!(template.Parameters is null)) {
                foreach(var (name, parameter) in template.Parameters) {
                    ValidateParameter(context, name, parameter);
                }
            }

            // validate mappings
            if(!(template.Mappings is null)) {
                foreach(var (name, mapping) in template.Mappings) {
                    ValidateMapping(context, name, mapping);
                }
            }

            // validate conditions
            if(!(template.Conditions is null)) {
                foreach(var (name, condition) in template.Conditions) {
                    ValidateCondition(context, name, condition);
                }
            }

            // validate outputs
            if(!(template.Outputs is null)) {
                foreach(var (name, output) in template.Outputs) {
                    ValidateOutput(context, name, output);
                }
            }

            // TODO: validate metadata
            // TODO: validate transform
        }

        private void ValidateTemplate(Context context, CloudFormationTemplate template) {

            // validate template version
            if(!(template.AWSTemplateFormatVersion is null) && (template.AWSTemplateFormatVersion != "2010-09-09")) {

                // TODO: invalid template version
            }

            // validate description
            if(!(template.Description is null) && (template.Description.Length > 1024)) {

                // TODO: description is too long
            }

            // validate there is at least one resource
            if(template.Resources is null) {

                // TODO: missing `Resources` section
            } else if(!template.Resources.Any()) {

                // TODO: empty `Resources` section
            }
        }

        private void ValidateResource(Context context, string name, CloudFormationResource resource) {
            throw new NotImplementedException();
        }

        private void ValidateCondition(Context context, string name, CloudFormationObject expression) {
            throw new NotImplementedException();
        }

        private void ValidateOutput(Context context, string name, CloudFormationOutput output) {
            throw new NotImplementedException();
        }

        private void ValidateMapping(Context context, string name, Dictionary<string, string> mapping) {
            throw new NotImplementedException();
        }

        private void ValidateParameter(Context context, string name, CloudFormationParameter parameter) {
            throw new NotImplementedException();
        }

        private void ValidateName(Context context, string name) {

            // TODO: missing location in name
            var location = SourceLocation.Empty;

            if(!CloudFormationValidationRules.IsValidCloudFormationName(name)) {

                // declaration name is not valid
                _report.Add(NameMustBeAlphanumericError(location));
            } else if(CloudFormationValidationRules.IsReservedCloudFormationName(name)) {

                // declaration uses a reserved name
                _report.Add(ReservedNameError(name, location));
            }
        }
    }
}