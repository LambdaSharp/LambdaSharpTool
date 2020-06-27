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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public static class Fn {

        //--- Class Methods ---
        public static ReferenceFunctionExpression Ref(string referenceName, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ReferenceFunctionExpression {
            ReferenceName = Literal(referenceName),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static ReferenceFunctionExpression Ref(LiteralExpression referenceName) => new ReferenceFunctionExpression {
            ReferenceName = referenceName,
            SourceLocation = referenceName.SourceLocation
        };

        public static ReferenceFunctionExpression FinalRef(string referenceName, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ReferenceFunctionExpression {
            ReferenceName = Literal(referenceName),
            SourceLocation = new SourceLocation(filePath, lineNumber),
            Final = true
        };

        public static GetAttFunctionExpression GetAtt(string referenceName, string attributeName, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new GetAttFunctionExpression {
            ReferenceName = Literal(referenceName, new SourceLocation(filePath, lineNumber)),
            AttributeName = Literal(attributeName, new SourceLocation(filePath, lineNumber))
        };

        public static SubFunctionExpression Sub(string formatString, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new SubFunctionExpression {
            FormatString = Literal(formatString),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static SubFunctionExpression Sub(string formatString, ObjectExpression parameters, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new SubFunctionExpression {
            FormatString = Literal(formatString),
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static JoinFunctionExpression Join(string separator, IEnumerable<AExpression> values, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new JoinFunctionExpression {
            Delimiter = Literal(separator),
            Values = new ListExpression(values ?? throw new ArgumentNullException(nameof(values))),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static JoinFunctionExpression Join(string separator, IEnumerable<AExpression> values, SourceLocation sourceLocation) => new JoinFunctionExpression {
            Delimiter = Literal(separator),
            Values = new ListExpression(values ?? throw new ArgumentNullException(nameof(values))),
            SourceLocation = sourceLocation
        };

        public static SelectFunctionExpression Select(int index, AExpression values, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new SelectFunctionExpression {
            Index = Literal(index),
            Values = values ?? throw new ArgumentNullException(nameof(values)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static SplitFunctionExpression Split(string delimiter, AExpression sourceString, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new SplitFunctionExpression {
            Delimiter = Literal(delimiter),
            SourceString = sourceString ?? throw new ArgumentNullException(nameof(sourceString)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static ImportValueFunctionExpression ImportValue(AExpression sharedValueToImport, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ImportValueFunctionExpression {
            SharedValueToImport = sharedValueToImport ?? throw new ArgumentNullException(nameof(sharedValueToImport)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static IfFunctionExpression If(string condition, AExpression ifTrue, AExpression ifFalse, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new IfFunctionExpression {
            Condition = Condition(condition),
            IfTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue)),
            IfFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static IfFunctionExpression If(AExpression condition, AExpression ifTrue, AExpression ifFalse, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new IfFunctionExpression {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition)),
            IfTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue)),
            IfFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static LiteralExpression Literal(string value, SourceLocation sourceLocation)
            => new LiteralExpression(value, LiteralType.String) {
                SourceLocation = sourceLocation
            };

        public static LiteralExpression Literal(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => new LiteralExpression(value, LiteralType.String) {
                SourceLocation = new SourceLocation(filePath, lineNumber)
            };

        public static LiteralExpression Literal(int value, SourceLocation sourceLocation)
            => new LiteralExpression(value.ToString(), LiteralType.Integer) {
                SourceLocation = sourceLocation
            };

        public static LiteralExpression Literal(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => new LiteralExpression(value.ToString(), LiteralType.Integer) {
                SourceLocation = new SourceLocation(filePath, lineNumber)
            };

        public static ConditionNotExpression Not(AExpression condition, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ConditionNotExpression {
            Value = condition ?? throw new ArgumentNullException(nameof(condition)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static ConditionEqualsExpression Equals(AExpression leftValue, AExpression rightValue, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ConditionEqualsExpression {
            LeftValue = leftValue ?? throw new ArgumentNullException(nameof(leftValue)),
            RightValue = rightValue ?? throw new ArgumentNullException(nameof(rightValue)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static ConditionAndExpression And(AExpression leftValue, AExpression rightValue, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ConditionAndExpression {
            LeftValue = leftValue ?? throw new ArgumentNullException(nameof(leftValue)),
            RightValue = rightValue ?? throw new ArgumentNullException(nameof(rightValue)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static ConditionOrExpression Or(AExpression leftValue, AExpression rightValue, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ConditionOrExpression {
            LeftValue = leftValue ?? throw new ArgumentNullException(nameof(leftValue)),
            RightValue = rightValue ?? throw new ArgumentNullException(nameof(rightValue)),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };

        public static ConditionReferenceExpression Condition(string referenceName, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new ConditionReferenceExpression {
            ReferenceName = Literal(referenceName),
            SourceLocation = new SourceLocation(filePath, lineNumber)
        };
    }
}