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

namespace LambdaSharp.CloudFormation.Builder.Functions {

    public abstract class ACloudFormationBuilderFunction {

        //--- Constructors ---
        protected ACloudFormationBuilderFunction(string functionName, CloudFormationBuilderValueType returnValueType) {
            FunctionName = functionName ?? throw new System.ArgumentNullException(nameof(functionName));
            ReturnValueType = returnValueType;
        }

        //--- Properties ---
        public string FunctionName { get; }

        public CloudFormationBuilderValueType ReturnValueType { get; }

        // TODO:
        //  * does the function take a literal, list, or map?
        //  * it literal, does it need to be a string or number?
        //  * if list, how many items and what are their names?
        //  * if map, what are the allowed/required keys
    }

    public class CloudFormationBuilderIntrinsicFunction : ACloudFormationBuilderFunction {

        //--- Constructors ---
        public CloudFormationBuilderIntrinsicFunction(string functionName, CloudFormationBuilderValueType returnValueType) : base(functionName, returnValueType) { }
    }
}