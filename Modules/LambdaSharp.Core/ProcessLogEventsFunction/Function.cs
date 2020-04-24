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
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using Amazon.SimpleNotificationService;
using LambdaSharp.Core.Registrations;
using LambdaSharp.Core.RollbarApi;
using LambdaSharp.ErrorReports;
using LambdaSharp.Logger;
using LambdaSharp.Records.Events;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaJsonSerializer))]

namespace LambdaSharp.Core.ProcessLogEvents {

    public class LogEventsMessage {

        //--- Properties ---
        public string Owner { get; set; }
        public string LogGroup { get; set; }
        public string LogStream { get; set; }
        public string MessageType { get; set; }
        public List<string> SubscriptionFilters { get; set; }
        public List<LogEventEntry> LogEvents { get; set; }
    }

    public class LogEventEntry {

        //--- Properties ---
        public string Id { get; set; }
        public long Timestamp { get; set; }
        public string Message { get; set; }
    }

    public class Function : ALambdaFunction<KinesisEvent, string>, ILogicDependencyProvider {

        //--- Constants ---
        private const string LOG_GROUP_PREFIX = "/aws/lambda/";
        private const int MAX_EVENTS_BATCHSIZE = 256 * 1024;

        //--- Fields ---
        private Logic _logic;
        private IAmazonSimpleNotificationService _snsClient;
        private string _errorTopic;
        private string _usageTopic;
        private RegistrationTable _registrations;
        private Dictionary<string, OwnerMetaData> _cachedRegistrations;
        private RollbarClient _rollbarClient;
        private int _errorsReportsCount;
        private int _warningsReportsCount;
        private IAmazonCloudWatchEvents _eventsClient;
        private List<PutEventsRequestEntry> _eventEntries = new List<PutEventsRequestEntry>();
        private int _eventsEntriesTotalSize = 0;

        //--- Properties ---
        private Logic Logic => _logic ?? throw new InvalidOperationException();
        private IAmazonSimpleNotificationService SnsClient => _snsClient ?? throw new InvalidOperationException();
        private IAmazonCloudWatchEvents EventsClient => _eventsClient ?? throw new InvalidOperationException();
        private Dictionary<string, OwnerMetaData> CachedRegistrations => _cachedRegistrations ?? throw new InvalidOperationException();
        private RollbarClient RollbarClient => _rollbarClient ?? throw new InvalidOperationException();
        private RegistrationTable Registrations => _registrations ?? throw new InvalidOperationException();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _logic = new Logic(this);
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _errorTopic = config.ReadText("ErrorReportTopic");
            _usageTopic = config.ReadText("UsageReportTopic");
            var tableName = config.ReadDynamoDBTableName("RegistrationTable");
            var dynamoClient = new AmazonDynamoDBClient();
            _registrations = new RegistrationTable(dynamoClient, tableName);
            _cachedRegistrations = new Dictionary<string, OwnerMetaData>();
            _rollbarClient = new RollbarClient(null, null, message => LogInfo(message));
            _eventsClient = new AmazonCloudWatchEventsClient();
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
                        LogEventsMessage message;
                        using(var stream = new MemoryStream()) {
                            using(var gzip = new GZipStream(record.Kinesis.Data, CompressionMode.Decompress)) {
                                gzip.CopyTo(stream);
                                stream.Position = 0;
                            }
                            message = LambdaSerializer.Deserialize<LogEventsMessage>(stream);
                        }

                        // validate message
                        if(
                            (message.LogGroup == null)
                            || (message.MessageType == null)
                            || (message.LogEvents == null)
                        ) {
                            LogWarn("invalid kinesis record #{0}", recordIndex);
                            continue;
                        }

                        // skip events from own module
                        if(message.LogGroup.Contains(Info.FunctionName)) {
                            LogInfo("skipping event from own event log");
                            continue;
                        }

                        // skip control messages
                        if(message.MessageType == "CONTROL_MESSAGE") {
                            LogInfo("skipping control message");
                            continue;
                        }
                        if(message.MessageType != "DATA_MESSAGE") {
                            LogWarn("unknown message type: {0}", message.MessageType);
                            continue;
                        }
                        if(!message.LogGroup.StartsWith(LOG_GROUP_PREFIX, StringComparison.Ordinal)) {
                            LogWarn("unexpected log group: {0}", message.LogGroup);
                            continue;
                        }

                        // process messages
                        var functionId = message.LogGroup.Substring(LOG_GROUP_PREFIX.Length);
                        LogInfo($"getting owner for function: {functionId}");
                        var owner = await GetOwnerMetaDataAsync($"F:{functionId}");
                        if(owner != null) {
                            var invalidCount = 0;
                            foreach(var entry in message.LogEvents) {

                                // validate entry
                                if((entry.Message == null) || (entry.Timestamp == 0)) {
                                    ++invalidCount;
                                    continue;
                                }
                                var timestamp = (entry.Timestamp != 0)
                                    ? DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp)
                                    : DateTimeOffset.UtcNow;
                                if(!await Logic.ProgressLogEntryAsync(owner, entry.Message, timestamp)) {
                                    LogWarn("unable to parse message: {0}", entry.Message);
                                }
                            }
                            if(invalidCount > 0) {
                                LogWarn("kinesis record #{0} contained one or more invalid entries: {1}", recordIndex, invalidCount);
                            }
                        } else {
                            LogWarn("unable to retrieve registration for: {0}", message.LogGroup);
                        }
                    } catch(Exception e) {
                        await SendErrorExceptionAsync(e);
                    }
                }

                // send accumulated events
                SendAccumulatedEvents();
                return "Ok";
            } catch(Exception e) {
                await SendErrorExceptionAsync(e);
                return $"Error: {e.Message}";
            } finally {

                // NOTE (2020-04-21, bjorg): we don't expect this to fail; but since it's done at the end of the processing function, we
                //  need to make sure it never fails; otherwise, the Kinesis stream processing is interrupted.
                try {
                    ReportMetrics();
                } catch(Exception e) {
                    LogError(e, "report metrics failed");
                }
            }
        }

        private async Task<OwnerMetaData> GetOwnerMetaDataAsync(string id) {
            OwnerMetaData result;
            if(!CachedRegistrations.TryGetValue(id, out result)) {
                result = await Registrations.GetOwnerMetaDataAsync(id);
                if(result != null) {
                    if(result.RollbarAccessToken != null) {
                        result.RollbarAccessToken = await DecryptSecretAsync(result.RollbarAccessToken);
                    }
                    CachedRegistrations[id] = result;
                }
            }
            return result;
        }

        private async Task SendErrorExceptionAsync(Exception exception, string format = null, params object[] args) {
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

        private Task PublishErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report) {

            // capture reporting metrics
            switch(report.Level) {
            case "ERROR":
                ++_errorsReportsCount;
                break;
            case "WARNING":
                ++_warningsReportsCount;
                break;
            }

            // send error report
            try {
                return (owner != null)
                    ? Task.WhenAll(new[] {
                        PublishErrorReportToRollbarAsync(owner, report),
                        SnsClient.PublishAsync(_errorTopic, LambdaSerializer.Serialize<LambdaErrorReport>(report))
                    })
                    : SnsClient.PublishAsync(_errorTopic, LambdaSerializer.Serialize<LambdaErrorReport>(report));
            } catch {

                // capture error report in log; it's the next best thing we can do
                Provider.Log(LambdaSerializer.Serialize<LambdaErrorReport>(report) + "\n");
                return Task.CompletedTask;
            }
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
            LogInfo($"metrics summary: errors-reported: {_errorsReportsCount}, warnings-reported: {_warningsReportsCount}");
            if((_errorsReportsCount > 0) || (_warningsReportsCount > 0)) {
                LogMetric(new LambdaMetric[] {
                    ("ErrorReport.Count", _errorsReportsCount, LambdaMetricUnit.Count),
                    ("WarningReport.Count", _warningsReportsCount, LambdaMetricUnit.Count)
                });
            }
        }

        private void SendAccumulatedEvents() {
            if(_eventEntries.Count > 0) {
                var eventEntries = _eventEntries;

                // send accumulated events
                RunTask(async () => {
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

        //--- ILogicDependencyProvider Members ---
        Task ILogicDependencyProvider.SendErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report)
            => PublishErrorReportAsync(owner, report);

        async Task ILogicDependencyProvider.SendUsageReportAsync(OwnerMetaData owner, UsageReport report)
            => await SnsClient.PublishAsync(_usageTopic, LambdaSerializer.Serialize(report));

        async Task ILogicDependencyProvider.SendEventAsync(OwnerMetaData owner, LambdaEventRecord record) {
            var moduleFullName = owner.Module?.Split(':', 2)[0] ?? "";
            var resources = new List<string> {
                $"lambdasharp:stack:{owner.ModuleId}",
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
            }

            // calculate size of new event entry
            var entrySize = GetEventEntrySize(entry);
            if((_eventsEntriesTotalSize + entrySize) > MAX_EVENTS_BATCHSIZE) {

                // too many pending events; send events now
                SendAccumulatedEvents();
            }
            _eventEntries.Add(entry);

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

            int GetUtf8Length(string text) => (text != null) ? Encoding.UTF8.GetByteCount(text) : 0;
        }

        void ILogicDependencyProvider.LogProcessingError(Exception exception)
            => LogError(exception);
    }
}
