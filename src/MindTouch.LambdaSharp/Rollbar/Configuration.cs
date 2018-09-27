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
using MindTouch.Rollbar.Builders;
using MindTouch.Rollbar.Data;
using Newtonsoft.Json;

namespace MindTouch.Rollbar {

    public class RollbarReporter {

        //--- Fields ---
        private readonly string _moduleName;
        private readonly string _environment;
        private readonly string _framework;
        private readonly string _gitSha;
        private readonly string _platform;
        private readonly JsonSerializerSettings _settings;
        private readonly PayloadBuilder _payloadBuilder;

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

            var frame = new FrameCollectionBuilder();
            var exception = new ExceptionInfoBuilder();
            var trace = new TraceBuilder(exception, frame);
            var traceChain = new TraceChainBuilder(trace);
            var body = new BodyBuilder(trace, traceChain);
            var title = new TitleBuilder();
            var data = new DataBuilder(this, body, title);
            _payloadBuilder = new PayloadBuilder(this, data);
        }

        //--- Properties ---
        public string AccessToken =>  _moduleName;
        public string Environment => _environment;
        public string Platform  => _platform;
        public string Language => "csharp";
        public string Framework => _framework;
        public string GitSha => _gitSha;

        //--- Methods ---
        public Payload CreateFromException(Exception exception, string description, string level)
            => _payloadBuilder.CreateFromException(exception, description, level);

        public Payload CreateFromMessage(string message, string level)
            => _payloadBuilder.CreateFromMessage(message, level);

        public Payload CreateWithFingerprintInput(Payload payload, string fingerprintInput)
            => _payloadBuilder.CreateWithFingerprintInput(payload, fingerprintInput);

    }
}
