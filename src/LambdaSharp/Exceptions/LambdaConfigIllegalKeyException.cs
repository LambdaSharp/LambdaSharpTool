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
    /// The <see cref="LambdaConfigIllegalKeyException"/> exception is thrown when an invalid configuration key is used.
    /// </summary>
    public class LambdaConfigIllegalKeyException : ALambdaConfigException {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="LambdaConfigIllegalKeyException"/> instance with the specified key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        public LambdaConfigIllegalKeyException(string key) : base("config key must be alphanumeric: '{0}'", key) { }
    }
}
