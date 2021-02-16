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

namespace LambdaSharp.Logging.Metrics.Models {

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
