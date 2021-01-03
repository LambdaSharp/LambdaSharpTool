/*
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
using LambdaSharp.Logging.ErrorReports;
using LambdaSharp.Logging.ErrorReports.Models;

namespace LambdaSharp.Records.ErrorReports {

    internal class LogErrorReportGenerator : ILambdaErrorReportGenerator {

        //--- Fields ---
        private readonly ILambdaFunctionDependencyProvider _provider;

        //--- Constructors ---
        public LogErrorReportGenerator(ILambdaFunctionDependencyProvider provider)
            => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        //--- Methods ---
        public LambdaErrorReport CreateReport(string requestId, string level, Exception exception, string format = null, params object[] args) {
            var message = LambdaErrorReportGenerator.FormatMessage(format, args) ?? exception?.Message;
            if(message != null) {
                _provider.Log($"*** {level.ToString().ToUpperInvariant()}: {message}\n{PrintException()}");
            }
            return null;

            // local functions
            string PrintException() => (exception != null) ? exception.ToString() + "\n" : "";
        }
    }
}
