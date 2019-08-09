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
    /// The <see cref="LambdaConfigValidationException"/> exception is thrown by the <see cref="LambdaConfig"/> validation helper methods.
    /// </summary>
    public class LambdaConfigValidationException : ALambdaConfigException {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="ALambdaConfigException"/> instance with the specified exception message.
        /// </summary>
        /// <param name="format">The exception message.</param>
        /// <param name="args">Optional arguments for the exception message.</param>
        public LambdaConfigValidationException(string format, params object[] args) : base(format, args) { }
    }
}