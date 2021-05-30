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

using System.Runtime.CompilerServices;
using LambdaSharp.CloudFormation.Syntax.Expressions;

namespace LambdaSharp.CloudFormation.Syntax.Declarations {

    public sealed class CloudFormationSyntaxCondition : ACloudFormationSyntaxDeclaration {

        //--- Fields ---
        private ACloudFormationSyntaxExpression? _value;

        //--- Constructors ---
        public CloudFormationSyntaxCondition(CloudFormationSyntaxLiteral logicalId, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) : base(logicalId, filePath, lineNumber) { }

        //--- Properties ---

        [Inspect]
        public ACloudFormationSyntaxExpression? Value {
            get => _value;
            set => _value = Adopt(value);
        }

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxCondition(LogicalId) {
            SourceLocation = SourceLocation,
            Value = Value
        };
    }
}