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

using System.Collections.Generic;
using LambdaSharp.CloudFormation.Reporting;
using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal interface ISyntaxProcessorDependencyProvider {

        //--- Properties ---
        IEnumerable<IReportEntry> ReportEntries { get; }

        //--- Methods ---
        void AddReportEntry(IReportEntry entry);
    }

    internal class DependencyRecord {

        //--- Types ---
        internal readonly struct Condition {

            //--- Fields ---
            public readonly bool IfTrueCondition;
            public readonly CloudFormationSyntaxLiteral ConditionName;

            //--- Constructors ---
            public Condition(bool ifTrueCondition, CloudFormationSyntaxLiteral conditionName) {
                IfTrueCondition = ifTrueCondition;
                ConditionName = conditionName ?? throw new System.ArgumentNullException(nameof(conditionName));
            }
        }

        //--- Constructors ---
        public DependencyRecord(ACloudFormationSyntaxDeclaration dependency, List<Condition> conditions) {
            Declaration = dependency ?? throw new System.ArgumentNullException(nameof(dependency));
            Conditions = conditions ?? throw new System.ArgumentNullException(nameof(conditions));
        }

        //--- Properties ---
        public ACloudFormationSyntaxDeclaration Declaration { get; }
        public List<Condition> Conditions { get; }
    }

    internal abstract class ASyntaxProcessor {

        //--- Constructors ---
        public ASyntaxProcessor(SyntaxProcessorState state) {
            State = state ?? throw new System.ArgumentNullException(nameof(state));
        }

        //--- Properties ---
        protected SyntaxProcessorState State { get; }
        protected ISyntaxProcessorDependencyProvider Provider => State.Provider;

        //--- Methods ---
        protected void Add(IReportEntry entry) => Provider.AddReportEntry(entry);
    }
}