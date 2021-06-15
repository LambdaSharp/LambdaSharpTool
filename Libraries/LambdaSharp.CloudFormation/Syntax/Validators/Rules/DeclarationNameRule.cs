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

using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;
using LambdaSharp.CloudFormation.Syntax.Validation;
using LambdaSharp.CloudFormation.Validation;

namespace LambdaSharp.CloudFormation.Syntax.Validators.Rules {

    internal sealed class DeclarationNameRule : ASyntaxRule<ACloudFormationSyntaxDeclaration> {

        //--- Methods ---
        public override void Validate(ACloudFormationSyntaxDeclaration declaration) {

            // check if name exists and is a string
            if(declaration.LogicalId is null) {
                Add(Errors.DeclarationMissingName(declaration.SourceLocation.Approx()));
                return;
            }
            if(declaration.LogicalId.ExpressionValueType != CloudFormationSyntaxValueType.String) {
                Add(Errors.DeclarationNameMustBeString(declaration.LogicalId.SourceLocation));
                return;
            }

            // check if declaration name follows CloudFormation rules
            var value = (string)declaration.LogicalId.Value;
            if(!CloudFormationRules.IsValidCloudFormationName(value)) {

                // declaration name is not valid
                Add(Errors.NameMustBeAlphanumeric(declaration.LogicalId.SourceLocation));
            } else if(CloudFormationRules.IsReservedCloudFormationName(value)) {

                // declaration uses a reserved name
                Add(Errors.CannotUseReservedName(value, declaration.LogicalId.SourceLocation));
            } else if(value.Length > CloudFormationLimits.DECLARATION_MAX_NAME_LENGTH) {

                // declaration name is too long
                Add(Errors.NameIsTooLong(CloudFormationLimits.DECLARATION_MAX_NAME_LENGTH, declaration.LogicalId.SourceLocation));
            }
        }
    }
}