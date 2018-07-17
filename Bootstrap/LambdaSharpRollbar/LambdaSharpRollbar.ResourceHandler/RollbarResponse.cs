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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MindTouch.LambdaSharp.CustomResource;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MindTouch.LambdaSharpRollbar.ResourceHandler {

    public class RollbarResponse {

        //--- Properties ---
        [JsonProperty("err")]
        public int Error { get; set; }

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class RollbarCreateProjectRequest {

        //--- Properties ---

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class RollbarProject {

        //--- Properties ---

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("date_created")]
        [JsonConverter(typeof(UnixDateTimeConverter ))]
        public DateTime Created { get; set; }

        [JsonProperty("date_modified")]
        [JsonConverter(typeof(UnixDateTimeConverter ))]
        public DateTime Modified { get; set; }
    }

    public class RollbarProjectToken {

        //--- Properties ---
        [JsonProperty("project_id")]
        public int ProjectId { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("date_created")]
        [JsonConverter(typeof(UnixDateTimeConverter ))]
        public DateTime Created { get; set; }

        [JsonProperty("date_modified")]
        [JsonConverter(typeof(UnixDateTimeConverter ))]
        public DateTime Modified { get; set; }
    }
}
