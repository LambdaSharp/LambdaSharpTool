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

namespace LambdaSharp.Logger {

    /// <summary>
    /// The <see cref="LambdaLogLevel"/> describes the severity of a log entry.
    /// </summary>
    public enum LambdaLogLevel {

        /// <summary>
        /// No error occurred. This log entry is for informational purposes only.
        /// </summary>
        INFO,

        /// <summary>
        /// An unexpected situation occurred that was dealt with. The successful processing of the message or
        /// invocation should not be impacted.
        /// </summary>
        WARNING,

        /// <summary>
        /// An error occurred while processing a message or invocation. The operation did not complete successfully.
        /// Future operations should not be affected by this error.
        /// </summary>
        ERROR,

        /// <summary>
        /// A fatal error occurred that prevents the current and all future messages or invocations from completing successfully.
        /// </summary>
        FATAL
    }
}
