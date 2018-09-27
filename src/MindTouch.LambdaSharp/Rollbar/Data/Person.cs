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
using System.Globalization;
using Newtonsoft.Json;

namespace MindTouch.Rollbar.Data {
    
    public class Person {
        
        //--- Constants ---
        private const int MAX_ID_LENGTH = 40;

        //--- Fields ---
        private readonly string _id;

        //--- Constructors ---
        public Person(string id) {
            if(string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException("Cannot be null or whitespace.", "id");
            }
            if(id.Length > MAX_ID_LENGTH) {
                throw new ArgumentOutOfRangeException("id", string.Format(CultureInfo.CurrentCulture, "Value cannot be longer than {0} characters.", MAX_ID_LENGTH));
            }
            _id = id;
        }

        //--- Properties ---
        [JsonProperty("id")]
        public string Id {
            get { return _id; }
        }
    }
}
