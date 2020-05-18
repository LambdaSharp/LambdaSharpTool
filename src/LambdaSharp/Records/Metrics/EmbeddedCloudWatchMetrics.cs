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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LambdaSharp.Records.Metrics {

    /// <summary>
    /// The <see cref="LambdaMetricsRecord"/> class defines a structured Lambda log entry
    /// for metrics that is compatible with embedded CloudWatch metrics format.
    /// </summary>
    public class LambdaMetricsRecord : ALambdaRecord {

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
        [JsonProperty("_aws")]
        public EmbeddedCloudWatchMetrics Aws { get; set; }

        /// <summary>
        /// Dictionary for holding metric target members that are added at the root of the object serialization.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> TargetMembers { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// The <see cref="EmbeddedCloudWatchMetrics"/> class holds the collection of all captured metrics.
    /// </summary>
    public class EmbeddedCloudWatchMetrics {

        //--- Properties ---

        /// <summary>
        /// Metric timestamp in Unix epoch milliseconds.
        /// </summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// List of CloudWatch metrics.
        /// </summary>
        public List<CloudWatchMetrics> CloudWatchMetrics { get; set; } = new List<CloudWatchMetrics>();
    }

    /// <summary>
    /// The <see cref="EmbeddedCloudWatchMetrics"/> class holds the collection of metrics belonging
    /// to a specific metrics namespace.
    /// </summary>
    public class CloudWatchMetrics {

        //--- Properties ---

        /// <summary>
        /// The namespace underwhich the metrics are aggregated.
        /// </summary>
        /// <value></value>
        public string Namespace { get; set; }

        /// <summary>
        /// The dimensions by which the metrics are partitioned.
        /// </summary>
        /// <returns></returns>
        public List<List<string>> Dimensions { get; set; } = new List<List<string>>();

        /// <summary>
        /// The collection of captured metrics and their units.
        /// </summary>
        public List<CloudWatchMetricValue> Metrics { get; set; } = new List<CloudWatchMetricValue>();
    }

    /// <summary>
    /// The <see cref="CloudWatchMetricValue"/> class holds the metric name and the unit.
    /// </summary>
    public class CloudWatchMetricValue {

        //--- Properties ---

        /// <summary>
        /// Metric name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Metric unit.
        /// </summary>
        public string Unit { get; set; }
    }
}
