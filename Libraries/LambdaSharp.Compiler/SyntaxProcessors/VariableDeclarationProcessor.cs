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

using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class VariableDeclarationProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public VariableDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectType<VariableDeclaration>(node => {

                // TODO: validate type

                // make variable references substitutable
                if(node.Value is ListExpression listExpression) {
                    Provider.DeclareReferenceExpression(node.FullName, node.Value = Fn.Join(",", listExpression, listExpression.SourceLocation));
                } else if(node.Value != null) {
                    Provider.DeclareReferenceExpression(node.FullName, node.Value);
                }
            });
        }
    }
}