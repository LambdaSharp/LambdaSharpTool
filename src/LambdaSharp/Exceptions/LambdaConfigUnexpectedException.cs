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
    /// The <see cref="LambdaConfigUnexpectedException"/> exception is thrown when an exception occurs inside a <see cref="LambdaConfig"/> instance
    /// operation and the exception is not derived from <see cref="ALambdaConfigException"/>. In that case <see cref="LambdaConfigUnexpectedException"/>
    /// exception is used to wrap the unexpected exception.
    /// </summary>
    public class LambdaConfigUnexpectedException : ALambdaConfigException {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="ALambdaConfigException"/> instance with the specified inner exception, configuration key path, and attempted action.
        /// </summary>
        /// <param name="innerException">The inner exception instance.</param>
        /// <param name="path">The configuration key path.</param>
        /// <param name="action">A description of the action that was being performed.</param>
        /// <returns></returns>
        public LambdaConfigUnexpectedException(Exception innerException, string path, string action) : base(innerException, "unexpected error accessing: '{0}' ({1})", path, action) { }
    }
}
