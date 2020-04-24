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

namespace LambdaSharp.Exceptions {

    /// <summary>
    /// The <see cref="LambdaMissingLambdaSerializerAttributeException"/> exception is thrown when now
    /// Lambda serializer is defined in the assembly containing the Lambda function.
    /// </summary>
    public class LambdaMissingLambdaSerializerAttributeException : ALambdaException {

        //--- Constructors ---

        /// <summary>
        /// Initialize a new instance.
        /// </summary>
        public LambdaMissingLambdaSerializerAttributeException() : base("could not find Amazon.Lambda.Core.LambdaSerializerAttribute on assembly") { }
    }
}