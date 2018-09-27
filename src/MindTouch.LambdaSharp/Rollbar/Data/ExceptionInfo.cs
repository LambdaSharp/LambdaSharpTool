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

    public class ExceptionInfo {
        
        //--- Fields ---
        private readonly string _className;
        private readonly string _description;
        private readonly string _message;

        //--- Constructors ---
        public ExceptionInfo(string className) {
            if(string.IsNullOrWhiteSpace(className)) {
                throw new ArgumentException("Cannot be null or whitespace.", "className");
            }
            _className = className;
        }

        public ExceptionInfo(string className, string message)
            : this(className) {
            _message = message;
        }

        public ExceptionInfo(string className, string message, string description)
            : this(className, message) {
            _description = description;
        }

        //--- Properties ---
        [JsonProperty("class")]
        public string ClassName {
            get { return _className; }
        }

        [JsonProperty("message")]
        public string Message {
            get { return _message; }
        }

        [JsonProperty("description")]
        public string Description {
            get { return _description; }
        }
    }
}
