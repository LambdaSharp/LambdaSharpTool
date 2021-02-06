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
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LambdaSharp.App.Config;
using LambdaSharp.App.Logging.Models;
using LambdaSharp.Exceptions;
using LambdaSharp.Logging;
using LambdaSharp.Logging.ErrorReports;
using LambdaSharp.Logging.Events.Models;
using LambdaSharp.Logging.Metrics;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App {

    /// <summary>
    /// The <see cref="LambdaSharpAppClient"/> class is used to sending logs, metrics, and events to the Lambdasharp App API.
    /// </summary>
    public sealed class LambdaSharpAppClient : ILambdaSharpLogger, IAsyncDisposable {

        //--- Types ---
        private class LambdaSharpInfo : ILambdaSharpInfo {

            //--- Fields ---
            private readonly LambdaSharpAppClient _client;

            //--- Constructors ---
            public LambdaSharpInfo(LambdaSharpAppClient client)
                => _client = client ?? throw new ArgumentNullException(nameof(client));

            //--- Properties ---
            public string ModuleId => _client.Config.ModuleId;
            public string ModuleInfo => _client.Config.ModuleInfo;
            public string FunctionName => null;
            public string AppName => _client.Config.AppName;
            public string AppId => _client.Config.AppId;
            public string AppInstanceId => _client.Config.AppInstanceId;
            public string AppEventSource => string.IsNullOrEmpty(_client.Config.AppEventSource) ? (string)null : _client.Config.AppEventSource;
            public string DeploymentTier => _client.Config.DeploymentTier;
            public string GitSha => _client.Config.GitSha;
            public string GitBranch => _client.Config.GitBranch;
        }

        //--- Class Fields ---
        private static readonly TimeSpan Frequency = TimeSpan.FromSeconds(1);
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreNullValues = true,
            WriteIndented = false
        };

        //--- Fields ---
        private readonly Uri _lambdaSharpLoggingApi;
        private readonly Uri _lambdaSharpEventsApi;
        private readonly HttpClient _httpClient;
        private readonly Timer _timer;
        private Task _previousOperationTask;
        private string _logStreamName;
        private string _sequenceToken;
        private readonly List<PutLogEventsRequestEntry> _logs = new List<PutLogEventsRequestEntry>();
        private readonly List<PutEventsRequestEntry> _events = new List<PutEventsRequestEntry>();
        private readonly Dictionary<Exception, LogLevel> _reportedExceptions = new Dictionary<Exception, LogLevel>();
        private readonly string _apiKey;
        private readonly LambdaSharpInfo _info;

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="LambdaSharpAppClient"/> instance.
        /// </summary>
        /// <param name="config">A <see cref="LambdaSharpAppConfig"/> instance.</param>
        /// <param name="httpClient">A <c>HttpClient</c> instance.</param>
        public LambdaSharpAppClient(LambdaSharpAppConfig config, HttpClient httpClient) {

            // initialize fields
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _lambdaSharpLoggingApi = new Uri(config.ApiUrl + "/logs");
            _lambdaSharpEventsApi = new Uri(config.ApiUrl + "/events");
            _apiKey = config.GetApiKey();
            _info = new LambdaSharpInfo(this);

            // only enable timer if not running with test configuration
            if(_lambdaSharpLoggingApi.Host != "localhost") {
                _timer = new Timer(OnTimer, state: null, dueTime: Frequency, period: Frequency);
            }

            // initialize
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Stopwatch = Stopwatch.StartNew();
            AppInstanceId = config.AppInstanceId;
            ErrorReportGenerator = new LambdaErrorReportGenerator(
                Config.ModuleId ?? "<MISSING>",
                Config.ModuleInfo ?? "<MISSING>",
                platform: $"Blazor WebAssembly [{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}] ({System.Environment.OSVersion})",
                functionId: null,
                functionName: null,
                Config.AppId ?? "<MISSING>",
                Config.AppName ?? "<MISSING>",
                Config.AppFramework ?? "<MISSING>",
                Config.GitSha,
                Config.GitBranch
            );
        }

        //--- Properties ---

        /// <summary>
        /// Retrieve the <see cref="ErrorReportGenerator"/> instance used to generate error reports.
        /// </summary>
        /// <value>The <see cref="ErrorReportGenerator"/> instance.</value>
        public ILambdaErrorReportGenerator ErrorReportGenerator { get; private set; }

        /// <summary>
        /// The <see cref="Info"/> property return information about the LambdaSharp environment.
        /// </summary>
        /// <value>The <see cref="ILambdaSharpInfo"/> instance.</value>
        public ILambdaSharpInfo Info => _info;

        /// <summary>
        /// The <see cref="DebugLoggingEnabled"/> property indicates if log statements using <see cref="LambdaLogLevel.DEBUG"/> are emitted.
        /// </summary>
        /// <value>Boolean indicating if requests and responses are logged</value>
        public bool DebugLoggingEnabled => Config.IsDevModeEnabled();

        private LambdaSharpAppConfig Config { get; }
        private Stopwatch Stopwatch { get; }
        private string AppInstanceId { get; }

        //--- Methods ---

        /// <summary>
        /// The <see cref="LogException(Exception)"/> method is only invoked when Lambda function <see cref="ErrorReportGenerator"/> instance
        /// has not yet been initialized of if an exception occurred while invoking <see cref="LogRecord(ALambdaLogRecord)"/>.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        public void LogException(Exception exception) => SendMessage(echo: true, $"EXCEPTION: {exception}\n");

        /// <summary>
        /// Log a message with the given severity level. The <c>format</c> string is used to create a unique signature for errors.
        /// Therefore, any error information that varies between occurrences should be provided in the <c>arguments</c> parameter.
        /// </summary>
        /// <remarks>
        /// Nothing is logged if both <paramref name="format"/> and <paramref name="exception"/> are null.
        /// </remarks>
        /// <param name="level">The severity level of the log message. See <see cref="LambdaLogLevel"/> for a description of the severity levels.</param>
        /// <param name="exception">Optional exception to log. The exception is logged with its description and stacktrace. This parameter can be <c>null</c>.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        public void Log(LambdaLogLevel level, Exception exception, string format, params object[] arguments) {
            var logLevel = level switch {
                LambdaLogLevel.DEBUG => LogLevel.Debug,
                LambdaLogLevel.INFO => LogLevel.Information,
                LambdaLogLevel.WARNING => LogLevel.Warning,
                LambdaLogLevel.ERROR => LogLevel.Error,
                LambdaLogLevel.FATAL => LogLevel.Critical,
                _ => LogLevel.None
            };
            Log(logLevel, echo: true, exception, format, arguments);
        }

        /// <summary>
        /// Log a message with the given severity level. The <c>format</c> string is used to create a unique signature for errors.
        /// Therefore, any error information that varies between occurrences should be provided in the <c>arguments</c> parameter.
        /// </summary>
        /// <remarks>
        /// Nothing is logged if both <paramref name="format"/> and <paramref name="exception"/> are null.
        /// </remarks>
        /// <param name="logLevel">The severity level of the log message. See <see cref="LogLevel"/> for a description of the severity levels.</param>
        /// <param name="echo">Echo log message to the browser console.</param>
        /// <param name="exception">Optional exception to log. The exception is logged with its description and stacktrace. This parameter can be <c>null</c>.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        public void Log(LogLevel logLevel, bool echo, Exception exception, string format, params object[] arguments) {

            // convert log level to logging label
            var logLevelText = logLevel switch {
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARNING",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "FATAL",
                _ => null
            };
            if(logLevelText == null) {
                return;
            }

            // emit logging message
            string message = LambdaErrorReportGenerator.FormatMessage(format, arguments) ?? exception?.Message;
            if((logLevel >= LogLevel.Warning) && (exception != null)) {

                // avoid reporting the same error multiple times as it works its way up the stack
                if(_reportedExceptions.TryGetValue(exception, out var previousLogLevel) && (previousLogLevel >= logLevel)) {
                    return;
                }
                _reportedExceptions[exception] = logLevel;

                // abort messages are printed, but not reported since they are not logic errors
                if(exception is LambdaAbortException) {
                    SendMessage(echo, $"*** ABORT: {message} [{Stopwatch.Elapsed:c}]\n{PrintException()}");
                    return;
                }
                try {
                    var report = ErrorReportGenerator.CreateReport(AppInstanceId, logLevelText, exception, format, arguments);
                    if(report != null) {
                        LogRecord(report);
                    }
                } catch(Exception e) {
                    LogException(e);
                    LogException(exception);
                }
            } else if(message != null) {
                SendMessage(echo, $"*** {logLevelText}: {message} [{Stopwatch.Elapsed:c}]\n{PrintException()}");
            }

            // record metrics on warnings, errors, and fatal errors being logged
            switch(logLevel) {
            case LogLevel.Warning:
                this.LogMetric("LogWarning.Count", 1, LambdaMetricUnit.Count);
                break;
            case LogLevel.Error:
                this.LogMetric("LogError.Count", 1, LambdaMetricUnit.Count);
                break;
            case LogLevel.Critical:
                this.LogMetric("LogFatal.Count", 1, LambdaMetricUnit.Count);
                break;
            default:

                // nothing to do
                break;
            }

            // local functions
            string PrintException() => (exception != null) ? exception.ToString() + "\n" : "";
        }

        /// <summary>
        /// Log a <see cref="ALambdaLogRecord"/> record instance.
        /// </summary>
        /// <param name="record">The record to log.</param>
        public void LogRecord(ALambdaLogRecord record) {
            SendMessage(echo: Config.IsDevModeEnabled(), JsonSerializer.Serialize<object>(record ?? throw new ArgumentNullException(nameof(record)), JsonSerializerOptions) + "\n");

            // emit events
            if(record is LambdaEventRecord eventRecord) {
                _events.Add(new PutEventsRequestEntry {
                    Source = eventRecord.Source,
                    Detail = eventRecord.Detail,
                    DetailType = eventRecord.DetailType,
                    Resources = eventRecord.Resources
                });
            }
        }

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge. The 'detail-type' property is set to the full type name of the detail value.
        /// </summary>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <c>string</c>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        public void LogEvent<T>(T detail, IEnumerable<string> resources = null)
            => LambdaSharp.Logging.Events.ILambdaSharpLoggerEx.LogEvent<T>(
                this,
                _info.AppEventSource ?? throw new InvalidOperationException("AppEventSource is not configured"),
                typeof(T).FullName,
                detail,
                resources
            );

        private void OnTimer(object _) {
            if(!(_previousOperationTask?.IsCompleted ?? true)) {

                // previous operation is still going; wait until next timer invocation to proceed
                return;
            }

            // this should never be true unless there is a logic issue in FlushAsync()
            if(_previousOperationTask?.IsFaulted ?? false) {
                Console.WriteLine($"*** EXCEPTION: {_previousOperationTask.Exception}");
            }

            // initialize invocation to FlushAsync(), but don't wait for it to finish
            _previousOperationTask = FlushAsync();
        }

        private async Task FlushAsync() {

            // check if any messages are pending
            if(!_events.Any() && !_logs.Any()) {
                return;
            }

            // check if a log stream must be created
            if(_logStreamName == null) {
                _logStreamName = AppInstanceId;
                var response = await CreateLogStreamAsync(new CreateLogStreamRequest {
                    LogStreamName = _logStreamName
                });
                if(response.Error != null) {
                    Console.WriteLine($"*** ERROR: unable to create log stream: {_logStreamName} (Error: {response.Error})");
                    return;
                }
            }

            await Task.WhenAll(
                _logs.Any()
                    ? Task.Run(async () => {

                        // NOTE (2020-08-06, bjorg): we limit the number of log message we send in the unlikely event that we have too many
                        //  See: https://docs.aws.amazon.com/AmazonCloudWatchLogs/latest/APIReference/API_PutLogEvents.html
                        const int MaxPayloadSize = 1_048_576;
                        const int MaxMessageCount = 10_000;

                        // consume as many accumulated log messages as possible
                        var payloadByteCount = 0;
                        var logs = _logs.Take(MaxMessageCount).TakeWhile(log => {
                            var logMessageByteCount = Encoding.UTF8.GetByteCount(log.Message) + 26;
                            if((payloadByteCount + logMessageByteCount) >= MaxPayloadSize) {
                                return false;
                            }
                            payloadByteCount += logMessageByteCount;
                            return true;
                        }).ToList();
                        _logs.RemoveRange(0, logs.Count);

                        // send log messages to CloudWatch Logs
                        try {
                            var response = await PutLogEventsAsync(new PutLogEventsRequest {
                                LogStreamName = _logStreamName,
                                LogEvents = logs,
                                SequenceToken = _sequenceToken
                            });

                            // on error, re-insert the log messages and try again later
                            if(response.Error != null) {
                                _logs.InsertRange(0, logs);
                                return;
                            }
                            _sequenceToken = response.NextSequenceToken;
                        } catch {

                            // on exception, re-insert the log messages and try again later
                            _logs.InsertRange(0, logs);
                        }
                    })
                    : Task.CompletedTask,
                _events.Any()
                    ? Task.Run(async () => {

                        // NOTE (2020-08-06, bjorg): we must limit the number of events to avoid sending too many
                        //  See: https://docs.aws.amazon.com/eventbridge/latest/APIReference/API_PutEvents.html
                        const int MaxEventCount = 10;

                        // consume as many accumulated events as possible
                        var events = _events.Take(MaxEventCount).ToList();
                        _events.RemoveRange(0, events.Count);

                        try {
                            var response = await PutEventsAsync(new PutEventsRequest {
                                Entries = events
                            });

                            // on error, re-insert events and try again later
                            if(response.Error != null) {
                                _events.InsertRange(0, events);
                                return;
                            }
                        } catch {

                            // on exception, re-insert events and try again later
                            _events.InsertRange(0, events);
                        }
                    })
                    : Task.CompletedTask
                );
        }

        private async Task<CreateLogStreamResponse> CreateLogStreamAsync(CreateLogStreamRequest request) {
            var httpRequest = new HttpRequestMessage {
                RequestUri = _lambdaSharpLoggingApi,
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(httpRequest);
            return await response.Content.ReadFromJsonAsync<CreateLogStreamResponse>();
        }

        private async Task<PutLogEventsResponse> PutLogEventsAsync(PutLogEventsRequest request) {
            var httpRequest = new HttpRequestMessage {
                RequestUri = _lambdaSharpLoggingApi,
                Method = HttpMethod.Put,
                Content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(httpRequest);
            return await response.Content.ReadFromJsonAsync<PutLogEventsResponse>();
        }

        private async Task<PutEventsResponse> PutEventsAsync(PutEventsRequest request) {
            var httpRequest = new HttpRequestMessage {
                RequestUri = _lambdaSharpEventsApi,
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(httpRequest);
            return await response.Content.ReadFromJsonAsync<PutEventsResponse>();
        }

        private void SendMessage(bool echo, string message) {

            // optionally echo message to web console
            if(echo) {
                Console.WriteLine(message.TrimEnd());
            }

            // queue message for server-side logging
            _logs.Add(new PutLogEventsRequestEntry {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Message = message ?? throw new ArgumentNullException(nameof(message))
            });
        }

        //--- IAsyncDisposable Members ---
        async ValueTask IAsyncDisposable.DisposeAsync() {

            // stop timer and wait for any lingering timer operations to finish
            await _timer.DisposeAsync();

            // wait for any in-flight operation to complete
            if(!(_previousOperationTask?.IsCompleted ?? true)) {
                await _previousOperationTask;
            }

            // flush all remaining messages
            while(_logs.Any() || _events.Any()) {
                await FlushAsync();
            }
        }
    }
}
