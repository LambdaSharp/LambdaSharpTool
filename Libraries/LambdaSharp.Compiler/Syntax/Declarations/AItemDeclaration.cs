/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    public abstract class AItemDeclaration : ADeclaration {

        //--- Types ---
        public readonly struct ConditionBranch {

            //--- Constructors ---
            public ConditionBranch(bool condition, AExpression expression)
                => (IfBranch, Expression) = (condition, expression ?? throw new ArgumentNullException(nameof(expression)));

            //--- Properties ---
            public bool IfBranch { get; }
            public AExpression Expression { get; }
        }

        public class DependencyRecord {

            //--- Constructors ---
            public DependencyRecord(AItemDeclaration referencedDeclaration, IEnumerable<ConditionBranch> conditions, AExpression expression) {
                ReferencedDeclaration = referencedDeclaration ?? throw new ArgumentNullException(nameof(referencedDeclaration));
                Conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
                Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            }

            //--- Properties ---
            public AItemDeclaration ReferencedDeclaration { get; }
            public IEnumerable<ConditionBranch> Conditions { get; }
            public AExpression Expression { get; }
        }

        internal class MissingDependencyRecord {

            //--- Constructors ---
            public MissingDependencyRecord(string declarationFullName, ASyntaxNode node) {
                MissingDeclarationFullName = declarationFullName ?? throw new ArgumentNullException(nameof(declarationFullName));
                Node = node ?? throw new ArgumentNullException(nameof(node));
            }

            //--- Properties ---
            public string MissingDeclarationFullName { get; }
            public ASyntaxNode Node{ get; }
        }

        //--- Fields ---
        private LiteralExpression? _description;
        private AExpression? _referenceExpression;
        private readonly List<DependencyRecord> _dependencies = new List<DependencyRecord>();
        private readonly List<DependencyRecord> _reverseDependencies = new List<DependencyRecord>();
        private readonly List<MissingDependencyRecord> _missingDependencies = new List<MissingDependencyRecord>();
        private SyntaxNodeCollection<AItemDeclaration> _declarations;
        private string? _fullName;
        private string? _logicalId;

        //--- Constructors ---
        protected AItemDeclaration(LiteralExpression itemName) {
            ItemName = SetParent(itemName ?? throw new ArgumentNullException(nameof(itemName)));
            _declarations = SetParent(new SyntaxNodeCollection<AItemDeclaration>());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Description {
            get => _description;
            set => _description = SetParent(value);
        }

        public LiteralExpression ItemName { get; }

        public string FullName {
            get {
                if(_fullName == null) {
                    var parentItemDeclaration = ParentItemDeclaration;
                    _fullName = (parentItemDeclaration != null)
                        ? $"{parentItemDeclaration.FullName}::{ItemName.Value}"
                        : ItemName.Value;
                }
                return _fullName;
            }
        }

        public string LogicalId {
            get {
                if(_logicalId == null) {
                    _logicalId = FullName.Replace("::", "");
                }
                return _logicalId;
            }
        }

        public bool DiscardIfNotReachable { get; set; }

        public SyntaxNodeCollection<AItemDeclaration> Declarations {
            get => _declarations ?? throw new InvalidOperationException();
            set => _declarations = SetParent(value ?? throw new ArgumentNullException());
        }

        // TODO: remove this
        /// <summary>
        /// CloudFormation expression to use when referencing the declaration. It could be a simple reference, a conditional, or an attribute, etc.
        /// </summary>
        public AExpression ReferenceExpression {
            get => _referenceExpression ?? throw new InvalidOperationException();
            set => _referenceExpression = SetParent(value ?? throw new ArgumentNullException());
        }

        /// <summary>
        /// List of declarations on which this declaration depends on.
        /// </summary>
        /// <param name="ReferenceName"></param>
        /// <param name="Conditions"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public IEnumerable<DependencyRecord> Dependencies => _dependencies;

        /// <summary>
        /// List of declarations that depend on this declaration.
        /// </summary>
        /// <typeparam name="ASyntaxNode"></typeparam>
        /// <returns></returns>
        public IEnumerable<DependencyRecord> ReverseDependencies => _reverseDependencies;

        //--- Methods ---
        public void TrackDependency(AItemDeclaration referencedDeclaration, AExpression dependentExpression) {
            if(referencedDeclaration is null) {
                throw new ArgumentNullException(nameof(referencedDeclaration));
            }
            if(dependentExpression is null) {
                throw new ArgumentNullException(nameof(dependentExpression));
            }
            var conditions = FindConditions(dependentExpression);
            _dependencies.Add(new AItemDeclaration.DependencyRecord(referencedDeclaration, conditions, dependentExpression));
            referencedDeclaration._reverseDependencies.Add(new AItemDeclaration.DependencyRecord(
                dependentExpression.ParentItemDeclaration ?? throw new ShouldNeverHappenException(),
                conditions,
                dependentExpression
            ));

            // local functions
            IEnumerable<ConditionBranch> FindConditions(ASyntaxNode node) {
                var conditions = new List<ConditionBranch>();
                ASyntaxNode? child = node;
                foreach(var parent in node.GetParents()) {

                    // check if parent is an !If expression
                    if(parent is IfFunctionExpression ifParent) {

                        // determine if reference came from IfTrue or IfFalse path
                        if(object.ReferenceEquals(child, ifParent.Condition)) {

                            // nothing to do
                        } else if(object.ReferenceEquals(child, ifParent.IfTrue)) {
                            conditions.Add(new ConditionBranch(condition: true, ifParent.Condition));
                        } else if(object.ReferenceEquals(child, ifParent.IfFalse)) {
                            conditions.Add(new ConditionBranch(condition: false, ifParent.Condition));
                        } else {
                            throw new ShouldNeverHappenException();
                        }
                    }
                    child = parent;
                }
                conditions.Reverse();
                return conditions;
            }
        }

        public void TrackMissingDependency(string declarationFullName, ASyntaxNode dependentNode)
            => _missingDependencies.Add(new MissingDependencyRecord(declarationFullName, dependentNode));

        public void UntrackDependency(AExpression dependentExpression)
            => _reverseDependencies.RemoveAll(reverseDependency => reverseDependency.Expression == dependentExpression);

        public void UntrackAllDependencies() {

            // iterate over all dependencies for this declaration and remove itself from the reverse dependencies list
            foreach(var dependency in _dependencies) {

                // TODO: this is what it should be, but not all expressions have `ReferencedDeclaration`; introduce interface we can filter on
                // dependency.Expression.ReferencedDeclaration = null;
                dependency.ReferencedDeclaration
                    ._reverseDependencies
                    .RemoveAll(reverseDependency => reverseDependency.ReferencedDeclaration == this);
            }
            _dependencies.Clear();
        }

        protected override void ParentChanged() {
            base.ParentChanged();

            // reset computed fullname and logical ID
            _fullName = null;
            _logicalId = null;
        }
    }
}