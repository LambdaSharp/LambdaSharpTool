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
using System.Collections.Generic;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal class Constants {

        //--- Constants ---
        public const string AWS_TEMPLATE_FORMAT_VERSION = "2010-09-09";
    }

    internal class Limits {

        // NOTE: AWS CloudFormation quotas: https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cloudformation-limits.html

        //--- Constants ---
        public const int MAX_TEMPLATE_DESCRIPTION_LENGTH = 1024;
        public const int MAX_PARAMETER_VALUE_LENGTH = 4096;

        // TODO: use these limits
        public const int MAX_PARAMETERS_COUNT = 200;
        public const int MAX_PARAMETER_NAME_LENGTH = 255;
        public const int MAX_MAPPINGS_COUNT = 200;
        public const int MAX_MAPPING_NAME_LENGTH = 255;
        public const int MAX_MAPPING_ATTRIBUTE_COUNT = 200;
        public const int MAX_OUTPUTS_COUNT = 200;
        public const int MAX_OUTPUT_NAME_LENGTH = 255;
        public const int MAX_RESOURCE_COUNT = 500;
        public const int MAX_RESOURCE_NAME_LENGTH = 255;
    }


    internal class TemplateDeclarationsValidator : ASyntaxProcessor {

        //--- Constructors ---
        public TemplateDeclarationsValidator(SyntaxProcessorState state, ISyntaxProcessorDependencyProvider provider) : base(state, provider) { }

        public void ValidateDeclarationsAndReferences(CloudFormationSyntaxTemplate template) {

            // validate template version
            if(!(template.AWSTemplateFormatVersion is null)) {
                if(template.AWSTemplateFormatVersion.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                    Add(Errors.ExpectedStringValue(template.AWSTemplateFormatVersion.SourceLocation));
                } else if(template.AWSTemplateFormatVersion.Value != Constants.AWS_TEMPLATE_FORMAT_VERSION) {
                    Add(Errors.TemplateVersionIsNotValid(Constants.AWS_TEMPLATE_FORMAT_VERSION, template.AWSTemplateFormatVersion.SourceLocation));
                }
            }

            // validate description
            if(!(template.Description is null)) {
                if(template.Description.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                    Add(Errors.ExpectedStringValue(template.Description.SourceLocation));
                } else if(((string)template.Description.Value).Length > Limits.MAX_TEMPLATE_DESCRIPTION_LENGTH) {
                    Add(Errors.TemplateDescriptionTooLong(Limits.MAX_TEMPLATE_DESCRIPTION_LENGTH, template.Description.SourceLocation));
                }
            }

            // validate there is at least one resource
            if(template.Resources is null) {
                Add(Errors.TemplateResourcesSectionMissing(template.SourceLocation.Approx()));
            } else if(!template.Resources.Any()) {
                Add(Errors.TemplateResourcesTooShort(template.Resources.SourceLocation));
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

            VisitDeclarations(template);
            DetectCircularReferences(template);
        }

        private void ValidateResource(CloudFormationSyntaxResource resource) {
            ValidateDeclarationName(resource);

            // type is a required value
            if(resource.Type is null) {
                Add(Errors.ResourceMissingType(resource.SourceLocation.Approx()));
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
                Add(Errors.ConditionMissingValue(condition.SourceLocation.Approx()));
            }
        }

        private void ValidateOutput(CloudFormationSyntaxOutput output) {
            ValidateDeclarationName(output);

            // value cannot be null
            if(output.Value is null) {
                Add(Errors.OutputMissingValue(output.SourceLocation.Approx()));
            }

            // export name cannot be null when export is defined
            if(!(output.Export is null) && (output.Export.Name is null)) {
                Add(Errors.OutputExportMissingName(output.Export.SourceLocation.Approx()));
            }

            // condition must be a string
            if(!(output.Condition is null) && (output.Condition.ExpressionValueType != CloudFormationSyntaxValueType.String)) {
                Add(Errors.ExpectedStringValue(output.Condition.SourceLocation));
            }
        }

        private void ValidateMapping(CloudFormationSyntaxMapping mapping) {
            ValidateDeclarationName(mapping);

            // value cannot be null
            if(mapping.Value is null) {
                Add(Errors.MappingMissingValue(mapping.SourceLocation.Approx()));
            } else {
                var reportCount = Provider.Report.Entries.Count();

                // validate level 1 map entries
                foreach(var (level1Key, level1Value) in mapping.Value) {

                    // level 1 key cannot be null
                    if(level1Key is null) {
                        Add(Errors.MappingLevel1KeyMissing(mapping.SourceLocation.Approx()));
                    } else if(!CloudFormationValidationRules.IsValidCloudFormationName(level1Key.Value)) {
                        Add(Errors.MappingKeyMustBeAlphanumeric(level1Key.SourceLocation));
                    }

                    // level 1 value must be a map
                    if(level1Value is CloudFormationSyntaxMap level1Map) {

                        // validate level 2 map entries
                        foreach(var (level2Key, level2Value) in level1Map) {

                            // key cannot be null
                            if(level2Key is null) {
                                Add(Errors.MappingLevel2KeyMissing(level1Map.SourceLocation.Approx()));
                            } else if(!CloudFormationValidationRules.IsValidCloudFormationName(level2Key.Value)) {
                                Add(Errors.MappingKeyMustBeAlphanumeric(level2Key.SourceLocation));
                            }

                            // level 2 value must a literal
                            if(!(level2Value is CloudFormationSyntaxLiteral)) {
                                Add(Errors.MappingLevel2ValueExpectedLiteral(level1Value.SourceLocation));
                            }
                        }
                    } else if(level1Value is null) {
                        Add(Errors.MappingLevel1ValueMissing(mapping.SourceLocation.Approx()));
                    } else {
                        Add(Errors.MappingLevel1ValueExpectedMap(level1Value.SourceLocation));
                    }
                }

                // if no errors were detected on the mapping, validate mapping structure
                if(reportCount == Provider.Report.Entries.Count()) {

                    // collect all defined level-2 keys
                    var allLevel2Keys = new HashSet<string>(
                        mapping.Value
                            .Select(kv => kv.Value)
                            .OfType<CloudFormationSyntaxMap>()
                            .SelectMany(map => map.Keys)
                            .Select(literal => literal.Value)
                    );

                    // check if level-2 keys are consistent
                    foreach(var (level1Key, level1Value) in mapping.Value) {
                        var level2KeysFound = new HashSet<string>(allLevel2Keys);
                        foreach(var (level2Key, level2Value) in (CloudFormationSyntaxMap)level1Value) {
                            level2KeysFound.Remove(level2Key.Value);
                        }
                        foreach(var missingKey in level2KeysFound.OrderBy(key => key)) {
                            Add(Errors.MappingLevel2KeyMissing(missingKey, level1Value.SourceLocation.Approx()));
                        }
                    }
                }
            }
        }

        private void ValidateParameter(CloudFormationSyntaxParameter parameter) {
            ValidateDeclarationName(parameter);

            // type is a required value
            if(parameter.Type is null) {
                Add(Errors.ParameterMissingType(parameter.SourceLocation.Approx()));
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
                    } else if(minLength > Limits.MAX_PARAMETER_VALUE_LENGTH) {
                        Add(Errors.MinLengthTooLarge(Limits.MAX_PARAMETER_VALUE_LENGTH, parameter.MinLength.SourceLocation));
                    }
                }
                if(parameter.MaxLength != null) {
                    if(!int.TryParse(parameter.MaxLength.Value, out var maxLength)) {
                        Add(Errors.MaxLengthMustBeAnInteger(parameter.MaxLength.SourceLocation));
                    } else if(maxLength <= 0) {
                        Add(Errors.MaxLengthMustBePositive(parameter.MaxLength.SourceLocation));
                    } else if(maxLength > Limits.MAX_PARAMETER_VALUE_LENGTH) {
                        Add(Errors.MaxLengthTooLarge(Limits.MAX_PARAMETER_VALUE_LENGTH, parameter.MaxLength.SourceLocation));
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
                Add(Errors.DeclarationMissingName(declaration.SourceLocation.Approx()));
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

        private void VisitDeclarations(CloudFormationSyntaxTemplate template) {

            // record all declarations in context
            template.InspectType<ACloudFormationSyntaxDeclaration>(node => {
                switch(node) {
                case CloudFormationSyntaxParameter parameter:
                    if(!State.Declare(parameter)) {
                        Add(Errors.DuplicateParameterDeclaration(parameter.LogicalId.Value, parameter.LogicalId.SourceLocation));
                    }
                    if(State.TryGetResource(parameter.LogicalId.Value, out _)) {
                        Add(Errors.AmbiguousParameterDeclaration(parameter.LogicalId.Value, parameter.LogicalId.SourceLocation));
                    }
                    break;
                case CloudFormationSyntaxResource resource:
                    if(!State.Declare(resource)) {
                        Add(Errors.DuplicateResourceDeclaration(resource.LogicalId.Value, resource.LogicalId.SourceLocation));
                    }
                    if(State.TryGetParameter(resource.LogicalId.Value, out _)) {
                        Add(Errors.AmbiguousResourceDeclaration(resource.LogicalId.Value, resource.LogicalId.SourceLocation));
                    }
                    break;
                case CloudFormationSyntaxOutput output:
                    if(!State.Declare(output)) {
                        Add(Errors.DuplicateOutputDeclaration(output.LogicalId.Value, output.LogicalId.SourceLocation));
                    }
                    break;
                case CloudFormationSyntaxCondition condition:
                    if(!State.Declare(condition)) {
                        Add(Errors.DuplicateConditionDeclaration(condition.LogicalId.Value, condition.LogicalId.SourceLocation));
                    }
                    break;
                case CloudFormationSyntaxMapping mapping:
                    if(!State.Declare(mapping)) {
                        Add(Errors.DuplicateMappingDeclaration(mapping.LogicalId.Value, mapping.LogicalId.SourceLocation));
                    }
                    break;
                default:

                    // TODO: throw better exception
                    throw new Exception("oops");
                }
            });

            // record explicit resource dependencies
            template.InspectType<CloudFormationSyntaxResource>(node => {
                if(!(node.DependsOn is null)) {
                    foreach(var dependsOn in node.DependsOn) {
                        if(State.TryGetResource(dependsOn.Value, out var resource)) {

                            // track dependency
                            State.AddDependency(dependsOn, resource);
                        } else {
                            Add(Errors.ResourceDependsOnNotFound(dependsOn.Value, dependsOn.SourceLocation));
                        }
                    }
                }
            });

            // record implicit dependencies via references
            template.InspectType<CloudFormationSyntaxFunctionInvocation>(node => {
                switch(node.Function.FunctionName) {

                // TODO: replace function names with constants
                case "Condition":

                    // verify a condition of this name exists
                    if(State.TryGetCondition(node.StringArgument, out var condition)) {

                        // track dependency
                        State.AddDependency(node, condition);
                    } else {
                        Add(Errors.ConditionNotFound(node.StringArgument, node.Argument.SourceLocation));
                    }
                    break;
                case "Fn::GetAtt":

                    // verify a resource of this name exists
                    if(State.TryGetResource(node.StringArgument, out var resource)) {

                        // track dependency
                        State.AddDependency(node, resource);
                    } else {
                        Add(Errors.GetAttResourceNotFound(node.StringArgument, node.Argument.SourceLocation));
                    }
                    break;
                case "Fn::FindInMap":

                    // verify a mapping of this name exists
                    if(State.TryGetMapping(node.StringArgument, out var mapping)) {

                        // track dependency
                        State.AddDependency(node, mapping);
                    } else {
                        Add(Errors.FindInMapMappingNotFound(node.StringArgument, node.Argument.SourceLocation));
                    }
                    break;
                case "Ref":

                    // for conditions, !Ref can only reference parameters, not resources
                    if(node.ParentDeclaration is CloudFormationSyntaxCondition) {

                        // verify a parameter of this name exists
                        if(State.TryGetParameter(node.StringArgument, out var parameter)) {

                            // track dependency
                            State.AddDependency(node, parameter);
                        } else if(State.TryGetResource(node.StringArgument, out resource)) {

                            // conditions cannot refer to resources
                            Add(Errors.RefResourceNotValidInCondition(node.StringArgument, node.Argument.SourceLocation));
                        } else {
                            Add(Errors.RefParameterNotFound(node.StringArgument, node.Argument.SourceLocation));
                        }
                    } else {

                        // verify a resource or parameter of this name exists
                        if(State.TryGetParameter(node.StringArgument, out var parameter)) {

                            // track dependency
                            State.AddDependency(node, parameter);
                        } else if(State.TryGetResource(node.StringArgument, out resource)) {

                            // track dependency
                            State.AddDependency(node, resource);
                        } else {
                            Add(Errors.RefParameterOrResourceNotFound(node.StringArgument, node.Argument.SourceLocation));
                        }
                    }
                    break;
                default:

                    // nothing to do
                    break;
                }
            });
        }

        private void DetectCircularReferences(CloudFormationSyntaxTemplate template) {

            // detect circular dependencies
            var declarations = State.Parameters.OfType<ACloudFormationSyntaxDeclaration>()
                .Union(State.Conditions)
                .Union(State.Mappings)
                .Union(State.Resources)
                .Union(State.Outputs)
                .ToList();
            var visited = new List<ACloudFormationSyntaxDeclaration>();
            var cycles = new List<IEnumerable<ACloudFormationSyntaxDeclaration>>();
            foreach(var declaration in declarations) {
                Visit(declaration, visited);
            }

            // report all unique cycles found
            if(cycles.Any()) {
                var fingerprints = new HashSet<string>();
                foreach(var cycle in cycles) {
                    var path = cycle.Select(dependency => dependency.LogicalId.Value).ToList();

                    // NOTE (2020-06-09, bjorg): cycles are reported for each node in the cycle; however, the nodes in the cycle
                    //  form a unique fingerprint to avoid duplicate reporting duplicate cycles
                    var fingerprint = string.Join(",", path.OrderBy(logicalId => logicalId));
                    if(fingerprints.Add(fingerprint)) {
                        Add(Errors.CircularDependencyDetected(string.Join(" -> ", path.Append(path.First())), cycle.First().SourceLocation));
                    }
                }
            }

            // local functions
            void Visit(ACloudFormationSyntaxDeclaration declaration, List<ACloudFormationSyntaxDeclaration> visited) {

                // check if we detected a cycle
                var index = visited.IndexOf(declaration);
                if(index >= 0) {

                    // extract sub-list that represents cycle since cycle may not point back to the first element
                    cycles.Add(visited.GetRange(index, visited.Count - index));
                    return;
                }

                // recurse into every dependency, until we exhaust the tree or find a circular dependency
                visited.Add(declaration);
                foreach(var dependency in State.GetDependencies(declaration)) {

                    // TODO: need to take conditions into account

                    Visit(dependency.Declaration, visited);
                }
                visited.Remove(declaration);
            }
        }
    }
}