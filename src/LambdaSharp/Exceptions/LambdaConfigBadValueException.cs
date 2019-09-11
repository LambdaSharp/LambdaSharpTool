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
    /// The <see cref="LambdaConfigBadValueException"/> exception is thrown when a configuration value fails to validate.
    /// </summary>
    public class LambdaConfigBadValueException : ALambdaConfigException {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="LambdaConfigBadValueException"/> instance with the specified exception message and configuration key path.
        /// </summary>
        /// <param name="innerException">The inner exception instance.</param>
        /// <param name="path">The configuration key path.</param>
        public LambdaConfigBadValueException(Exception innerException, string path) : base(innerException, "error while validating config key value: '{0}'", path) { }
    }
}
