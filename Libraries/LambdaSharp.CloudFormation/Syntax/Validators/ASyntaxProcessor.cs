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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LambdaSharp.CloudFormation.Reporting;
using LambdaSharp.CloudFormation.Syntax.Declarations;
using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal interface ISyntaxProcessorDependencyProvider {

        //--- Properties ---
        IReport Report { get; }
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

    internal sealed class SyntaxProcessorState {

        //--- Fields ---
        private readonly Dictionary<string, CloudFormationSyntaxParameter> _parameters = new Dictionary<string, CloudFormationSyntaxParameter>();
        private readonly Dictionary<string, CloudFormationSyntaxCondition> _conditions = new Dictionary<string, CloudFormationSyntaxCondition>();
        private readonly Dictionary<string, CloudFormationSyntaxMapping> _mappings = new Dictionary<string, CloudFormationSyntaxMapping>();
        private readonly Dictionary<string, CloudFormationSyntaxResource> _resources = new Dictionary<string, CloudFormationSyntaxResource>();
        private readonly Dictionary<string, CloudFormationSyntaxOutput> _outputs = new Dictionary<string, CloudFormationSyntaxOutput>();
        private readonly Dictionary<ACloudFormationSyntaxDeclaration, List<DependencyRecord>> _dependencies = new Dictionary<ACloudFormationSyntaxDeclaration, List<DependencyRecord>>();
        private readonly Dictionary<ACloudFormationSyntaxDeclaration, List<DependencyRecord>> _reverseDependencies = new Dictionary<ACloudFormationSyntaxDeclaration, List<DependencyRecord>>();

        //--- Constructors ---
        public SyntaxProcessorState(CloudFormationSyntaxTemplate template) => Template = template ?? throw new System.ArgumentNullException(nameof(template));

        //--- Properties ---
        public CloudFormationSyntaxTemplate Template { get; }
        public IEnumerable<CloudFormationSyntaxParameter> Parameters => _parameters.Values;
        public IEnumerable<CloudFormationSyntaxCondition> Conditions => _conditions.Values;
        public IEnumerable<CloudFormationSyntaxMapping> Mappings => _mappings.Values;
        public IEnumerable<CloudFormationSyntaxResource> Resources => _resources.Values;
        public IEnumerable<CloudFormationSyntaxOutput> Outputs => _outputs.Values;

        //--- Methods ---
        public bool Declare(CloudFormationSyntaxParameter parameter) => _parameters.TryAdd(parameter.LogicalId.Value, parameter);
        public bool Declare(CloudFormationSyntaxCondition condition) => _conditions.TryAdd(condition.LogicalId.Value, condition);
        public bool Declare(CloudFormationSyntaxMapping mapping) => _mappings.TryAdd(mapping.LogicalId.Value, mapping);
        public bool Declare(CloudFormationSyntaxResource resource) => _resources.TryAdd(resource.LogicalId.Value, resource);
        public bool Declare(CloudFormationSyntaxOutput output) => _outputs.TryAdd(output.LogicalId.Value, output);
        public bool TryGetParameter(string name, [NotNullWhen(true)] out CloudFormationSyntaxParameter? parameter) => _parameters.TryGetValue(name, out parameter);
        public bool TryGetCondition(string name, [NotNullWhen(true)] out CloudFormationSyntaxCondition? condition) => _conditions.TryGetValue(name, out condition);
        public bool TryGetMapping(string name, [NotNullWhen(true)] out CloudFormationSyntaxMapping? mapping) => _mappings.TryGetValue(name, out mapping);
        public bool TryGetResource(string name, [NotNullWhen(true)] out CloudFormationSyntaxResource? resource) => _resources.TryGetValue(name, out resource);
        public bool TryGetOutput(string name, [NotNullWhen(true)] out CloudFormationSyntaxOutput? output) => _outputs.TryGetValue(name, out output);

        public void AddDependency(ACloudFormationSyntaxExpression expression, ACloudFormationSyntaxDeclaration dependency) {
            var fromDeclaration = expression.ParentDeclaration;
            var parents = expression.Parents;

            // collect conditions for the dependency
            var conditions = new List<DependencyRecord.Condition>();
            var current = expression;
            var parent = expression.Parent;
            while(parent is ACloudFormationSyntaxExpression parentExpression) {

                // check if parent is an invocation of 'Fn::If'
                if(
                    (parent is CloudFormationSyntaxFunctionInvocation parentFunctionInvocation)
                    && (parentFunctionInvocation.Function.FunctionName == "Fn::If")
                ) {

                    // validate 'Fn::If' invocation has the right shape
                    if(
                        (parentFunctionInvocation.Argument is CloudFormationSyntaxList argumentList)
                        && (argumentList.Count == 3)
                        && (argumentList[0] is CloudFormationSyntaxLiteral conditionName)
                    ) {

                        // check if current expression is in the 'true' branch of the condition
                        var ifTrueCondition = object.ReferenceEquals(argumentList[1], current);
                        conditions.Add(new DependencyRecord.Condition(ifTrueCondition, conditionName));
                    } else {

                        // nothing to do; invalid 'Fn::If' invocation is reported during expression validation
                    }
                }

                // move up the tree
                current = parentExpression;
                parent = current.Parent;
            }

            // check if parent declaration is a resource with a condition
            if((fromDeclaration is CloudFormationSyntaxResource resource) && (resource.Condition != null)) {
                conditions.Add(new DependencyRecord.Condition(ifTrueCondition: true, resource.Condition));
            }

            // add dependency
            if(!(_dependencies.TryGetValue(fromDeclaration, out var dependents))) {
                dependents = new List<DependencyRecord>();
                _dependencies.Add(fromDeclaration, dependents);
            }
            dependents.Add(new DependencyRecord(dependency, conditions));

            // add reverse dependency
            if(!(_reverseDependencies.TryGetValue(dependency, out var reverseDependents))) {
                reverseDependents = new List<DependencyRecord>();
                _reverseDependencies.Add(dependency, reverseDependents);
            }
            reverseDependents.Add(new DependencyRecord(fromDeclaration, conditions));
        }

        public IEnumerable<DependencyRecord> GetDependencies(ACloudFormationSyntaxDeclaration declaration)
            => _dependencies.TryGetValue(declaration, out var dependencies)
                ? dependencies
                : Enumerable.Empty<DependencyRecord>();
    }

    internal abstract class ASyntaxProcessor {

        //--- Constructors ---
        public ASyntaxProcessor(SyntaxProcessorState state, ISyntaxProcessorDependencyProvider provider) {
            State = state ?? throw new System.ArgumentNullException(nameof(state));
            Provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
        }

        //--- Properties ---
        protected SyntaxProcessorState State { get; }
        protected ISyntaxProcessorDependencyProvider Provider { get; }
        protected IReport Report => Provider.Report;

        //--- Methods ---
        protected void Add(IReportEntry entry) => Report.Add(entry);
    }
}