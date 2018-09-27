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
    
    public class Payload {
        
        //--- Fields ---
        private readonly string _accessToken;
        private readonly RollbarData _data;

        //--- Constructors ---
        public Payload(string accessToken, RollbarData data) {
            if(string.IsNullOrWhiteSpace(accessToken)) {
                throw new ArgumentException("Cannot be null or whitespace.", "accessToken");
            }
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            _accessToken = accessToken;
            _data = data;
        }

        //--- Properties ---
        [JsonProperty("access_token")]
        public string AccessToken {
            get { return _accessToken; }
        }

        [JsonProperty("data")]
        public RollbarData RollbarData {
            get { return _data; }
        }
    }
}
