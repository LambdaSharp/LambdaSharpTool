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

    internal sealed class MacroDeclarationProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public MacroDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectType<MacroDeclaration>(node => {

                // check macro handler
                AExpression? handler = null;
                if(node.Handler == null) {

                    // TODO: error
                } else if(Provider.TryGetItem(node.Handler.Value, out var referencedDeclaration)) {

                    // TODO: handler could be a AWS::Lambda::Function resource, a parameter, or even a constant

                    // ensure handler is referencing a Lambda function
                    if(!(referencedDeclaration is FunctionDeclaration)) {
                        Logger.Log(Error.HandlerMustBeAFunction, node.Handler);
                    }
                    handler = Fn.Ref(node.Handler);
                } else {
                    Logger.Log(Error.ReferenceDoesNotExist(node.Handler.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.Handler.Value, node);
                }

                // initialize macro resource properties
                node.Properties["Name"] = Fn.Sub($"${{DeploymentPrefix}}{node.ItemName}");
                if(node.Description != null) {
                    node.Properties["Description"] = node.Description;
                }
                if(handler != null) {
                    node.Properties["FunctionName"] = handler;
                }
            });
        }
    }
}