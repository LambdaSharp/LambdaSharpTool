/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Linq;
using LambdaSharp.Records.ErrorReports;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="ILambdaErrorReportGenerator"/> interface is used to create
    /// <see cref="LambdaErrorReport"/> instances.
    /// </summary>
    public interface ILambdaErrorReportGenerator {

        /// <summary>
        /// The <see cref="FormatMessage(string,object[])"/> method behaves identically to the <see cref="string.Format(string,object[])"/> method
        /// when the <paramref name="format"/> parameter is not <c>null</c> and the <paramref name="args"/> parameter is non-empty.
        /// If the <paramref name="format"/> parameter is <c>null</c>, this method returns <c>null</c>. If the <paramref name="args"/> parameter is empty,
        /// this method return the value of the <paramref name="format"/> parameter.
        /// </summary>
        /// <param name="format">An optional message.</param>
        /// <param name="args">Optional arguments for the error message.</param>
        /// <returns>The formatted string.</returns>
        public static string? FormatMessage(string? format, object?[]? args) {
            if(format == null) {
                return null;
            }
            if((args == null) || (args.Length == 0)) {
                return format;
            }
            try {
                return string.Format(format, args);
            } catch {
                return $@"{format}({string.Join(", ", args.Select(SafeToString))})";
            }

            // local functions
            string SafeToString(object? arg) {
                try {
                    return arg?.ToString() ?? "<NULL>";
                } catch {
                    return "<ERROR>";
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// The <see cref="CreateReport(string,string,Exception,string,object[])"/> method create a new
        /// <see cref="LambdaErrorReport"/> instance from the provided parameters.
        /// </summary>
        /// <remarks>
        /// See <cref name="FormatMessage(string,object[])"/> for details on how the <paramref name="args"/> parameter
        /// impacts the processing of the <paramref name="format"/> parameter.
        /// </remarks>
        /// <param name="requestId">The AWS request ID.</param>
        /// <param name="level">The severity level of the error report.</param>
        /// <param name="exception">An optional exception instance.</param>
        /// <param name="format">An optional message.</param>
        /// <param name="args">Optional arguments for the error message.</param>
        /// <returns>A new <see cref="LambdaErrorReport"/> instance.</returns>
        LambdaErrorReport? CreateReport(string requestId, string level, Exception exception, string? format = null, params object?[] args);
    }
}
