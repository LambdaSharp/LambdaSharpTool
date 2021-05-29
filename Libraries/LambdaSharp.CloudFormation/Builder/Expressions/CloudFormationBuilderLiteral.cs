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

namespace LambdaSharp.CloudFormation.Builder.Expressions {

    public sealed class CloudFormationBuilderLiteral : ACloudFormationBuilderExpression {

        //--- Fields ---
        private readonly object _value;
        private readonly CloudFormationBuilderValueType _type;

        //--- Constructors ---
        public CloudFormationBuilderLiteral(string value) {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _type = CloudFormationBuilderValueType.String;
        }

        public CloudFormationBuilderLiteral(int value) {
            _value = value;
            _type = CloudFormationBuilderValueType.Number;
        }

        private CloudFormationBuilderLiteral(object value, CloudFormationBuilderValueType type) {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _type = type;
        }

        //--- Properties ---
        public override CloudFormationBuilderValueType ExpressionValueType => _type;
        public string Value => _value.ToString() ?? throw new NullValueException();

        //--- Methods ---
        public override ACloudFormationBuilderNode CloneNode() => new CloudFormationBuilderLiteral(_value, _type) {
            SourceLocation = SourceLocation
        };
    }
}