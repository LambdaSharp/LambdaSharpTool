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
using System.Linq;
using LambdaSharp.Records.Events;
using LambdaSharp.Records.Metrics;

namespace LambdaSharp.Logger {

    /// <summary>
    /// <see cref="ILambdaLogLevelLoggerEx"/> adds logging functionality as extension methods to the <see cref="ILambdaLogLevelLogger"/> interface.
    /// </summary>
    /// <seealso cref="LambdaLogLevel"/>
    public static class ILambdaLogLevelLoggerEx {

        //--- Extension Methods ---

        /// <summary>
        /// Log a debugging message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogDebug(this ILambdaLogLevelLogger logger, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.DEBUG, exception: null, format: format, arguments: arguments);

        /// <summary>
        /// Log an informational message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogInfo(this ILambdaLogLevelLogger logger, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.INFO, exception: null, format: format, arguments: arguments);

        /// <summary>
        /// Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogWarn(this ILambdaLogLevelLogger logger, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.WARNING, exception: null, format: format, arguments: arguments);

        /// <summary>
        /// Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogError(this ILambdaLogLevelLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.ERROR, exception, exception.Message, new object[0]);

        /// <summary>
        /// Log an exception with a custom message as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogError(this ILambdaLogLevelLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.ERROR, exception, format, arguments);

        /// <summary>
        /// Log an exception as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaLogLevelLogger,Exception)"/> or <see cref="LogErrorAsWarning(ILambdaLogLevelLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsInfo(this ILambdaLogLevelLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.INFO, exception, exception.Message, new object[0]);

        /// <summary>
        /// Log an exception with a custom message as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaLogLevelLogger,Exception)"/> or <see cref="LogErrorAsWarning(ILambdaLogLevelLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsInfo(this ILambdaLogLevelLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.INFO, exception, format, arguments);

        /// <summary>
        /// Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaLogLevelLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsWarning(this ILambdaLogLevelLogger logger, Exception exception)
            => logger.Log(LambdaLogLevel.WARNING, exception, exception.Message, new object[0]);

        /// <summary>
        /// Log an exception with a custom message as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(ILambdaLogLevelLogger,Exception)"/>.
        /// </remarks>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogErrorAsWarning(this ILambdaLogLevelLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.WARNING, exception, format, arguments);

        /// <summary>
        /// Log a fatal exception with a custom message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        /// <seealso cref="LambdaLogLevel"/>
        public static void LogFatal(this ILambdaLogLevelLogger logger, Exception exception, string format, params object[] arguments)
            => logger.Log(LambdaLogLevel.FATAL, exception, format, arguments);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="logger">The <see cref="ILambdaLogLevelLogger"/> instance to use.</param>
        /// <param name="namespace">Metric namespace.</param>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        public static void LogMetric(
            this ILambdaLogLevelLogger logger,
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
