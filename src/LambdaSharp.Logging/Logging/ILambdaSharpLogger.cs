﻿/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Text.RegularExpressions;

namespace LambdaSharp.Logging {

    /// <summary>
    /// <see cref="ILambdaSharpLogger"/> provides the fundamental logging capabilities. Additional logging methods are provide
    /// as extension methods by <see cref="ILambdaSharpLoggerEx"/>.
    /// </summary>
    public interface ILambdaSharpLogger {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Info"/> property return information about the LambdaSharp environment.
        /// </summary>
        /// <value>The <see cref="ILambdaSharpInfo"/> instance.</value>
        ILambdaSharpInfo Info { get; }

        /// <summary>
        /// The <see cref="DebugLoggingEnabled"/> property indicates if log statements using <see cref="LambdaLogLevel.DEBUG"/> are emitted.
        /// </summary>
        /// <value>Boolean indicating if requests and responses are logged.</value>
        bool DebugLoggingEnabled { get; }

        //--- Methods ---

        /// <summary>
        /// Log a message wit the given severity level. The <c>format</c> string is used to create a unique signature for errors.
        /// Therefore, any error information that varies between occurrences should be provided in the <c>arguments</c> parameter.
        /// </summary>
        /// <remarks>
        /// Nothing is logged if both <paramref name="format"/> and <paramref name="exception"/> are null.
        /// </remarks>
        /// <param name="level">The severity level of the log message. See <see cref="LambdaLogLevel"/> for a description of the severity levels.</param>
        /// <param name="exception">Optional exception to log. The exception is logged with its description and stacktrace. This parameter can be <c>null</c>.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        void Log(LambdaLogLevel level, Exception exception, string format, params object[] arguments);

        /// <summary>
        /// Log a <see cref="ALambdaLogRecord"/> record instance.
        /// </summary>
        /// <param name="record">The record to log.</param>
        void LogRecord(ALambdaLogRecord record);
    }
}
