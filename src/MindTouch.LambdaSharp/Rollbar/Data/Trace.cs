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
    
    public class Trace {
        
        //--- Fields ---
        private readonly ExceptionInfo _exception;
        private readonly IEnumerable<Frame> _frames;

        //--- Constructors ---
        public Trace(ExceptionInfo exception, IEnumerable<Frame> frames) {
            if(exception == null) {
                throw new ArgumentNullException("exception");
            }
            if(frames == null) {
                throw new ArgumentNullException("frames");
            }
            if(frames.Any(p => p == null)) {
                throw new ArgumentException("Collection cannot contain any null items.", "frames");
            }
            _exception = exception;
            _frames = frames;
        }

        //--- Properties ---
        [JsonProperty("exception")]
        public ExceptionInfo Exception {
            get { return _exception; }
        }

        [JsonProperty("frames")]
        public IEnumerable<Frame> Frames {
            get { return _frames; }
        }
    }
}
