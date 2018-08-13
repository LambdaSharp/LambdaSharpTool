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
    
    public class Frame {
        
        //--- Fields ---
        private readonly string _code;
        private readonly CodeContext _codeContext;
        private readonly int? _columnNumber;
        private readonly string _fileName;
        private readonly int? _lineNumber;
        private readonly string _method;

        //--- Constructors ---
        public Frame(string fileName) {
            if(string.IsNullOrWhiteSpace(fileName)) {
                throw new ArgumentNullException(nameof(fileName));
            }
            _fileName = fileName;
        }

        public Frame(string fileName, int? lineNumber, int? columnNumber, string method)
            : this(fileName) {
            _lineNumber = lineNumber;
            _columnNumber = columnNumber;
            _method = method;
        }

        public Frame(string fileName, int? lineNumber, int? columnNumber, string method, string code, CodeContext codeContext)
            : this(fileName, lineNumber, columnNumber, method) {
            _code = code;
            _codeContext = codeContext;
        }

        //--- Properties ---
        [JsonProperty("filename")]
        public string FileName {
            get { return _fileName; }
        }

        [JsonProperty("lineno")]
        public int? LineNumber {
            get { return _lineNumber; }
        }

        [JsonProperty("colno")]
        public int? ColumnNumber {
            get { return _columnNumber; }
        }

        [JsonProperty("method")]
        public string Method {
            get { return _method; }
        }

        [JsonProperty("code")]
        public string Code {
            get { return _code; }
        }

        [JsonProperty("context")]
        public CodeContext CodeContext {
            get { return _codeContext; }
        }
    }
}
