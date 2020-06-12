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
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Processors {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ReferenceProcessor : AProcessor {

        //--- Class Fields ---
        #region Errors/Warnings
        private static readonly ErrorFunc ReferenceMustBeParameter = parameter => new Error(0, $"{parameter} must be a parameter");
        private static readonly ErrorFunc ReferenceDoesNotExist = parameter => new Error(0, $"undefined reference to {parameter}");
        private static readonly ErrorFunc ReferenceMustBeResourceInstance = parameter => new Error(0, $"{parameter} must be a CloudFormation stack resource");
        private static readonly Error GetAttCannotBeUsedInAConditionDeclaration = new Error(0, "condition cannot use !GetAtt function");
        private static readonly ErrorFunc ReferenceMustBeResourceOrParameterOrVariable = parameter => new Error(0, $"{parameter} must be a resource, parameter, or variable");
        private static readonly ErrorFunc IdentifierMustReferToAConditionDeclaration = parameter => new Error(0, $"identifier {parameter} must refer to a Condition");
        private static readonly ErrorFunc IdentifierMustReferToAMappingDeclaration = parameter => new Error(0, $"identifier {parameter} must refer to a Mapping");
        private static readonly ErrorFunc CircularDependencyDetected = parameter => new Error(0, $"circular dependency {parameter}");
        #endregion

        //--- Constructors ---
        public ReferenceProcessor(IProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {

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
                case ResourceDeclaration resourceDeclaration:
                    ValidateDependsOn(resourceDeclaration, resourceDeclaration.DependsOn);
                    break;
                case NestedModuleDeclaration nestedModuleDeclaration:
                    ValidateDependsOn(nestedModuleDeclaration, nestedModuleDeclaration.DependsOn);
                    break;
                }
            });

            // check for circular dependencies
            DetectCircularDependencies(Provider.Declarations);

            // TODO: check reference integrity for conditional resources
            //  Build a truth table for: RefereeDeclaration implies ReferencedDeclaration [A |- B => !A || B]
            //  One column per "fact" (i.e. !if condition name/expression)
            //  Any row with an outcome of 'false' is a situation where the referenced declaration does not exist, but the referee does

            return;

            // local functions
            void ValidateGetAttFunctionExpression(GetAttFunctionExpression node) {
                var referenceName = node.ReferenceName;

                // validate reference
                if(Provider.TryGetItem(referenceName.Value, out var referencedDeclaration)) {
                    if(node.ParentItemDeclaration is ConditionDeclaration) {
                        Logger.Log(GetAttCannotBeUsedInAConditionDeclaration, node);
                    }
                    if(referencedDeclaration is IResourceDeclaration resourceDeclaration) {

                        // NOTE (2020-01-29, bjorg): we only need this check because 'ResourceDeclaration' can have an explicit resource ARN vs. being an instance of a resource
                        if(resourceDeclaration.HasPhysicalId) {
                            node.ReferencedDeclaration = referencedDeclaration;
                        } else {
                            Logger.Log(ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                        }
                    } else {
                        Logger.Log(ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                    }
                } else {
                    Logger.Log(ReferenceDoesNotExist(node.ReferenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.ReferenceName.Value, node);
                }
            }

            void ValidateReferenceExpression(ReferenceFunctionExpression node) {
                var referenceName = node.ReferenceName;

                // validate reference
                if(Provider.TryGetItem(referenceName.Value, out var referencedDeclaration)) {
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
                            Logger.Log(ReferenceMustBeParameter(referenceName.Value), referenceName);
                            break;
                        case ParameterDeclaration _:
                        case PseudoParameterDeclaration _:
                            node.ReferencedDeclaration = referencedDeclaration;
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
                            Logger.Log(ReferenceMustBeResourceOrParameterOrVariable(referenceName.Value), referenceName);
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
                            break;
                        default:
                            throw new ShouldNeverHappenException($"unsupported type: {referencedDeclaration?.GetType().Name ?? "<null>"}");
                        }
                    }
                } else {
                    Logger.Log(ReferenceDoesNotExist(node.ReferenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.ReferenceName.Value, node);
                }
            }

            void ValidateConditionExpression(ConditionReferenceExpression node) {
                var referenceName = node.ReferenceName;

                // validate reference
                if(Provider.TryGetItem(referenceName.Value, out var referencedDeclaration)) {
                    if(referencedDeclaration is ConditionDeclaration conditionDeclaration) {
                        node.ReferencedDeclaration = conditionDeclaration;
                    } else {
                        Logger.Log(IdentifierMustReferToAConditionDeclaration(referenceName.Value), referenceName);
                    }
                } else {
                    Logger.Log(ReferenceDoesNotExist(node.ReferenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.ReferenceName.Value, node);
                }
            }

            void ValidateFindInMapFunctionExpression(FindInMapFunctionExpression node) {
                var referenceName = node.MapName;

                // validate reference
                if(Provider.TryGetItem(referenceName.Value, out var referencedDeclaration)) {
                    if(referencedDeclaration is MappingDeclaration mappingDeclaration) {
                        node.ReferencedDeclaration = mappingDeclaration;
                    } else {
                        Logger.Log(IdentifierMustReferToAMappingDeclaration(referenceName.Value), referenceName);
                    }
                } else {
                    Logger.Log(ReferenceDoesNotExist(referenceName.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(referenceName.Value, node);
                }
            }

            void ValidateDependsOn(AItemDeclaration node, SyntaxNodeCollection<LiteralExpression> dependsOn) {
                foreach(var referenceName in dependsOn) {
                    if(Provider.TryGetItem(referenceName.Value, out var referencedDeclaration)) {
                        if(referencedDeclaration is IResourceDeclaration resourceDeclaration) {

                            // NOTE (2020-01-29, bjorg): we only need this check because 'ResourceDeclaration' can have an explicit resource ARN vs. being an instance of a resource
                            if(resourceDeclaration.HasPhysicalId) {
                                node.TrackDependency(referencedDeclaration, referenceName);
                            } else {
                                Logger.Log(ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                            }
                        } else {
                            Logger.Log(ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                        }
                    } else {
                        Logger.Log(ReferenceDoesNotExist(referenceName.Value), node);
                        node.TrackMissingDependency(referenceName.Value, node);
                    }
                }
            }
        }

        private void DetectCircularDependencies(IEnumerable<AItemDeclaration> declarations) {
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
                        Logger.Log(CircularDependencyDetected(string.Join(" -> ", path.Append(path.First()))), cycle.First());
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
                foreach(var reference in declaration.Dependencies.Select(dependency => dependency.ReferencedDeclaration).Distinct()) {
                    Visit(reference, visited);
                }
                visited.Remove(declaration);
            }
        }
    }
}