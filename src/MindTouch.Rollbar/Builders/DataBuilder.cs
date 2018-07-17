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
using MindTouch.Rollbar.Data;

namespace MindTouch.Rollbar.Builders {
    public class DataBuilder : IDataBuilder {

        //--- Class Fields ---
        private static readonly HashAlgorithm _algorithm = SHA1.Create();

        //--- Fields ---
        private readonly IBodyBuilder _bodyBuilder;
        private readonly RollbarConfiguration _configuration;
        private readonly ITitleBuilder _titleBuilder;

        //--- Constructors ---
        public DataBuilder(
            RollbarConfiguration configuration,
            IBodyBuilder bodyBuilder,
            ITitleBuilder titleBuilder
        ) {
            if(configuration == null) {
                throw new ArgumentNullException("configuration");
            }
            if(bodyBuilder == null) {
                throw new ArgumentNullException("bodyBuilder");
            }
            if(titleBuilder == null) {
                throw new ArgumentNullException("titleBuilder");
            }
            _configuration = configuration;
            _bodyBuilder = bodyBuilder;
            _titleBuilder = titleBuilder;
        }

        //--- Methods ---
        public RollbarData CreateFromException(Exception exception, string description, string level) {
            var body = _bodyBuilder.CreateFromException(exception, description);
            var fingerprinter = exception as IRollbarFingerprinter;
            var fingerprint = fingerprinter == null
                                  ? CreateFingerprintFromBody(body)
                                  : CreateFingerprintFromFingerprinter(fingerprinter);
            var data = CreateFromBody(body, fingerprint, level);
            return data;
        }

        public RollbarData CreateFromMessage(string message, string level) {
            var body = _bodyBuilder.CreateFromMessage(message);
            var fingerprint = CreateFingerprintFromBody(body);
            var data = CreateFromBody(body, fingerprint, level);
            return data;
        }

        public RollbarData CreateWithContext(RollbarData data, Context context) {
            return new RollbarData(data, context);
        }

        public RollbarData CreateWithFingerprintInput(RollbarData data, string fingerprintInput) {
            return new RollbarData(data, ToHash(fingerprintInput));
        }

        public RollbarData CreateWithContextAndFingerprintInput(RollbarData data, Context context, string fingerprintInput) {
            return new RollbarData(data, context, ToHash(fingerprintInput));
        }

        private RollbarData CreateFromBody(Body body, string fingerprint, string level) {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = Convert.ToInt64((DateTime.UtcNow - epoch).TotalSeconds);
            var platform = _configuration.Platform;
            var language = _configuration.Language;
            var framework = _configuration.Framework;
            var title = _titleBuilder.CreateFromBody(body);
            var server = ServerBuilder.Instance.Server;
            string codeVersion = null;
            if(!string.IsNullOrWhiteSpace(_configuration.GitSha)) {
                codeVersion = _configuration.GitSha;
            }
            return new RollbarData(
                _configuration.Environment,
                body,
                level,
                timestamp,
                codeVersion,
                platform,
                language,
                framework,
                fingerprint,
                title,
                server);
        }

        private static string CreateFingerprintFromBody(Body body) {
            var value = ToString(body);
            return ToHash(value);
        }

        private static string CreateFingerprintFromFingerprinter(IRollbarFingerprinter fingerprinter) {
            var value = fingerprinter.FingerprintValue;
            return ToHash(value);
        }

        private static string ToHash(string value) {
            var hash = _algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            return string.Concat(hash.Select(x => x.ToString("X2")));
        }

        private static string ToString(Body body) {
            if(body.Message != null) {
                return body.Message.Body;
            }
            if(body.Trace != null) {
                return CreateFromTraces(body.Trace);
            }
            return CreateFromTraces(body.TraceChain.ToArray());
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
    }
}
