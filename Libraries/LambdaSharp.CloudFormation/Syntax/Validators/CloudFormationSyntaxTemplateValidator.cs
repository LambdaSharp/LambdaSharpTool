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
using LambdaSharp.CloudFormation.Reporting;
using LambdaSharp.CloudFormation.Syntax.Declarations;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    // TODO:
    //  - can a condition and a resource have the same name?

    public sealed class CloudFormationSyntaxTemplateValidator {

        //--- Types ---
        private class DependencyProvider : ISyntaxProcessorDependencyProvider {

            //--- Constructors ---
            public DependencyProvider(IReport report) {
                Report = report ?? throw new ArgumentNullException(nameof(report));
            }

            //--- Properties ---
            public IReport Report { get; }
        }

        //--- Fields ---
        private readonly IReport _report;

        //--- Constructors ---
        public CloudFormationSyntaxTemplateValidator(IReport report)
            => _report = report ?? throw new ArgumentNullException(nameof(report));

        //--- Methods ---
        public void Validate(CloudFormationSyntaxTemplate template) {
            if(template is null) {
                throw new ArgumentNullException(nameof(template));
            }
            var state = new SyntaxProcessorState(template);
            var provider = new DependencyProvider(_report);

            // validate structure
            new SyntaxTreeIntegrityProcessor(state, provider).ValidateIntegrity(template);

            // validate template declarations
            new TemplateDeclarationsValidator(state, provider).ValidateDeclarationsAndReferences(template);
        }
    }
}