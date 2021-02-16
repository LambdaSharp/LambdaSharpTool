/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
 * lambdasharp.net
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

using LambdaSharp.Logging;

namespace LambdaSharp.Core.LoggingStreamAnalyzerFunction {

    public class LambdaUsageRecord : ALambdaLogRecord {

        //--- Constructors ---
        public LambdaUsageRecord() {
            Type = "LambdaUsage";
            Version = "2020-05-05";
        }

        //--- Properties ---
        public string? ModuleInfo { get; set; }
        public string? FunctionId { get; set; }
        public string? ModuleId { get; set; }
        public string? Module { get; set; }
        public string? Function { get; set; }
        public float BilledDuration { get; set; }
        public float UsedDuration { get; set; }
        public float UsedDurationPercent { get; set; }
        public float MaxDuration { get; set; }
        public int MaxMemory { get; set; }
        public int UsedMemory { get; set; }
        public float UsedMemoryPercent { get; set; }
        public float? InitDuration { get; set; }
    }
}
