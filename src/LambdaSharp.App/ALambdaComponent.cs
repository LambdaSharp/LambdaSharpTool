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
using LambdaSharp.Logging;
using LambdaSharp.Logging.Events;
using LambdaSharp.Logging.Metrics;
using Microsoft.AspNetCore.Components;

namespace LambdaSharp.App {

    /// <summary>
    /// The <see cref="ALambdaComponent"/> class is used for creating Blazor components with direct access to all the <see cref="ILambdaSharpLogger"/> methods.
    /// </summary>
    public abstract class ALambdaComponent : ComponentBase, ILambdaSharpLogger {

        //--- Properties ---

        /// <summary>
        /// The <see cref="AppClient"/> property return the client for communicating with the LambdaSharp App API.
        /// </summary>
        /// <value>The <see cref="AppClient"/> instance.</value>
        [Inject]
        protected LambdaSharpAppClient AppClient { get; set; }

        /// <summary>
        /// The <see cref="Info"/> property return information about the LambdaSharp environment.
        /// </summary>
        /// <value>The <see cref="ILambdaSharpInfo"/> instance.</value>
        public ILambdaSharpInfo Info => AppClient.Info;

        /// <summary>
        /// The <see cref="DebugLoggingEnabled"/> property indicates if log statements using <see cref="LambdaLogLevel.DEBUG"/> are emitted.
        /// </summary>
        /// <value>Boolean indicating if requests and responses are logged</value>
        public bool DebugLoggingEnabled => AppClient.DebugLoggingEnabled;

        //--- Methods ---

        /// <summary>
        /// Log a debugging message. This message will only appear in the log when debug logging is enabled and will not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        protected void LogDebug(string format, params object[] arguments) => AppClient.LogDebug(format, arguments);

        /// <summary>
        /// Log an informational message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        protected void LogInfo(string format, params object[] arguments) => AppClient.LogInfo(format, arguments);

        /// <summary>
        /// Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        protected void LogWarn(string format, params object[] arguments) => AppClient.LogWarn(format, arguments);

        /// <summary>
        /// Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        protected void LogError(Exception exception) => AppClient.LogError(exception);

        /// <summary>
        /// Log an exception with a custom message as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogError(Exception exception, string format, params object[] arguments) => AppClient.LogError(exception, format, arguments);

        /// <summary>
        /// Log an exception as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/> or <see cref="LogErrorAsWarning(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        protected void LogErrorAsInfo(Exception exception) => AppClient.LogErrorAsInfo(exception);

        /// <summary>
        /// Log an exception with a custom message as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/> or <see cref="LogErrorAsWarning(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogErrorAsInfo(Exception exception, string format, params object[] arguments) => AppClient.LogErrorAsInfo(exception, format, arguments);

        /// <summary>
        /// Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        protected void LogErrorAsWarning(Exception exception) => AppClient.LogErrorAsWarning(exception);

        /// <summary>
        /// Log an exception with a custom message as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogErrorAsWarning(Exception exception, string format, params object[] arguments) => AppClient.LogErrorAsWarning(exception, format, arguments);

        /// <summary>
        /// Log an exception with a custom message as a fatal error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogFatal(Exception exception, string format, params object[] arguments) => AppClient.LogFatal(exception, format, arguments);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        protected void LogMetric(
            string name,
            double value,
            LambdaMetricUnit unit
        ) => AppClient.LogMetric(name, value, unit);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        protected void LogMetric(
            string name,
            double value,
            LambdaMetricUnit unit,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) => AppClient.LogMetric(name, value, unit, dimensionNames, dimensionValues);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        protected void LogMetric(IEnumerable<LambdaMetric> metrics) => LogMetric(metrics, Array.Empty<string>(), new Dictionary<string, string>());

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        protected virtual void LogMetric(
            IEnumerable<LambdaMetric> metrics,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) => AppClient.LogMetric(metrics, dimensionNames, dimensionValues);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge. The 'detail-type' property is set to the full type name of the detail value.
        /// </summary>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        protected void LogEvent<T>(T detail, IEnumerable<string> resources = null)
            => AppClient.LogEvent(detail, resources);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge. The 'detail-type' property is set to the full type name of the detail value.
        /// </summary>
        /// <param name="source">Name of the event source.</param>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        protected void LogEvent<T>(string source, T detail, IEnumerable<string> resources = null)
            => AppClient.LogEvent(source, detail, resources);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge.
        /// </summary>
        /// <param name="source">Name of the event source.</param>
        /// <param name="detailType">Free-form string used to decide what fields to expect in the event detail.</param>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        protected void LogEvent<T>(string source, string detailType, T detail, IEnumerable<string> resources = null)
            => AppClient.LogEvent(source, detailType, detail, resources);

        /// <summary>
        /// Log a message wit the given severity level. The <c>format</c> string is used to create a unique signature for errors.
        /// Therefore, any error information that varies between occurrences should be provided in the <c>arguments</c> parameter.
        /// </summary>
        /// <remarks>
        /// Nothing is logged if both <paramref name="format"/> and <paramref name="exception"/> are null.
        /// </remarks>
        /// <param name="level">The severity level of the log message. See <see cref="LambdaLogLevel"/> for a description of the severity levels.</param>
        /// <param name="exception">Optional exception to log. The exception is logged with its description and stacktrace. This parameter can be <c>null</c>.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        public void Log(LambdaLogLevel level, Exception exception, string format, params object[] arguments)
            => AppClient.Log(level, exception, format, arguments);

        /// <summary>
        /// Log a <see cref="ALambdaLogRecord"/> record instance.
        /// </summary>
        /// <param name="record">The record to log.</param>
        public void LogRecord(ALambdaLogRecord record)
            => AppClient.LogRecord(record);
    }
}
