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
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class ExpressionNormalization : AValidator {

        //--- Constructors ---
        public ExpressionNormalization(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Normalize(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.Inspect(node => {
                switch(node) {
                case SubFunctionExpression subFunctionExpression:
                    NormalizeSubFunctionExpression(subFunctionExpression);
                    break;
                }
            });

            // local functions
            void NormalizeSubFunctionExpression(SubFunctionExpression node) {

                // NOTE (2019-12-07, bjorg): convert all nested !Ref and !GetAtt expressions into
                //  explit expressions using local !Sub parameters; this allows us track these
                //  references as dependencies, as well as allowing us later to analyze
                //  and resolve these references without having to parse the !Sub format string anymore;
                //  during the optimization phase, the !Ref and !GetAtt expressions are inlined again
                //  where possible.

                // replace as many ${VAR} occurrences as possible in the format string
                var replaceFormatString =  ReplaceSubPattern(
                    node.FormatString.Value,
                    (reference, attribute, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) => {

                        // compute source location based on line/column offsets
                        var sourceLocation = new SourceLocation(
                            node.FormatString.SourceLocation.FilePath,
                            node.FormatString.SourceLocation.LineNumberStart + startLineOffset,
                            node.FormatString.SourceLocation.LineNumberStart + endLineOffset,
                            node.FormatString.SourceLocation.ColumnNumberStart + startColumnOffset,
                            node.FormatString.SourceLocation.ColumnNumberStart + endColumnOffset
                        );

                        // check if reference is to a local !Sub parameter
                        if(node.Parameters.ContainsKey(reference)) {

                            // local references cannot have an attribute suffix
                            if(attribute != null) {
                                Logger.Log(Error.SubFunctionParametersCannotUseAttributeNotation(reference), sourceLocation);
                            }
                        }

                        // check if embedded expression is a !Ref or !GetAtt expression
                        AExpression argExpression;
                        if(attribute == null) {

                            // create explicit !Ref expression
                            argExpression = new ReferenceFunctionExpression {
                                SourceLocation = sourceLocation,
                                ReferenceName = Fn.Literal(reference)
                            };
                        } else {

                            // create explicit !GetAtt expression
                            argExpression = new GetAttFunctionExpression {
                                SourceLocation = sourceLocation,
                                ReferenceName = Fn.Literal(reference),
                                AttributeName = Fn.Literal(attribute)
                            };
                        }

                        // move the resolved expression into !Sub parameters
                        var argName = $"P{node.Parameters.Count}";
                        node.Parameters[argName] = argExpression;

                        // substitute found value as new argument
                        return "${" + argName + "}";
                    }
                );
                node.FormatString = Fn.Literal(replaceFormatString, node.FormatString.SourceLocation);
            }

            string ReplaceSubPattern(string subPattern, Func<string, string?, int, int, int, int, string> replace) {
                return Regex.Replace(subPattern, @"\$\{(?!\!)[^\}]+\}", match => {

                    // parse matche expression into Reference.Attribute
                    var matchText = match.ToString();
                    var namePair = matchText
                        .Substring(2, matchText.Length - 3)
                        .Trim()
                        .Split('.', 2);
                    var reference = namePair[0].Trim();
                    var attribute = (namePair.Length == 2) ? namePair[1].Trim() : null;

                    // compute matched expression position
                    var startLineOffset = subPattern.Take(match.Index).Count(c => c == '\n');
                    var endLineOffset = subPattern.Take(match.Index + matchText.Length).Count(c => c == '\n');
                    var startColumnOffset = subPattern.Take(match.Index).Reverse().TakeWhile(c => c != '\n').Count();
                    var endColumnOffset = subPattern.Take(match.Index + matchText.Length).Reverse().TakeWhile(c => c != '\n').Count();

                    // invoke callback
                    return replace(reference, attribute, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) ?? matchText;
                });
            }
        }
    }
}