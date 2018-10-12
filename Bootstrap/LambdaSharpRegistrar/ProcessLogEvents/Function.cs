/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.Serialization.Json;
using Amazon.SimpleNotificationService;
using MindTouch.LambdaSharp;
using MindTouch.LambdaSharp.Reports;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MindTouch.LambdaSharpRegistrar.ProcessLogEvents {

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
        private string _errorTopicArn;
        private string _usageTopicArn;
        private RegistrationTable _registrations;
        private Dictionary<string, OwnerMetaData> _cachedRegistrations;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _logic = new Logic(this);
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _errorTopicArn = config.ReadText("ErrorReportTopic");
            _usageTopicArn = config.ReadText("UsageReportTopic");
            var tableName = config.ReadText("RegistrationTable");
            var dynamoClient = new AmazonDynamoDBClient();
            _registrations = new RegistrationTable(dynamoClient, tableName);
            _cachedRegistrations = new Dictionary<string, OwnerMetaData>();
        }

        public override async Task<string> ProcessMessageAsync(KinesisEvent kinesis, ILambdaContext context) {

            // NOTE: this function is responsible for error logs parsing; therefore, it CANNOT error out itself;
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
                        if(message.LogGroup.Contains(FunctionName)) {
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
                    _cachedRegistrations[id] = result;
                }
            }
            return result;
        }

        private async Task SendErrorExceptionAsync(Exception exception, string format = null, params object[] args) {
            try {
                var report = ErrorReporter.CreateReport(RequestId, LambdaLogLevel.ERROR.ToString(), exception, format, args);
                await PublishErrorReportAsync(report);
            } catch(Exception) {

                // log the error; it's the best we can do
                LogError(exception, format, args);
            }
        }

        private async Task PublishErrorReportAsync(ErrorReport report) {
            try {
                await _snsClient.PublishAsync(_errorTopicArn, SerializeJson(report));
            } catch(Exception) {
                LambdaLogger.Log(SerializeJson(report) + "\n");
            }
        }

        //--- ILogicDependencyProvider Members ---
        ErrorReport ILogicDependencyProvider.DeserializeErrorReport(string jsonReport)
            => DeserializeJson<ErrorReport>(jsonReport);

        Task ILogicDependencyProvider.SendErrorReportAsync(ErrorReport report)
            => PublishErrorReportAsync(report);

        async Task ILogicDependencyProvider.SendUsageReportAsync(UsageReport report)
            => await _snsClient.PublishAsync(_usageTopicArn, SerializeJson(report));

        void ILogicDependencyProvider.WriteLine(string message)
            => LogInfo(message);
    }
}
