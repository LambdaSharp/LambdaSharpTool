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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LambdaSharp.Logging.Metrics.Models {

    /// <summary>
    /// The <see cref="LambdaMetricsRecord"/> class defines a structured Lambda log entry
    /// for metrics that is compatible with embedded CloudWatch metrics format.
    /// </summary>
    public class LambdaMetricsRecord : ALambdaLogRecord {

        // https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html

        //--- Constructors ---

        /// <summary>
        /// Create a new <see cref="LambdaMetricsRecord"/> instance.
        /// </summary>
        public LambdaMetricsRecord() {
            Type = "LambdaMetrics";
            Version = "2020-05-05";
        }

        //--- Properties ---

        /// <summary>
        /// Embedded CloudWatch metrics metadata object.
        /// </summary>
        [JsonPropertyName("_aws")]
        public EmbeddedCloudWatchMetrics Aws { get; set; }

        /// <summary>
        /// Dictionary for holding metric target members that are added at the root of the object serialization.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> TargetMembers { get; set; } = new Dictionary<string, object>();
    }
}
