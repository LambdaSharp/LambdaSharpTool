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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LambdaSharp.DynamoDB.Serialization;

namespace LambdaSharp.DynamoDB.Native.Internal {

    internal partial class DynamoRequestConverter {

        //--- Methods ---

        /// <summary>
        /// Parses a LINQ expression to as a record attribute path. Max depth supported by DynamoDB is 32 attributes.
        /// </summary>
        /// <code>
        /// ATTRIBUTE-PATH ::=
        ///     parameter
        ///     |  ATTRIBUTE-PATH '.' member-name
        ///     |  ATTRIBUTE-PATH '[' int-expression ']'
        ///     |  ATTRIBUTE-PATH '[' string-expression ']'
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns>Returns <c>true</c> when the expression was parsed successfully.</returns>
        public bool TryParseAttributePath(Expression? expression, [NotNullWhen(true)] out string? output) {
            if(expression is null) {
                output = null;
                return false;
            }

            // expression must contain lambda parameter, but just be the lambda parameter
            if(!(expression is ParameterExpression) && expression.IsParametric()) {
                output = ParseAttributePath(expression, 1);
                return true;
            }
            output = null;
            return false;

            // local functions
            string ParseAttributePath(Expression? expression, int depth) {
                if(depth > 32) {
                    throw new NotSupportedException("exceeded the maximum depth for an attribute path (max: 32)");
                }
                switch(expression) {

                // check: base-expression "[" int "]"
                case IndexExpression indexExpression when (indexExpression.Arguments.Count == 1):
                    if(indexExpression.Arguments[0].IsParametric()) {
                        throw new NotSupportedException($"indexer cannot use lambda parameter: {indexExpression.Arguments[0]}");
                    }
                    var indexExpressionIndex = indexExpression.Arguments[0].Evaluate();
                    if(indexExpressionIndex is int) {
                        return $"{ParseAttributePath(indexExpression.Object, depth + 1)}[{indexExpressionIndex}]";
                    }
                    throw new NotSupportedException($"indexer must be int expression: {indexExpression.Arguments[0]}");

                // check: base-expression "[" int "]"
                case BinaryExpression binaryExpression when (binaryExpression.NodeType == ExpressionType.ArrayIndex):
                    if(binaryExpression.Right.IsParametric()) {
                        throw new NotSupportedException($"indexer cannot use lambda parameter: {binaryExpression.Right}");
                    }
                    var binaryExpressionIndex = binaryExpression.Right.Evaluate();
                    if(binaryExpressionIndex is int) {
                        return $"{ParseAttributePath(binaryExpression.Left, depth + 1)}[{binaryExpressionIndex}]";
                    }
                    throw new NotSupportedException($"indexer must be int expression: {binaryExpression.Right}");

                // check: base-expression "." member-name
                case MemberExpression memberExpression:
                    if(memberExpression.Expression is null) {
                        throw new NotSupportedException($"base-expression must use lambda parameter: {expression}");
                    }
                    return Join(ParseAttributePath(memberExpression.Expression, depth + 1), GetMemberName(memberExpression.Member));

                // check: base-expression "[" int|string "]"
                case MethodCallExpression methodCallExpression when (methodCallExpression.Method.Name == "get_Item") && (methodCallExpression.Arguments.Count == 1):
                    if(methodCallExpression.Arguments[0].IsParametric()) {
                        throw new NotSupportedException($"indexer cannot use lambda parameter: {expression}");
                    }
                    var methodCallIndex = methodCallExpression.Arguments[0].Evaluate();
                    if(methodCallIndex is string methodCallIndexStringValue) {
                    return Join(ParseAttributePath(methodCallExpression.Object, depth + 1), GetAttributeName(methodCallIndexStringValue));
                    }
                    if(methodCallIndex is int) {
                        return $"{ParseAttributePath(methodCallExpression.Object, depth + 1)}[{(int)methodCallIndex}]";
                    }
                    throw new NotSupportedException($"indexer must be a string or int expression: {methodCallExpression.Arguments[0]}");

                // check: base-expression === lambda parameter
                case ParameterExpression parameterExpression:

                    // NOTE (2021-06-05, bjorg): there can only be one parameter, so no need to check that the name matches
                    return "";
                }
                throw new NotSupportedException($"invalid attribute path expression: {expression?.ToString() ?? "<null>"}");

                // local functions
                static string Join(string left, string right) => (left.Length == 0) ? right : (left + "." + right);
            }

            string GetMemberName(MemberInfo memberInfo) {

                // check if the serialization property name is overwritten by DynamoPropertyNameAttribute
                var attribute = memberInfo.GetCustomAttribute<DynamoPropertyNameAttribute>();
                return (attribute is null)
                    ? GetAttributeName(memberInfo.Name)
                    : GetAttributeName(attribute.Name);
            }
        }

        public string ParseAttributePath(Expression expression) {
            if(!TryParseAttributePath(expression, out var output)) {
                throw new NotSupportedException($"invalid attribute path expression: {expression}");
            }
            return output;
        }

        /// <summary>
        /// Parses a LINQ expression to as a value expression.
        /// </summary>
        /// <code>
        /// VALUE ::=
        ///     SET-OPERAND
        ///     | SET-OPERAND '+' SET-OPERAND
        ///     | SET-OPERAND '-' SET-OPERAND
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        /// <param name="precedence">The precedence level of the rendered expression.</param>
        /// <returns>Returns <c>true</c> when the expression was parsed successfully.</returns>
        public bool TryParseValue(Expression expression, [NotNullWhen(true)] out string? output, out Precedence precedence) {
            switch(expression) {
            case BinaryExpression binaryExpression:
                string op;
                switch(binaryExpression.NodeType) {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    op = "+";
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    op = "-";
                    break;
                default:
                    throw new NotSupportedException($"operator must be '+' or '-': {expression}");
                }
                if(!TryParseSetOperand(binaryExpression.Left, out var renderLeft)) {
                    throw new NotSupportedException($"left value of '{op}' must be an operand expression: {binaryExpression.Left}");
                }
                if(!TryParseSetOperand(binaryExpression.Right, out var renderRight)) {
                    throw new NotSupportedException($"right value of '{op}' must be an operand expression: {binaryExpression.Right}");
                }
                (output, precedence) = Combine(op, Precedence.ScalarAddSubtract, (Expression: renderLeft, Precedence: Precedence.Atomic), (Expression: renderRight, Precedence: Precedence.Atomic));
                return true;
            default:
                precedence = Precedence.Atomic;
                return TryParseSetOperand(expression, out output);
            }
        }

        public string ParseValue(Expression expression) {
            if(!TryParseValue(expression, out var output, out _)) {
                throw new NotSupportedException($"invalid value expression: {expression}");
            }
            return output;
        }

        /// <summary>
        /// Parses a LINQ expression to a SET action operand.
        /// </summary>
        /// <code>
        /// SET-OPERAND ::=
        ///     ATTRIBUTE-PATH
        ///     | SET-FUNCTION
        ///     | LITERAL
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        public bool TryParseSetOperand(Expression expression, [NotNullWhen(true)] out string? output)
            => TryParseSetFunction(expression, out output)
                || TryParseAttributePath(expression, out output)
                || TryParseLiteral(expression, out output);

        /// <summary>
        /// Parses a LINQ expression as a literal value.
        /// </summary>
        /// <code>
        /// LITERAL ::=
        ///     | null-expression
        ///     | bool-expression
        ///     | binary-expression
        ///     | string-expression
        ///     | int-expression
        ///     | long-expression
        ///     | double-expression
        ///     | decimal-expression
        ///     | list-expression
        ///     | map-expression
        ///     | binary-set-expression
        ///     | string-set-expression
        ///     | int-set-expression
        ///     | long-set-expression
        ///     | double-set-expression
        ///     | decimal-set-expression
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns>Returns <c>true</c> when the expression was parsed successfully.</returns>
        public bool TryParseLiteral(Expression expression, [NotNullWhen(true)] out string? output) {
            if(!expression.IsParametric()) {
                var result = expression.Evaluate();
                output = GetExpressionValueName(result);
                return true;
            }
            output = null;
            return false;
        }

        /// <summary>
        /// Parses a LINQ expression as a SET function.
        /// </summary>
        /// <code>
        /// SET-FUNCTION ::=
        ///     IF-NOT-EXIST
        ///     | LIST-APPEND
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns>Returns <c>true</c> when the expression was parsed successfully.</returns>
        public bool TryParseSetFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            return TryParseIfNotExistsSetFunction(expression, out output)
                || TryParseListAppendSetFunction(expression, out output);
        }

        /// <summary>
        /// Parses a LINQ expression as a <c>if_not_exists</c> function invocation.
        /// </summary>
        /// <code>
        /// IF-NOT-EXIST ::=
        ///     'if_not_exists' '(' ATTRIBUTE-PATH, VALUE ')'
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns>Returns <c>true</c> when the expression was parsed successfully.</returns>
        public bool TryParseIfNotExistsSetFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(
                (expression is MethodCallExpression methodCallExpression)
                && (methodCallExpression.Method.DeclaringType == typeof(DynamoUpdate))
                && (methodCallExpression.Method.Name == nameof(DynamoUpdate.IfNotExists))
            ) {
                if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                    throw new NotSupportedException($"argument 'path' in 'if_not_exists' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                }
                if(!TryParseValue(methodCallExpression.Arguments[1], out var value, out _)) {
                    throw new NotSupportedException($"argument 'value' in 'if_not_exists' must be a value expression: {methodCallExpression.Arguments[1]}");
                }
                output = $"if_not_exists({attributePath}, {value})";
                return true;
            }
            output = null;
            return false;
        }

        /// <summary>
        /// Parses a LINQ expression as a <c>list_append</c> function invocation.
        /// </summary>
        /// <code>
        /// LIST-APPEND ::=
        ///     'list_append' '(' SET-OPERAND, SET-OPERAND ')'
        /// </code>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="output">The rendered expression when <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns>Returns <c>true</c> when the expression was parsed successfully.</returns>
        public bool TryParseListAppendSetFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(
                (expression is MethodCallExpression methodCallExpression)
                && (methodCallExpression.Method.DeclaringType == typeof(DynamoUpdate))
                && (methodCallExpression.Method.Name == nameof(DynamoUpdate.ListAppend))
            ) {
                if(!TryParseSetOperand(methodCallExpression.Arguments[0], out var list1)) {
                    throw new NotSupportedException($"argument 'list1' in 'list_append' must be an operand expression: {methodCallExpression.Arguments[1]}");
                }
                if(!TryParseSetOperand(methodCallExpression.Arguments[1], out var list2)) {
                    throw new NotSupportedException($"argument 'list2' in 'list_append' must be an operand expression: {methodCallExpression.Arguments[1]}");
                }
                output = $"list_append({list1}, {list2})";
                return true;
            }
            output = null;
            return false;
        }

        public bool TryParseCondition(Expression expression, [NotNullWhen(true)] out string? output, out Precedence precedence) {

            // check: binary operation
            if(expression is BinaryExpression binaryExpression) {
                switch(binaryExpression.NodeType) {

                // CONDITION-OPERAND '=' CONDITION-OPERAND
                case ExpressionType.Equal:
                    (output, precedence) = ParseBinaryOperands("=", Precedence.ScalarComparison, binaryExpression);
                    return true;

                // CONDITION-OPERAND '<>' CONDITION-OPERAND
                case ExpressionType.NotEqual:
                    (output, precedence) = ParseBinaryOperands("<>", Precedence.ScalarComparison, binaryExpression);
                    return true;

                // CONDITION-OPERAND '<' CONDITION-OPERAND
                case ExpressionType.LessThan:
                    (output, precedence) = ParseBinaryOperands("<", Precedence.ScalarComparison, binaryExpression);
                    return true;

                // CONDITION-OPERAND '<=' CONDITION-OPERAND
                case ExpressionType.LessThanOrEqual:
                    (output, precedence) = ParseBinaryOperands("<=", Precedence.ScalarComparison, binaryExpression);
                    return true;

                // CONDITION-OPERAND '>' CONDITION-OPERAND
                case ExpressionType.GreaterThan:
                    (output, precedence) = ParseBinaryOperands(">", Precedence.ScalarComparison, binaryExpression);
                    return true;

                // CONDITION-OPERAND '>=' CONDITION-OPERAND
                case ExpressionType.GreaterThanOrEqual:
                    (output, precedence) = ParseBinaryOperands(">=", Precedence.ScalarComparison, binaryExpression);
                    return true;

                // condition-expression 'AND' condition-expression
                case ExpressionType.AndAlso:
                    (output, precedence) = ParseBinaryExpressions("AND", Precedence.AndOperator, binaryExpression);
                    return true;

                // condition-expression 'OR' condition-expression
                case ExpressionType.OrElse:
                    (output, precedence) = ParseBinaryExpressions("OR", Precedence.OrOperator, binaryExpression);
                    return true;
                }
            }

            // check: 'not' CONDITION-EXPRESSION
            if(
                (expression is UnaryExpression unaryExpression)
                && (unaryExpression.NodeType == ExpressionType.Not)
            ) {
                if(!TryParseCondition(unaryExpression.Operand, out var notOperandRender, out var notOperandPrecedence)) {
                    throw new NotSupportedException($"value must be an operand expression: {unaryExpression.Operand}");
                }
                (output, precedence) = Prefix("NOT", Precedence.NotOperator, (notOperandRender, notOperandPrecedence));
                return true;
            }

            // check: DynamoDB special operators
            if(
                (expression is MethodCallExpression methodCallExpression)
                && (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
            ) {
                switch(methodCallExpression.Method.Name) {

                // check: CONDITION-OPERAND `between` CONDITION-OPERAND `and` CONDITION-OPERAND
                case nameof(DynamoCondition.Between):
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[0], out var inBetweenOperandRender)) {
                        throw new NotSupportedException($"operand value must be an operand expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[1], out var inBetweenLowerBoundRender)) {
                        throw new NotSupportedException($"lower-bound value must be an operand expression: {methodCallExpression.Arguments[1]}");
                    }
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[2], out var inBetweenUpperBoundRender)) {
                        throw new NotSupportedException($"upper-bound value must be an operand expression: {methodCallExpression.Arguments[2]}");
                    }
                    output = $"{inBetweenOperandRender} BETWEEN {inBetweenLowerBoundRender} AND {inBetweenUpperBoundRender}";
                    precedence = Precedence.BetweenOperator;
                    return true;

                // CONDITION-OPERAND 'in' ( CONDITION-OPERAND (',' CONDITION-OPERAND)* )
                case nameof(DynamoCondition.In):
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[0], out var inOperandRender)) {
                        throw new NotSupportedException($"operand value must be an operand expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(methodCallExpression.Arguments[1].IsParametric()) {
                        throw new NotSupportedException($"in collection cannot use lambda parameter: {methodCallExpression.Arguments[1]}");
                    }
                    var inCollection = methodCallExpression.Arguments[1].EvaluateTo<IEnumerable>().Cast<object>();
                    if((inCollection.Count() == 0) || (inCollection.Count() > 100)) {
                        throw new NotSupportedException("in collection must have at least 1 element and at most 100");
                    }
                    output = $"{inOperandRender} IN ({string.Join(", ", inCollection.Select(item => GetExpressionValueName(item)))})";
                    precedence = Precedence.InOperator;
                    return true;
                }
            }

            // check: CONDITION-FUNCTION
            if(TryParseConditionFunction(expression, out output)) {
                precedence = Precedence.Atomic;
                return true;
            }
            output = null;
            precedence = Precedence.Undefined;
            return false;

            // local function
            (string, Precedence) ParseBinaryOperands(string op, Precedence precedence, BinaryExpression binaryExpression) {
                var left = binaryExpression.Left;
                var right = binaryExpression.Right;

                // NOTE (2021-06-27, bjorg): special case for enums which are automatically cast to int
                if(
                    (binaryExpression.Left is UnaryExpression leftUnaryExpression)
                    && (leftUnaryExpression.NodeType == ExpressionType.Convert)
                    && leftUnaryExpression.Operand.Type.IsEnum
                    && (leftUnaryExpression.Type == typeof(int))
                    && (binaryExpression.Right.Type == typeof(int))
                ) {

                    // keep the original enum reference
                    left = leftUnaryExpression.Operand;

                    // convert the right constant expression from int enum value to string enum value
                    if(right is ConstantExpression rightConstantExpression) {
                        if(rightConstantExpression.Value is null) {
                            throw new NotSupportedException($"right constant expression cannot be 'null': {right}");
                        }
                        right = Expression.Constant(Enum.GetName(leftUnaryExpression.Operand.Type, rightConstantExpression.Value));
                    } else if(
                        (right is UnaryExpression rightUnaryExpression)
                        && (rightUnaryExpression.NodeType == ExpressionType.Convert)
                    ) {
                        var rightValue = rightUnaryExpression.Operand.Evaluate() ?? throw new NotSupportedException($"enum value cannot be 'null': {rightUnaryExpression.Operand}");
                        right = Expression.Constant(Enum.GetName(rightUnaryExpression.Operand.Type, rightValue));
                    }
                }

                // try parsing left and right expressions as operands
                if(!TryParseConditionOperand(left, out var leftOperandRender)) {
                    throw new NotSupportedException($"left value for '{op}' operation must be an operand expression: {left}");
                }
                if(!TryParseConditionOperand(right, out var rightOperandRender)) {
                    throw new NotSupportedException($"right value for '{op}' operation must be an operand expression: {right}");
                }
                return ($"{leftOperandRender} {op} {rightOperandRender}", precedence);
            }

            (string, Precedence) ParseBinaryExpressions(string op, Precedence precedence, BinaryExpression binaryExpression) {
                if(!TryParseCondition(binaryExpression.Left, out var leftOperandRender, out var leftOperandPrecedence)) {
                    throw new NotSupportedException($"left value for '{op}' operation must be an operand expression: {binaryExpression.Left}");
                }
                if(!TryParseCondition(binaryExpression.Right, out var rightOperandRender, out var rightOperandPrecedence)) {
                    throw new NotSupportedException($"right value for '{op}' operation must be an operand expression: {binaryExpression.Right}");
                }
                return Combine(op, precedence, (leftOperandRender, leftOperandPrecedence), (rightOperandRender, rightOperandPrecedence));
            }
        }

        public (string Expression, Precedence precedence) ParseCondition(Expression expression) {
            if(!TryParseCondition(expression, out var output, out var precedence)) {
                throw new NotSupportedException($"invalid condition expression: {expression}");
            }
            return (output, precedence);
        }

        public bool TryParseConditionOperand(Expression expression, [NotNullWhen(true)] out string? output)
            => TryParseConditionFunction(expression, out output)
                || TryParseAttributePath(expression, out output)
                || TryParseLiteral(expression, out output);

        public bool TryParseConditionFunction(Expression expression, [NotNullWhen(true)] out string? output)
            => TryParseAttributeExistsConditionFunction(expression, out output)
                || TryParseAttributeDoesNotExistsConditionFunction(expression, out output)
                || TryParseAttributeTypeConditionFunction(expression, out output)
                || TryParseBeginsWithConditionFunction(expression, out output)
                || TryParseContainsConditionFunction(expression, out output)
                || TryParseSizeConditionFunction(expression, out output);

        public bool TryParseAttributeExistsConditionFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(
                (expression is MethodCallExpression methodCallExpression)
                && (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
                && (methodCallExpression.Method.Name == nameof(DynamoCondition.Exists))
            ) {
                if(methodCallExpression.Arguments[0] is ParameterExpression) {
                    output = $"attribute_exists(PK)";
                    return true;
                }
                if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                    throw new NotSupportedException($"argument 'path' in 'attribute_exists' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                }
                output = $"attribute_exists({attributePath})";
                return true;
            }
            output = null;
            return false;
        }

        public bool TryParseAttributeDoesNotExistsConditionFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(
                (expression is MethodCallExpression methodCallExpression)
                && (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
                && (methodCallExpression.Method.Name == nameof(DynamoCondition.DoesNotExist))
            ) {
                if(methodCallExpression.Arguments[0] is ParameterExpression) {
                    output = $"attribute_not_exists(PK)";
                    return true;
                }
                if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                    throw new NotSupportedException($"argument 'path' in 'attribute_not_exists' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                }
                output = $"attribute_not_exists({attributePath})";
                return true;
            }
            output = null;
            return false;
        }

        public bool TryParseAttributeTypeConditionFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(ExpressionValues is null) {
                throw new InvalidOperationException("instance was initialized without expression values");
            }
            if(
                (expression is MethodCallExpression methodCallExpression)
                && (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
                && (methodCallExpression.Method.Name == nameof(DynamoCondition.HasType))
            ) {
                if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                    throw new NotSupportedException($"argument 'path' in 'attribute_type' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                }
                if(!TryParseLiteral(methodCallExpression.Arguments[1], out var attributeType)) {
                    throw new NotSupportedException($"argument 'type' in 'attribute_type' must be a string expression: {methodCallExpression.Arguments[1]}");
                }

                // validate the 'type' parameter value
                switch(ExpressionValues[attributeType].S) {
                case "B":
                case "BOOL":
                case "BS":
                case "L":
                case "M":
                case "N":
                case "NS":
                case "NULL":
                case "S":
                case "SS":
                    break;
                default:
                    throw new NotSupportedException("type must be one of: B, BOOL, BS, L, M, N, NS, NULL, S, or SS");
                }
                output = $"attribute_type({attributePath}, {attributeType})";
                return true;
            }
            output = null;
            return false;
        }

        public bool TryParseBeginsWithConditionFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(expression is MethodCallExpression methodCallExpression) {

                // check: `DynamoCondition.BeginsWith(expression, string-expression)` method invocation
                if(
                    (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
                    && (methodCallExpression.Method.Name == nameof(DynamoCondition.BeginsWith))
                ) {
                    if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                        throw new NotSupportedException($"argument 'path' in 'begins_with' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseLiteral(methodCallExpression.Arguments[1], out var substrRender)) {
                        throw new NotSupportedException($"argument 'substr' in 'begins_with' must be a string expression: {methodCallExpression.Arguments[1]}");
                    }
                    output = $"begins_with({attributePath}, {substrRender})";
                    return true;
                }

                // check: `String.StartsWith(string-expression)` method invocation
                if(
                    (methodCallExpression.Method.DeclaringType == typeof(String))
                    && (methodCallExpression.Method.Name == nameof(String.StartsWith))
                ) {
                    if(methodCallExpression.Arguments.Count != 1) {
                        throw new NotSupportedException($"only 'String.BeginsWIth(string-expression)' is supported: {methodCallExpression}");
                    }
                    if(!TryParseAttributePath(methodCallExpression.Object, out var attributePath)) {
                        throw new NotSupportedException($"argument 'this' in 'String.BeginsWith(string-expression)' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseLiteral(methodCallExpression.Arguments[0], out var substrRender)) {
                        throw new NotSupportedException($"argument 'value' in 'String.BeginsWith(string-expression)' must be a string expression: {methodCallExpression.Arguments[0]}");
                    }
                    output = $"begins_with({attributePath}, {substrRender})";
                    return true;
                }
            }
            output = null;
            return false;
        }

        public bool TryParseContainsConditionFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(expression is MethodCallExpression methodCallExpression) {

                // check: `DynamoCondition.Contains(attribute-path, operand)` method invocation
                if(
                    (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
                    && (methodCallExpression.Method.Name == nameof(DynamoCondition.Contains))
                ) {
                    if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                        throw new NotSupportedException($"argument 'path' in 'contains' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[1], out var operand)) {
                        throw new NotSupportedException($"argument 'operand' in 'contains' must be an operand expression: {methodCallExpression.Arguments[1]}");
                    }
                    output = $"contains({attributePath}, {operand})";
                    return true;
                }

                // check: `Enumerable.Contains(attribute-path, operand)` method invocation
                if(
                    (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
                    && (methodCallExpression.Method.Name == nameof(Enumerable.Contains))
                ) {
                    if(methodCallExpression.Arguments.Count != 2) {
                        throw new NotSupportedException($"only 'Enumerable.Contains(attribute-path, operand)' is supported: {expression}");
                    }
                    if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                        throw new NotSupportedException($"argument 'this' in 'Enumerable.Contains()' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[1], out var operand)) {
                        throw new NotSupportedException($"argument 'value' in 'Enumerable.Contains()' must be an operand expression: {methodCallExpression.Arguments[1]}");
                    }
                    output = $"contains({attributePath}, {operand})";
                    return true;
                }

                // check: `IList.Contains(operand)` method invocation
                if(
                    !(methodCallExpression.Object is null)
                    && typeof(IList).IsAssignableFrom(methodCallExpression.Object.Type)
                    && (methodCallExpression.Method.Name == nameof(IList.Contains))
                ) {
                    if(methodCallExpression.Arguments.Count != 1) {
                        throw new NotSupportedException($"only 'IList.Contains(operand)' is supported: {expression}");
                    }
                    if(!TryParseAttributePath(methodCallExpression.Object, out var attributePath)) {
                        throw new NotSupportedException($"argument 'this' in 'IList.Contains(operand)' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[0], out var operand)) {
                        throw new NotSupportedException($"argument 'value' in 'IList.Contains(operand)' must be an operand expression: {methodCallExpression.Arguments[1]}");
                    }
                    output = $"contains({attributePath}, {operand})";
                    return true;
                }

                // check: `IList<T>.Contains(operand)` method invocation
                if(
                    !(methodCallExpression.Object is null)
                    && (
                        (methodCallExpression.Object.Type.IsGenericType && (methodCallExpression.Object.Type.GetGenericTypeDefinition() == typeof(IList<>)))
                        || methodCallExpression.Object.Type.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>)))
                    )
                    && (methodCallExpression.Method.Name == nameof(IList.Contains))
                ) {
                    if(methodCallExpression.Arguments.Count != 1) {
                        throw new NotSupportedException($"only 'IList<T>.Contains(operand)' is supported: {expression}");
                    }
                    if(!TryParseAttributePath(methodCallExpression.Object, out var attributePath)) {
                        throw new NotSupportedException($"argument 'this' in 'IList.Contains(operand)' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    if(!TryParseConditionOperand(methodCallExpression.Arguments[0], out var operand)) {
                        throw new NotSupportedException($"argument 'value' in 'IList.Contains()' must be an operand expression: {methodCallExpression.Arguments[1]}");
                    }
                    output = $"contains({attributePath}, {operand})";
                    return true;
                }
            }
            output = null;
            return false;
        }

        public bool TryParseSizeConditionFunction(Expression expression, [NotNullWhen(true)] out string? output) {
            if(expression is MethodCallExpression methodCallExpression) {

                // check: `DynamoCondition.Size(attribute-path)` method invocation
                if(
                    (methodCallExpression.Method.DeclaringType == typeof(DynamoCondition))
                    && (methodCallExpression.Method.Name == nameof(DynamoCondition.Size))
                ) {
                    if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                        throw new NotSupportedException($"argument 'path' in 'size' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    output = $"size({attributePath})";
                    return true;
                }

                // check: `Enumerable.Count(expression)` method invocation
                if(
                    (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
                    && (methodCallExpression.Method.Name == nameof(Enumerable.Count))
                ) {
                    if(methodCallExpression.Arguments.Count != 1) {
                        throw new NotSupportedException($"only 'Enumerable.Count(attribute-path)' is supported: {expression}");
                    }
                    if(!TryParseAttributePath(methodCallExpression.Arguments[0], out var attributePath)) {
                        throw new NotSupportedException($"argument 'this' in 'Enumerable.Count()' must be an attribute-path expression: {methodCallExpression.Arguments[0]}");
                    }
                    output = $"size({attributePath})";
                    return true;
                }
            }

            // check: `ICollection.Count`/`ICollection<T>.Count` property access
            if(
                (expression is MemberExpression memberExpression)
                && (memberExpression.Member.Name == nameof(ICollection.Count))
                && (!(memberExpression.Expression is null))
                && (
                    typeof(ICollection).IsAssignableFrom(memberExpression.Expression.Type)
                    || memberExpression.Expression.Type.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICollection<>)))
                )
            ) {
                if(!TryParseAttributePath(memberExpression.Expression, out var attributePath)) {
                    throw new NotSupportedException($"the 'Count' property must be applied to an attribute-path expression: {memberExpression.Expression}");
                }
                output = $"size({attributePath})";
                return true;
            }
            output = null;
            return false;
        }
    }
}
