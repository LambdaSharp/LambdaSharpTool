/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Collections.Specialized;
using System.Globalization;

namespace LambdaSharp.Reports {

    public class ErrorReport {

        //--- Properties ---

        // Report
        public string Source { get; set; } = "LambdaError";
        public string Version { get; set; } = "2018-12-31";

        // Origin
        public string Module { get; set; }
        public string ModuleId { get; set; }
        public string FunctionId { get; set; }
        public string FunctionName { get; set; }
        public string Platform { get; set; }
        public string Framework { get; set; }
        public string Language { get; set; }
        public string GitSha { get; set; }
        public string GitBranch { get; set; }

        // Occurrence
        public string RequestId { get; set; }
        public string Level { get; set; }
        public string Fingerprint { get; set; }
        public long Timestamp { get; set; }
        public string Message { get; set; }
        public string Raw { get; set; }
        public IEnumerable<ErrorReportStackTrace> Traces { get; set; }
    }
}
