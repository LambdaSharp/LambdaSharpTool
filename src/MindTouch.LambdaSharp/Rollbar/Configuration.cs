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

        //--- Fields ---
        private readonly string _moduleName;
        private readonly string _environment;
        private readonly string _framework;
        private readonly string _gitSha;
        private readonly string _platform;
        private readonly JsonSerializerSettings _settings;
        private readonly ExceptionInfoBuilder _exceptionBuilder;
        private readonly FrameCollectionBuilder _frameBuilder;
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
            var exceptions = exception.FlattenHierarchy();
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
            var title = _titleBuilder.CreateFromBody(body);
            return new RollbarData(
                Environment,
                body,
                level,
                timestamp,
                GitSha,
                Platform,
                Language,
                Framework,
                fingerprint,
                title,
                ServerBuilder.Instance.Server
            );
        }

        private Trace CreateTraceFromException(Exception exception, string description) {
            var ex = _exceptionBuilder.CreateFromException(exception, description);
            var frames = _frameBuilder.CreateFromException(exception);
            return new Trace(ex, frames);
        }
    }
}
