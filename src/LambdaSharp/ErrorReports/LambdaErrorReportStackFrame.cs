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
    /// The <see cref="LambdaErrorReportStackFrame"/> class describes an execution stack frame.
    /// </summary>
    public class LambdaErrorReportStackFrame {

        //--- Properties ---

        /// <summary>
        /// The <see cref="FileName"/> property describes the source code file name of the stack trace.
        /// </summary>
        /// <value>Source code file name or <c>null</c> if missing..</value>
        public string FileName { get; set; }

        /// <summary>
        /// The <see cref="LineNumber"/> property describes the line number in the source code.
        /// </summary>
        /// <value>Source code line number or <c>null</c> if missing.</value>
        public int? LineNumber { get; set; }

        /// <summary>
        /// The <see cref="ColumnNumber"/> property describes the column number in the source code.
        /// </summary>
        /// <value>Source code column number or <c>null</c> if missing.</value>
        public int? ColumnNumber { get; set; }

        /// <summary>
        /// The <see cref="MethodName"/> property describes the method name in which the stack frame is located.
        /// </summary>
        /// <value>Name of the method.</value>
        public string MethodName { get; set; }
    }
}
