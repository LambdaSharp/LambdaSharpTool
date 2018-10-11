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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp.Reports;

namespace MindTouch.LambdaSharpRegistrar.ProcessLogEvents {

    public interface ILogicDependencyProvider {

        //--- Methods ---
        Task SendErrorReportAsync(ErrorReport report);
        Task SendUsageReportAsync(UsageReport report);
        ErrorReport DeserializeErrorReport(string jsonReport);
        void WriteLine(string message);
    }

    public class UsageReport {

        //--- Properties ---
        public TimeSpan BilledDuration { get; set; }
        public TimeSpan UsedDuration { get; set; }
        public float UsedDurationPercent { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public int MaxMemory { get; set; }
        public int UsedMemory { get; set; }
        public float UsedMemoryPercent { get; set; }
    }

    public class OwnerMetaData {

        //--- Properties ---
        public string LogGroupName { get; set; }
        public string ModuleName { get; set; }
        public string ModuleVersion { get; set; }
        public string DeploymentTier { get; set; }
        public string ModuleId { get; set; }
        public string FunctionName { get; set; }
        public string Platform { get; set; }
        public string Framework { get; set; }
        public string Language { get; set; }
        public string GitSha { get; set; }
        public string GitBranch { get; set; }
        public int MaxMemory { get; set; }
        public TimeSpan MaxDuration { get; set; }
    }

    public class Logic {

        //--- Types ---
        private delegate Task MatchHandlerAsync(OwnerMetaData owner, Match match, string timestamp);

        //--- Class Methods ---
        private static (Regex Regex, MatchHandlerAsync HandlerAsync) CreateMatchPattern(string pattern, MatchHandlerAsync handler)
            => (Regex: new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled), HandlerAsync: handler);

        //--- Fields ---
        private ILogicDependencyProvider _provider;
        private IEnumerable<(Regex Regex, MatchHandlerAsync HandlerAsync)> _mappings;

        //--- Constructors ---
        public Logic(ILogicDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _mappings = new (Regex Regex, MatchHandlerAsync HandlerAsync)[] {
                CreateMatchPattern(@"^(?<ErrorMessage>[^:]+): LambdaException$", MatchLambdaExceptionAsync),
                CreateMatchPattern(@"^(?<Timestamp>[\d\-T\.:Z]+) (?<RequestId>[\da-f\-]+) (?<ErrorMessage>Task timed out after (?<Duration>[\d\.]+) seconds)$", MatchTimeoutAsync),
                CreateMatchPattern(@"^RequestId: (?<RequestId>[\da-f\-]+) (?<ErrorMessage>Process exited before completing request)$", MatchProcessExitedBeforeCompletionAsync),
                CreateMatchPattern(@"^REPORT RequestId: (?<RequestId>[\da-f\-]+)\s*Duration: (?<UsedDuration>[\d\.]+) ms\s*Billed Duration: (?<BilledDuration>[\d\.]+) ms\s*Memory Size: (?<MaxMemory>[\d\.]+) MB\s*Max Memory Used: (?<UsedMemory>[\d\.]+) MB\s*$", MatchExecutionReportAsync)
            };
        }

        //--- Methods ---
        public Task ProgressLogEntryAsync(OwnerMetaData owner, string message, string timestamp) {
            if(message.StartsWith("{") && message.EndsWith("}")) {
                var report = _provider.DeserializeErrorReport(message);
                if((report.Version == "2018-09-27") && (report.Source == "LambdaError")) {
                    return _provider.SendErrorReportAsync(report);
                }

                // TODO: bad json document
                throw new Exception("bad json document");
            }
            foreach(var mapping in _mappings) {
                var match = mapping.Regex.Match(message);
                if(match.Success) {
                    return mapping.HandlerAsync(owner, match, timestamp);
                }
            }
            return Task.CompletedTask;
        }

        private Task MatchLambdaExceptionAsync(OwnerMetaData owner, Match match, string timestamp) {
            _provider.WriteLine($"*** MatchLambdaExceptionAsync: {match}");
            PrintMatch(match);

            return SendErrorReport(owner, report => {
                report.Message = match.Groups["ErrorMessage"].Value;
                report.RequestId = match.Groups["RequestId"].Value;
                report.Timestamp = long.Parse(timestamp);
            });
        }

        private Task MatchTimeoutAsync(OwnerMetaData owner, Match match, string timestamp) {
            _provider.WriteLine($"*** MatchTimeOutAsync: {match}");
            PrintMatch(match);

            return SendErrorReport(owner, report => {
                report.Message = match.Groups["ErrorMessage"].Value;
                report.RequestId = match.Groups["RequestId"].Value;
                report.Timestamp = long.Parse(timestamp);
            });
        }

        private Task MatchProcessExitedBeforeCompletionAsync(OwnerMetaData owner, Match match, string timestamp) {
            _provider.WriteLine($"*** MatchProcessExitedAsync: {match}");
            PrintMatch(match);

            return SendErrorReport(owner, report => {
                report.Message = match.Groups["ErrorMessage"].Value;
                report.RequestId = match.Groups["RequestId"].Value;
                report.Timestamp = long.Parse(timestamp);
            });
        }

        private Task MatchExecutionReportAsync(OwnerMetaData owner, Match match, string timestamp) {
            _provider.WriteLine($"*** MatchExecutionReportAsync: {match}");
            PrintMatch(match);

            var requestId = match.Groups["RequestId"].Value;
            var usedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["UsedDuration"].Value));
            var billedDuration = TimeSpan.FromMilliseconds(double.Parse(match.Groups["BilledDuration"].Value));
            var maxMemory = int.Parse(match.Groups["MaxMemory"].Value);
            var usedMemory = int.Parse(match.Groups["UsedMemory"].Value);
            return _provider.SendUsageReportAsync(new UsageReport {
                BilledDuration = billedDuration,
                UsedDuration = usedDuration,
                UsedDurationPercent = (float)usedDuration.TotalMilliseconds / (float)owner.MaxDuration.TotalMilliseconds,
                MaxDuration = owner.MaxDuration,
                MaxMemory = maxMemory,
                UsedMemory = usedMemory,
                UsedMemoryPercent = (float)usedMemory / (float)owner.MaxMemory
            });
        }

        private void PrintMatch(Match match) {
            var index = 0;
            foreach(Group group in match.Groups.Skip(1)) {
                _provider.WriteLine($"{group.Name} = {group.Value}");
                ++index;
            }
        }

        private Task SendErrorReport(OwnerMetaData owner, Action<ErrorReport> preparer) {
            var report = new ErrorReport {
                ModuleName = owner.ModuleName,
                ModuleVersion = owner.ModuleVersion,
                DeploymentTier = owner.DeploymentTier,
                ModuleId = owner.ModuleId,
                FunctionName = owner.FunctionName,
                Platform = owner.Platform,
                Framework = owner.Framework,
                Language = owner.Language,
                GitSha = owner.GitSha,
                GitBranch = owner.GitBranch
            };
            preparer(report);
            return _provider.SendErrorReportAsync(report);
        }
    }
}
