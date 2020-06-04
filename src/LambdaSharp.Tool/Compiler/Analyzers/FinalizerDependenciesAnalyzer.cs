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

using System.Linq;
using LambdaSharp.Compiler;
using LambdaSharp.Compiler.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class FinalizerDependenciesAnalyzer {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public FinalizerDependenciesAnalyzer(Builder builder) => _builder = builder;

        //--- Methods ---
        public void Visit() {

            // check Finalizer::Invocation exists and that it is a module finalizer
            if(
                _builder.TryGetItemDeclaration("Finalizer::Invocation", out var finalizerInvocationDeclaration)
                && (finalizerInvocationDeclaration is ResourceDeclaration finalizerInvocationResourceDeclaration)
                && (finalizerInvocationResourceDeclaration.Type.Value == "Module::Finalizer")
            ) {
                var allResourceDeclaration = _builder.ItemDeclarations
                    .OfType<IResourceDeclaration>()
                    .Where(declaration => declaration.FullName != "Finalizer::Invocation")
                    .ToList();

                // finalizer invocation depends on all non-conditional resources
                finalizerInvocationResourceDeclaration.DependsOn = allResourceDeclaration
                    .Where(declaration => (declaration is IConditionalResourceDeclaration conditionalResourceDeclaration) && (conditionalResourceDeclaration.If == null))
                    .Select(declaration => Fn.Literal(declaration.FullName))
                    .OrderBy(fullName => fullName.Value)
                    .ToSyntaxNodes();

                // NOTE: for conditional resources, we need to take a dependency via an expression; however
                //  this approach doesn't work for custom resources because they don't support !Ref
                finalizerInvocationResourceDeclaration.Properties["DependsOn"] = new ListExpression(
                    allResourceDeclaration
                        .OfType<IConditionalResourceDeclaration>()
                        .Where(conditionalResourceDeclaration => conditionalResourceDeclaration.If != null)
                        .Select(conditionalResourceDeclaration => Fn.If(
                            conditionalResourceDeclaration.IfConditionName,
                            _builder.GetExportReference(conditionalResourceDeclaration),
                            Fn.Ref("AWS::NoValue"))
                        )
                ) {
                    SourceLocation = finalizerInvocationResourceDeclaration.SourceLocation
                };
            }
        }
    }
}
