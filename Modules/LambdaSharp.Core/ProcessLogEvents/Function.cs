/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.Serialization.Json;
using Amazon.SimpleNotificationService;
using LambdaSharp;
using LambdaSharp.Core.Registrations;
using LambdaSharp.Core.RollbarApi;
using LambdaSharp.ErrorReports;
using LambdaSharp.Logger;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

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
        public string Timestamp { get; set; }
        public string Message { get; set; }
    }

    public class Function : ALambdaFunction<KinesisEvent, string>, ILogicDependencyProvider {

        //--- Constants ---
        private const string LOG_GROUP_PREFIX = "/aws/lambda/";

        //--- Fields ---
        private Logic _logic;
        private IAmazonSimpleNotificationService _snsClient;
        private string _errorTopic;
        private string _usageTopic;
        private RegistrationTable _registrations;
        private Dictionary<string, OwnerMetaData> _cachedRegistrations;
        private RollbarClient _rollbarClient;

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
        }

        public override async Task<string> ProcessMessageAsync(KinesisEvent kinesis) {

            // NOTE (2018-12-11, bjorg): this function is responsible for error logs parsing; therefore, it CANNOT error out itself;
            //  instead, it must rely on aggressive exception handling and redirect those message where appropriate.

            try {
                foreach(var record in kinesis.Records) {
                    try {
                        LogEventsMessage message;
                        using(var stream = new MemoryStream()) {
                            using(var gzip = new GZipStream(record.Kinesis.Data, CompressionMode.Decompress)) {
                                gzip.CopyTo(stream);
                                stream.Position = 0;
                            }
                            message = DeserializeJson<LogEventsMessage>(stream);
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
                        LogInfo($"getting owner for function: {functionId}" );
                        var owner = await GetOwnerMetaDataAsync($"F:{functionId}");
                        if(owner != null) {
                            foreach(var entry in message.LogEvents) {
                                if(!await _logic.ProgressLogEntryAsync(owner, entry.Message, entry.Timestamp)) {
                                    LogWarn("Unable to parse message: {0}", entry.Message);
                                }
                            }
                        } else {
                            LogWarn("unable to retrieve registration for: {0}", message.LogGroup);
                        }
                    } catch(Exception e) {
                        await SendErrorExceptionAsync(e);
                    }
                }
                return "Ok";
            } catch(Exception e) {
                await SendErrorExceptionAsync(e);
                return $"Error: {e.Message}";
            }
        }

        private async Task<OwnerMetaData> GetOwnerMetaDataAsync(string id) {
            OwnerMetaData result;
            if(!_cachedRegistrations.TryGetValue(id, out result)) {
                result = await _registrations.GetOwnerMetaDataAsync(id);
                if(result != null) {
                    if(result.RollbarAccessToken != null) {
                        result.RollbarAccessToken = await DecryptSecretAsync(result.RollbarAccessToken);
                    }
                    _cachedRegistrations[id] = result;
                }
            }
            return result;
        }

        private async Task SendErrorExceptionAsync(Exception exception, string format = null, params object[] args) {
            try {
                var report = ErrorReportGenerator.CreateReport(CurrentContext.AwsRequestId, LambdaLogLevel.ERROR.ToString(), exception, format, args);
                await PublishErrorReportAsync(null, report);
            } catch {

                // log the error; it's the best we can do
                LogError(exception, format, args);
            }
        }

        private Task PublishErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report) {
            try {
                return Task.WhenAll(new[] {
                    PublishErrorReportToRollbarAsync(owner, report),
                    _snsClient.PublishAsync(_errorTopic, SerializeJson(report))
                });
            } catch {

                // capture error report in log; it's the next best thing we can do
                Provider.Log(SerializeJson(report) + "\n");
                return Task.CompletedTask;
            }
        }

        private async Task PublishErrorReportToRollbarAsync(OwnerMetaData owner, LambdaErrorReport report) {
            if(owner == null) {
                throw new ArgumentNullException(nameof(owner));
            }
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
                        report.Message,
                        report.Module,
                        report.ModuleId,
                        report.FunctionId,
                        report.FunctionName,
                        report.GitBranch,
                        report.RequestId
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
                var response = _rollbarClient.SendRollbarPayload(rollbar);
                LogInfo($"Rollbar.SendRollbarPayload() succeeded: {response}");
            } catch(WebException e) {
                if(e.Response == null) {
                    LogWarn($"Rollbar request failed (status: {e.Status}, message: {e.Message})");
                } else {
                    using(var stream = e.Response.GetResponseStream()) {
                        if(stream == null) {
                            LogWarn($"Rollbar.SendRollbarPayload() failed: {e.Status}");
                        }
                        using(var reader = new StreamReader(stream)) {
                            LogWarn($"Rollbar.SendRollbarPayload() failed: {reader.ReadToEnd()}");
                        }
                    }
                }
            } catch(Exception e) {
                LogErrorAsWarning(e, "Rollbar.SendRollbarPayload() failed");
            }
        }

        //--- ILogicDependencyProvider Members ---
        LambdaErrorReport ILogicDependencyProvider.DeserializeErrorReport(string jsonReport)
            => DeserializeJson<LambdaErrorReport>(jsonReport);

        Task ILogicDependencyProvider.SendErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report)
            => PublishErrorReportAsync(owner, report);

        async Task ILogicDependencyProvider.SendUsageReportAsync(OwnerMetaData owner, UsageReport report)
            => await _snsClient.PublishAsync(_usageTopic, SerializeJson(report));

        void ILogicDependencyProvider.LogProcessingError(Exception exception)
            => LogError(exception);
    }
}
