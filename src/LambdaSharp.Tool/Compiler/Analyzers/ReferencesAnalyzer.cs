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
using System.Text.RegularExpressions;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class ReferencesAnalyzer : ASyntaxAnalyzer {

        //--- Constants ---
        private const string SUBVARIABLE_PATTERN = @"\$\{(?!\!)[^\}]+\}";

        //--- Class Methods ---
        private static string ReplaceSubPattern(string subPattern, Func<string, string, int, int, int, int, string> replace)
            => Regex.Replace(subPattern, SUBVARIABLE_PATTERN, match => {
                var matchText = match.ToString();
                var name = matchText.Substring(2, matchText.Length - 3).Trim().Split('.', 2);
                var key = name[0].Trim();
                var suffix = (name.Length == 2) ? name[1].Trim() : null;
                var startLineOffset = subPattern.Take(match.Index).Count(c => c == '\n');
                var endLineOffset = subPattern.Take(match.Index + matchText.Length).Count(c => c == '\n');
                var startColumnOffset = subPattern.Take(match.Index).Reverse().TakeWhile(c => c != '\n').Count();
                var endColumnOffset = subPattern.Take(match.Index + matchText.Length).Reverse().TakeWhile(c => c != '\n').Count();
                return replace(key, suffix, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) ?? matchText;
            });

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public ReferencesAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) {
            var referenceName = node.ReferenceName.Value;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName, out var referencedDeclaration)) {
                node.ParentItemDeclaration.Dependencies.Add((ReferenceName: referenceName, Conditions: FindConditions(node), Expression: node));
                referencedDeclaration.ReverseDependencies.Add(node);
            } else {
                _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {
                node.ParentItemDeclaration.Dependencies.Add((ReferenceName: referenceName.Value, Conditions: FindConditions(node), Expression: node));
                referencedDeclaration.ReverseDependencies.Add(node);
            } else {
                _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, SubFunctionExpression node) {

            // NOTE (2019-12-07, bjorg): convert all nested !Ref and !GetAtt expressions into
            //  explit expressions using local !Sub parameters; this allows us track these
            //  references as dependencies, as well as allowing us later to analyze
            //  and resolve these references without having to parse the !Sub format string anymore;
            //  during the optimization phase, the !Ref and !GetAtt expressions are inlined again
            //  where possible.

            // replace as many ${VAR} occurrences as possible in the format string
            node.FormatString.Value = ReplaceSubPattern(
                node.FormatString.Value,
                (subReferenceName, subAttributeName, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) => {

                    // compute source location based on line/column offsets
                    var sourceLocation = new Parser.SourceLocation {
                        FilePath = node.FormatString.SourceLocation.FilePath,
                        LineNumberStart = node.FormatString.SourceLocation.LineNumberStart + startLineOffset,
                        LineNumberEnd = node.FormatString.SourceLocation.LineNumberStart + endLineOffset,
                        ColumnNumberStart = node.FormatString.SourceLocation.ColumnNumberStart + startColumnOffset,
                        ColumnNumberEnd = node.FormatString.SourceLocation.ColumnNumberStart + endColumnOffset
                    };

                    // check if reference is to a local !Sub parameter
                    if(node.Parameters.ContainsKey(subReferenceName)) {
                        if(subAttributeName != null) {
                            _builder.Log(Error.SubFunctionParametersCannotUseAttributeNotation(subReferenceName), sourceLocation);
                        }
                    } else if(_builder.TryGetItemDeclaration(subReferenceName, out var referencedDeclaration)) {

                        // check if embedded expression is a !Ref or !GetAtt expression
                        var argName = $"P{node.Parameters.Count}";
                        AExpression argExpression;
                        if(subAttributeName == null) {

                            // create explicit !Ref expression
                            argExpression = new ReferenceFunctionExpression {
                                SourceLocation = sourceLocation,
                                ReferenceName = ASyntaxAnalyzer.Literal(subReferenceName)
                            };
                        } else {

                            // create explicit !GetAtt expression
                            argExpression = new GetAttFunctionExpression {
                                SourceLocation = sourceLocation,
                                ReferenceName = ASyntaxAnalyzer.Literal(subReferenceName),
                                AttributeName = ASyntaxAnalyzer.Literal(subAttributeName)
                            };
                        }

                        // move the resolved expression into !Sub parameters
                        node.Parameters[argName] = argExpression;
                        argExpression.Visit(node.Parameters, new SyntaxHierarchyAnalyzer(_builder));

                        // substitute found value as new argument
                        return $"${{{argName}}}";
                    } else {
                        _builder.Log(Error.UnknownIdentifier(subReferenceName), sourceLocation);
                    }
                    return null;
                }
            );
        }

        public override void VisitStart(ASyntaxNode parent, ConditionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {
                referencedDeclaration.ReverseDependencies.Add(node);
                node.ParentItemDeclaration.Dependencies.Add((ReferenceName: referenceName.Value, Conditions: Enumerable.Empty<AExpression>(), Expression: node));
            } else {
                _builder.Log(Error.UnknownIdentifier(node.ReferenceName.Value), node);
            }
        }

        public override void VisitStart(ASyntaxNode parent, FindInMapFunctionExpression node) {
            var referenceName = node.MapName.Value;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName, out var referencedDeclaration)) {
                referencedDeclaration.ReverseDependencies.Add(node);
                node.ParentItemDeclaration.Dependencies.Add((ReferenceName: referenceName, Conditions: Enumerable.Empty<AExpression>(), Expression: node));
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
