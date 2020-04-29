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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.DynamoDBv2;
using Amazon.Lambda.KinesisEvents;
using LambdaSharp.Core.Registrations;
using LambdaSharp.Core.RollbarApi;
using LambdaSharp.ErrorReports;
using LambdaSharp.Logger;
using LambdaSharp.Records.Events;
using LambdaSharp.Records.Metrics;

namespace LambdaSharp.Core.ProcessLogEventsFunction {

    public class LogEventsMessage {

        //--- Properties ---
        public string? Owner { get; set; }
        public string? LogGroup { get; set; }
        public string? LogStream { get; set; }
        public string? MessageType { get; set; }
        public List<string>? SubscriptionFilters { get; set; }
        public List<LogEventEntry>? LogEvents { get; set; }
    }

    public class LogEventEntry {

        //--- Properties ---
        public string? Id { get; set; }
        public long Timestamp { get; set; }
        public string? Message { get; set; }
    }

    public class Function : ALambdaFunction<KinesisEvent, string>, ILogicDependencyProvider {

        //--- Constants ---
        private const string LOG_GROUP_PREFIX = "/aws/lambda/";
        private const int MAX_EVENTS_BATCHSIZE = 256 * 1024;
        private const int MAX_EVENTS_COUNT = 10;

        //--- Fields ---
        private Logic? _logic;
        private RegistrationTable? _registrations;
        private Dictionary<string, OwnerMetaData>? _cachedRegistrations;
        private RollbarClient? _rollbarClient;
        private int _errorsReportsCount;
        private int _warningsReportsCount;
        private IAmazonCloudWatchEvents? _eventsClient;
        private List<PutEventsRequestEntry> _eventEntries = new List<PutEventsRequestEntry>();
        private int _eventsEntriesTotalSize = 0;
        private OwnerMetaData? _selfMetaData;

        //--- Properties ---
        private Logic Logic => _logic ?? throw new InvalidOperationException();
        private IAmazonCloudWatchEvents EventsClient => _eventsClient ?? throw new InvalidOperationException();
        private Dictionary<string, OwnerMetaData> CachedRegistrations => _cachedRegistrations ?? throw new InvalidOperationException();
        private RollbarClient RollbarClient => _rollbarClient ?? throw new InvalidOperationException();
        private RegistrationTable Registrations => _registrations ?? throw new InvalidOperationException();
        private OwnerMetaData SelfMetaData => _selfMetaData ?? throw new InvalidOperationException();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _logic = new Logic(this, LambdaSerializer);
            var tableName = config.ReadDynamoDBTableName("RegistrationTable");
            var dynamoClient = new AmazonDynamoDBClient();
            _registrations = new RegistrationTable(dynamoClient, tableName);
            _cachedRegistrations = new Dictionary<string, OwnerMetaData>();
            _rollbarClient = new RollbarClient(null, null, message => LogInfo(message));
            _eventsClient = new AmazonCloudWatchEventsClient();
            _selfMetaData = new OwnerMetaData {
                Module = $"{Info.ModuleFullName}:{Info.ModuleVersion}",
                ModuleId = Info.ModuleId,
                FunctionId = Info.FunctionId,
                FunctionName = Info.FunctionName,
                FunctionLogGroupName = CurrentContext.LogGroupName,
                FunctionPlatform = "AWS Lambda",
                FunctionFramework = Info.FunctionFramework,
                FunctionLanguage = "csharp"
            };
        }

        public override async Task<string> ProcessMessageAsync(KinesisEvent kinesis) {

            // NOTE (2018-12-11, bjorg): this function is responsible for error logs parsing; therefore, it CANNOT error out itself;
            //  instead, it must rely on aggressive exception handling and redirect those message where appropriate.

            ResetMetrics();
            try {
                var recordIndex = -1;
                foreach(var record in kinesis.Records) {
                    ++recordIndex;
                    try {

                        // deserialize kinesis record into a CloudWatch Logs event
                        LogEventsMessage logEvent;
                        using(var stream = new MemoryStream()) {
                            using(var gzip = new GZipStream(record.Kinesis.Data, CompressionMode.Decompress)) {
                                gzip.CopyTo(stream);
                                stream.Position = 0;
                            }
                            logEvent = LambdaSerializer.Deserialize<LogEventsMessage>(stream);
                        }

                        // validate log event
                        if(
                            (logEvent.LogGroup == null)
                            || (logEvent.MessageType == null)
                            || (logEvent.LogEvents == null)
                        ) {
                            LogWarn("invalid kinesis record #{0}\n{1}", recordIndex, LambdaSerializer.Serialize(logEvent));
                            continue;
                        }

                        // skip log event from own module
                        if(logEvent.LogGroup.Contains(Info.FunctionName)) {
                            LogInfo("skipping event from own event log");
                            continue;
                        }

                        // skip control log event
                        if(logEvent.MessageType == "CONTROL_MESSAGE") {
                            LogInfo("skipping control message");
                            continue;
                        }

                        // check if this log event is expected
                        if(logEvent.MessageType != "DATA_MESSAGE") {
                            LogWarn("unknown message type\n{0}", LambdaSerializer.Serialize(logEvent));
                            continue;
                        }
                        if(!logEvent.LogGroup.StartsWith(LOG_GROUP_PREFIX, StringComparison.Ordinal)) {
                            LogWarn("unexpected log group\n{0}", LambdaSerializer.Serialize(logEvent));
                            continue;
                        }

                        // use CloudWatch log name to identify owner of the log event
                        var functionId = logEvent.LogGroup.Substring(LOG_GROUP_PREFIX.Length);
                        var owner = await GetOwnerMetaDataAsync($"F:{functionId}");

                        // check if the log event owner was found
                        if(owner != null) {

                            // process entries in log event
                            foreach(var entry in logEvent.LogEvents) {
                                try {
                                    if(!await Logic.ProgressLogEntryAsync(
                                        owner,
                                        entry.Message,
                                        DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp)
                                    )) {
                                        throw new ProcessLogEventsException("invalid or unrecognized log event entry");
                                    }
                                } catch(Exception e) {
                                    LogError(e, "processing log event entry failed: {0}\n{1}", functionId, LambdaSerializer.Serialize(entry));
                                }
                            }
                        } else {
                            throw new ProcessLogEventsException("unable to retrieve registration for log event entry");
                        }
                    } catch(Exception e) {
                        LogError(e, "processing kinesis record #{0} failed\n{1}", recordIndex, LambdaSerializer.Serialize(record));
                    }
                }
                return "Ok";
            } finally {

                // NOTE (2020-04-21, bjorg): we don't expect this to fail; but since it's done at the end of the processing function, we
                //  need to make sure it never fails; otherwise, the Kinesis stream processing is interrupted.
                try {
                    ReportMetrics();
                } catch(Exception e) {
                    LogError(e, "report metrics failed");
                }

                // send accumulated events
                try {
                    PurgeEventEntries();
                } catch(Exception exception) {
                    Provider.Log($"EXCEPTION: {exception}\n");
                }
            }
        }

        private async Task<OwnerMetaData?> GetOwnerMetaDataAsync(string functionId) {

            // check if the owner has already been retrieved previously
            OwnerMetaData? result;
            if(!CachedRegistrations.TryGetValue(functionId, out result)) {
                result = await Registrations.GetOwnerMetaDataAsync(functionId);
                if(result != null) {

                    // check if a Rollbar access token is present that needs to be decrypted
                    if(result.RollbarAccessToken != null) {
                        result.RollbarAccessToken = await DecryptSecretAsync(result.RollbarAccessToken);
                    }

                    // cache owner record for future look-up
                    CachedRegistrations[functionId] = result;
                }
            }
            return result;
        }

        private async Task SendErrorExceptionAsync(Exception exception, string? format = null, params object[] args) {
            try {
                var report = ErrorReportGenerator.CreateReport(CurrentContext.AwsRequestId, LambdaLogLevel.ERROR.ToString(), exception, format, args);
                if(report != null) {
                    await PublishErrorReportAsync(null, report);
                }
            } catch {

                // log the error; it's the best we can do
                LogError(exception, format, args);
            }
        }

        private Task PublishErrorReportAsync(OwnerMetaData? owner, LambdaErrorReport report) {

            // capture reporting metrics
            switch(report.Level) {
            case "ERROR":
                ++_errorsReportsCount;
                break;
            case "WARNING":
                ++_warningsReportsCount;
                break;
            }

            // send error report as event
            try {
                SendEvent(owner ?? SelfMetaData, new LambdaEventRecord {
                    App = (owner == null)
                        ? "LambdaSharp.Core"
                        : "LambdaSharp.Core/Logs",
                    Type = "LambdaError",
                    Details = LambdaSerializer.Serialize(report)
                });
            } catch {

                // capture error report in log; it's the next best thing we can do
                Provider.Log(LambdaSerializer.Serialize<LambdaErrorReport>(report) + "\n");
            }

            // TODO: consider moving this functionality to another Lambda function
            if(owner != null) {
                try {
                    return PublishErrorReportToRollbarAsync(owner, report);
                } catch {

                    // capture error report in log; it's the next best thing we can do
                    Provider.Log(LambdaSerializer.Serialize<LambdaErrorReport>(report) + "\n");
                }
            }
            return Task.CompletedTask;
        }

        private async Task PublishErrorReportToRollbarAsync(OwnerMetaData owner, LambdaErrorReport report) {
            if(owner.RollbarAccessToken == null) {
                return;
            }

            // convert error report into rollbar data structure
            var rollbar = new Rollbar {
                AccessToken = owner.RollbarAccessToken,
                Data = new Data {
                    Environment = report.ModuleId,
                    Level = report.Level?.ToLowerInvariant() ?? "error",
                    Timestamp = report.Timestamp,
                    CodeVersion = report.GitSha,
                    Platform = report.Platform,
                    Language = report.Language,
                    Framework = report.Framework,
                    Fingerprint = report.Fingerprint,
                    Title = $"{report.FunctionName}: {report.Message}",
                    Custom = new {
                        Message = report.Message,
                        Module = report.Module,
                        ModuleId = report.ModuleId,
                        FunctionId = report.FunctionId,
                        FunctionName = report.FunctionName,
                        GitBranch = report.GitBranch,
                        RequestId = report.RequestId
                    },
                    Body = new DataBody {
                        TraceChain = report.Traces?.Select(trace => new Trace {
                            Exception = new ExceptionClass {
                                Class = trace.Exception?.Type,
                                Message = trace.Exception?.Message,
                                Description = trace.Exception?.StackTrace
                            },
                            Frames = trace.Frames?.Select(frame => new Frame {
                                Filename = frame.FileName,
                                Lineno = frame.LineNumber.GetValueOrDefault(),
                                Method = frame.MethodName
                            }).ToArray()
                        }).ToArray()
                    }
                }
            };

            // in case there are no captured traces, inject a simple error message
            if(rollbar.Data.Body.TraceChain?.Any() != true) {
                rollbar.Data.Body.TraceChain = null;
                rollbar.Data.Body = new DataBody {
                    Message = new Message {
                        Body = report.Raw
                    }
                };
            }

            // send payload to rollbar
            try {
                var response = RollbarClient.SendRollbarPayload(rollbar);
                LogInfo($"Rollbar.SendRollbarPayload() succeeded: {response}");
            } catch(WebException e) {
                if(e.Response == null) {
                    LogWarn($"Rollbar request failed (status: {e.Status}, message: {e.Message})");
                } else {
                    using(var stream = e.Response.GetResponseStream()) {
                        if(stream == null) {
                            LogWarn($"Rollbar.SendRollbarPayload() failed: {e.Status}");
                        } else {
                            using(var reader = new StreamReader(stream)) {
                                LogWarn($"Rollbar.SendRollbarPayload() failed: {reader.ReadToEnd()}");
                            }
                        }
                    }
                }
            } catch(Exception e) {
                LogErrorAsWarning(e, "Rollbar.SendRollbarPayload() failed");
            }
        }

        private void ResetMetrics() {
            _errorsReportsCount = 0;
            _warningsReportsCount = 0;
        }

        private void ReportMetrics() {
            var metrics = new List<LambdaMetric>();
            if(_errorsReportsCount > 0) {
                metrics.Add(("ErrorReport.Count", _errorsReportsCount, LambdaMetricUnit.Count));
            }
            if(_warningsReportsCount > 0) {
                metrics.Add(("WarningReport.Count", _warningsReportsCount, LambdaMetricUnit.Count));
            }
            LogMetric(metrics);
        }

        private void PurgeEventEntries() {
            if(_eventEntries.Count > 0) {
                var eventEntries = _eventEntries;

                // send accumulated events
                RunTask(async () => {

                    // send events
                    try {
                        await EventsClient.PutEventsAsync(new PutEventsRequest {
                            Entries = eventEntries
                        });
                    } catch(Exception e) {
                        LogErrorAsWarning(e, "EventsClient.PutEvents() failed");
                    }
                });

                // reset events accumulator
                _eventEntries = new List<PutEventsRequestEntry>();
                _eventsEntriesTotalSize = 0;
            }
        }

        private void AddEventEntry(PutEventsRequestEntry entry) {

            // calculate size of new event entry
            var entrySize = GetEventEntrySize(entry);
            if((_eventsEntriesTotalSize + entrySize) > MAX_EVENTS_BATCHSIZE) {
                PurgeEventEntries();
            }
            _eventEntries.Add(entry);
            if(_eventEntries.Count == MAX_EVENTS_COUNT) {
                PurgeEventEntries();
            }

            // local functions
            int GetEventEntrySize(PutEventsRequestEntry eventEntry) {
                var size = 0;
                if(eventEntry.Time != null) {
                    size += 14;
                }
                size += GetUtf8Length(eventEntry.Source);
                size += GetUtf8Length(eventEntry.DetailType);
                size += GetUtf8Length(eventEntry.Detail);
                foreach(var resource in eventEntry.Resources) {
                    size += GetUtf8Length(resource);
                }
                return size;
            }

            int GetUtf8Length(string? text) => (text != null) ? Encoding.UTF8.GetByteCount(text) : 0;
        }

        private void SendEvent(OwnerMetaData owner, LambdaEventRecord record) {
            var moduleFullName = owner?.Module?.Split(':', 2)[0] ?? "";
            var resources = new List<string> {
                $"lambdasharp:stack:{owner?.ModuleId}",
                $"lambdasharp:module:{moduleFullName}",
                $"lambdasharp:tier:{Info.DeploymentTier}"
            };
            if(record.Resources != null) {
                resources.AddRange(resources);
            }
            var entry = new PutEventsRequestEntry {
                Source = record.App,
                DetailType = record.Type,
                Detail = record.Details,
                Resources = resources
            };
            if(DateTime.TryParse(record.Time, out var timestamp)) {
                entry.Time = timestamp;
            } else {
                entry.Time = DateTime.UtcNow;
            }
            AddEventEntry(entry);
        }

        protected override void RecordErrorReport(LambdaErrorReport report) {
            base.RecordErrorReport(report);

            // publish error to the event bus
            PublishErrorReportAsync(owner: null, report).GetAwaiter().GetResult();
        }

        protected override void RecordException(Exception exception) {
            base.RecordException(exception);

            // publish exception to the event bus
            SendEvent(SelfMetaData, new LambdaEventRecord {
                App = "LambdaSharp.Core",
                Type = "InternalError",
                Details = LambdaSerializer.Serialize(new {
                    Message = exception?.Message,
                    Type = exception?.GetType().FullName,
                    Raw = exception?.ToString()
                })
            });
        }

        //--- ILogicDependencyProvider Members ---
        Task ILogicDependencyProvider.SendErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report)
            => PublishErrorReportAsync(owner, report);

        async Task ILogicDependencyProvider.SendUsageReportAsync(OwnerMetaData owner, UsageReport report)
            => SendEvent(owner, new LambdaEventRecord {
                App = "LambdaSharp.Core/Logs",
                Type = "UsageReport",
                Details = LambdaSerializer.Serialize(report)
            });

        async Task ILogicDependencyProvider.SendEventAsync(OwnerMetaData owner, LambdaEventRecord record) {
            SendEvent(owner, record);
            if((record.Time != null) && DateTimeOffset.TryParse(record.Time, out var sentTime)) {
                LogMetric("Event.Latency", (DateTimeOffset.UtcNow - sentTime).TotalMilliseconds, LambdaMetricUnit.Milliseconds);
            }
        }

        async Task ILogicDependencyProvider.SendMetricsAsync(OwnerMetaData owner, LambdaMetricsRecord record)
            => SendEvent(owner, new LambdaEventRecord {
                Time = DateTimeOffset.FromUnixTimeMilliseconds(record.Aws.Timestamp).ToRfc3339Timestamp(),
                App = "LambdaSharp.Core/Logs",
                Type = "LambdaMetrics",
                Details = LambdaSerializer.Serialize(record)
            });
    }
}
