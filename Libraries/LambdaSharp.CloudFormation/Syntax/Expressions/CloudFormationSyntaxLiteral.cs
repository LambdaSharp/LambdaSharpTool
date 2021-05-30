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

namespace LambdaSharp.CloudFormation.Syntax.Expressions {

    public sealed class CloudFormationSyntaxLiteral : ACloudFormationSyntaxExpression {

        //--- Fields ---
        private readonly object _value;
        private readonly CloudFormationSyntaxValueType _type;

        //--- Constructors ---
        public CloudFormationSyntaxLiteral(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _type = CloudFormationSyntaxValueType.String;
        }

        public CloudFormationSyntaxLiteral(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _value = value;
            _type = CloudFormationSyntaxValueType.Number;
        }

        private CloudFormationSyntaxLiteral(object value, CloudFormationSyntaxValueType type, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _type = type;
        }

        //--- Properties ---
        public override CloudFormationSyntaxValueType ExpressionValueType => _type;
        public string Value => _value.ToString() ?? throw new NullValueException();

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxLiteral(_value, _type) {
            SourceLocation = SourceLocation
        };
    }
}