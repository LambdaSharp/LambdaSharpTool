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

using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class FinalizerDependenciesProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public FinalizerDependenciesProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process() {
            if(
                Provider.TryGetItem("Finalizer::Invocation", out var finalizerInvocationDeclaration)
                && (finalizerInvocationDeclaration is ResourceDeclaration finalizerInvocationResourceDeclaration)
                && (finalizerInvocationResourceDeclaration.Type?.Value == "Module::Finalizer")
            ) {

                // find all instantiated resources
                var allResourceDeclaration = Provider.Declarations
                    .OfType<IResourceDeclaration>()
                    .Where(declaration => declaration.HasInitialization && (declaration.FullName != "Finalizer::Invocation"))
                    .ToList();

                // finalizer invocation depends on all non-conditional resources
                finalizerInvocationResourceDeclaration.DependsOn.AddRange(allResourceDeclaration
                    .Where(declaration => declaration.Condition == null)
                    .Select(declaration => Fn.Literal(declaration.FullName))
                    .OrderBy(fullName => fullName.Value)
                );

                // NOTE: for conditional resources, we need to take a dependency via an expression
                finalizerInvocationResourceDeclaration.Properties["DependsOn"] = new ListExpression(
                    allResourceDeclaration
                        .Where(declaration => declaration.Condition != null)
                        .Select(declaration => {
                            Provider.TryGetValueExpression(declaration.FullName, out var valueExpression);
                            return Fn.If(
                                declaration.Condition ?? throw new ShouldNeverHappenException(),
                                valueExpression ?? Fn.Ref(declaration.FullName),
                                Fn.Ref("AWS::NoValue")
                            );
                        })
                ) {
                    SourceLocation = finalizerInvocationResourceDeclaration.SourceLocation
                };
            }
        }
    }
}