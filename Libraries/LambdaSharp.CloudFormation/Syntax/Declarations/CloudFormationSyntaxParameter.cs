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

    public sealed class CloudFormationSyntaxParameter : ACloudFormationSyntaxDeclaration {

        //--- Fields ---
        private CloudFormationSyntaxLiteral? _type;
        private CloudFormationSyntaxLiteral? _noEcho;
        private CloudFormationSyntaxLiteral? _default;
        private CloudFormationSyntaxLiteral? _constraintDescription;
        private CloudFormationSyntaxLiteral? _allowedPattern;
        private CloudFormationSyntaxList? _allowedValues;
        private CloudFormationSyntaxLiteral? _maxLength;
        private CloudFormationSyntaxLiteral? _maxValue;
        private CloudFormationSyntaxLiteral? _minLength;
        private CloudFormationSyntaxLiteral? _minValue;

        //--- Constructors ---
        public CloudFormationSyntaxParameter(CloudFormationSyntaxLiteral logicalId, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) : base(logicalId, filePath, lineNumber) { }

        //--- Properties ---

        [Inspect]
        public CloudFormationSyntaxLiteral? Type {
            get => _type;
            set => _type = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? NoEcho {
            get => _noEcho;
            set => _noEcho = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? Default {
            get => _default;
            set => _default = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? ConstraintDescription {
            get => _constraintDescription;
            set => _constraintDescription = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? AllowedPattern {
            get => _allowedPattern;
            set => _allowedPattern = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxList? AllowedValues {
            get => _allowedValues;
            set => _allowedValues = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? MaxLength {
            get => _maxLength;
            set => _maxLength = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? MaxValue {
            get => _maxValue;
            set => _maxValue = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? MinLength {
            get => _minLength;
            set => _minLength = Adopt(value);
        }

        [Inspect]
        public CloudFormationSyntaxLiteral? MinValue {
            get => _minValue;
            set => _minValue = Adopt(value);
        }

        //--- Methods ---
        public override ACloudFormationSyntaxNode CloneNode() => new CloudFormationSyntaxParameter(LogicalId) {
            SourceLocation = SourceLocation,
            Type = Type,
            NoEcho = NoEcho,
            Default = Default,
            ConstraintDescription = ConstraintDescription,
            AllowedPattern = AllowedPattern,
            AllowedValues = AllowedValues,
            MaxLength = MaxLength,
            MaxValue = MaxValue,
            MinLength = MinLength,
            MinValue = MinValue
        };
    }
}