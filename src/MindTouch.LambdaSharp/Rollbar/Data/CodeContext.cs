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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MindTouch.Rollbar.Data {
    
    public class CodeContext {
        
        //--- Fields ---
        private readonly IEnumerable<string> _post;
        private readonly IEnumerable<string> _pre;

        //--- Constructors ---
        public CodeContext(IEnumerable<string> pre, IEnumerable<string> post) {
            _pre = pre;
            _post = post;
        }

        //--- Properties ---
        [JsonProperty("pre")]
        public IEnumerable<string> Pre {
            get { return _pre; }
        }

        [JsonProperty("post")]
        public IEnumerable<object> Post {
            get { return _post; }
        }
    }
}
