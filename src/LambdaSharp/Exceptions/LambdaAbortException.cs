/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
    /// The <see cref="LambdaAbortException"/> class is used to exit a Lambda function with a processing error,
    /// but without reporting the error to the log aggregator. This behavior is useful for intentionally triggering
    /// the native retry logic of the AWS Lambda runtime without causing false-positive errors to appear in the logs.
    /// </summary>
    public class LambdaAbortException : ALambdaException {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="LambdaAbortException"/> instance with the specified exception message.
        /// </summary>
        /// <param name="format">The exception message.</param>
        /// <param name="args">Optional arguments for the exception message.</param>
        public LambdaAbortException(string format, params object[] args) : base(format, args) { }
    }
}
