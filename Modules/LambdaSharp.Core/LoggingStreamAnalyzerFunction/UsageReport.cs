/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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

using System;
using LambdaSharp.Records;

namespace LambdaSharp.Core.LoggingStreamAnalyzerFunction {

    public class UsageReport : ALambdaRecord {

        //--- Constructors ---
        public UsageReport() {
            Source = "UsageReport";
            Version = "2020-05-04";
        }

        //--- Properties ---
        public float BilledDuration { get; set; }
        public float UsedDuration { get; set; }
        public float UsedDurationPercent { get; set; }
        public float MaxDuration { get; set; }
        public int MaxMemory { get; set; }
        public int UsedMemory { get; set; }
        public float UsedMemoryPercent { get; set; }
        public TimeSpan InitDuration { get; set; }
    }
}
