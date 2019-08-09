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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LambdaSharp.Exceptions;

namespace LambdaSharp.ErrorReports {

    /// <summary>
    /// The <see cref="LambdaErrorReportGenerator"/> class is used to create
    /// <see cref="LambdaErrorReport"/> instances. Each Lambda function is
    /// initialized with an instance of <see cref="LambdaErrorReportGenerator"/>
    /// class using its configuration and runtime settings.
    /// Use the <see cref="ALambdaFunction.ErrorReportGenerator"/> property to access
    /// the initialized <see cref="LambdaErrorReportGenerator"/> instance in the
    /// Lambda function.
    /// </summary>
    public class LambdaErrorReportGenerator {

        //--- Constants ---
        private const string LANGUAGE = "csharp";

        //--- Class Fields ---
        private static readonly HashAlgorithm _algorithm = MD5.Create();
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //--- Class Methods ---
        private static string ToHash(string value) {
            var hash = _algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            return string.Concat(hash.Select(x => x.ToString("X2")));
        }

        private IEnumerable<Exception> FlattenExceptions(Exception exception) {
            if(exception == null) {
                return null;
            }
            return Enumerate().ToArray();

            // local functions
            IEnumerable<Exception> Enumerate() {
                var current = exception;
                do {
                    yield return current;
                    current = current.InnerException;
                } while(current != null);
            }
        }

        /// <summary>
        /// The <see cref="FormatMessage(string,object[])"/> method behaves identically to the <see cref="string.Format(string,object[])"/> method
        /// when the <paramref name="format"/> parameter is not <c>null</c> and the <paramref name="args"/> parameter is non-empty.
        /// If the <paramref name="format"/> parameter is <c>null</c>, this method returns <c>null</c>. If the <paramref name="args"/> parameter is empty,
        /// this method return the value of the <paramref name="format"/> parameter.
        /// </summary>
        /// <param name="format">An optional message.</param>
        /// <param name="args">Optional arguments for the error message.</param>
        /// <returns>The formatted string.</returns>
        public static string FormatMessage(string format, object[] args) {
            if(format == null) {
                return null;
            }
            if(args.Length == 0) {
                return format;
            }
            try {
                return string.Format(format, args);
            } catch {
                return format + "(" + string.Join(", ", args.Select(arg => {
                    try {
                        return arg.ToString();
                    } catch {
                        return "<ERROR>";
                    }
                })) + ")";
            }
        }

        //--- Fields ---
        private readonly string _moduleId;
        private readonly string _moduleInfo;
        private readonly string _functionId;
        private readonly string _functionName;
        private readonly string _framework;
        private readonly string _gitSha;
        private readonly string _gitBranch;
        private readonly string _platform;

        //--- Constructors ---

        /// <summary>
        /// Creates a new <see cref="LambdaErrorReportGenerator"/> instance with the specified configuration settings.
        /// </summary>
        /// <param name="moduleId">The ID of the deployed LambdaSharp module.</param>
        /// <param name="moduleInfo">The LambdaSharp module name and version.</param>
        /// <param name="functionId">The ID of the deployed Lambda function.</param>
        /// <param name="functionName">The Lambda function name.</param>
        /// <param name="framework">The Lambda execution framework.</param>
        /// <param name="gitSha">An optional git SHA.</param>
        /// <param name="gitBranch">An optional git branch name.</param>
        public LambdaErrorReportGenerator(
            string moduleId,
            string moduleInfo,
            string functionId,
            string functionName,
            string framework,
            string gitSha,
            string gitBranch
        ) {
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _moduleInfo = moduleInfo ?? throw new ArgumentNullException(nameof(moduleInfo));
            _platform = $"AWS Lambda ({System.Environment.OSVersion})";
            _functionId = functionId ?? throw new ArgumentNullException(nameof(functionName));
            _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
            _framework = framework;
            _gitSha = gitSha;
            _gitBranch = gitBranch;
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
        public LambdaErrorReport CreateReport(string requestId, string level, Exception exception, string format = null, params object[] args) {
            var message = FormatMessage(format, args) ?? exception?.Message;
            if(message == null) {
                return null;
            }
            var fingerprint = ToHash(
                (exception is ILambdaExceptionFingerprinter fingerprinter)
                ? fingerprinter.FingerprintValue
                : message
            );
            var traces = FlattenExceptions(exception)
                ?.Select(CreateStackTraceFromException)
                .Reverse()
                .ToList();
            var timestamp = Convert.ToInt64((DateTime.UtcNow - _epoch).TotalSeconds);
            return new LambdaErrorReport {
                Module = _moduleInfo,
                ModuleId = _moduleId,
                RequestId = requestId,
                Level = level,
                Fingerprint = fingerprint,
                Timestamp = timestamp,
                Message = message,
                Traces = traces,
                Platform = _platform,
                FunctionId = _functionId,
                FunctionName = _functionName,
                Framework = _framework,
                Language = LANGUAGE,
                GitSha = _gitSha,
                GitBranch = _gitBranch
            };
        }

        private LambdaErrorReportStackTrace CreateStackTraceFromException(Exception exception) {
            var stackFrames = new StackTrace(exception, true).GetFrames();
            return new LambdaErrorReportStackTrace {
                Exception = new LambdaErrorReportExceptionInfo {
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                },
                Frames = stackFrames?.Select(frame => {

                    // capture information about invoked method
                    string methodName = null;
                    var method = frame.GetMethod();
                    if(method != null) {
                        var methodParams = method.GetParameters();

                        // add method parameters to the method name. helpful for resolving overloads.
                        methodName = method.Name;
                        if(methodParams.Length > 0) {
                            var paramDesc = string.Join(", ", methodParams.Select(p => p.ParameterType + " " + p.Name));
                            methodName = methodName + "(" + paramDesc + ")";
                        }
                    }

                    // try to figure out code line-number
                    int? lineNumber = frame.GetFileLineNumber();
                    if(lineNumber <= 0) {
                        lineNumber = null;
                    }

                    // file names aren't always available, so use the type name instead, if possible
                    var fileName = frame.GetFileName();
                    if(string.IsNullOrEmpty(fileName)) {
                        fileName = method.ReflectedType.ToString();
                    }
                    return new LambdaErrorReportStackFrame {
                        FileName = fileName,
                        LineNumber = lineNumber,
                        ColumnNumber = null,
                        MethodName = methodName
                    };
                }).ToArray()
            };
        }
    }
}
