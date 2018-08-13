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

namespace MindTouch.Rollbar.Data {
    
    public class Server {
        
        //--- Constants ---
        private const int MAX_CODE_VERSION_LENGTH = 40;
        
        //--- Fields ---
        private readonly string _branch;
        private readonly string _codeVersion;
        private readonly string _host;
        private readonly string _root;

        //--- Constructors ---
        public Server(string host, string root, string branch, string codeVersion) {
            _host = host;
            _root = root;
            _branch = branch;
            if(!string.IsNullOrWhiteSpace(codeVersion)) {
                var trim = codeVersion.Trim();
                if(trim.Length > MAX_CODE_VERSION_LENGTH) {
                    throw new ArgumentOutOfRangeException(nameof(codeVersion), trim, "value too long");
                }
                _codeVersion = trim;
            }
        }

        //--- Properties ---
        [JsonProperty("host")]
        public string Host {
            get { return _host; }
        }

        [JsonProperty("root")]
        public string Root {
            get { return _root; }
        }

        [JsonProperty("branch")]
        public string Branch {
            get { return _branch; }
        }

        [JsonProperty("code_version")]
        public string CodeVersion {
            get { return _codeVersion; }
        }
    }
}
