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
using System.Text.RegularExpressions;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal class TemplateDeclarationsValidator : ASyntaxProcessor {

        //--- Constants ---
        private const int MAX_TEMPLATE_DESCRIPTION_LENGTH = 1024;
        private const int MAX_PARAMETER_VALUE_LENGTH = 4_000;

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
                } else if(((string)template.Description.Value).Length > MAX_TEMPLATE_DESCRIPTION_LENGTH) {
                    Add(Errors.TemplateDescriptionTooLong(MAX_TEMPLATE_DESCRIPTION_LENGTH, template.Description.SourceLocation));
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
            ValidateDeclarationName(resource);

            // type is a required value
            if(resource.Type is null) {
                Add(Errors.ResourceMissingType(resource.SourceLocation.NotExact()));
            } else if(resource.Type.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                Add(Errors.ResourceTypeExpectedString(resource.SourceLocation));
            }

            // condition must be a string
            if(!(resource.Condition is null) && (resource.Condition.ExpressionValueType != CloudFormationSyntaxValueType.String)) {
                Add(Errors.ExpectedStringValue(resource.Condition.SourceLocation));
            }

            // depends-on must a list of string values
            if(!(resource.DependsOn is null)) {
                foreach(var dependsOn in resource.DependsOn) {
                    if(dependsOn.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                        Add(Errors.ExpectedStringValue(dependsOn.SourceLocation));
                    }
                }
            }

            // deletion policy must be a string value
            if(!(resource.DeletionPolicy is null) && (resource.DeletionPolicy.ExpressionValueType != CloudFormationSyntaxValueType.String)) {
                Add(Errors.ExpectedStringValue(resource.DeletionPolicy.SourceLocation));
            }
        }

        private void ValidateCondition(CloudFormationSyntaxCondition condition) {
            ValidateDeclarationName(condition);

            // value cannot be null
            if(condition.Value is null) {
                Add(Errors.ConditionMissingValue(condition.SourceLocation.NotExact()));
            }
        }

        private void ValidateOutput(CloudFormationSyntaxOutput output) {
            ValidateDeclarationName(output);

            // value cannot be null
            if(output.Value is null) {
                Add(Errors.OutputMissingValue(output.SourceLocation.NotExact()));
            }

            // export name cannot be null when export is defined
            if(!(output.Export is null) && (output.Export.Name is null)) {
                Add(Errors.OutputExportMissingName(output.Export.SourceLocation.NotExact()));
            }

            // condition must be a string
            if(!(output.Condition is null) && (output.Condition.ExpressionValueType != CloudFormationSyntaxValueType.String)) {
                Add(Errors.ExpectedStringValue(output.Condition.SourceLocation));
            }
        }

        private void ValidateMapping(CloudFormationSyntaxMapping mapping) {
            ValidateDeclarationName(mapping);

            // TODO: confirm map structure

            // value cannot be null
            if(mapping.Value is null) {
                Add(Errors.MappingMissingValue(mapping.SourceLocation.NotExact()));
            } else {

                // validate level 1 map entries
                foreach(var (level1Key, level1Value) in mapping.Value) {

                    // level 1 key cannot be null
                    if(level1Key is null) {
                        Add(Errors.MappingLevel1KeyMissing(mapping.SourceLocation.NotExact()));
                    }

                    // level 1 value must be a map
                    if(level1Value is CloudFormationSyntaxMap level1Map) {

                        // validate level 2 map entries
                        foreach(var (level2Key, level2Value) in level1Map) {

                            // key cannot be null
                            if(level2Key is null) {
                                Add(Errors.MappingLevel2KeyMissing(level1Map.SourceLocation.NotExact()));
                            }

                            // level 2 value must a literal
                            if(!(level2Value is CloudFormationSyntaxLiteral)) {
                                Add(Errors.MappingLevel2ValueExpectedLiteral(level1Value.SourceLocation));
                            }
                        }
                    } else if(level1Value is null) {
                        Add(Errors.MappingLevel1ValueMissing(mapping.SourceLocation.NotExact()));
                    } else {
                        Add(Errors.MappingLevel1ValueExpectedMap(level1Value.SourceLocation));
                    }
                }
            }
        }

        private void ValidateParameter(CloudFormationSyntaxParameter parameter) {
            ValidateDeclarationName(parameter);

            // type is a required value
            if(parameter.Type is null) {
                Add(Errors.ParameterMissingType(parameter.SourceLocation.NotExact()));
            } else if(parameter.Type.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                Add(Errors.ParameterTypeExpectedString(parameter.SourceLocation));
            }

            // only the 'Number' type can have the 'MinValue' and 'MaxValue' attributes
            if(parameter.Type?.Value == "Number") {
                if((parameter.MinValue != null) && !int.TryParse(parameter.MinValue.Value, out _)) {
                    Add(Errors.MinValueMustBeAnInteger(parameter.MinValue.SourceLocation));
                }
                if((parameter.MaxValue != null) && !int.TryParse(parameter.MaxValue.Value, out _)) {
                    Add(Errors.MaxValueMustBeAnInteger(parameter.MaxValue.SourceLocation));
                }
                if(
                    (parameter.MinValue != null)
                    && (parameter.MaxValue != null)
                    && int.TryParse(parameter.MinValue.Value, out var minValueRange)
                    && int.TryParse(parameter.MaxValue.Value, out var maxValueRange)
                    && (maxValueRange < minValueRange)
                ) {
                    Add(Errors.MinMaxValueInvalidRange(parameter.MinValue.SourceLocation));
                }
            } else {

                // ensure Number parameter options are not used
                if(parameter.MinValue != null) {
                    Add(Errors.MinValueAttributeRequiresNumberType(parameter.MinValue.SourceLocation));
                }
                if(parameter.MaxValue != null) {
                    Add(Errors.MaxValueAttributeRequiresNumberType(parameter.MaxValue.SourceLocation));
                }
            }

            // only the 'String' type can have the 'AllowedPattern', 'MinLength', and 'MaxLength' attributes
            if(parameter.Type?.Value == "String") {

                // the 'AllowedPattern' attribute must be a valid regex expression
                if(parameter.AllowedPattern != null) {

                    // check if 'AllowedPattern' is a valid regular expression
                    try {
                        new Regex(parameter.AllowedPattern.Value);
                    } catch {
                        Add(Errors.AllowedPatternAttributeInvalid(parameter.AllowedPattern.SourceLocation));
                    }
                } else if(parameter.ConstraintDescription != null) {

                    // the 'ConstraintDescription' attribute is only valid in conjunction with the 'AllowedPattern' attribute
                    Add(Errors.ConstraintDescriptionAttributeRequiresAllowedPatternAttribute(parameter.ConstraintDescription.SourceLocation));
                }
                if(parameter.MinLength != null) {
                    if(!int.TryParse(parameter.MinLength.Value, out var minLength)) {
                        Add(Errors.MinLengthMustBeAnInteger(parameter.MinLength.SourceLocation));
                    } else if(minLength < 0) {
                        Add(Errors.MinLengthMustBeNonNegative(parameter.MinLength.SourceLocation));
                    } else if(minLength > MAX_PARAMETER_VALUE_LENGTH) {
                        Add(Errors.MinLengthTooLarge(MAX_PARAMETER_VALUE_LENGTH, parameter.MinLength.SourceLocation));
                    }
                }
                if(parameter.MaxLength != null) {
                    if(!int.TryParse(parameter.MaxLength.Value, out var maxLength)) {
                        Add(Errors.MaxLengthMustBeAnInteger(parameter.MaxLength.SourceLocation));
                    } else if(maxLength <= 0) {
                        Add(Errors.MaxLengthMustBePositive(parameter.MaxLength.SourceLocation));
                    } else if(maxLength > MAX_PARAMETER_VALUE_LENGTH) {
                        Add(Errors.MaxLengthTooLarge(MAX_PARAMETER_VALUE_LENGTH, parameter.MaxLength.SourceLocation));
                    }
                }
                if(
                    (parameter.MinLength != null)
                    && (parameter.MaxLength != null)
                    && int.TryParse(parameter.MinLength.Value, out var minLengthRange)
                    && int.TryParse(parameter.MaxLength.Value, out var maxLengthRange)
                    && (maxLengthRange < minLengthRange)
                ) {
                    Add(Errors.MinMaxLengthInvalidRange(parameter.MinLength.SourceLocation));
                }
            } else {

                // ensure String parameter options are not used
                if(parameter.AllowedPattern != null) {
                    Add(Errors.AllowedPatternAttributeRequiresStringType(parameter.AllowedPattern.SourceLocation));
                }
                if(parameter.ConstraintDescription != null) {
                    Add(Errors.ConstraintDescriptionAttributeRequiresStringType(parameter.ConstraintDescription.SourceLocation));
                }
                if(parameter.MinLength != null) {
                    Add(Errors.MinLengthAttributeRequiresStringType(parameter.MinLength.SourceLocation));
                }
                if(parameter.MaxLength != null) {
                    Add(Errors.MaxLengthAttributeRequiresStringType(parameter.MaxLength.SourceLocation));
                }
            }
       }

        private void ValidateDeclarationName(ACloudFormationSyntaxDeclaration declaration) {

            // check if name exists and is a string
            if(declaration.LogicalId is null) {
                Add(Errors.DeclarationMissingName(declaration.SourceLocation.NotExact()));
                return;
            }
            if(declaration.LogicalId.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                Add(Errors.DeclarationNameMustBeString(declaration.LogicalId.SourceLocation));
                return;
            }

            // check if name follows CloudFormation rules
            var value = (string)declaration.LogicalId.Value;
            if(!CloudFormationValidationRules.IsValidCloudFormationName(value)) {

                // declaration name is not valid
                Add(Errors.NameMustBeAlphanumeric(declaration.LogicalId.SourceLocation));
            } else if(CloudFormationValidationRules.IsReservedCloudFormationName(value)) {

                // declaration uses a reserved name
                Add(Errors.CannotUseReservedName(value, declaration.LogicalId.SourceLocation));
            }
        }
    }
}