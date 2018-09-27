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
using System.Collections.Specialized;
using System.Globalization;
using Newtonsoft.Json;

namespace MindTouch.Rollbar.Data {
    
    public class RollbarData {
        
        //--- Constants ---
        private const int MAX_ENVIRONMENT_LENGTH = 255;
        private const int MAX_CODE_VERSION_LENGTH = 40;
        private const int MAX_TITLE_LENGTH = 255;
        
        //--- Fields ---
        private readonly string _applicationContext;
        private readonly Body _body;
        private readonly string _codeVersion;
        private readonly NameValueCollection _custom;
        private readonly string _environment;
        private readonly string _fingerprint;
        private readonly string _framework;
        private readonly string _language;
        private readonly string _level;
        private readonly Person _person;
        private readonly string _platform;
        private readonly Request _request;
        private readonly Server _server;
        private readonly long? _timestamp;
        private readonly string _title;
        
        //--- Constructors ---
        public RollbarData(string environment, Body body) {
            if(string.IsNullOrWhiteSpace(environment)) {
                throw new ArgumentNullException(nameof(environment));
            }
            if(environment.Length > MAX_ENVIRONMENT_LENGTH) {
                throw new ArgumentOutOfRangeException(
                    "environment",
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Value cannot be longer than {0} characters.",
                        MAX_ENVIRONMENT_LENGTH));
            }
            if(body == null) {
                throw new ArgumentNullException("body");
            }
            _environment = environment;
            _body = body;
        }

        public RollbarData(
            string environment,
            Body body,
            string level,
            long? timestamp,
            string codeVersion,
            string platform,
            string language,
            string framework,
            string fingerprint,
            string title,
            Server server)
            : this(environment, body) {
            _level = level;
            _timestamp = timestamp;
            _platform = platform;
            _language = language;
            _framework = framework;
            _server = server;
            _fingerprint = fingerprint;
            if (string.IsNullOrWhiteSpace(title)) {
                _title = title;
            } else {
                var trim = title.Trim();
                _title = trim.Length > MAX_TITLE_LENGTH ? trim.Substring(0, MAX_TITLE_LENGTH) : trim;
            }
            if (!string.IsNullOrWhiteSpace(codeVersion)) {
                var trim = codeVersion.Trim();
                if(trim.Length > MAX_CODE_VERSION_LENGTH) {
                    throw new ArgumentOutOfRangeException(nameof(codeVersion), trim, "value too long");
                }
                _codeVersion = trim;
            }
        }

        public RollbarData(RollbarData data, Context context)
            : this(
                data.Environment,
                data.Body,
                data.Level,
                data.Timestamp,
                data.CodeVersion,
                data.Platform,
                data.Language,
                data.Framework,
                data.Fingerprint,
                data.Title,
                data.Server) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            if(context == null) {
                throw new ArgumentNullException("context");
            }
            _applicationContext = context.ApplicationContext;
            _request = context.Request;
            _person = context.Person;
            _custom = context.Custom;
        }

        public RollbarData(RollbarData data, string fingerprint)
            : this(
                data.Environment,
                data.Body,
                data.Level,
                data.Timestamp,
                data.CodeVersion,
                data.Platform,
                data.Language,
                data.Framework,
                fingerprint,
                data.Title,
                data.Server) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            _applicationContext = data.ApplicationContext;
            _request = data.Request;
            _person = data.Person;
            _custom = data.Custom;
        }

        public RollbarData(RollbarData data, Context context, string fingerprint)
            : this(
                data.Environment,
                data.Body,
                data.Level,
                data.Timestamp,
                data.CodeVersion,
                data.Platform,
                data.Language,
                data.Framework,
                fingerprint,
                data.Title,
                data.Server) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            if(context == null) {
                throw new ArgumentNullException("context");
            }
            _applicationContext = context.ApplicationContext;
            _request = context.Request;
            _person = context.Person;
            _custom = context.Custom;
        }

        //--- Properties ---
        [JsonProperty("environment")]
        public string Environment {
            get { return _environment; }
        }

        [JsonProperty("body")]
        public Body Body {
            get { return _body; }
        }

        [JsonProperty("level")]
        public string Level {
            get { return _level; }
        }

        [JsonProperty("timestamp")]
        public long? Timestamp {
            get { return _timestamp; }
        }

        [JsonProperty("code_version")]
        public string CodeVersion {
            get { return _codeVersion; }
        }

        [JsonProperty("platform")]
        public string Platform {
            get { return _platform; }
        }

        [JsonProperty("language")]
        public string Language {
            get { return _language; }
        }

        [JsonProperty("framework")]
        public string Framework {
            get { return _framework; }
        }

        [JsonProperty("context")]
        public string ApplicationContext {
            get { return _applicationContext; }
        }

        [JsonProperty("request")]
        public Request Request {
            get { return _request; }
        }

        [JsonProperty("person")]
        public Person Person {
            get { return _person; }
        }

        [JsonProperty("server")]
        public Server Server {
            get { return _server; }
        }

        [JsonProperty("custom")]
        public NameValueCollection Custom {
            get { return _custom; }
        }

        [JsonProperty("fingerprint")]
        public string Fingerprint {
            get { return _fingerprint; }
        }

        [JsonProperty("title")]
        public string Title {
            get { return _title; }
        }
    }
}
