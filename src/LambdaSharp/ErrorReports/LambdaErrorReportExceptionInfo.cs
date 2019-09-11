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

namespace LambdaSharp.ErrorReports {

    /// <summary>
    /// The <see cref="LambdaErrorReportExceptionInfo"/> class describes a runtime exception.
    /// </summary>
    public class LambdaErrorReportExceptionInfo {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Type"/> property holds the full exception type name.
        /// </summary>
        /// <value>The type name of the exception.</value>
        public string Type { get; set; }

        /// <summary>
        /// The <see cref="Message"/> property holds the exception message.
        /// </summary>
        /// <value>The exception message.</value>
        public string Message { get; set; }

        /// <summary>
        /// The <see cref="StackTrace"/> property holds the unparsed exception stack trace.
        /// </summary>
        /// <value>The exception stack trace.</value>
        public string StackTrace { get; set; }
    }
}
