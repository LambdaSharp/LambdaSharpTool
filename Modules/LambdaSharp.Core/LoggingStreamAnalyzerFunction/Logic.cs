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

namespace LambdaSharp.Core.LoggingStreamAnalyzerFunction {

    public interface ILogicDependencyProvider {

        //--- Methods ---
        Task SendErrorReportAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaErrorReport report);
        Task SendUsageReportAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaUsageRecord report);
        Task SendEventAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaEventRecord record);
        Task SendMetricsAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaMetricsRecord record);
    }

    public class LambdaLogRecord : ALambdaRecord {

        //--- Properties ---

        // NOTE (2020-05-05, bjorg): 'Type' used to be called 'Source' pre-0.8
        public string? Source { get; set; }
    }

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
        private static Regex _csharpTrace = new Regex(@"^\s+at (?<Method>.+?)( in (?<File>.+?):line (?<Line>.+))?$");

        //--- Class Methods ---
        private static (Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern) CreateMatchPattern(string pattern, MatchHandlerAsync handler)
            => (
                Regex: new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline),
                HandlerAsync: handler,
                Pattern: pattern
            );

        private static LambdaErrorReport PopulateLambdaErrorReport(LambdaErrorReport report, OwnerMetaData owner, string message, DateTimeOffset timestamp, string pattern) {

            // fill-in error report with owner information
            report.ModuleInfo = owner.ModuleInfo;
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

                // Lambda runtime exceptions need to be reported; these occur when there is an exception to locate
                //  the constructor or inside the constructor.
                CreateMatchPattern(@"^(?<ErrorMessage>[^:]+): LambdaException$", MatchLambdaExceptionAsync),

                // NOTE (2020-05-12, bjorg): this message is shown when an exception bubbles out of the Lambda function; since
                //  all exceptions are already logged as LambdaError records, this log entry is not needed.
                CreateMatchPattern(@"^.*: [\w]*Exception$", IgnoreEntryAsync),

                // NOTE (2020-05-12, bjorg): this message is shown when an exception occurs in the constructor or the function
                //  entry point could not be found; both errors are reported already by other log entries.
                CreateMatchPattern(@"^.*\[WARN\] .* run_dotnet\(dotnet_path, &args\) failed.*$", IgnoreEntryAsync),

                // NOTE (2020-05-12, bjorg): this message is shown when an exception occurs in the constructor or the function
                //  entry point could not be found; both errors are reported already by other log entries.
                CreateMatchPattern(@"^Unknown application error occurred.*$", IgnoreEntryAsync),

                // Lambda timeout error
                CreateMatchPattern(@"^(?<Timestamp>[\d\-T\.:Z]+)\s+(?<RequestId>[\da-f\-]+) (?<ErrorMessage>Task timed out after (?<Duration>[\d\.]+) seconds)$", MatchTimeoutAsync),
                CreateMatchPattern(@"^RequestId: (?<RequestId>[\da-f\-]+) (?<ErrorMessage>Process exited before completing request)$", MatchProcessExitedBeforeCompletionAsync),

                // Javascript errors
                CreateMatchPattern(@"^(?<Timestamp>[\d\-T\.:Z]+)\s+(?<RequestId>[\da-f\-]+)\s+(?<ErrorMessage>{""errorMessage.*})\s*$", MatchJavascriptExceptionAsync),
                CreateMatchPattern(@"^(?<ErrorMessage>[^:]+): SyntaxError$", MatchJavascriptSyntaxErrorAsync),
            };
        }

        //--- Methods ---
        public async Task ProgressLogEntryAsync(OwnerMetaData owner, string? message, DateTimeOffset timestamp) {
            if(message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if(timestamp == DateTimeOffset.UnixEpoch) {
                throw new ArgumentException("timestamp not set", nameof(timestamp));
            }
            foreach(var mapping in _mappings) {
                var match = mapping.Regex.Match(message);
                if(match.Success) {
                    await mapping.HandlerAsync(owner, message, timestamp, match, mapping.Pattern);
                    return;
                }
            }
            await UnrecognizedEntryAsync(owner, message, timestamp);
        }

        private Task UnrecognizedEntryAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, "^.*$");
            report.Level = "WARNING";
            report.Message = message;
            return _provider.SendErrorReportAsync(owner, timestamp, report);
        }

        private Task IgnoreEntryAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {

            // nothing to do
            return Task.CompletedTask;
        }

        private async Task MatchLambdaSharpJsonLogEntryAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var text = match.ToString();
            var record = _serializer.Deserialize<LambdaLogRecord>(text);

            // check for pre-0.8 log record
            if((record.Type == null) && (record.Source != null)) {
                if(record.Source == "LambdaError") {

                    // report error record
                    var errorReport = _serializer.Deserialize<LambdaErrorReport>(text);

                    // convert old format into new
                    errorReport.ModuleInfo = errorReport.Module;
                    errorReport.Module = errorReport.Module?.Split(':', 2)[0];
                    await _provider.SendErrorReportAsync(owner, timestamp, errorReport);
                } else {
                    throw new ProcessLogEventsException($"unrecognized record 'Source' property: {record.Source}");
                }
            } else {
                switch(record.Type) {
                case "LambdaError":

                    // report error record
                    var errorReport = _serializer.Deserialize<LambdaErrorReport>(text);
                    await _provider.SendErrorReportAsync(owner, timestamp, errorReport);
                    break;
                case "LambdaEvent":

                    // report event record
                    var eventRecord = _serializer.Deserialize<LambdaEventRecord>(text);
                    await _provider.SendEventAsync(owner, timestamp, eventRecord);
                    break;
                case "LambdaMetrics":

                    // report metrics record
                    var metricsRecord = _serializer.Deserialize<LambdaMetricsRecord>(text);
                    await _provider.SendMetricsAsync(owner, timestamp, metricsRecord);
                    break;
                case null:
                    throw new ProcessLogEventsException($"missing record '{nameof(record.Type)}' property");
                default:
                    throw new ProcessLogEventsException($"unrecognized record '{nameof(record.Type)}' property: {record.Type}");
                }
            }
        }

        private Task MatchLambdaExceptionAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;

            // remove empty lines, but keep the indentation
            var lines = message.Split("\n", StringSplitOptions.RemoveEmptyEntries).ToList();

            // convert lines into one or more stack traces
            var traces = new List<LambdaErrorReportStackTrace>();
            report.Traces = traces;
            while(lines.Any()) {
                var trace = new LambdaErrorReportStackTrace();

                // process exception message
                var messageLine = lines.First();
                lines.RemoveAt(0);
                var lastColon = messageLine.LastIndexOf(':');
                if(lastColon >= 0) {
                    trace.Exception = new LambdaErrorReportExceptionInfo {
                        Type = messageLine.Substring(lastColon + 1).Trim(),
                        Message = messageLine.Substring(0, lastColon).Trim()
                    };
                } else {
                    trace.Exception = new LambdaErrorReportExceptionInfo {
                        Type = "Exception",
                        Message = messageLine
                    };
                }
                traces.Add(trace);

                // grab as many matching frame lines as possible and remove them from the list
                var frameLineMatches = lines.Select(line => (Line: line, Match: _csharpTrace.Match(line)))
                    .TakeWhile(tuple => tuple.Match.Success)
                    .ToList();
                lines.RemoveRange(0, frameLineMatches.Count);
                trace.Exception.StackTrace = string.Join("\n", frameLineMatches.Select(tuple => tuple.Line));

                // convert each trace line into a frame
                var frames = new List<LambdaErrorReportStackFrame>();
                foreach(var frameLineMatch in frameLineMatches) {
                    var frame = new LambdaErrorReportStackFrame {
                        MethodName = frameLineMatch.Match.Groups["Method"].Value
                    };

                    // check if the trace line contains information about the originating file and line number
                    var file = frameLineMatch.Match.Groups["File"].Value;
                    if(!string.IsNullOrEmpty(file)) {
                        frame.FileName = file.Trim();
                        if(int.TryParse(frameLineMatch.Match.Groups["Line"].Value, out var line)) {
                            frame.LineNumber = line;
                        }
                    }
                    frames.Add(frame);
                }

                // only set set frames if any were generated
                if(frames.Any()) {
                    trace.Frames = frames;
                }
            }
            return _provider.SendErrorReportAsync(owner, timestamp, report);
        }

        private Task MatchTimeoutAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = $"Lambda timed out after {match.Groups["Duration"].Value} seconds";
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, timestamp, report);
        }

        private Task MatchProcessExitedBeforeCompletionAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = "Lambda exited before completing request";
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, timestamp, report);
        }

        private Task MatchExecutionReportAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            var requestId = match.Groups["RequestId"].Value;
            var usedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["UsedDuration"].Value));
            var billedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["BilledDuration"].Value));
            var maxMemory = int.Parse(match.Groups["MaxMemory"].Value);
            var usedMemory = int.Parse(match.Groups["UsedMemory"].Value);
            var initDuration = !string.IsNullOrEmpty(match.Groups["InitDuration"].Value)
                ? (TimeSpan?)TimeSpan.FromMilliseconds(double.Parse(match.Groups["InitDuration"].Value))
                : null;
            var usage = new LambdaUsageRecord {
                ModuleInfo = owner.ModuleInfo,
                Module = owner.Module,
                ModuleId = owner.ModuleId,
                FunctionId = owner.FunctionId,
                Function = owner.FunctionName,
                BilledDuration = (float)billedDuration.TotalSeconds,
                UsedDuration = (float)usedDuration.TotalSeconds,
                UsedDurationPercent = (float)(usedDuration.TotalSeconds / owner.FunctionMaxDuration.TotalSeconds),
                MaxDuration = (float)owner.FunctionMaxDuration.TotalSeconds,
                MaxMemory = maxMemory,
                UsedMemory = usedMemory,
                UsedMemoryPercent = (float)usedMemory / (float)owner.FunctionMaxMemory,
                InitDuration = (float?)initDuration?.TotalSeconds
            };
            var tasks = new List<Task> {
                _provider.SendUsageReportAsync(owner, timestamp, usage)
            };

            // send error report if usage is near or exceeding limits
            report.Message = null;
            report.RequestId = requestId;
            if(usage.UsedMemoryPercent >= 1.0f) {

                // report out-of-memory error
                report.Level = "ERROR";
                report.Message = $"Lambda ran out of memory (Max: {owner.FunctionMaxMemory} MB)";
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
                tasks.Add(_provider.SendErrorReportAsync(owner, timestamp, report));
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
            return _provider.SendErrorReportAsync(owner, timestamp, report);
        }

        private Task MatchJavascriptSyntaxErrorAsync(OwnerMetaData owner, string message, DateTimeOffset timestamp, Match match, string pattern) {
            var report = PopulateLambdaErrorReport(new LambdaErrorReport(), owner, message, timestamp, pattern);
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, timestamp, report);
        }
    }
}
