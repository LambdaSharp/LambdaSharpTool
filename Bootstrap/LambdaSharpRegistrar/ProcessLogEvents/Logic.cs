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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp.Reports;
using MindTouch.LambdaSharpRegistrar.Registrations;

namespace MindTouch.LambdaSharpRegistrar.ProcessLogEvents {

    public interface ILogicDependencyProvider {

        //--- Methods ---
        Task SendErrorReportAsync(OwnerMetaData owner, ErrorReport report);
        Task SendUsageReportAsync(OwnerMetaData owner, UsageReport report);
        ErrorReport DeserializeErrorReport(string jsonReport);
        void WriteLine(string message);
    }

    public class Logic {

        //--- Types ---
        private delegate Task MatchHandlerAsync(OwnerMetaData owner, ErrorReport report, Match match);

        //--- Class Methods ---
        private static (Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern) CreateMatchPattern(string pattern, MatchHandlerAsync handler)
            => (
                Regex: new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline),
                HandlerAsync: handler,
                Pattern: pattern
            );

        public static string ToMD5Hash(string text) {
            using(var md5 = MD5.Create()) {
                return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
        }

        public static string ToHexString(IEnumerable<byte> bytes)
            => string.Concat(bytes.Select(x => x.ToString("X2")));

        //--- Fields ---
        private ILogicDependencyProvider _provider;
        private IEnumerable<(Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern)> _mappings;

        //--- Constructors ---
        public Logic(ILogicDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _mappings = new (Regex Regex, MatchHandlerAsync HandlerAsync, string Pattern)[] {
                CreateMatchPattern(@"^START RequestId: (?<RequestId>[\da-f\-]+).*$", IgnoreEntryAsync),
                CreateMatchPattern(@"^END RequestId: (?<RequestId>[\da-f\-]+).*$", IgnoreEntryAsync),
                CreateMatchPattern(@"^REPORT RequestId: (?<RequestId>[\da-f\-]+)\s*Duration: (?<UsedDuration>[\d\.]+) ms\s*Billed Duration: (?<BilledDuration>[\d\.]+) ms\s*Memory Size: (?<MaxMemory>[\d\.]+) MB\s*Max Memory Used: (?<UsedMemory>[\d\.]+) MB\s*$", MatchExecutionReportAsync),
                CreateMatchPattern(@"^\s*{.*}\s*$", MatchLambdaSharpJsonLogEntryAsync),
                CreateMatchPattern(@"^(?<ErrorMessage>[^:]+): LambdaException$", MatchLambdaExceptionAsync),
                CreateMatchPattern(@"^(?<Timestamp>[\d\-T\.:Z]+) (?<RequestId>[\da-f\-]+) (?<ErrorMessage>Task timed out after (?<Duration>[\d\.]+) seconds)$", MatchTimeoutAsync),
                CreateMatchPattern(@"^RequestId: (?<RequestId>[\da-f\-]+) (?<ErrorMessage>Process exited before completing request)$", MatchProcessExitedBeforeCompletionAsync)
            };
        }

        //--- Methods ---
        public async Task<bool> ProgressLogEntryAsync(OwnerMetaData owner, string message, string timestamp) {
            foreach(var mapping in _mappings) {
                var match = mapping.Regex.Match(message);
                if(match.Success) {

                    // fill-in error report with owner information
                    var report = new ErrorReport {
                        ModuleName = owner.ModuleName,
                        ModuleVersion = owner.ModuleVersion,
                        Tier = owner.Tier,
                        ModuleId = owner.ModuleId,
                        FunctionId = owner.FunctionId,
                        FunctionName = owner.FunctionName,
                        Platform = owner.FunctionPlatform,
                        Framework = owner.FunctionFramework,
                        Language = owner.FunctionLanguage,
                        Level = "ERROR",
                        Raw = message.Trim(),
                        Timestamp = long.Parse(timestamp),
                        Fingerprint = ToMD5Hash($"{owner.FunctionId}:{mapping.Pattern}")
                    };

                    // have handler fill in the rest from the matched error line
                    await mapping.HandlerAsync(owner, report, match);
                    return true;
                }
            }
            return false;
        }

        private Task IgnoreEntryAsync(OwnerMetaData owner, ErrorReport report, Match match) {

            // nothing to do
            return Task.CompletedTask;
        }

        private async Task MatchLambdaSharpJsonLogEntryAsync(OwnerMetaData owner, ErrorReport report, Match match) {
            report = _provider.DeserializeErrorReport(match.ToString());
            if((report.Version == "2018-09-27") && (report.Source == "LambdaError")) {
                await _provider.SendErrorReportAsync(owner, report);
            } else {

                // TODO: bad json document
                throw new Exception("bad json document");
            }
        }

        private Task MatchLambdaExceptionAsync(OwnerMetaData owner, ErrorReport report, Match match) {
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchTimeoutAsync(OwnerMetaData owner, ErrorReport report, Match match) {
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchProcessExitedBeforeCompletionAsync(OwnerMetaData owner, ErrorReport report, Match match) {
            report.Message = match.Groups["ErrorMessage"].Value;
            report.RequestId = match.Groups["RequestId"].Value;
            return _provider.SendErrorReportAsync(owner, report);
        }

        private Task MatchExecutionReportAsync(OwnerMetaData owner, ErrorReport report, Match match) {
            var requestId = match.Groups["RequestId"].Value;
            var usedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["UsedDuration"].Value));
            var billedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["BilledDuration"].Value));
            var maxMemory = int.Parse(match.Groups["MaxMemory"].Value);
            var usedMemory = int.Parse(match.Groups["UsedMemory"].Value);
            var usage = new UsageReport {
                BilledDuration = billedDuration,
                UsedDuration = usedDuration,
                UsedDurationPercent = (float)usedDuration.TotalMilliseconds / (float)owner.FunctionMaxDuration.TotalMilliseconds,
                MaxDuration = owner.FunctionMaxDuration,
                MaxMemory = maxMemory,
                UsedMemory = usedMemory,
                UsedMemoryPercent = (float)usedMemory / (float)owner.FunctionMaxMemory
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
                report.Message = $"Process ran out of memory (Max: {usage.UsedMemory} MB)";
                report.Fingerprint = ToMD5Hash($"{owner.FunctionId}-Process ran out of memory");
            } else if(usage.UsedDurationPercent >= 1.0f) {

                // nothing to do since timeouts are reported separately
            } else if((usage.UsedDurationPercent >= 0.85f) || (usage.UsedMemoryPercent >= 0.85f)) {

                // report near out-of-memory/timeout warning
                report.Level = "WARNING";
                report.Message = $"Process nearing execution limits (Memory {usage.UsedMemoryPercent:P2}, Duration: {usage.UsedDurationPercent:P2})";
                report.Fingerprint = ToMD5Hash($"{owner.FunctionId}-Process nearing execution limits");
            }
            if(report.Message != null) {
                tasks.Add(_provider.SendErrorReportAsync(owner, report));
            }
            return Task.WhenAll(tasks);
        }
    }
}
