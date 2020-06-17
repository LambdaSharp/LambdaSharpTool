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

namespace LambdaSharp.CloudFormation.Template {

    public static class CloudFormationFunction {

        //--- Class Methods ---
        public static CloudFormationLiteral Literal(string value) => new CloudFormationLiteral(value);
        public static CloudFormationLiteral Literal(int value) => new CloudFormationLiteral(value);

        public static CloudFormationObject Ref(string name) => new CloudFormationObject {
            ["Ref"] = new CloudFormationLiteral(name)
        };

        public static CloudFormationObject Not(ACloudFormationExpression expression) => new CloudFormationObject {
            ["Fn::Not"] = new CloudFormationList {
                expression
            }
        };

        public static CloudFormationObject Equals(ACloudFormationExpression left, ACloudFormationExpression right) => new CloudFormationObject {
            ["Fn::Equals"] = new CloudFormationList {
                left,
                right
            }
        };

        public static CloudFormationObject Select(int index, ACloudFormationExpression list) => new CloudFormationObject {
            ["Fn::Select"] = new CloudFormationList {
                Literal(index),
                list
            }
        };

        public static CloudFormationObject Split(string delimiter, ACloudFormationExpression value) => new CloudFormationObject {
            ["Fn::Split"] = new CloudFormationList {
                Literal(delimiter),
                value
            }
        };

        public static CloudFormationObject And(ACloudFormationExpression left, ACloudFormationExpression right) => new CloudFormationObject {
            ["Fn::And"] = new CloudFormationList {
                left,
                right
            }
        };

        public static CloudFormationObject If(string condition, ACloudFormationExpression ifTrue, ACloudFormationExpression ifFalse) => new CloudFormationObject {
            ["Fn::If"] = new CloudFormationList {
                Literal(condition),
                ifTrue,
                ifFalse
            }
        };

        public static CloudFormationObject ImportValue(ACloudFormationExpression expression) => new CloudFormationObject {
            ["Fn::ImportValue"] = new CloudFormationList {
                expression
            }
        };

        public static CloudFormationObject Sub(string formatString) => new CloudFormationObject {
            ["Fn::Sub"] = new CloudFormationList {
                Literal(formatString)
            }
        };

        public static CloudFormationObject Sub(string formatString, ACloudFormationExpression parameters) => new CloudFormationObject {
            ["Fn::Sub"] = new CloudFormationList {
                Literal(formatString),
                parameters
            }
        };
    }
}