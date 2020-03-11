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

#nullable disable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Tool.Compiler.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class LinkReferencesAnalyzer : ASyntaxAnalyzer {

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
        public LinkReferencesAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override bool VisitStart(GetAttFunctionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {
                if(node.ParentItemDeclaration is ConditionDeclaration) {
                    _builder.Log(Error.GetAttCannotBeUsedInAConditionDeclaration, node);
                }
                if(referencedDeclaration is IResourceDeclaration resourceDeclaration) {

                    // NOTE (2020-01-29, bjorg): we only need this check because 'ResourceDeclaration' can have an explicit resource ARN vs. being an instance of a resource
                    if(resourceDeclaration.CloudFormationType == null) {
                        _builder.Log(Error.ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                    } else {
                        node.ReferencedDeclaration = referencedDeclaration;
                    }
                } else {
                    _builder.Log(Error.ReferenceMustBeResourceInstance(referenceName.Value), referenceName);
                }
            } else {

                // TODO: track missing references
                _builder.Log(Error.ReferenceDoesNotExist(node.ReferenceName.Value), node);
            }
            return true;
        }

        public override bool VisitStart(ReferenceFunctionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {
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
                        _builder.Log(Error.ReferenceMustBeParameter(referenceName.Value), referenceName);
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
                        _builder.Log(Error.ReferenceMustBeResourceOrParameterOrVariable(referenceName.Value), referenceName);
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

                // TODO: track missing references
                _builder.Log(Error.ReferenceDoesNotExist(node.ReferenceName.Value), node);
            }
            return true;
        }

        public override bool VisitStart(SubFunctionExpression node) {

            // NOTE (2019-12-07, bjorg): convert all nested !Ref and !GetAtt expressions into
            //  explit expressions using local !Sub parameters; this allows us track these
            //  references as dependencies, as well as allowing us later to analyze
            //  and resolve these references without having to parse the !Sub format string anymore;
            //  during the optimization phase, the !Ref and !GetAtt expressions are inlined again
            //  where possible.

            // replace as many ${VAR} occurrences as possible in the format string
            node.FormatString = new LiteralExpression(ReplaceSubPattern(
                node.FormatString.Value,
                (subReferenceName, subAttributeName, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) => {

                    // compute source location based on line/column offsets
                    var sourceLocation = new SourceLocation(
                        node.FormatString.SourceLocation.FilePath,
                        node.FormatString.SourceLocation.LineNumberStart + startLineOffset,
                        node.FormatString.SourceLocation.LineNumberStart + endLineOffset,
                        node.FormatString.SourceLocation.ColumnNumberStart + startColumnOffset,
                        node.FormatString.SourceLocation.ColumnNumberStart + endColumnOffset
                    );

                    // check if reference is to a local !Sub parameter
                    if(node.Parameters.ContainsKey(subReferenceName)) {
                        if(subAttributeName != null) {
                            _builder.Log(Error.SubFunctionParametersCannotUseAttributeNotation(subReferenceName), sourceLocation);
                        }
                    } else if(_builder.TryGetItemDeclaration(subReferenceName, out var referencedDeclaration)) {

                        // check if embedded expression is a !Ref or !GetAtt expression
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
                        var argName = $"P{node.Parameters.Count}";
                        node.Parameters[argName] = argExpression;

                        // substitute found value as new argument
                        return $"${{{argName}}}";
                    } else {

                        // TODO: track missing references
                        _builder.Log(Error.ReferenceDoesNotExist(subReferenceName), sourceLocation);
                    }
                    return null;
                }
            )) {
                SourceLocation = node.FormatString.SourceLocation
            };
            return true;
        }

        public override bool VisitStart(ConditionExpression node) {
            var referenceName = node.ReferenceName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {
                if(referencedDeclaration is ConditionDeclaration conditionDeclaration) {
                    node.ReferencedDeclaration = conditionDeclaration;
                } else {
                    _builder.Log(Error.IdentifierMustReferToAConditionDeclaration(referenceName.Value), referenceName);
                }
            } else {

                // TODO: track missing references
                _builder.Log(Error.ReferenceDoesNotExist(node.ReferenceName.Value), node);
            }
            return true;
        }

        public override bool VisitStart(FindInMapFunctionExpression node) {
            var referenceName = node.MapName;

            // validate reference
            if(_builder.TryGetItemDeclaration(referenceName.Value, out var referencedDeclaration)) {
                if(referencedDeclaration is MappingDeclaration mappingDeclaration) {
                    node.ReferencedDeclaration = mappingDeclaration;
                } else {
                    _builder.Log(Error.IdentifierMustReferToAMappingDeclaration(referenceName.Value), referenceName);
                }
            } else {

                // TODO: track missing references
                _builder.Log(Error.ReferenceDoesNotExist(referenceName.Value), node);
            }
            return true;
        }

        public override bool VisitStart(ParameterDeclaration node) {
            ValidateFunctionScope(node.Scope, node);
            return true;
        }

        public override bool VisitStart(ImportDeclaration node) {
            ValidateFunctionScope(node.Scope, node);
            return true;
        }

        public override bool VisitStart(VariableDeclaration node) {
            ValidateFunctionScope(node.Scope, node);
            return true;
        }

        public override bool VisitStart(ResourceDeclaration node) {
            ValidateFunctionScope(node.Scope, node);
            return true;
        }

        public override bool VisitStart(PackageDeclaration node) {
            ValidateFunctionScope(node.Scope, node);
            return true;
        }

        public override bool VisitStart(FunctionDeclaration node) {
            ValidateFunctionScope(node.Scope, node);
            return true;
        }

        public override bool VisitStart(ResourceTypeDeclaration node) {
            if(_builder.TryGetItemDeclaration(node.Handler.Value, out var referencedDeclaration)) {
                if(referencedDeclaration is FunctionDeclaration) {

                    // nothing to do
                } else if((referencedDeclaration is ResourceDeclaration resourceDeclaration) && (resourceDeclaration.Type?.Value == "AWS::SNS::Topic")) {

                    // nothing to do
                } else {
                    _builder.Log(Error.HandlerMustBeAFunctionOrSnsTopic, node.Handler);
                }
            } else {

                // TODO: track missing references
                _builder.Log(Error.ReferenceDoesNotExist(node.Handler.Value), node);
            }
            return true;
        }

        public override bool VisitStart(MacroDeclaration node) {
            if(_builder.TryGetItemDeclaration(node.Handler.Value, out var referencedDeclaration)) {
                if(!(referencedDeclaration is FunctionDeclaration)) {
                    _builder.Log(Error.HandlerMustBeAFunction, node.Handler);
                }
            } else {

                // TODO: track missing references
                _builder.Log(Error.ReferenceDoesNotExist(node.Handler.Value), node);
            }
            return true;
        }

        private void ValidateFunctionScope(SyntaxNodeCollection<LiteralExpression> scopeExpression, AItemDeclaration declaration) {
            foreach(var innerScopeExpression in scopeExpression) {
                ValidateScope(innerScopeExpression);
            }

            // validate functions
            void ValidateScope(LiteralExpression scope) {
                switch(scope.Value) {
                case "*":
                case "all":

                    // nothing to do; wildcards are valid even when no functions are defined
                    break;
                case "public":

                    // nothing to do; 'public' is a reserved scope keyword
                    break;
                default:
                    if(_builder.TryGetItemDeclaration(scope.Value, out var scopeReferenceDeclaration)) {
                        if(!(scopeReferenceDeclaration is FunctionDeclaration)) {
                            _builder.Log(Error.ReferenceMustBeFunction(scope.Value), scope);
                        } else if(scopeReferenceDeclaration == declaration) {
                            _builder.Log(Error.ReferenceCannotBeSelf(scope.Value), scope);
                        }
                    } else {

                        // TODO: track missing references
                        _builder.Log(Error.ReferenceDoesNotExist(scope.Value), scope);
                    }
                    break;
                }
            }
        }
    }
}
