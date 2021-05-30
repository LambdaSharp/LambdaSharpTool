/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Runtime.CompilerServices;
using LambdaSharp.CloudFormation.Syntax.Functions;

namespace LambdaSharp.CloudFormation.Syntax.Expressions {

    public class CloudFormationSyntaxFunctionInvocation : ACloudFormationSyntaxExpression {

        //--- Constructors ---
        public CloudFormationSyntaxFunctionInvocation(ACloudFormationSyntaxFunction function, ACloudFormationSyntaxExpression argument, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            Argument = Adopt(argument ?? throw new ArgumentNullException(nameof(argument)));
        }

        //--- Properties ---
        public ACloudFormationSyntaxFunction Function { get; }
        public ACloudFormationSyntaxExpression Argument { get; }
        public override CloudFormationSyntaxValueType ExpressionValueType => Function.ReturnValueType;

        // TODO: need to do better than this
        public string StringArgument => ((CloudFormationSyntaxLiteral)Argument).Value;

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxFunctionInvocation(Function, Argument) {
            SourceLocation = SourceLocation
        };
    }
}