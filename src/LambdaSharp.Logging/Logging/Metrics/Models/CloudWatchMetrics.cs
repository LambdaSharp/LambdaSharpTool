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

namespace LambdaSharp.Logging.Metrics.Models {

    /// <summary>
    /// The <see cref="EmbeddedCloudWatchMetrics"/> class holds the collection of metrics belonging
    /// to a specific metrics namespace.
    /// </summary>
    public class CloudWatchMetrics {

        //--- Properties ---

        /// <summary>
        /// The namespace underwhich the metrics are aggregated.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// The dimensions by which the metrics are partitioned.
        /// </summary>
        public List<List<string>> Dimensions { get; set; } = new List<List<string>>();

        /// <summary>
        /// The collection of captured metrics and their units.
        /// </summary>
        public List<CloudWatchMetricValue> Metrics { get; set; } = new List<CloudWatchMetricValue>();
    }
}
