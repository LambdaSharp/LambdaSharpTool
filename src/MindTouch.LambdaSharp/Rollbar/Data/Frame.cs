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
        private readonly int? _columnNumber;
        private readonly string _fileName;
        private readonly int? _lineNumber;
        private readonly string _method;

        //--- Constructors ---
        public Frame(string fileName) {
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public Frame(string fileName, int? lineNumber, int? columnNumber, string method) : this(fileName) {
            _lineNumber = lineNumber;
            _columnNumber = columnNumber;
            _method = method;
        }

        //--- Properties ---
        [JsonProperty("filename")]
        public string FileName => _fileName;

        [JsonProperty("lineno")]
        public int? LineNumber => _lineNumber;

        [JsonProperty("colno")]
        public int? ColumnNumber => _columnNumber;

        [JsonProperty("method")]
        public string Method => _method;
    }
}
