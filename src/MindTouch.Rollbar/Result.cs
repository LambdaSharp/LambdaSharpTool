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


using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MindTouch.Rollbar {

    public class Result {

        //--- Fields ---
        private readonly string _message;
        private readonly string _rawResponse;
        private readonly HttpStatusCode _statusCode;
        private readonly string _uuid;

        public Result(HttpStatusCode httpStatusCode, string rawResponse) {
            _statusCode = httpStatusCode;
            _rawResponse = rawResponse;
            if(!string.IsNullOrEmpty(rawResponse)) {
                try {
                    var hash = JsonConvert.DeserializeObject<JObject>(rawResponse);
                    if(hash["message"] != null) {
                        _message = hash["message"].Value<string>();
                    }
                    if(hash["result"] != null && hash["result"]["uuid"] != null) {
                        _uuid = hash["result"]["uuid"].Value<string>();
                    }
                } catch { }
            }
        }

        //--- Properties ---
        public HttpStatusCode HttpStatusCode => _statusCode;
        public string Message => _message;
        public string UUID => _uuid;
        public bool IsSuccess => HttpStatusCode == HttpStatusCode.OK;

        //--- Methods ---
        public string Description {
            get {
                switch(HttpStatusCode) {
                case HttpStatusCode.OK:
                    return "Success. The item was accepted for processing.";
                case HttpStatusCode.BadRequest:
                    return "Bad request. No JSON payload was found, or it could not be decoded.";
                case HttpStatusCode.Unauthorized:
                    return "Unauthorized. No access token was found in the request.";
                case HttpStatusCode.Forbidden:
                    return "Access denied. Check that your access_token is valid, enabled, and has the correct scope.";
                case HttpStatusCode.RequestEntityTooLarge:
                    return "Request Too Large. Max payload size is 128kb. Try removing or truncating unnecessary large data included in the payload, like whole binary files or long strings.";
                case (HttpStatusCode)422:
                    return "Unprocessable payload. A syntactically valid JSON payload was found, but it had one or more semantic errors.";
                case (HttpStatusCode)429:
                    return "Too Many Requests. Request dropped because the rate limit has been reached for this access token, or the account is on the Free plan and the plan limit has been reached.";
                case HttpStatusCode.InternalServerError:
                    return "Internal server error. There was an error on Rollbar's end.";
                default:
                    return "Unknown";
                }
            }
        }

        public override string ToString() {
            var description = HttpStatusCode + ": " + Description;
            if(!IsSuccess) {
                description += ": " + (_message ?? _rawResponse);
            }
            return description;
        }
    }
}
