/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MindTouch.Rollbar.Data;
using Newtonsoft.Json;

namespace MindTouch.Rollbar {

    public class RollbarReporter {

        //--- Constants ---
        private const int MAX_TITLE_LENGTH = 255;
        private const string LANGUAGE = "csharp";

        //--- Class Fields ---
        private static readonly HashAlgorithm _algorithm = SHA1.Create();
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //--- Class Methods ---
        private static string CreateFingerprintFromBody(Body body) {
            string text;
            if(body.Message != null) {
                text = body.Message.Body;
            } else if(body.Trace != null) {
                text = CreateFromTraces(new[] { body.Trace });
            } else {
                text = CreateFromTraces(body.TraceChain.ToArray());
            }
            return ToHash(text);
        }

        private static string CreateFingerprintFromFingerprinter(ILambdaExceptionFingerprinter fingerprinter)
            => ToHash(fingerprinter.FingerprintValue);

        private static string ToHash(string value) {
            var hash = _algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            return string.Concat(hash.Select(x => x.ToString("X2")));
        }

        private static string CreateFromTraces(Trace[] traces) {
            var sb = new StringBuilder();
            foreach(var trace in traces) {
                sb.AppendLine(trace.Exception.ClassName);
                foreach(var frame in trace.Frames) {
                    if(frame.LineNumber != null) {
                        sb.AppendFormat($"\tat {frame.Method} in {frame.FileName}:{frame.LineNumber}");
                    } else {
                        sb.AppendFormat($"\tat {frame.Method} in {frame.FileName}");
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private IEnumerable<Exception> FlattenExceptions(Exception exception) {
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

        //--- Fields ---
        private readonly string _moduleName;
        private readonly string _environment;
        private readonly string _framework;
        private readonly string _gitSha;
        private readonly string _platform;
        private readonly JsonSerializerSettings _settings;
        private readonly Server _server;

        //--- Constructors ---
        public RollbarReporter(
            string moduleName,
            string environment,
            string platform,
            string framework,
            string gitSha
        ) {
            _moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _platform = $"AWS Lambda ({System.Environment.OSVersion})";
            _framework = framework;
            _gitSha = gitSha;
            _settings = new JsonSerializerSettings {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            _settings.Converters.Add(new NameValueCollectionConverter());
            var assembly = Assembly.GetExecutingAssembly();
            var host = Environment.MachineName;
            var root = Path.GetDirectoryName(assembly.GetName().CodeBase);
            _server = new Server(host, root, branch: null, codeVersion: null);
        }

        //--- Properties ---

        // TODO: convert `AccessToken` to `ModuleName`
        public string AccessToken =>  _moduleName;

        //--- Methods ---
        public Payload CreateFromException(Exception exception, string description, string level) {
            Body body;
            var exceptions = FlattenExceptions(exception);
            var traces = new List<Trace>();
            traces.Add(CreateTraceFromException(exceptions.First(), description));
            traces.AddRange(exceptions.Skip(1).Select(ex => CreateTraceFromException(ex, description: null)));
            traces.Reverse();
            body = new Body(traces);
            var fingerprint = (exception is ILambdaExceptionFingerprinter fingerprinter)
                ? CreateFingerprintFromFingerprinter(fingerprinter)
                : CreateFingerprintFromBody(body);
            var data = CreateFromBody(body, fingerprint, level);
            return new Payload(AccessToken, data);
        }

        public Payload CreateWithFingerprintInput(Payload payload, string fingerprintInput) {
            var data = new RollbarData(payload.RollbarData, ToHash(fingerprintInput));
            return new Payload(AccessToken, data);
        }

        public Payload CreateFromMessage(string message, string level) {
            var body = new Body(new Message(message));
            var fingerprint = CreateFingerprintFromBody(body);
            var data = CreateFromBody(body, fingerprint, level);
            return new Payload(AccessToken, data);
        }

        private RollbarData CreateFromBody(Body body, string fingerprint, string level) {
            var timestamp = Convert.ToInt64((DateTime.UtcNow - _epoch).TotalSeconds);
            var title = CreateTitleFromBody(body);
            return new RollbarData(
                _environment,
                body,
                level,
                timestamp,
                _gitSha,
                _platform,
                LANGUAGE,
                _framework,
                fingerprint,
                title,
                _server
            );
        }

        private Trace CreateTraceFromException(Exception exception, string description) {
            var ex = new ExceptionInfo(
                exception.GetType().Name,
                exception.Message,
                string.IsNullOrWhiteSpace(description)

                    // use the exception message when no custom description is provided
                    ? exception.ToString()

                    // combine the custom description with the exception message
                    : $"{description}{System.Environment.NewLine}{exception}"
            );
            var frames = CreateFrameCollectionFromException(exception);
            return new Trace(ex, frames);
        }

        private string CreateTitleFromBody(Body body) {
            string title;
            if(body.Message != null) {
                title = body.Message.Body;
            } else {
                var trace = body.Trace ?? body.TraceChain.LastOrDefault() ?? body.TraceChain.Last();
                title = $"{trace.Exception.ClassName}: {trace.Exception.Message}";
            }
            return (title.Length > MAX_TITLE_LENGTH) ? title.Substring(0, MAX_TITLE_LENGTH) : title;
        }

        private IEnumerable<Frame> CreateFrameCollectionFromException(Exception exception) {
            var stackTrace = new System.Diagnostics.StackTrace(exception, true);
            var lines = new List<Frame>();
            var stackFrames = stackTrace.GetFrames();
            if(stackFrames == null || stackFrames.Length == 0) {
                return lines;
            }

            // process all stack frames
            foreach(var frame in stackFrames) {
                var lineNumber = frame.GetFileLineNumber();
                var fileName = frame.GetFileName();

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

                // when the line number is zero, you can try using the IL offset
                if(lineNumber == 0) {
                    lineNumber = frame.GetILOffset();
                }

                if(lineNumber == -1) {
                    lineNumber = frame.GetNativeOffset();
                }

                // line numbers less than 0 are not accepted
                if(lineNumber < 0) {
                    lineNumber = 0;
                }

                // file names aren't always available, so use the type name instead, if possible
                if(string.IsNullOrEmpty(fileName)) {
                    fileName = method.ReflectedType.ToString();
                }

                // NOTE: Set CodeContext and Code (lines of code above and below the line that raised the exception).
                lines.Add(new Frame(fileName, lineNumber, null, methodName));
            }

            return lines.ToArray();
        }
    }
}
