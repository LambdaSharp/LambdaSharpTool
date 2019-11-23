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
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class ReferencesAnalyzer : ASyntaxAnalyzer {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public ReferencesAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) {
            var referenceName = node.ReferenceName.Value;

            // check if reference is to CloudFormation pseudo-parameter
            if(referenceName.StartsWith("AWS::", StringComparison.Ordinal)) {
                _builder.Log(Error.NameIsNotAResource(node.ReferenceName.Value), node);
                return;
            }

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName, out var referencedDeclaration)) {

                // confirm reference is to a referenceable declaration
                switch(referencedDeclaration) {
                case FunctionDeclaration _:
                case MacroDeclaration _:
                case NestedModuleDeclaration _:
                case ResourceDeclaration resourceDeclaration when (resourceDeclaration.Value == null): // NOTE: only allowed if not a resource reference
                    node.ReferencedDeclaration = referencedDeclaration;
                    referencedDeclaration.ReverseDependencies.Add(node);
                    break;
                case ParameterDeclaration _:
                case ImportDeclaration _:
                case VariableDeclaration _:
                case GroupDeclaration _:
                case ConditionDeclaration _:
                case PackageDeclaration _:
                case MappingDeclaration _:
                case ResourceTypeDeclaration _:
                    _builder.Log(Error.NameMustBeACloudFormationResource(node.ReferenceName.Value), node);
                    return;
                default:
                    throw new ShouldNeverHappenException($"unexpected type: {referencedDeclaration.GetType().Name}");
                }

                // find all conditions to reach the !GetAtt expression
                var conditions = FindConditions(node);

                // register reference with referenced declaration
                var declaration = node.Parents.OfType<AItemDeclaration>().First();
                declaration.Dependencies.Add((ReferenceName: referenceName, Conditions: conditions, Node: node));
            } else {
                _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) {
            var referenceName = node.ReferenceName;

            // check if reference is to CloudFormation pseudo-parameter
            if(referenceName.Value.StartsWith("AWS::", StringComparison.Ordinal)) {
                switch(referenceName.Value) {
                case "AWS::AccountId":
                case "AWS::NotificationARNs":
                case "AWS::NoValue":
                case "AWS::Partition":
                case "AWS::Region":
                case "AWS::StackId":
                case "AWS::StackName":
                case "AWS::URLSuffix":

                    // nothing to do
                    return;
                default:
                    _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
                    break;
                }
                return;
            }

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {

                // confirm reference is to a referenceable declaration
                switch(referencedDeclaration) {
                case FunctionDeclaration _:
                case MacroDeclaration _:
                case NestedModuleDeclaration _:
                case ResourceDeclaration resourceDeclaration when (resourceDeclaration.Value == null): // NOTE: only allowed if not a resource reference
                case ParameterDeclaration _:
                case ImportDeclaration _:
                case VariableDeclaration _:
                case PackageDeclaration _:
                    node.ReferencedDeclaration = referencedDeclaration;
                    referencedDeclaration.ReverseDependencies.Add(node);
                    break;
                case GroupDeclaration _:
                case ConditionDeclaration _:
                case MappingDeclaration _:
                case ResourceTypeDeclaration _:
                    _builder.Log(Error.IdentifierReferesToInvalidDeclarationType(node.ReferenceName.Value), node);
                    return;
                default:
                    throw new ApplicationException($"should never happen: {referencedDeclaration.GetType().Name}");
                }

                // find all conditions to reach this node
                var conditions = FindConditions(node);

                // register reference with declaration
                var declaration = node.Parents.OfType<AItemDeclaration>().First();
                declaration.Dependencies.Add((ReferenceName: referenceName.Value, Conditions: conditions, Node: node));
            } else {
                _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ConditionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {

                // confirm reference is to a condition declaration
                if(referencedDeclaration is ConditionDeclaration referencedConditionDeclaration) {
                    node.ReferencedDeclaration = referencedConditionDeclaration;
                    referencedDeclaration.ReverseDependencies.Add(node);

                    // register reference with declaration
                    var declaration = node.Parents.OfType<AItemDeclaration>().First();
                    declaration.Dependencies.Add((ReferenceName: referenceName.Value, Conditions: Enumerable.Empty<AExpression>(), Node: node));
                } else {
                    _builder.Log(Error.IdentifierMustReferToACondition(node.ReferenceName.Value), node);
                }
            } else {
                _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, FindInMapFunctionExpression node) {
            var referenceName = node.MapName.Value;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName, out var referencedDeclaration)) {

                // confirm reference is to a condition declaration
                if(referencedDeclaration is MappingDeclaration referencedMappingDeclaration) {
                    node.ReferencedDeclaration = referencedMappingDeclaration;
                    referencedDeclaration.ReverseDependencies.Add(node);

                    // register reference with declaration
                    var declaration = node.Parents.OfType<AItemDeclaration>().First();
                    declaration.Dependencies.Add((ReferenceName: referenceName, Conditions: Enumerable.Empty<AExpression>(), Node: node));
                } else {
                    _builder.Log(Error.IdentifierMustReferToACondition(referenceName), node);
                }
            } else {
                _builder.Log(Error.UnknownIdentifier(referenceName), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {
            if(_builder.TryGetItemDeclaration(node.Handler.Value, out var referencedDeclaration)) {
                if(referencedDeclaration is FunctionDeclaration) {

                    // nothing to do
                } else if((referencedDeclaration is ResourceDeclaration resourceDeclaration) && (resourceDeclaration.Type?.Value == "AWS::SNS::Topic")) {

                    // nothing to do
                } else {
                    _builder.Log(Error.HandlerMustBeAFunctionOrSnsTopic, node.Handler);
                }
            } else {
                _builder.Log(Error.UnknownIdentifier(node.Handler.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) {
            if(_builder.TryGetItemDeclaration(node.Handler.Value, out var referencedDeclaration)) {
                if(!(referencedDeclaration is FunctionDeclaration)) {
                    _builder.Log(Error.HandlerMustBeAFunction, node.Handler);
                }
            } else {
                _builder.Log(Error.UnknownIdentifier(node.Handler.Value), node);
            }
        }

        private IEnumerable<AExpression> FindConditions(ASyntaxNode node) {
            var conditions = new List<AExpression>();
            ASyntaxNode previousParent = null;
            foreach(var parent in node.Parents) {

                // check if parent is an !If expression
                if(parent is IfFunctionExpression ifParent) {

                    // determine if reference came from IfTrue or IfFalse path
                    if(object.ReferenceEquals(ifParent.IfTrue, previousParent)) {
                        conditions.Add(ifParent.Condition);
                    } else if(object.ReferenceEquals(ifParent.IfFalse, previousParent)) {

                        // for IfFalse, create a !Not intermediary node
                        conditions.Add(new NotConditionExpression {
                            SourceLocation = ifParent.Condition.SourceLocation,
                            Value = ifParent.Condition
                        });
                    } else {
                        throw new ShouldNeverHappenException();
                    }
                }
                previousParent = parent;
            }
            conditions.Reverse();
            return conditions;
        }
    }
}
