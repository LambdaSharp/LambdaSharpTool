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
using System.Threading.Tasks;
using LambdaSharp.Compiler.Model;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler {

    public class ModuleScope {

        //--- Class Fields ---
        private static Regex ValidResourceNameRegex = new Regex("[a-zA-Z][a-zA-Z0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Fields ---
        private readonly ModuleDeclaration _moduleDeclaration;

        public ModuleScope(ModuleDeclaration moduleDeclaration) {
            _moduleDeclaration = moduleDeclaration ?? throw new System.ArgumentNullException(nameof(moduleDeclaration));
        }

        //--- Constructors ---

        //--- Properties ---
        private ILogger Logger { get; }

        //--- Methods ---
        public async Task ConvertToCloudFormationAsync(ILogger logger) {
            if(logger == null) {
                throw new ArgumentNullException(nameof(logger));
            }

            // TODO: validate module name

            // find all module dependencies
            var dependencies = FindDependencies();
            var cloudformationSpec = _moduleDeclaration.CloudFormation;

            // TODO: download external dependencies

            // normalize AST for analysis
            Normalize();

            // TODO: register local resource types

            // TODO: ensure that all references can be resolved
            // TODO: detect circular references
            // TODO: ensure that constructed resources have all required properties
            // TODO: ensure that referenced attributes exist

            FindReferencableDeclarations();

            // optimize AST
            Optimize();

            throw new NotImplementedException();
        }

        public IEnumerable<(ASyntaxNode node, ModuleManifestDependencyType type, string moduleInfo)> FindDependencies() {
            var result = new List<(ASyntaxNode node, ModuleManifestDependencyType type, string moduleInfo)>();
            _moduleDeclaration.InspectNode(node => {
                switch(node) {
                case ModuleDeclaration moduleDeclaration:

                    // module have an implicit dependency on LambdaSharp.Core@lambdasharp unless explicitly disabled
                    if(moduleDeclaration.HasModuleRegistration) {
                        result.Add((node, ModuleManifestDependencyType.Shared, "LambdaSharp.Core@lambdasharp"));
                    }
                    break;
                case UsingModuleDeclaration usingModuleDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(usingModuleDeclaration.ModuleName.Value, out var usingModuleInfo)) {
                        Logger.Log(Error.ModuleAttributeInvalid, usingModuleDeclaration.ModuleName);
                    } else {

                        // default to deployment bucket as origin when missing
                        if(usingModuleInfo.Origin == null) {
                            usingModuleInfo = usingModuleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                        }

                        // add module reference as a shared dependency
                        result.Add((usingModuleDeclaration, ModuleManifestDependencyType.Shared, usingModuleInfo.ToString()));
                    }
                    break;
                case NestedModuleDeclaration nestedModuleDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(nestedModuleDeclaration.Module?.Value, out var nestedModuleInfo)) {
                        Logger.Log(Error.ModuleAttributeInvalid, (ISyntaxNode?)nestedModuleDeclaration.Module ?? (ISyntaxNode)nestedModuleDeclaration);
                    } else {

                        // default to deployment bucket as origin when missing
                        if(nestedModuleInfo.Origin == null) {
                            nestedModuleInfo = nestedModuleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                        }

                        // add module reference as a nested dependency
                        result.Add((node, ModuleManifestDependencyType.Nested, nestedModuleInfo.ToString()));
                    }
                    break;
                }
            });
            return result;
        }

        public Dictionary<string, ADeclaration> FindReferencableDeclarations() {
            var logicalIds = new HashSet<string>();
            var result = new Dictionary<string, ADeclaration>();
            _moduleDeclaration.InspectNode(node => {
                switch(node) {
                case ParameterDeclaration parameterDeclaration:
                    ValidateItemDeclaration(parameterDeclaration);
                    break;
                case ResourceDeclaration resourceDeclaration:
                    ValidateItemDeclaration(resourceDeclaration);
                    break;
                case FunctionDeclaration functionDeclaration:
                    ValidateItemDeclaration(functionDeclaration);
                    break;
                case VariableDeclaration variableDeclaration:
                    ValidateItemDeclaration(variableDeclaration);
                    break;
                case NestedModuleDeclaration nestedModuleDeclaration:
                    ValidateItemDeclaration(nestedModuleDeclaration);
                    break;
                case ConditionDeclaration conditionDeclaration:
                    ValidateItemDeclaration(conditionDeclaration);
                    break;
                case ImportDeclaration importDeclaration:
                    ValidateItemDeclaration(importDeclaration);
                    break;
                case GroupDeclaration groupDeclaration:
                    ValidateItemDeclaration(groupDeclaration);
                    break;
                case PackageDeclaration packageDeclaration:
                    ValidateItemDeclaration(packageDeclaration);
                    break;
                case MappingDeclaration mappingDeclaration:
                    ValidateItemDeclaration(mappingDeclaration);
                    break;
                case MacroDeclaration macroDeclaration:
                    ValidateItemDeclaration(macroDeclaration);
                    break;
                }
            });
            return result;

            // local functions
            void ValidateItemDeclaration(AItemDeclaration itemDeclaration) {

                // check for reserved names
                if(!ValidResourceNameRegex.IsMatch(itemDeclaration.ItemName.Value)) {
                    Logger.Log(Error.NameMustBeAlphanumeric, itemDeclaration);
                } else if(!result!.TryAdd(itemDeclaration.FullName, itemDeclaration)) {
                    Logger.Log(Error.DuplicateName(itemDeclaration.FullName), itemDeclaration);
                } else if(!logicalIds!.Add(itemDeclaration.LogicalId)) {
                    Logger.Log(Error.AmbiguousLogicalId(itemDeclaration.LogicalId), itemDeclaration.ItemName);
                }
            }
        }

        void Normalize() {
            _moduleDeclaration.InspectNode(node => {
                switch(node) {
                case SubFunctionExpression subFunctionExpression:
                    NormalizeSubFunctionExpression(subFunctionExpression);
                    break;
                }
            });

            // local function
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

        private void Optimize() {

            // TODO: inline !Ref/!GetAtt expressions in !Sub whenever possible
            // TODO: remove any unused resources that can be garbage collected
        }
    }
}