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
using Newtonsoft.Json;

namespace MindTouch.Rollbar {

    public class RollbarConfiguration {

        //--- Constants ---
        private const string DEFAULT_LANGUAGE = "csharp";
        private const string GIT_SHA_ENVIRONMENT_VARIABLE = "GIT_SHA";

        //--- Fields ---
        private readonly string _accessToken;
        private readonly string _proxy;
        private readonly string _environment;
        private readonly string _framework;
        private readonly string _gitSha;
        private readonly string _platform;
        private readonly JsonSerializerSettings _settings;

        //--- Constructors ---
        public RollbarConfiguration(
            string accessToken,
            string proxy,
            string environment,
            string platform,
            string framework,
            string gitSha
        ) {
            if(string.IsNullOrWhiteSpace(accessToken)) {
                throw new ArgumentNullException(nameof(accessToken));
            }
            _accessToken = accessToken;
            _proxy = proxy;
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _platform = platform ?? System.Environment.OSVersion.ToString();
            _framework = framework ?? ".NET " + System.Environment.Version;
            _gitSha = gitSha ?? System.Environment.GetEnvironmentVariable(GIT_SHA_ENVIRONMENT_VARIABLE);
            _settings = new JsonSerializerSettings {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            _settings.Converters.Add(new NameValueCollectionConverter());
        }

        //--- Properties ---
        public string AccessToken =>  _accessToken;
        public string Environment => _environment;
        public string Platform  => _platform;
        public string Language => DEFAULT_LANGUAGE;
        public string Framework => _framework;
        public string GitSha => _gitSha;
    }
}
