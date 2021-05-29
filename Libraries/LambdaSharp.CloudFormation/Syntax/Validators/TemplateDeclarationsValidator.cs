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
using System.Linq;
using LambdaSharp.CloudFormation.Validation;
using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal class TemplateDeclarationsValidator : ASyntaxProcessor {

        //--- Constructors ---
        public TemplateDeclarationsValidator(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        public void Validate(CloudFormationSyntaxTemplate template) {

            // validate template version
            if(!(template.AWSTemplateFormatVersion is null)) {
                if(template.AWSTemplateFormatVersion.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                    Add(Errors.ExpectedStringValue(template.AWSTemplateFormatVersion.SourceLocation));
                } else if(template.AWSTemplateFormatVersion.Value != "2010-09-09") {
                    Add(Errors.TemplateVersionIsNotValid(template.AWSTemplateFormatVersion.SourceLocation));
                }
            }

            // validate description
            if(!(template.Description is null)) {
                if(template.Description.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                    Add(Errors.ExpectedStringValue(template.Description.SourceLocation));
                } else if(((string)template.Description.Value).Length > 1024) {
                    Add(Errors.TemplateDescriptionTooLong(template.Description.SourceLocation));
                }
            }

            // validate there is at least one resource
            if(template.Resources is null) {
                Add(Errors.TemplateResourcesSectionMissing(template.SourceLocation));
            } else if(!template.Resources.Any()) {
                Add(Errors.TemplateResourcesTooShort(template.SourceLocation));
            }

            // validate parameters
            if(!(template.Parameters is null)) {
                foreach(var parameter in template.Parameters) {
                    ValidateParameter(parameter);
                }
            }

            // validate mappings
            if(!(template.Mappings is null)) {
                foreach(var mapping in template.Mappings) {
                    ValidateMapping(mapping);
                }
            }

            // validate conditions
            if(!(template.Conditions is null)) {
                foreach(var condition in template.Conditions) {
                    ValidateCondition(condition);
                }
            }

            // validate outputs
            if(!(template.Outputs is null)) {
                foreach(var output in template.Outputs) {
                    ValidateOutput(output);
                }
            }

            // TODO: validate metadata
            // TODO: validate transform
        }

        private void ValidateResource(CloudFormationSyntaxResource resource) {
            throw new NotImplementedException();
        }

        private void ValidateCondition(CloudFormationSyntaxCondition condition) {
            throw new NotImplementedException();
        }

        private void ValidateOutput(CloudFormationSyntaxOutput output) {
            throw new NotImplementedException();
        }

        private void ValidateMapping(CloudFormationSyntaxMapping mapping) {
            throw new NotImplementedException();
        }

        private void ValidateParameter(CloudFormationSyntaxParameter parameter) {
            throw new NotImplementedException();
        }

        private void ValidateName(ACloudFormationSyntaxNode parent, CloudFormationSyntaxLiteral name) {

            // check if name exists and has the right type
            if(name is null) {
                Add(Errors.DeclarationMissingName(parent.SourceLocation.WithExact(exact: false)));
                return;
            }
            if(name.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                Add(Errors.DeclarationNameMustBeString(name.SourceLocation));
                return;
            }

            // check if name follows CloudFormation rules
            var value = (string)name.Value;
            if(!CloudFormationValidationRules.IsValidCloudFormationName(value)) {

                // declaration name is not valid
                Add(Errors.NameMustBeAlphanumeric(name.SourceLocation));
            } else if(CloudFormationValidationRules.IsReservedCloudFormationName(value)) {

                // declaration uses a reserved name
                Add(Errors.CannotUseReservedName(value, name.SourceLocation));
            }
        }
    }
}