/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class ReferenceValidator : AValidator {

        //--- Constructors ---
        public ReferenceValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration, Dictionary<string, AItemDeclaration> declarations) {

            // check for referential integrity
            moduleDeclaration.Inspect(node => {
                switch(node) {
                case GetAttFunctionExpression getAttFunctionExpression:
                    ValidateGetAttFunctionExpression(getAttFunctionExpression);
                    break;
                case ReferenceFunctionExpression referenceFunctionExpression:
                    ValidateReferenceExpression(referenceFunctionExpression);
                    break;
                case ConditionReferenceExpression conditionExpression:
                    ValidateConditionExpression(conditionExpression);
                    break;
                case FindInMapFunctionExpression findInMapFunctionExpression:
                    ValidateFindInMapFunctionExpression(findInMapFunctionExpression);
                    break;
                }
            });

            // check for circular dependencies
            DetectCircularDependencies(declarations.Values);
            return;

            // local functions
            void ValidateGetAttFunctionExpression(GetAttFunctionExpression node) {
                var referenceName = node.ReferenceName;

                // validate reference
                if(declarations.TryGetValue(referenceName.Value, out var referencedDeclaration)) {
                    if(node.ParentItemDeclaration is ConditionDeclaration) {
                        Logger.Log(Error.GetAttCannotBeUsedInAConditionDeclaration, node);
                    }
                    if(referencedDeclaration is IResourceDeclaration resourceDeclaration) {

                        // NOTE (2020-01-29, bjorg): we only need this check because 'ResourceDeclaration' can have an explicit resource ARN vs. being an instance of a resource
                        if(resourceDeclaration.CloudFormationType == null) {
                            Logger.Log(Error.ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                        } else {
                            node.ReferencedDeclaration = referencedDeclaration;
                            ValidateConditionalReferences(node.ParentItemDeclaration, referencedDeclaration);
                        }
                    } else {
                        Logger.Log(Error.ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                    }
                } else {
                    Logger.Log(Error.ReferenceDoesNotExist(node.ReferenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.ReferenceName.Value, node);
                }
            }

            void ValidateReferenceExpression(ReferenceFunctionExpression node) {
                var referenceName = node.ReferenceName;

                // validate reference
                if(declarations.TryGetValue(referenceName.Value, out var referencedDeclaration)) {
                    if(node.ParentItemDeclaration is ConditionDeclaration) {

                        // validate the declaration type
                        switch(referencedDeclaration) {
                        case ConditionDeclaration _:
                        case MappingDeclaration _:
                        case ResourceTypeDeclaration _:
                        case GroupDeclaration _:
                        case VariableDeclaration _:
                        case PackageDeclaration _:
                        case FunctionDeclaration _:
                        case MacroDeclaration _:
                        case NestedModuleDeclaration _:
                        case ResourceDeclaration _:
                        case ImportDeclaration _:
                            Logger.Log(Error.ReferenceMustBeParameter(referenceName.Value), referenceName);
                            break;
                        case ParameterDeclaration _:
                        case PseudoParameterDeclaration _:
                            node.ReferencedDeclaration = referencedDeclaration;
                            ValidateConditionalReferences(node.ParentItemDeclaration, referencedDeclaration);
                            break;
                        default:
                            throw new ShouldNeverHappenException($"unsupported type: {referencedDeclaration?.GetType().Name ?? "<null>"}");
                        }
                    } else {

                        // validate the declaration type
                        switch(referencedDeclaration) {
                        case ConditionDeclaration _:
                        case MappingDeclaration _:
                        case ResourceTypeDeclaration _:
                        case GroupDeclaration _:
                            Logger.Log(Error.ReferenceMustBeResourceOrParameterOrVariable(referenceName.Value), referenceName);
                            break;
                        case ParameterDeclaration _:
                        case PseudoParameterDeclaration _:
                        case VariableDeclaration _:
                        case PackageDeclaration _:
                        case FunctionDeclaration _:
                        case MacroDeclaration _:
                        case NestedModuleDeclaration _:
                        case ResourceDeclaration _:
                        case ImportDeclaration _:
                            node.ReferencedDeclaration = referencedDeclaration;
                            ValidateConditionalReferences(node.ParentItemDeclaration, referencedDeclaration);
                            break;
                        default:
                            throw new ShouldNeverHappenException($"unsupported type: {referencedDeclaration?.GetType().Name ?? "<null>"}");
                        }
                    }
                } else {
                    Logger.Log(Error.ReferenceDoesNotExist(node.ReferenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.ReferenceName.Value, node);
                }
            }

            void ValidateConditionExpression(ConditionReferenceExpression node) {
                var referenceName = node.ReferenceName;

                // validate reference
                if(declarations.TryGetValue(referenceName.Value, out var referencedDeclaration)) {
                    if(referencedDeclaration is ConditionDeclaration conditionDeclaration) {
                        node.ReferencedDeclaration = conditionDeclaration;
                        ValidateConditionalReferences(node.ParentItemDeclaration, referencedDeclaration);
                    } else {
                        Logger.Log(Error.IdentifierMustReferToAConditionDeclaration(referenceName.Value), referenceName);
                    }
                } else {
                    Logger.Log(Error.ReferenceDoesNotExist(node.ReferenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.ReferenceName.Value, node);
                }
            }

            void ValidateFindInMapFunctionExpression(FindInMapFunctionExpression node) {
                var referenceName = node.MapName;

                // validate reference
                if(declarations.TryGetValue(referenceName.Value, out var referencedDeclaration)) {
                    if(referencedDeclaration is MappingDeclaration mappingDeclaration) {
                        node.ReferencedDeclaration = mappingDeclaration;
                        ValidateConditionalReferences(node.ParentItemDeclaration, referencedDeclaration);
                    } else {
                        Logger.Log(Error.IdentifierMustReferToAMappingDeclaration(referenceName.Value), referenceName);
                    }
                } else {
                    Logger.Log(Error.ReferenceDoesNotExist(referenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(referenceName.Value, node);
                }
            }
        }

        private void DetectCircularDependencies(IEnumerable<AItemDeclaration> declarations) {

            // TODO: make sure that 'DependsOn' dependencies are tracked as well

            var visited = new List<AItemDeclaration>();
            var cycles = new List<IEnumerable<AItemDeclaration>>();
            foreach(var declaration in declarations) {
                Visit(declaration, visited);
            }

            // report all unique cycles found
            if(cycles.Any()) {
                var fingerprints = new HashSet<string>();
                foreach(var cycle in cycles) {
                    var path = cycle.Select(dependency => dependency.FullName).ToList();

                    // NOTE (2020-06-09, bjorg): cycles are reported for each node in the cycle; however, the nodes in the cycle
                    //  form a unique fingerprint to avoid duplicate reporting duplicate cycles
                    var fingerprint = string.Join(",", path.OrderBy(fullname => fullname));
                    if(fingerprints.Add(fingerprint)) {
                        Logger.Log(Error.CircularDependencyDetected(string.Join(" -> ", path.Append(path.First()))), cycle.First());
                    }
                }
            }

            // local functions
            void Visit(AItemDeclaration declaration, List<AItemDeclaration> visited) {

                // check if we detected a cycle
                var index = visited.IndexOf(declaration);
                if(index >= 0) {

                    // extract sub-list that represents cycle since cycle may not point back to the first element
                    cycles.Add(visited.GetRange(index, visited.Count - index));
                    return;
                }

                // recurse into every dependency, until we exhaust the tree or find a circular dependency
                visited.Add(declaration);
                foreach(var dependency in declaration.Dependencies) {
                    Visit(dependency.ReferencedDeclaration, visited);
                }
                visited.Remove(declaration);
            }
        }

        // TODO: make AItemDeclaration? non-nullable once ParentItemDeclaration is fixed
        private void ValidateConditionalReferences(AItemDeclaration? referrerDeclaration, AItemDeclaration? refereeDeclaration) {

            // check if referee always exist; if so, there is nothing to check
            var refereeCondition = (refereeDeclaration as IConditionalResourceDeclaration)?.If;
            if(refereeCondition == null) {

                // nothing to check
                return;
            }

            // check if referrer can have a condition
            if(referrerDeclaration is IConditionalResourceDeclaration conditionalResourceDeclaration) {

                // TODO: check if referrer has as non-weaker condition
            }
        }
    }
}