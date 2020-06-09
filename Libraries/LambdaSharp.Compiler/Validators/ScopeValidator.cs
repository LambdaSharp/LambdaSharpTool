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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class ScopeValidator : AValidator {

        //--- Constructors ---
        public ScopeValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration, Dictionary<string, AItemDeclaration> declarations) {
            moduleDeclaration.InspectType<IScopedDeclaration>(node => {
                if(!(node is AItemDeclaration itemDeclaration)) {
                    throw new ShouldNeverHappenException($"unexpected type {node.GetType().FullName}");
                }
                foreach(var scope in node.Scope ?? Enumerable.Empty<LiteralExpression>()) {
                    ValidateScope(itemDeclaration, scope);
                }
            });

            // local functions
            void ValidateScope(AItemDeclaration declaration, LiteralExpression scope) {
                switch(scope.Value) {
                case "*":
                case "all":

                    // nothing to do; wildcards are valid even when no functions are defined
                    break;
                case "public":

                    // nothing to do; 'public' is a reserved scope keyword
                    break;
                default:
                    if(declarations.TryGetValue(scope.Value, out var scopeReferenceDeclaration)) {
                        if(!(scopeReferenceDeclaration is FunctionDeclaration)) {
                            Logger.Log(Error.ReferenceMustBeFunction(scope.Value), scope);
                        } else if(scopeReferenceDeclaration == declaration) {
                            Logger.Log(Error.ReferenceCannotBeSelf(scope.Value), scope);
                        }
                    } else {
                        Logger.Log(Error.ReferenceDoesNotExist(scope.Value), scope);
                        declaration.TrackMissingDependency(scope.Value, declaration);
                    }
                    break;
                }
            }
        }
    }
}