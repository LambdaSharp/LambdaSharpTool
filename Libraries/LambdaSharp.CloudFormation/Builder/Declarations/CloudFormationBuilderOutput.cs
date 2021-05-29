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

    public class CloudFormationBuilderOutput : ACloudFormationBuilderDeclaration {

        //--- Types ---
        public class Export : ACloudFormationBuilderNode {

            //--- Fields ---
            private ACloudFormationBuilderExpression? _name;

            //--- Properties ---
            public ACloudFormationBuilderExpression? Name {
                get => _name;
                set => _name = Adopt(value);
            }
        }

        //--- Fields ---
        private CloudFormationBuilderLiteral? _description;
        private ACloudFormationBuilderExpression? _value;
        private CloudFormationBuilderLiteral? _condition;

        //--- Constructors ---
        public CloudFormationBuilderOutput(CloudFormationBuilderLiteral logicalId) : base(logicalId) { }

        //--- Properties ---

        [Inspect]
        public CloudFormationBuilderLiteral? Description {
            get => _description;
            set => _description = Adopt(value);
        }

        [Inspect]
        public ACloudFormationBuilderExpression? Value {
            get => _value;
            set => _value = Adopt(value);
        }

        [Inspect]
        public CloudFormationBuilderLiteral? Condition {
            get => _condition;
            set => _condition = Adopt(value);
        }
    }
}