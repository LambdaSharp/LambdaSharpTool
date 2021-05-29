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

using LambdaSharp.CloudFormation.Builder.Expressions;

namespace LambdaSharp.CloudFormation.Builder.Declarations {

    public sealed class CloudFormationBuilderParameter : ACloudFormationBuilderNode {

        //--- Fields ---
        private CloudFormationBuilderLiteral? _type;
        private CloudFormationBuilderLiteral? _noEcho;
        private CloudFormationBuilderLiteral? _default;
        private CloudFormationBuilderLiteral? _constraintDescription;
        private CloudFormationBuilderLiteral? _allowedPattern;
        private CloudFormationBuilderList? _allowedValues;
        private CloudFormationBuilderLiteral? _maxLength;
        private CloudFormationBuilderLiteral? _maxValue;
        private CloudFormationBuilderLiteral? _minLength;
        private CloudFormationBuilderLiteral? _minValue;

        //--- Properties ---

        [Inspect]
        public CloudFormationBuilderLiteral? Type {
            get => _type;
            set => _type = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? NoEcho {
            get => _noEcho;
            set => _noEcho = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? Default {
            get => _default;
            set => _default = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? ConstraintDescription {
            get => _constraintDescription;
            set => _constraintDescription = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? AllowedPattern {
            get => _allowedPattern;
            set => _allowedPattern = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderList? AllowedValues {
            get => _allowedValues;
            set => _allowedValues = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? MaxLength {
            get => _maxLength;
            set => _maxLength = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? MaxValue {
            get => _maxValue;
            set => _maxValue = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? MinLength {
            get => _minLength;
            set => _minLength = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? MinValue {
            get => _minValue;
            set => _minValue = Adopt(value);
        }
    }
}