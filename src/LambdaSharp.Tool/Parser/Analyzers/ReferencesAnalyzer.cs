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
using LambdaSharp.Tool.Parser.Syntax;

namespace LambdaSharp.Tool.Parser.Analyzers {

    public class ReferencesAnalyzer : ASyntaxVisitor {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public ReferencesAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) {
            var referenceName = node.ReferenceName.Value;

            // check if reference is to CloudFormation pseudo-parameter
            if(referenceName.StartsWith("AWS::", StringComparison.Ordinal)) {
                _builder.LogError($"{node.ReferenceName} is not a resource", node.SourceLocation);
                return;
            }

            // validate reference
            if(_builder.TryGetProperties(referenceName, out var properties)) {

                // confirm reference is to a referenceable declaration
                switch(properties.Declaration) {
                case FunctionDeclaration _:
                case MacroDeclaration _:
                case NestedModuleDeclaration _:
                case ResourceDeclaration resourceDeclaration when (resourceDeclaration.Value == null): // NOTE: only allowed if not a resource reference
                    properties.ReverseDependencies.Add(node);
                    break;
                case ParameterDeclaration _:
                case ImportDeclaration _:
                case VariableDeclaration _:
                case GroupDeclaration _:
                case ConditionDeclaration _:
                case PackageDeclaration _:
                case MappingDeclaration _:
                case ResourceTypeDeclaration _:
                    _builder.LogError($"identifier {node.ReferenceName} must refer to a CloudFormation resource", node.SourceLocation);
                    return;
                default:
                    throw new ApplicationException($"should never happen: {properties.Declaration?.GetType().Name ?? "<null>"}");
                }

                // find all conditions to reach this node
                var conditions = FindConditions(node);

                // register reference with declaration
                var declaration = node.Parents.OfType<ADeclaration>().First();
                _builder.GetProperties(declaration)
                    .Dependencies.Add((ReferenceName: referenceName, Conditions: conditions, Node: node));
            } else {
                _builder.LogError($"unknown identifier {node.ReferenceName}", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) {
            var referenceName = node.ReferenceName.Value;

            // check if reference is to CloudFormation pseudo-parameter
            if(referenceName.StartsWith("AWS::", StringComparison.Ordinal)) {
                switch(referenceName) {
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
                    _builder.LogError($"unknown identifier {node.ReferenceName}", node.SourceLocation);
                    break;
                }
                return;
            }

            // validate reference
            if(_builder.TryGetProperties(referenceName, out var properties)) {

                // confirm reference is to a referenceable declaration
                switch(properties.Declaration) {
                case FunctionDeclaration _:
                case MacroDeclaration _:
                case NestedModuleDeclaration _:
                case ResourceDeclaration resourceDeclaration when (resourceDeclaration.Value == null): // NOTE: only allowed if not a resource reference
                case ParameterDeclaration _:
                case ImportDeclaration _:
                case VariableDeclaration _:
                case PackageDeclaration _:
                    properties.ReverseDependencies.Add(node);
                    break;
                case GroupDeclaration _:
                case ConditionDeclaration _:
                case MappingDeclaration _:
                case ResourceTypeDeclaration _:
                    _builder.LogError($"identifier {node.ReferenceName} cannot refer to this declaration type", node.SourceLocation);
                    return;
                default:
                    throw new ApplicationException($"should never happen: {properties.Declaration?.GetType().Name ?? "<null>"}");
                }

                // find all conditions to reach this node
                var conditions = FindConditions(node);

                // register reference with declaration
                var declaration = node.Parents.OfType<ADeclaration>().First();
                _builder.GetProperties(declaration)
                    .Dependencies.Add((ReferenceName: referenceName, Conditions: conditions, Node: node));
            } else {
                _builder.LogError($"unknown identifier {node.ReferenceName}", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ConditionNameConditionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetProperties(referenceName, out var properties)) {

                // confirm reference is to a condition declaration
                if(properties.Declaration is ConditionDeclaration) {
                    properties.ReverseDependencies.Add(node);

                    // register reference with declaration
                    var declaration = node.Parents.OfType<ADeclaration>().First();
                    _builder.GetProperties(declaration)
                        .Dependencies.Add((ReferenceName: referenceName, Conditions: Enumerable.Empty<AConditionExpression>(), Node: node));
                } else {
                    _builder.LogError($"identifier {node.ReferenceName} must refer to a Condition", node.SourceLocation);
                }
            } else {
                _builder.LogError($"unknown identifier {node.ReferenceName}", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, MappingNameLiteral node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetProperties(referenceName, out var properties)) {

                // confirm reference is to a condition declaration
                if(properties.Declaration is MappingDeclaration) {
                    properties.ReverseDependencies.Add(node);

                    // register reference with declaration
                    var declaration = node.Parents.OfType<ADeclaration>().First();
                    _builder.GetProperties(declaration)
                        .Dependencies.Add((ReferenceName: referenceName, Conditions: Enumerable.Empty<AConditionExpression>(), Node: node));
                } else {
                    _builder.LogError($"identifier {node.ReferenceName} must refer to a Condition", node.SourceLocation);
                }
            } else {
                _builder.LogError($"unknown identifier {node.ReferenceName}", node.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {
            if(_builder.TryGetProperties(node.Handler.Value, out var properties)) {
                if(properties.Declaration is FunctionDeclaration) {

                    // nothing to do
                } else if((properties.Declaration is ResourceDeclaration resourceDeclaration) && (resourceDeclaration.Type?.Value == "AWS::SNS::Topic")) {

                    // nothing to do
                } else {
                    _builder.LogError($"Handler must reference a Function or AWS::SNS::Topic resource declaration", node.Handler.SourceLocation);
                }
            } else {
                _builder.LogError($"unknown identifier {node.Handler.Value}", node.Handler.SourceLocation);
            }
        }

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) {
            if(_builder.TryGetProperties(node.Handler.Value, out var properties)) {
                if(!(properties.Declaration is FunctionDeclaration)) {
                    _builder.LogError($"Handler must reference a Function declaration", node.Handler.SourceLocation);
                }
            } else {
                _builder.LogError($"unknown identifier {node.Handler.Value}", node.Handler.SourceLocation);
            }
        }

        private IEnumerable<AConditionExpression> FindConditions(ASyntaxNode node) {
            var conditions = new List<AConditionExpression>();
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
                        throw new ApplicationException("this shouldn't happen");
                    }
                }
                previousParent = parent;
            }
            conditions.Reverse();
            return conditions;
        }
    }
}
