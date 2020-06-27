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

using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class IsDefinedProcessor : ASyntaxProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly Error MustBeUsedInIfExpressionCondition = new Error(0, "!IsDefined can only be used as a condition in an !If expression");
        #endregion

        //--- Constructors ---
        public IsDefinedProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        public void Process() {
            var substitutions = new Dictionary<ISyntaxNode, ISyntaxNode>();
            while(true) {
                substitutions.Clear();

                // find !IfDefined expressions that can be substituted
                Inspect(node => {
                    switch(node) {
                    case IfFunctionExpression ifFunctionExpression:
                        if(ifFunctionExpression.Condition is ConditionIsDefinedExpression ifFunctionExpressionCondition) {

                            // substitute !If expression with appropriate branch
                            substitutions[ifFunctionExpression] = Provider.TryGetItem(ifFunctionExpressionCondition.ReferenceName.Value, out var _)
                                ? ifFunctionExpression.IfTrue
                                : ifFunctionExpression.IfFalse;
                        }
                        break;
                    case ConditionIsDefinedExpression conditionIsDefinedExpression:

                        // TODO: can !IsDefined be used as a resource definition condition?

                        // validate the parent is an !If expression
                        if(
                            !(conditionIsDefinedExpression.Parent is IfFunctionExpression conditionIsDefinedExpressionParent)
                            || !object.ReferenceEquals(conditionIsDefinedExpression, conditionIsDefinedExpressionParent.Condition)
                        ) {
                            Logger.Log(MustBeUsedInIfExpressionCondition, conditionIsDefinedExpression);
                        }
                        break;
                    }
                });

                // stop when no more substitutions can be found
                if(!substitutions.Any()) {
                    break;
                }

                // apply substitions to tree
                Substitute(node => {
                    if(substitutions.TryGetValue(node, out var newNode)) {
                        return newNode;
                    }
                    return node;
                });
            }
        }
    }
}