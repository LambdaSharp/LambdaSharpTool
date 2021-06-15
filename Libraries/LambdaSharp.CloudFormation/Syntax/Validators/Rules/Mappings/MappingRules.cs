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
using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;
using LambdaSharp.CloudFormation.Syntax.Validation;

namespace LambdaSharp.CloudFormation.Syntax.Validators.Rules.Mappings {

    // TODO: this rule depends on dependencies having been computed already
    internal sealed class MappingUsedRule : ASyntaxRule<CloudFormationSyntaxMapping> {

        //--- Methods ---
        public override void Validate(CloudFormationSyntaxMapping mapping) {
            if(!State.GetReverseDependencies(mapping).Any()) {
                Add(Warnings.MappingIsNeverUsed(mapping.LogicalId.Value, mapping.SourceLocation));
            }
        }
    }

    internal sealed class MappingLimitRule : ASyntaxRule<CloudFormationSyntaxMapping> {

        //--- Methods ---
        public override void Validate(CloudFormationSyntaxMapping mapping) {

            // TODO:
            //  * validate that a mapping has no more than 200 attributes
            throw new NotImplementedException();
        }
    }

    internal sealed class MappingDeclarationLimitRule : ASyntaxRule<CloudFormationSyntaxList<CloudFormationSyntaxMapping>> {

        //--- Methods ---
        public override void Validate(CloudFormationSyntaxList<CloudFormationSyntaxMapping> mappings) {
            if((mappings != null) && (mappings.Count > CloudFormationLimits.MAPPINGS_MAX_COUNT)) {
                Add(Errors.MappingTooManyDeclarations(CloudFormationLimits.MAPPINGS_MAX_COUNT, mappings.SourceLocation));
            }
        }
    }
}