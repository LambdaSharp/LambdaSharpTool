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
using System.Collections.Generic;
using System.Linq;

namespace LambdaSharp.ErrorReports {

    /// <summary>
    /// The <see cref="LambdaErrorReportStackTrace"/> class describes an exception trace, which
    /// includes information about the exception and the stack frames between where the exception
    /// was thrown and where it was caught.
    /// </summary>
    public class LambdaErrorReportStackTrace {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Exception"/> property holds information about the exception.
        /// </summary>
        /// <value>The exception details.</value>
        public LambdaErrorReportExceptionInfo Exception { get; set; }

        /// <summary>
        /// The <see cref="Frames"/> property holds the stack frames between where the exception
        /// was thrown and where it was caught.
        /// </summary>
        /// <value>The exception stack frames.</value>
        public IEnumerable<LambdaErrorReportStackFrame> Frames { get; set; }
    }
}
