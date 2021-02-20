/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class MappingDeclarationProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public MappingDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {
                moduleDeclaration.InspectType<MappingDeclaration>(node => {

                    // check if object expression is valid (must have first- and second-level keys)
                    if(node.Value.Any()) {
                        var topLevelKeys = new HashSet<string>();
                        var secondLevelKeys = new HashSet<string>();

                        // check that all first-level keys have object expressions
                        foreach(var topLevelEntry in node.Value) {

                            // validate top-level key

                            if(!CloudFormationValidationRules.IsValidCloudFormationName(topLevelEntry.Key.Value)) {
                                Logger.Log(Error.MappingKeyMustBeAlphanumeric, topLevelEntry.Key);
                            }
                            if(!topLevelKeys.Add(topLevelEntry.Key.Value)) {
                                Logger.Log(Error.MappingDuplicateKey, topLevelEntry.Key);
                            }

                            // validate top-level value
                            if(topLevelEntry.Value is ObjectExpression secondLevelObjectExpression) {
                                if(secondLevelObjectExpression.Any()) {
                                    secondLevelKeys.Clear();

                                    // check that all second-level keys have literal expressions
                                    foreach(var secondLevelEntry in secondLevelObjectExpression) {

                                        // validate top-level key
                                        if(!CloudFormationValidationRules.IsValidCloudFormationName(secondLevelEntry.Key.Value)) {
                                            Logger.Log(Error.MappingKeyMustBeAlphanumeric, secondLevelEntry.Key);
                                        }
                                        if(!secondLevelKeys.Add(secondLevelEntry.Key.Value)) {
                                            Logger.Log(Error.MappingDuplicateKey, secondLevelEntry.Key);
                                        }

                                        // validate second-level value
                                        if(!IsListOrLiteral(secondLevelEntry.Value)) {
                                            Logger.Log(Error.MappingExpectedListOrLiteral, secondLevelEntry.Value);
                                        }
                                    }
                                } else {
                                    Logger.Log(Error.MappingDeclarationSecondLevelIsMissing, secondLevelObjectExpression);
                                }
                            } else {
                                Logger.Log(Error.ExpectedMapExpression, topLevelEntry.Value);
                            }
                        }
                    } else {
                        Logger.Log(Error.MappingDeclarationTopLevelIsMissing, node);
                    }

                    // local functions
                    bool IsListOrLiteral(AExpression value)
                        => (value is LiteralExpression)
                            || ((value is ListExpression listExpression) && listExpression.All(item => IsListOrLiteral(item)));
            });
        }
    }
}