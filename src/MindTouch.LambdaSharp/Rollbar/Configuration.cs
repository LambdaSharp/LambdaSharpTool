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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MindTouch.Rollbar.Builders;
using MindTouch.Rollbar.Data;
using Newtonsoft.Json;

namespace MindTouch.Rollbar {

    public class RollbarReporter {

        //--- Class Fields ---
        private static readonly HashAlgorithm _algorithm = SHA1.Create();

        //--- Class Methods ---
        private static string CreateFingerprintFromBody(Body body) {
            string text;
            if(body.Message != null) {
                text = body.Message.Body;
            } else if(body.Trace != null) {
                text = CreateFromTraces(body.Trace);
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

        private static string CreateFromTraces(params Trace[] traces) {
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

        //--- Fields ---
        private readonly string _moduleName;
        private readonly string _environment;
        private readonly string _framework;
        private readonly string _gitSha;
        private readonly string _platform;
        private readonly JsonSerializerSettings _settings;
        private readonly ExceptionInfoBuilder _exceptionBuilder;
        private readonly FrameCollectionBuilder _frameBuilder;
        private readonly TraceBuilder _traceBuilder;
        private readonly TraceChainBuilder _traceChainBuilder;
        private readonly TitleBuilder _titleBuilder;

        //--- Constructors ---
        public RollbarReporter(
            string moduleName,
            string environment,
            string platform,
            string framework,
            string gitSha
        ) {
            if(string.IsNullOrWhiteSpace(moduleName)) {
                throw new ArgumentNullException(nameof(moduleName));
            }
            _moduleName = moduleName;
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _platform = $"AWS Lambda ({System.Environment.OSVersion})";
            _framework = framework;
            _gitSha = gitSha;
            _settings = new JsonSerializerSettings {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            _settings.Converters.Add(new NameValueCollectionConverter());
            _frameBuilder = new FrameCollectionBuilder();
            _exceptionBuilder = new ExceptionInfoBuilder();
            _traceBuilder = new TraceBuilder(_exceptionBuilder, _frameBuilder);
            _traceChainBuilder = new TraceChainBuilder(_traceBuilder);
            _titleBuilder = new TitleBuilder();
        }

        //--- Properties ---

        // TODO: convert `AccessToken` to `ModuleName`
        public string AccessToken =>  _moduleName;
        public string Environment => _environment;
        public string Platform  => _platform;
        public string Language => "csharp";
        public string Framework => _framework;
        public string GitSha => _gitSha;

        //--- Methods ---
        public Payload CreateFromException(Exception exception, string description, string level) {
            Body body;
            if(exception.InnerException == null) {
                var trace = _traceBuilder.CreateFromException(exception, description);
                body = new Body(trace);
            } else {
                var traces = _traceChainBuilder.CreateFromException(exception, description);
                body = new Body(traces);
            }
            var fingerprinter = exception as ILambdaExceptionFingerprinter;
            var fingerprint = (fingerprinter == null)
                ? CreateFingerprintFromBody(body)
                : CreateFingerprintFromFingerprinter(fingerprinter);
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
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = Convert.ToInt64((DateTime.UtcNow - epoch).TotalSeconds);
            var platform = Platform;
            var language = Language;
            var framework = Framework;
            var title = _titleBuilder.CreateFromBody(body);
            var server = ServerBuilder.Instance.Server;
            string codeVersion = GitSha;
            return new RollbarData(
                Environment,
                body,
                level,
                timestamp,
                codeVersion,
                platform,
                language,
                framework,
                fingerprint,
                title,
                server
            );
        }
    }
}
