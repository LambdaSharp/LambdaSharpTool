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

    internal sealed class ExpressionTypeProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public ExpressionTypeProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //
        public void Process() {

            // TODO: validate types in references
            // TODO: validate expression nesting (e.g. !And must contain condition expressions, etc.)
            // TODO: ensure that !GetAtt referenced attributes exist
            // TODO: annotate expression types

            Inspect(node => {
                switch(node) {
                case ResourceTypeDeclaration resourceTypeDeclaration:

                    // validate the 'ResourceType' handler is either a Lambda function or an SNS Topic
                    if(
                        (resourceTypeDeclaration.Handler is ReferenceFunctionExpression resourceTypeHandlerReferenceExpression)
                        && Provider.TryGetItem(resourceTypeHandlerReferenceExpression.ReferenceName.Value, out var resourceTypeHandlerDeclaration)
                    ) {
                        if(resourceTypeHandlerDeclaration is FunctionDeclaration) {

                            // nothing to do; handler is a Lambda function
                        } else if((resourceTypeHandlerDeclaration is ResourceDeclaration resourceDeclaration) && (resourceDeclaration.Type?.Value == "AWS::SNS::Topic")) {

                            // nothing to do; handler is an SNS Topic
                        } else {
                            Logger.Log(Error.HandlerMustBeAFunctionOrSnsTopic, resourceTypeDeclaration.Handler);
                        }
                    }
                    break;
                case MacroDeclaration macroDeclaration:
                    if(
                        (macroDeclaration.Handler is ReferenceFunctionExpression macroHandlerReferenceExpression)
                        && Provider.TryGetItem(macroHandlerReferenceExpression.ReferenceName.Value, out var macroHandlerDeclaration)
                    ) {
                        if(macroHandlerDeclaration is FunctionDeclaration) {

                            // nothing to do; handler is a Lambda function
                        } {
                            Logger.Log(Error.HandlerMustBeAFunctionOrSnsTopic, macroDeclaration.Handler);
                        }
                    }
                    break;
                }
            });
        }
    }
}