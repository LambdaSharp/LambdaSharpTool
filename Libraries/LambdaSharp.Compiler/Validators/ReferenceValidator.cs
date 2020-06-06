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
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class ReferenceValidator : AValidator {

        //--- Constructors ---
        public ReferenceValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration, Dictionary<string, AItemDeclaration> declarations) {

            // check for referential integrity
            moduleDeclaration.InspectNode(node => {
                switch(node) {
                case GetAttFunctionExpression getAttFunctionExpression:
                    ValidateGetAttFunctionExpression(getAttFunctionExpression);
                    break;
                case ReferenceFunctionExpression referenceFunctionExpression:
                    ValidateReferenceExpression(referenceFunctionExpression);
                    break;
                case ConditionExpression conditionExpression:
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

            void ValidateConditionExpression(ConditionExpression node) {
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
            var visited = new HashSet<AItemDeclaration>();
            var found = new List<AItemDeclaration>();

            // build list of discovered declarations in reverse discovery order
            foreach(var declaration in declarations) {
                Visit(declaration, current => current.Dependencies, visited, found);
            }

            // revisit discovered delcarations in reverse order to determine circular dependencies
            visited.Clear();
            foreach(var declaration in Enumerable.Reverse(found)) {
                var circularDependencies = new List<AItemDeclaration>();
                Visit(declaration, current => declaration.ReverseDependencies, visited, circularDependencies);
                if(circularDependencies.Count > 1) {
                    var circularPath = string.Join(" -> ", circularDependencies.Select(dependency => dependency.FullName));
                    Logger.Log(Error.CircularDependencyDetected(circularPath), circularDependencies.First());
                }
            }

            // local functions
            void Visit(
                AItemDeclaration declaration,
                Func<AItemDeclaration, IEnumerable<AItemDeclaration.DependencyRecord>> getDependencies,
                HashSet<AItemDeclaration> visited,
                List<AItemDeclaration> finished
            ) {

                // check if declaration has already been visited
                if(!visited.Add(declaration)) {
                    return;
                }

                // recurse over all declarations this declaration is dependent on
                foreach(var dependency in getDependencies(declaration)) {
                    Visit(dependency.ReferencedDeclaration, getDependencies, visited, finished);
                }
                finished.Add(declaration);
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