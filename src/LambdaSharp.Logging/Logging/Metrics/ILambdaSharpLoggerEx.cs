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

using System;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Internal;
using LambdaSharp.Logging.Internal;
using LambdaSharp.Logging.Metrics.Models;

namespace LambdaSharp.Logging.Metrics {

    /// <summary>
    /// <see cref="ILambdaSharpLoggerEx"/> adds logging functionality as extension methods to the <see cref="ILambdaSharpLogger"/> interface.
    /// </summary>
    /// <seealso cref="LambdaLogLevel"/>
    public static class ILambdaSharpLoggerEx {

        //--- Extension Methods ---

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        public static void LogMetric(
            this ILambdaSharpLogger logger,
            string name,
            double value,
            LambdaMetricUnit unit
        ) => logger.LogMetric(new[] { new LambdaMetric(name, value, unit) });

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        public static void LogMetric(
            this ILambdaSharpLogger logger,
            string name,
            double value,
            LambdaMetricUnit unit,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) => logger.LogMetric(new[] { new LambdaMetric(name, value, unit) }, dimensionNames, dimensionValues);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        public static void LogMetric(this ILambdaSharpLogger logger, IEnumerable<LambdaMetric> metrics)
            => logger.LogMetric(metrics, Array.Empty<string>(), new Dictionary<string, string>());

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        public static void LogMetric(
            this ILambdaSharpLogger logger,
            IEnumerable<LambdaMetric> metrics,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) {
            if(!metrics.Any()) {
                return;
            }
            IEnumerable<string> newDimensionNames;
            Dictionary<string, string> newDimensionValues;
            if(logger.Info.ModuleId != null) {
                if(logger.Info.FunctionName != null) {

                    // dimension the metric by 'Stack' and 'Function'
                    newDimensionNames = dimensionNames.Union(new[] { "Stack", "Stack,Function" }).Distinct().ToList();
                    newDimensionValues = new Dictionary<string, string>(dimensionValues) {
                        ["Stack"] = logger.Info.ModuleId,
                        ["Function"] = logger.Info.FunctionName
                    };
                } else if(logger.Info.AppName != null) {

                    // dimension the metric by 'Stack' and 'App'
                    newDimensionNames = dimensionNames.Union(new[] { "Stack", "Stack,App" }).Distinct().ToList();
                    newDimensionValues = new Dictionary<string, string>(dimensionValues) {
                        ["Stack"] = logger.Info.ModuleId,
                        ["App"] = logger.Info.AppName
                    };
                } else {
                    newDimensionNames = new List<string>();
                    newDimensionValues = new Dictionary<string, string>(dimensionValues);
                }
            } else {
                if(logger.Info.FunctionName != null) {

                    // dimension the metric by 'Function' only
                    newDimensionNames = dimensionNames.Union(new[] { "Function" }).Distinct().ToList();
                    newDimensionValues = new Dictionary<string, string>(dimensionValues) {
                        ["Function"] = logger.Info.FunctionName
                    };
                } else if(logger.Info.AppName != null) {

                    // dimension the metric by 'App' only
                    newDimensionNames = dimensionNames.Union(new[] { "App" }).Distinct().ToList();
                    newDimensionValues = new Dictionary<string, string>(dimensionValues) {
                        ["App"] = logger.Info.AppName
                    };
                } else {
                    newDimensionNames = new List<string>();
                    newDimensionValues = new Dictionary<string, string>(dimensionValues);
                }
            }

            // add git sha and git branch as extra metadata when available
            if(logger.Info.GitSha != null) {
                newDimensionValues["GitSha"] = logger.Info.GitSha;
            }
            if(logger.Info.GitBranch != null) {
                newDimensionValues["GitBranch"] = logger.Info.GitBranch;
            }
            logger.LogMetric($"Module:{logger.Info.GetModuleFullName()}", metrics, newDimensionNames, newDimensionValues);
        }

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaSharpLogger"/> instance to use.</param>
        /// <param name="namespace">Metric namespace.</param>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        public static void LogMetric(
            this ILambdaSharpLogger logger,
            string @namespace,
            IEnumerable<LambdaMetric> metrics,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) {
            if(!metrics.Any()) {

                // nothing to do
                return;
            }
            if(dimensionNames.Count() > 9) {
                throw new ArgumentException("metric cannot exceed 9 dimensions", nameof(dimensionNames));
            }
            var targets = new Dictionary<string, object>();

            // validate and process metrics
            var index = 0;
            foreach(var metric in metrics) {
                if(string.IsNullOrEmpty(metric.Name)) {
                    throw new ArgumentException($"metric name cannot be empty (index {index})");
                }
                AssertNotReservedName(metric.Name, "metric");
                if(double.IsNaN(metric.Value) || double.IsNegativeInfinity(metric.Value) || double.IsPositiveInfinity(metric.Value)) {

                    // these values are rejected by CloudWatch metrics
                    throw new ArgumentException($"metric '{metric.Name}' has an out-of-range value");
                }
                if(!targets.TryAdd(metric.Name, metric.Value)) {
                    throw new ArgumentException($"conflicting metric name: {metric.Name}");
                }
                ++index;
            }

            // validate and process dimension values
            index = 0;
            foreach(var dimensionValue in dimensionValues) {
                if(string.IsNullOrEmpty(dimensionValue.Key)) {
                    throw new ArgumentException($"dimension name cannot be empty (index {index})");
                }
                if(string.IsNullOrEmpty(dimensionValue.Value)) {
                    throw new ArgumentException($"dimension value cannot be empty (index {index})");
                }
                AssertNotReservedName(dimensionValue.Key, "dimension");
                if(!targets.TryAdd(dimensionValue.Key, dimensionValue.Value)) {
                    throw new ArgumentException($"conflicting dimension name: {dimensionValue.Key}");
                }
                ++index;
            }

            // validate and process metric dimensions
            var metricDimensions = dimensionNames
                .Select(dimension => dimension
                    .Split(',')
                    .Select(dim => dim.Trim())
                    .ToList()
                )
                .ToList();
            foreach(var metricDimension in metricDimensions.SelectMany(dimension => dimension)) {
                if(!dimensionValues.ContainsKey(metricDimension)) {
                    throw new ArgumentException($"missing dimension value: {metricDimension}");
                }
            }

            // create embedded metrics data-structure:
            var record = new LambdaMetricsRecord {
                Aws = new EmbeddedCloudWatchMetrics {
                    CloudWatchMetrics = {
                        new CloudWatchMetrics {
                            Namespace = @namespace,
                            Dimensions = metricDimensions,
                            Metrics = metrics.Select(metric => new CloudWatchMetricValue {
                                Name = metric.Name,
                                Unit = ConvertUnit(metric.Unit)
                            }).ToList()
                        }
                    }
                },
                TargetMembers = targets
            };

            // log metric record
            logger.LogRecord(record);

            // local functions
            void AssertNotReservedName(string name, string parameterType) {
                switch(name) {
                case "_aws":
                case "Source":
                case "Version":
                    throw new ArgumentException($"{parameterType} name cannot be named '{name}'");
                default:
                    break;
                }
            }

            string ConvertUnit(LambdaMetricUnit unit) {
                switch(unit) {

                // these enum names need to be mapped to their correct CloudWatch metrics unit counterpart
                case LambdaMetricUnit.BytesPerSecond:
                    return "Bytes/Second";
                case LambdaMetricUnit.KilobytesPerSecond:
                    return "Kilobytes/Second";
                case LambdaMetricUnit.MegabytesPerSecond:
                    return "Megabytes/Second";
                case LambdaMetricUnit.GigabytesPerSecond:
                    return "Gigabytes/Second";
                case LambdaMetricUnit.TerabytesPerSecond:
                    return "Terabytes/Second";
                case LambdaMetricUnit.BitsPerSecond:
                    return "Bits/Second";
                case LambdaMetricUnit.KilobitsPerSecond:
                    return "Kilobits/Second";
                case LambdaMetricUnit.MegabitsPerSecond:
                    return "Megabits/Second";
                case LambdaMetricUnit.GigabitsPerSecond:
                    return "Gigabits/Second";
                case LambdaMetricUnit.TerabitsPerSecond:
                    return "Terabits/Second";
                case LambdaMetricUnit.CountPerSecond:
                    return "Count/Second";

                // the remaining enums are good as is
                default:
                    return unit.ToString();
                }
            }
        }
    }
}
