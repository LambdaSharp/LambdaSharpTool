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

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="LambdaMetric"/> struct describes a logged metric entry.
    /// </summary>
    public struct LambdaMetric {

        //--- Operators ---

        /// <summary>
        /// Implicitly convert a <c>(string Name, double Value, LambdaMetricUnit Unit)</c> tuple to a <see cref="LambdaMetric"/> instance.
        /// </summary>
        /// <param name="metric"><c>(string Name, double Value, LambdaMetricUnit Unit)</c> metric tuple.</param>
        public static implicit operator LambdaMetric((string Name, double Value, LambdaMetricUnit Unit) metric)
            => new LambdaMetric(metric.Name, metric.Value, metric.Unit);

        /// <summary>
        /// Implicitly convert a <c>(string Name, int Value, LambdaMetricUnit Unit)</c> tuple to a <see cref="LambdaMetric"/> instance.
        /// </summary>
        /// <param name="metric"><c>(string Name, int Value, LambdaMetricUnit Unit)</c> metric tuple.</param>
        public static implicit operator LambdaMetric((string Name, int Value, LambdaMetricUnit Unit) metric)
            => new LambdaMetric(metric.Name, metric.Value, metric.Unit);

        //--- Constructors ---

        /// <summary>
        /// Create a new <see cref="LambdaMetric"/> instance.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        public LambdaMetric(string name, double value, LambdaMetricUnit unit)
            => (Name, Value, Unit) = (name ?? throw new ArgumentNullException(nameof(name)), value, unit);

        /// <summary>
        /// Create a new <see cref="LambdaMetric"/> instance.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        public LambdaMetric(string name, int value, LambdaMetricUnit unit)
            => (Name, Value, Unit) = (name ?? throw new ArgumentNullException(nameof(name)), value, unit);

        //--- Properties ---

        /// <summary>
        /// Metric name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Metric value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Metric unit.
        /// </summary>
        public LambdaMetricUnit Unit { get; }
    }

    /// <summary>
    /// The <see cref="LambdaMetricUnit"/> describes the measurement unit for a logged metric.
    /// </summary>
    public enum LambdaMetricUnit {

        /// <summary>
        /// Seconds
        /// </summary>
        Seconds,

        /// <summary>
        /// Microseconds
        /// </summary>
        Microseconds,

        /// <summary>
        /// Milliseconds
        /// </summary>
        Milliseconds,

        /// <summary>
        /// Bytes
        /// </summary>
        Bytes,

        /// <summary>
        /// Kilobytes
        /// </summary>
        Kilobytes,

        /// <summary>
        /// Megabytes
        /// </summary>
        Megabytes,

        /// <summary>
        /// Gigabytes
        /// </summary>
        Gigabytes,

        /// <summary>
        /// Terabytes
        /// </summary>
        Terabytes,

        /// <summary>
        /// Bits
        /// </summary>
        Bits,

        /// <summary>
        /// Kilobits
        /// </summary>
        Kilobits,

        /// <summary>
        /// Megabits
        /// </summary>
        Megabits,

        /// <summary>
        /// Gigabits
        /// </summary>
        Gigabits,

        /// <summary>
        /// Terabits
        /// </summary>
        Terabits,

        /// <summary>
        /// Percent
        /// </summary>
        Percent,

        /// <summary>
        /// Count
        /// </summary>
        Count,

        /// <summary>
        /// Bytes/Second
        /// </summary>
        BytesPerSecond,

        /// <summary>
        /// Kilobytes/Second
        /// </summary>
        KilobytesPerSecond,

        /// <summary>
        /// Megabytes/Second
        /// </summary>
        MegabytesPerSecond,

        /// <summary>
        /// Gigabytes/Second
        /// </summary>
        GigabytesPerSecond,

        /// <summary>
        /// Terabytes/Second
        /// </summary>
        TerabytesPerSecond,

        /// <summary>
        /// Bits/Second
        /// </summary>
        BitsPerSecond,

        /// <summary>
        /// Kilobits/Second
        /// </summary>
        KilobitsPerSecond,

        /// <summary>
        /// Megabits/Second
        /// </summary>
        MegabitsPerSecond,

        /// <summary>
        /// Gigabits/Second
        /// </summary>
        GigabitsPerSecond,

        /// <summary>
        /// Terabits/Second
        /// </summary>
        TerabitsPerSecond,

        /// <summary>
        /// Count/Second
        /// </summary>
        CountPerSecond,

        /// <summary>
        /// None
        /// </summary>
        None
    }
}
