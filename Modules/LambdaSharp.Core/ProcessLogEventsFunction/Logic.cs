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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp.Core.Registrations;
using LambdaSharp.ErrorReports;
using LambdaSharp.Records;
using LambdaSharp.Records.Events;
using LambdaSharp.Records.Metrics;

namespace LambdaSharp.Core.ProcessLogEventsFunction {

    public interface ILogicDependencyProvider {

        //--- Methods ---
        Task SendErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report);
        Task SendUsageReportAsync(OwnerMetaData owner, UsageReport report);
        Task SendEventAsync(OwnerMetaData owner, LambdaEventRecord record);
        Task SendMetricsAsync(OwnerMetaData owner, LambdaMetricsRecord record);
    }

    public class LambdaLogRecord : ALambdaRecord { }

    public class Logic {

        //--- Types ---
        private delegate Task MatchHandlerAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern);

        private class JavascriptException {

            //--- Properties ---
            public string? ErrorMessage { get; set; }
            public string? ErrorType { get; set; }
            public List<string>? StackTrace { get; set; }
        }

        //--- Class Fields ---
        private static Regex _javascriptTrace = new Regex(@"(?<Function>.*)\((?<File>.*):(?<Line>[\d]+):(?<Column>[\d]+)\)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        //--- Class Methods ---
        private static (Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern) CreateMatchPattern(string pattern, MatchHandlerAsync handler)
            => (
                Regex: new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline),
                HandlerAsync: handler,
                Pattern: pattern
            );

        private static LambdaErrorReport PopulateLambdaErrorReport(LambdaErrorReport report, OwnerMetaData owner, string message, DateTimeOffset timestamp, string pattern) {

            // fill-in error report with owner information
            report.Module = owner.Module;
            report.Module = owner.Module;
            report.ModuleId = owner.ModuleId;
            report.FunctionId = owner.FunctionId;
            report.FunctionName = owner.FunctionName;
            report.Platform = owner.FunctionPlatform;
            report.Framework = owner.FunctionFramework;
            report.Language = owner.FunctionLanguage;
            report.Level = "ERROR";
            report.Raw = message.Trim();
            report.Timestamp = timestamp.ToUnixTimeMilliseconds();
            report.Fingerprint = ToMD5Hash($"{owner.FunctionId}:{pattern}");
            return report;
        }

        private static string ToMD5Hash(string text) {
            using(var md5 = MD5.Create()) {
                return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
        }

        private static string ToHexString(IEnumerable<byte> bytes)
            => string.Concat(bytes.Select(x => x.ToString("X2")));

        //--- Fields ---
        private readonly ILogicDependencyProvider _provider;
        private readonly IEnumerable<(Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern)> _mappings;
        private readonly ILambdaSerializer _serializer;

        //--- Constructors ---
        public Logic(ILogicDependencyProvider provider, ILambdaSerializer serializer) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _mappings = new (Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern)[] {

                // Lambda report entries
                CreateMatchPattern(@"^START RequestId: (?<RequestId>[\da-f\-]+).*$", IgnoreEntryAsync),
                CreateMatchPattern(@"^END RequestId: (?<RequestId>[\da-f\-]+).*$", IgnoreEntryAsync),
                CreateMatchPattern(@"^REPORT RequestId: (?<RequestId>[\da-f\-]+)\s*Duration: (?<UsedDuration>[\d\.]+) ms\s*Billed Duration: (?<BilledDuration>[\d\.]+) ms\s*Memory Size: (?<MaxMemory>[\d\.]+) MB\s*Max Memory Used: (?<UsedMemory>[\d\.]+) MB\s*Init Duration: (?<InitDuration>[\d\.]+) ms", MatchExecutionReportAsync),
                CreateMatchPattern(@"^REPORT RequestId: (?<RequestId>[\da-f\-]+)\s*Duration: (?<UsedDuration>[\d\.]+) ms\s*Billed Duration: (?<BilledDuration>[\d\.]+) ms\s*Memory Size: (?<MaxMemory>[\d\.]+) MB\s*Max Memory Used: (?<UsedMemory>[\d\.]+) MB", MatchExecutionReportAsync),

                // LambdaSharp error report
                CreateMatchPattern(@"^\s*{.*}\s*$", MatchLambdaSharpJsonLogEntryAsync),

                // Lambda .NET exception
                CreateMatchPattern(@"^(?<ErrorMessage>[^:]+): LambdaException$", MatchLambdaExceptionAsync),

                // Lambda timeout error
                CreateMatchPattern(@"^(?<Timestamp>[\d\-T\.:Z]+)\s+(?<RequestId>[\da-f\-]+) (?<ErrorMessage>Task timed out after (?<Duration>[\d\.]+) seconds)$", MatchTimeoutAsync),
                CreateMatchPattern(@"^RequestId: (?<RequestId>[\da-f\-]+) (?<ErrorMessage>Process exited before completing request)$", MatchProcessExitedBeforeCompletionAsync),

                // Javascript errors
                CreateMatchPattern(@"^(?<Timestamp>[\d\-T\.:Z]+)\s+(?<RequestId>[\da-f\-]+)\s+(?<ErrorMessage>{""errorMessage.*})\s*$", MatchJavascriptExceptionAsync),
                CreateMatchPattern(@"^(?<ErrorMessage>[^:]+): SyntaxError$", MatchJavascriptSyntaxErrorAsync),
            };
        }

        //--- Methods ---
        public async Task<bool> ProgressLogEntryAsync(OwnerMetaData owner, string? message, DateTimeOffset timestamp) {
            if((message == null) || (timestamp == DateTimeOffset.UnixEpoch)) {
                return false;
            }
            foreach(var mapping in _mappings) {
                var match = mapping.Regex.Match(message);
                if(match.Success) {
                    await mapping.HandlerAsync(owner, message, timestamp, match, mapping.Pattern);
                    return true;
                }
            }
            return false;
        }

        private Task IgnoreEntryAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {

            // nothing to do
            return Task.CompletedTask;
        }

        private async Task MatchLambdaSharpJsonLogEntryAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var text = match.ToString();
            var record = _serializer.Deserialize<LambdaLogRecord>(text);
            switch(record.Source) {
            case "LambdaError":

                // report error record
                var errorReport = _serializer.Deserialize<LambdaErrorReport>(text);
                await _provider.SendErrorReportAsync(owner, errorReport);
                break;
            case "LambdaEvent":

                // report event record
                var eventRecord = _serializer.Deserialize<LambdaEventRecord>(text);
                if(eventRecord.Time == null) {
                    eventRecord.Time = timestamp.ToRfc3339Timestamp();
                }
                await _provider.SendEventAsync(owner, eventRecord);
                break;
            case "LambdaMetrics":

                // report metrics record
                var metricsRecord = _serializer.Deserialize<LambdaMetricsRecord>(text);
                await _provider.SendMetricsAsync(owner, metricsRecord);
                break;
            case null:
                throw new ProcessLogEventsException("missing record 'Source' property");
            default:
                throw new ProcessLogEventsException($"unrecognized record 'Source' property: {record.Source}");
            }
        }

        private Task MatchLambdaExceptionAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchTimeoutAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = $"Lambda timed out after {match.Groups["Duration"].Value} seconds";
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchProcessExitedBeforeCompletionAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = "Lambda exited before completing request";
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchExecutionReportAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            var requestId = match.Groups["RequestId"].Value;
            var usedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["UsedDuration"].Value));
            var billedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["BilledDuration"].Value));
            var maxMemory = int.Parse(match.Groups["MaxMemory"].Value);
            var usedMemory = int.Parse(match.Groups["UsedMemory"].Value);
            var initDuration = !string.IsNullOrEmpty(match.Groups["InitDuration"].Value)
                ? TimeSpan.FromMilliseconds(double.Parse(match.Groups["InitDuration"].Value))
                : TimeSpan.Zero;
            var usage = new UsageReport {
                BilledDuration = billedDuration,
                UsedDuration = usedDuration,
                UsedDurationPercent = (float)usedDuration.TotalMilliseconds / (float)owner.FunctionMaxDuration.TotalMilliseconds,
                MaxDuration = owner.FunctionMaxDuration,
                MaxMemory = maxMemory,
                UsedMemory = usedMemory,
                UsedMemoryPercent = (float)usedMemory / (float)owner.FunctionMaxMemory,
                InitDuration = initDuration
            };
            var tasks = new List<Task> {
                _provider.SendUsageReportAsync(owner, usage)
            };

            // send error report if usage is near or exceeding limits
            report.Message = null;
            report.RequestId = requestId;
            if(usage.UsedMemoryPercent >= 1.0f) {

                // report out-of-memory error
                report.Level = "ERROR";
                report.Message = $"Lambda ran out of memory (Max: {usage.UsedMemory} MB)";
                report.Fingerprint = ToMD5Hash($"{owner.FunctionId}-Lambda ran out of memory");
            } else if(usage.UsedDurationPercent >= 1.0F) {

                // nothing to do since timeouts are reported separately
            } else if((usage.UsedDurationPercent >= 0.85F) || (usage.UsedMemoryPercent >= 0.85F)) {

                // report near out-of-memory/timeout warning
                report.Level = "WARNING";
                report.Message = $"Lambda nearing execution limits (Memory {usage.UsedMemoryPercent:P2}, Duration: {usage.UsedDurationPercent:P2})";
                report.Fingerprint = ToMD5Hash($"{owner.FunctionId}-Lambda nearing execution limits");
            }
            if(report.Message != null) {
                tasks.Add(_provider.SendErrorReportAsync(owner, report));
            }
            return Task.WhenAll(tasks);
        }

        private Task MatchJavascriptExceptionAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.RequestId = match.Groups["RequestId"].Value;
            var error = _serializer.Deserialize<JavascriptException>(match.Groups["ErrorMessage"].Value);
            report.Message = error.ErrorMessage;
            if(error.StackTrace?.Any() == true) {
                report.Traces = new List<LambdaErrorReportStackTrace> {
                    new LambdaErrorReportStackTrace {
                        Exception = new LambdaErrorReportExceptionInfo {
                            Type = "Error",
                            Message = error.ErrorMessage
                        },
                        Frames = error.StackTrace.Select(trace => {
                            var traceMatch = _javascriptTrace.Match(trace);
                            var frame = new LambdaErrorReportStackFrame {
                                MethodName = traceMatch.Groups["Function"].Value.Trim(),
                                FileName = traceMatch.Groups["File"].Value
                            };
                            if(int.TryParse(traceMatch.Groups["Line"].Value, out var line)) {
                                frame.LineNumber = line;
                            }
                            return frame;
                        }).ToList()
                    }
                };
            }
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchJavascriptSyntaxErrorAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }
    }
}
