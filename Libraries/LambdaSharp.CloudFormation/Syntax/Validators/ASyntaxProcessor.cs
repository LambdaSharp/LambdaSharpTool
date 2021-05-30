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

namespace LambdaSharp.CloudFormation.Syntax.Validators {

    internal interface ISyntaxProcessorDependencyProvider {

        //--- Properties ---
        IReport Report { get; }
    }

    internal sealed class SyntaxProcessorState {

        //--- Fields ---
        private readonly Dictionary<string, CloudFormationSyntaxParameter> _parameters = new Dictionary<string, CloudFormationSyntaxParameter>();
        private readonly Dictionary<string, CloudFormationSyntaxCondition> _conditions = new Dictionary<string, CloudFormationSyntaxCondition>();
        private readonly Dictionary<string, CloudFormationSyntaxMapping> _mappings = new Dictionary<string, CloudFormationSyntaxMapping>();
        private readonly Dictionary<string, CloudFormationSyntaxResource> _resources = new Dictionary<string, CloudFormationSyntaxResource>();
        private readonly Dictionary<string, CloudFormationSyntaxOutput> _outputs = new Dictionary<string, CloudFormationSyntaxOutput>();
        private readonly Dictionary<ACloudFormationSyntaxDeclaration, HashSet<ACloudFormationSyntaxDeclaration>> _dependencies = new Dictionary<ACloudFormationSyntaxDeclaration, HashSet<ACloudFormationSyntaxDeclaration>>();
        private readonly Dictionary<ACloudFormationSyntaxDeclaration, HashSet<ACloudFormationSyntaxDeclaration>> _reverseDependencies = new Dictionary<ACloudFormationSyntaxDeclaration, HashSet<ACloudFormationSyntaxDeclaration>>();

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

        public void AddDependency(ACloudFormationSyntaxDeclaration from, ACloudFormationSyntaxDeclaration to) {

            // add dependency
            if(!(_dependencies.TryGetValue(from, out var dependents))) {
                dependents = new HashSet<ACloudFormationSyntaxDeclaration>();
                _dependencies.Add(from, dependents);
            }
            dependents.Add(to);

            // add reverse dependency
            if(!(_reverseDependencies.TryGetValue(to, out var reverseDependents))) {
                reverseDependents = new HashSet<ACloudFormationSyntaxDeclaration>();
                _reverseDependencies.Add(to, reverseDependents);
            }
            reverseDependents.Add(from);
        }

        public IEnumerable<ACloudFormationSyntaxDeclaration> GetDependencies(ACloudFormationSyntaxDeclaration declaration)
            => _dependencies.TryGetValue(declaration, out var dependencies)
                ? dependencies
                : Enumerable.Empty<ACloudFormationSyntaxDeclaration>();
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