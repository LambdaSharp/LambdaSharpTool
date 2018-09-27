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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MindTouch.Rollbar.Data {

    public class Body {

        //--- Fields ---
        private readonly Message _message;
        private readonly Trace _trace;
        private readonly IEnumerable<Trace> _traceChain;

        //--- Constructors ---
        public Body(Trace trace) {
            if(trace == null) {
                throw new ArgumentNullException("trace");
            }
            _trace = trace;
        }

        public Body(IEnumerable<Trace> chain) {
            if(chain == null) {
                throw new ArgumentNullException("chain");
            }
            if(!chain.Any()) {
                throw new ArgumentException("Collection must not be empty", "chain");
            }
            if(!chain.All(p => p != null)) {
                throw new ArgumentException("Collection cannot contain any null items.", "chain");
            }
            _traceChain = chain;
        }

        public Body(Message message) {
            if(message == null) {
                throw new ArgumentNullException("message");
            }
            _message = message;
        }

        //--- Properties ---
        [JsonProperty("trace")]
        public Trace Trace {
            get { return _trace; }
        }

        [JsonProperty("trace_chain")]
        public IEnumerable<Trace> TraceChain {
            get { return _traceChain; }
        }

        [JsonProperty("message")]
        public Message Message {
            get { return _message; }
        }
    }
}
