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

using System;

namespace LambdaSharp.Exceptions {

    /// <summary>
    /// The <see cref="LambdaRetriableException"/> class represents a runtime error that
    /// can be retried. If the Lambda function allows retries, this exception will be logged
    /// as having a <i>Warning</i> severity rather than an <i>Error</i> severity.
    /// See <see cref="Logger.LambdaLogLevel"/> for a description of the severity levels.
    /// </summary>
    public class LambdaRetriableException : ALambdaException {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="LambdaRetriableException"/> instance with the specified exception message and arguments.
        /// </summary>
        /// <param name="format">The exception message.</param>
        /// <param name="args">Optional arguments for the exception message.</param>
        public LambdaRetriableException(string format, params object[] args)
            : base(format, args) { }

        /// <summary>
        /// Initializes a <see cref="LambdaRetriableException"/> instance with the specified exception message and inner exception.
        /// </summary>
        /// <param name="innerException">The inner exception instance.</param>
        /// <param name="format">The exception message.</param>
        /// <param name="args">Optional arguments for the exception message.</param>
        public LambdaRetriableException(Exception innerException, string format, params object[] args)
            : base(innerException, format, args) { }
    }
}