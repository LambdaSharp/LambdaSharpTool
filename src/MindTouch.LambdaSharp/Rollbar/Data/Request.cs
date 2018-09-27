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


using System.Collections.Specialized;
using Newtonsoft.Json;

namespace MindTouch.Rollbar.Data {
    
    public class Request {
        
        //--- Fields ---
        private readonly string _body;
        private readonly NameValueCollection _headers;
        private readonly string _method;
        private readonly string _queryString;
        private readonly string _url;
        private readonly string _userIpAddress;

        //--- Constructors ---
        public Request(string url, string method, NameValueCollection headers, string queryString, string body, string userIpAddress) {
            _url = url;
            _method = method;
            _headers = headers;
            _queryString = queryString;
            _body = body;
            _userIpAddress = userIpAddress;
        }

        //--- Properties ---
        [JsonProperty("url")]
        public string Url {
            get { return _url; }
        }

        [JsonProperty("method")]
        public string Method {
            get { return _method; }
        }

        [JsonProperty("headers")]
        public NameValueCollection Headers {
            get { return _headers; }
        }

        [JsonProperty("query_string")]
        public string QueryString {
            get { return _queryString; }
        }

        [JsonProperty("body")]
        public string Body {
            get { return _body; }
        }

        [JsonProperty("user_ip")]
        public string UserIpAddress {
            get { return _userIpAddress; }
        }
    }
}
