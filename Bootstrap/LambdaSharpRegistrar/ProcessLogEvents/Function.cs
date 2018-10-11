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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //--- Fields ---
        private Logic _logic;
        private IAmazonSimpleNotificationService _snsClient;
        private string _errorTopicArn;
        private string _usageTopicArn;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _logic = new Logic(this);
            _snsClient = new AmazonSimpleNotificationServiceClient();
            _errorTopicArn = config.ReadText("ErrorReportTopic");
            _usageTopicArn = config.ReadText("UsageReportTopic");
        }

        public override async Task<string> ProcessMessageAsync(KinesisEvent kinesis, ILambdaContext context) {
            foreach(var record in kinesis.Records) {
                LogEventsMessage events;
                using(var stream = new MemoryStream()) {
                    using(var gzip = new GZipStream(record.Kinesis.Data, CompressionMode.Decompress)) {
                        gzip.CopyTo(stream);
                        stream.Position = 0;
                    }
                    events = DeserializeJson<LogEventsMessage>(stream);
                }
                await HandleLogEventMessage(events);
            }
            return "Ok";
        }

        private async Task HandleLogEventMessage(LogEventsMessage logEventMessage) {

            // skip events from own module
            if(logEventMessage.LogGroup.Contains(ModuleName) || !logEventMessage.LogEvents.Any()) {
                return;
            }

            // skip control messages
            if(logEventMessage.MessageType == "CONTROL_MESSAGE ") {
                return;
            }
            if(logEventMessage.MessageType != "DATA_MESSAGE ") {

                // TODO: report unexpected messages
                return;
            }

            // TODO: continue here
            OwnerMetaData owner = null;
            foreach(var entry in logEventMessage.LogEvents) {
                await _logic.ProgressLogEntryAsync(owner, entry.Message, entry.Timestamp);
            }
        }

        //--- ILogicDependencyProvider Members ---
        ErrorReport ILogicDependencyProvider.DeserializeErrorReport(string jsonReport)
            => DeserializeJson<ErrorReport>(jsonReport);

        async Task ILogicDependencyProvider.SendErrorReportAsync(ErrorReport report)
            => await _snsClient.PublishAsync(_errorTopicArn, SerializeJson(report));

        async Task ILogicDependencyProvider.SendUsageReportAsync(UsageReport report)
            => await _snsClient.PublishAsync(_usageTopicArn, SerializeJson(report));

        void ILogicDependencyProvider.WriteLine(string message)
            => LogInfo(message);
    }
}
