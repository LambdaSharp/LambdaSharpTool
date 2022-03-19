/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.Linq.Expressions;

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal static class ExpressionEx {

        //--- Class Methods ---
        public static bool IsParametric(this Expression expression) {
            if(expression is null) {
                throw new ArgumentNullException(nameof(expression));
            }
            switch(expression) {

            // supported expression types
            case BinaryExpression binaryExpression:
                return binaryExpression.Left.IsParametric() || binaryExpression.Right.IsParametric();
            case ConditionalExpression conditionalExpression:
                return conditionalExpression.Test.IsParametric() || conditionalExpression.IfTrue.IsParametric() || conditionalExpression.IfFalse.IsParametric();
            case ConstantExpression constantExpression:
                return false;
            case DebugInfoExpression debugInfoExpression:
                return false;
            case DefaultExpression defaultExpression:
                return false;
            case IndexExpression indexExpression when !(indexExpression.Object is null):
                return indexExpression.Object.IsParametric() || indexExpression.Arguments.Any(argumentExpression => argumentExpression.IsParametric());
            case InvocationExpression invocationExpression:
                return invocationExpression.Expression.IsParametric() || invocationExpression.Arguments.Any(argumentExpression => argumentExpression.IsParametric());
            case ListInitExpression listInitExpression:
                return listInitExpression.Initializers.Any(initializerExpression => initializerExpression.Arguments.Any(argumentExpression => argumentExpression.IsParametric()));
            case MemberExpression memberExpression:
                return memberExpression.Expression?.IsParametric() ?? false;
            case MemberInitExpression memberInitExpression:
                return memberInitExpression.NewExpression.IsParametric();
            case MethodCallExpression methodCallExpression:
                return (methodCallExpression.Object?.IsParametric() ?? false) || methodCallExpression.Arguments.Any(argumentExpression => argumentExpression.IsParametric());
            case NewArrayExpression newArrayExpression:
                return newArrayExpression.Expressions.Any(argumentExpression => argumentExpression.IsParametric());
            case NewExpression newExpression:
                return newExpression.Arguments.Any(argumentExpression => argumentExpression.IsParametric());
            case ParameterExpression parameterExpression:
                return true;
            case SwitchExpression switchExpression:
                return switchExpression.SwitchValue.IsParametric()
                    || switchExpression.Cases.Any(caseExpression =>
                        caseExpression.TestValues.Any(testValueExpression => testValueExpression.IsParametric())
                        || caseExpression.Body.IsParametric()
                    );
            case TypeBinaryExpression typeBinaryExpression:
                return typeBinaryExpression.Expression.IsParametric();
            case UnaryExpression unaryExpression:
                return unaryExpression.Operand.IsParametric();

            // unsupported expressions
            case BlockExpression blockExpression:
            case DynamicExpression dynamicExpression:
            case GotoExpression gotoExpression:
            case LabelExpression labelExpression:
            case LambdaExpression lambdaExpression:
            case LoopExpression loopExpression:
            case RuntimeVariablesExpression runtimeVariablesExpression:
            case TryExpression tryExpression:
                throw new NotSupportedException($"expression of type {expression.GetType().FullName} is not supported: {expression}");

            // unexpected expression type
            default:
                throw new NotSupportedException($"unexpected expression type {expression?.GetType().FullName ?? "<null>"}: {expression}");
            }
        }

        public static object? Evaluate(this Expression expression) =>
            (expression is ConstantExpression constantExpression)
                ? constantExpression.Value
                : Expression.Lambda(expression).Compile().DynamicInvoke();

        public static T EvaluateTo<T>(this Expression expression) {

            // evaluate expression
            var result = (expression is ConstantExpression constantExpression)
                ? constantExpression.Value
                : Expression.Lambda(expression).Compile().DynamicInvoke();

            // validate result is of expected type
            if(!(result is T typedResult)) {
                throw new NotSupportedException($"expression must return value of type {typeof(T).FullName ?? "<null>"}: {expression}");
            }
            return typedResult;
        }
    }
}
