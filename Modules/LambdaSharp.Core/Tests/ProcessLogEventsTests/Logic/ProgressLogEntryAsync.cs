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
using System.Threading.Tasks;
using FluentAssertions;
using LambdaSharp.Core.Registrations;
using LambdaSharp.ErrorReports;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LambdaSharp.Core.ProcessLogEvents.Tests {

    public class ProgressLogEntryAsync {

        //--- Types ---
        private class MockDependencyProvider : ILogicDependencyProvider {

            //--- Fields ---
            public LambdaErrorReport ErrorReport;
            public UsageReport UsageReport;
            private ITestOutputHelper _output;

            //--- Constructors ---
            public MockDependencyProvider(ITestOutputHelper output) {
                _output = output;
            }

            //--- Methods ---
            public LambdaErrorReport DeserializeErrorReport(string jsonReport)
                => JsonConvert.DeserializeObject<LambdaErrorReport>(jsonReport);

            public Task SendErrorReportAsync(OwnerMetaData owner, LambdaErrorReport report) {
                ErrorReport.Should().BeNull();
                ErrorReport = report;
                return Task.CompletedTask;
            }

            public Task SendUsageReportAsync(OwnerMetaData owner, UsageReport report) {
                UsageReport.Should().BeNull();
                UsageReport = report;
                return Task.CompletedTask;
            }

            public void LogProcessingError(Exception exception) {
                _output.WriteLine(exception.ToString());
            }
        }

        //--- Fields ---
        private MockDependencyProvider _provider;
        private Logic _logic;
        private OwnerMetaData _owner;

        //--- Constructors ---
        public ProgressLogEntryAsync(ITestOutputHelper output) {
            _provider = new MockDependencyProvider(output);
            _logic = new Logic(_provider);
            _owner = new OwnerMetaData {
                Module = "Test.Module:1.0@origin",
                ModuleId = "ModuleId",
                FunctionId = "ModuleName-FunctionName-NT5EUXTNTXXD",
                FunctionName = "FunctionName",
                FunctionLogGroupName = "/aws/lambda/MyTestFunction",
                FunctionPlatform = "Platform",
                FunctionFramework = "Framework",
                FunctionLanguage = "Language",
                FunctionMaxDuration = TimeSpan.FromMilliseconds(10000),
                FunctionMaxMemory = 128
            };
        }

        //--- Methods ---
        [Fact]
        public void LambdaSharpJsonLogEntry() {
            var success = _logic.ProgressLogEntryAsync(_owner, "{\"Source\":\"LambdaError\",\"Version\":\"2018-09-27\",\"Module\":\"Test.Module:1.0@origin\",\"ModuleName\":\"ModuleName\",\"ModuleVersion\":\"ModuleVersion\",\"ModuleId\":\"ModuleId\",\"FunctionId\":\"ModuleName-FunctionName-NT5EUXTNTXXD\",\"FunctionName\":\"FunctionName\",\"Platform\":\"Platform\",\"Framework\":\"Framework\",\"Language\":\"Language\",\"GitSha\":\"GitSha\",\"GitBranch\":\"GitBranch\",\"RequestId\":\"RequestId\",\"Level\":\"Level\",\"Fingerprint\":\"Fingerprint\",\"Timestamp\":1539361232,\"Message\":\"failed during message stream processing\"}", "1539238963679").Result;
            success.Should().Be(true);
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("failed during message stream processing");
            _provider.ErrorReport.Timestamp.Should().Be(1539361232);
            _provider.ErrorReport.RequestId.Should().Be("RequestId");
        }

        [Fact]
        public void LambdaException() {
            var success = _logic.ProgressLogEntryAsync(_owner, "Unable to load type 'LambdaSharp.Core.ProcessLogEvents.Function' from assembly 'ProcessLogEvents'.: LambdaException", "1539238963679").Result;
            success.Should().Be(true);
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Unable to load type 'LambdaSharp.Core.ProcessLogEvents.Function' from assembly 'ProcessLogEvents'.");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("");
        }

        [Fact]
        public void Timeout() {
            var success = _logic.ProgressLogEntryAsync(_owner, "2018-10-11T07:00:40.906Z 546933ad-cd23-11e8-bb5d-7f3682cfa000 Task timed out after 15.02 seconds", "1539238963679").Result;
            success.Should().Be(true);
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Lambda timed out after 15.02 seconds");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("546933ad-cd23-11e8-bb5d-7f3682cfa000");
        }

        [Fact]
        public void ProcessExitedBeforeCompletion() {
            var success = _logic.ProgressLogEntryAsync(_owner, "RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6 Process exited before completing request", "1539238963679").Result;
            success.Should().Be(true);
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Lambda exited before completing request");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("813a64e4-cd22-11e8-acad-d7f8fa4137e6");
        }

        [Fact]
        public void ExecutionReport() {
            var success = _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 5169911c-b198-496a-b235-ab77e8a93e97\tDuration: 0.58 ms\tBilled Duration: 100 ms Memory Size: 128 MB\tMax Memory Used: 20 MB\t", "1539238963679").Result;
            success.Should().Be(true);
            _provider.UsageReport.Should().NotBeNull();
            _provider.ErrorReport.Should().BeNull();
            _provider.UsageReport.UsedDuration.Should().Be(TimeSpan.FromMilliseconds(0.58));
            _provider.UsageReport.BilledDuration.Should().Be(TimeSpan.FromMilliseconds(100));
            _provider.UsageReport.MaxDuration.Should().Be(TimeSpan.FromSeconds(10));
            _provider.UsageReport.UsedDurationPercent.Should().BeApproximately(0.0001F, 0.00001F);
            _provider.UsageReport.MaxMemory.Should().Be(128);
            _provider.UsageReport.UsedMemory.Should().Be(20);
            _provider.UsageReport.UsedMemoryPercent.Should().BeApproximately(0.15625F, 0.0001F);
        }

        [Fact]
        public void ExecutionReportOutOfMemory() {
            var success = _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6\tDuration: 1062.06 ms\tBilled Duration: 1000 ms \tMemory Size: 128 MB\tMax Memory Used: 128 MB", "1539238963679").Result;
            success.Should().Be(true);
            _provider.UsageReport.Should().NotBeNull();
            _provider.UsageReport.UsedDuration.Should().Be(TimeSpan.FromMilliseconds(1062.06));
            _provider.UsageReport.BilledDuration.Should().Be(TimeSpan.FromMilliseconds(1000));
            _provider.UsageReport.MaxDuration.Should().Be(TimeSpan.FromSeconds(10));
            _provider.UsageReport.UsedDurationPercent.Should().BeApproximately(0.1062F, 0.00001F);
            _provider.UsageReport.MaxMemory.Should().Be(128);
            _provider.UsageReport.UsedMemory.Should().Be(128);
            _provider.UsageReport.UsedMemoryPercent.Should().BeApproximately(1F, 0.0001F);

            CommonErrorReportAsserts(usageReportCheck: false);
            _provider.ErrorReport.Message.Should().Be("Lambda ran out of memory (Max: 128 MB)");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("813a64e4-cd22-11e8-acad-d7f8fa4137e6");
        }

        private void CommonErrorReportAsserts(bool usageReportCheck = true) {
            _provider.ErrorReport.Should().NotBeNull();
            _provider.ErrorReport.Module.Should().Be("Test.Module:1.0@origin");
            _provider.ErrorReport.ModuleId.Should().Be("ModuleId");
            _provider.ErrorReport.FunctionId.Should().Be("ModuleName-FunctionName-NT5EUXTNTXXD");
            _provider.ErrorReport.FunctionName.Should().Be("FunctionName");
            _provider.ErrorReport.Platform.Should().Be("Platform");
            _provider.ErrorReport.Framework.Should().Be("Framework");
            _provider.ErrorReport.Language.Should().Be("Language");
            if(usageReportCheck) {
                _provider.UsageReport.Should().BeNull();
            }
        }
    }
}
