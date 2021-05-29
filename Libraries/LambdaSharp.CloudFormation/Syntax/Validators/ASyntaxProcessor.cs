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

    internal interface ISyntaxProcessorDependencyProvider {

        //--- Properties ---
        IReport Report { get; }
    }

    internal abstract class ASyntaxProcessor {

        //--- Constructors ---
        public ASyntaxProcessor(ISyntaxProcessorDependencyProvider provider)
            => Provider = provider ?? throw new System.ArgumentNullException(nameof(provider));

        //--- Properties ---
        protected ISyntaxProcessorDependencyProvider Provider { get; }
        protected IReport Report => Provider.Report;

        //--- Methods ---
        protected void Add(IReportEntry entry) => Report.Add(entry);
    }
}