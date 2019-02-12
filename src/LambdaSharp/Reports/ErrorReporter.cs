/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using LambdaSharp.Reports;

namespace LambdaSharp.Reports {

    public class ErrorReporter {

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
        public ErrorReporter(
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
        public ErrorReport CreateReport(string requestId, string level, Exception exception, string format = null, params object[] args) {
            var message = FormatMessage(format, args) ?? exception?.Message;
            if(message == null) {
                return null;
            }
            var fingerprint = ToHash(
                (exception is ILambdaExceptionFingerprinter fingerprinter)
                ? fingerprinter.FingerprintValue
                : ToHash(message)
            );
            var traces = FlattenExceptions(exception)
                ?.Select(CreateStackTraceFromException)
                .Reverse()
                .ToList();
            var timestamp = Convert.ToInt64((DateTime.UtcNow - _epoch).TotalSeconds);
            return new ErrorReport {
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

        private ErrorReportStackTrace CreateStackTraceFromException(Exception exception) {
            var stackFrames = new StackTrace(exception, true).GetFrames();
            return new ErrorReportStackTrace {
                Exception = new ErrorReportExceptionInfo {
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
                    return new ErrorReportStackFrame {
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
