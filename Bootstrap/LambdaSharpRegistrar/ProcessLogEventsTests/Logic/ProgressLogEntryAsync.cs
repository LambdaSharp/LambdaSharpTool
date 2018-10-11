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
using System.Threading.Tasks;
using FluentAssertions;
using MindTouch.LambdaSharp.Reports;
using Xunit;
using Xunit.Abstractions;

namespace MindTouch.LambdaSharpRegistrar.ProcessLogEvents.Tests {

    // TODO:
    // * test exception in lambda constructor
    // * usage report getting close to being a time-out (error vs. warning)
    // * usage report getting close to being out-of-memory (error vs. warning)

    public class ProgressLogEntryAsync {

        //--- Types ---
        private class MockDependencyProvider : ILogicDependencyProvider {

            //--- Fields ---
            public ErrorReport ErrorReport;
            public UsageReport UsageReport;
            private ITestOutputHelper _output;

            //--- Constructors ---
            public MockDependencyProvider(ITestOutputHelper output) {
                _output = output;
            }

            //--- Methods ---
            public ErrorReport DeserializeErrorReport(string jsonReport) {
                throw new NotImplementedException();
            }

            public Task SendErrorReportAsync(ErrorReport report) {
                Assert.Equal(null, ErrorReport);
                ErrorReport = report;
                return Task.CompletedTask;
            }

            public Task SendUsageReportAsync(UsageReport report) {
                Assert.Equal(null, UsageReport);
                UsageReport = report;
                return Task.CompletedTask;
            }

            public void WriteLine(string message) {
                _output.WriteLine(message);
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
                LogGroupName = "/aws/lambda/MyTestFunction",
                ModuleName = "ModuleName",
                ModuleVersion = "ModuleVersion",
                DeploymentTier = "DeploymentTier",
                ModuleId = "ModuleId",
                FunctionName = "FunctionName",
                Platform = "Platform",
                Framework = "Framework",
                Language = "Language",
                GitSha = "GitSha",
                GitBranch = "GitBranch",
                MaxDuration = TimeSpan.FromMilliseconds(10000),
                MaxMemory = 128
            };
        }

        //--- Methods ---
        [Fact]
        public void LambdaException() {
            _logic.ProgressLogEntryAsync(_owner, "Unable to load type 'MindTouch.LambdaSharpRegistrar.ProcessLogEvents.Function' from assembly 'ProcessLogEvents'.: LambdaException", "1539238963679").Wait();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Unable to load type 'MindTouch.LambdaSharpRegistrar.ProcessLogEvents.Function' from assembly 'ProcessLogEvents'.");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("");
        }

        [Fact]
        public void Timeout() {
            _logic.ProgressLogEntryAsync(_owner, "2018-10-11T07:00:40.906Z 546933ad-cd23-11e8-bb5d-7f3682cfa000 Task timed out after 15.02 seconds", "1539238963679").Wait();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Task timed out after 15.02 seconds");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("546933ad-cd23-11e8-bb5d-7f3682cfa000");
        }

        [Fact]
        public void ProcessExitedBeforeCompletion() {
            _logic.ProgressLogEntryAsync(_owner, "RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6 Process exited before completing request", "1539238963679").Wait();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Process exited before completing request");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("813a64e4-cd22-11e8-acad-d7f8fa4137e6");
        }

        [Fact]
        public void ExecutionReportOutOfMemory() {

            // TODO: this test should also issue an out-of-memory error

            _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6\tDuration: 1062.06 ms\tBilled Duration: 1000 ms \tMemory Size: 128 MB\tMax Memory Used: 128 MB", "1539238963679").Wait();
            _provider.UsageReport.Should().NotBeNull();
            _provider.ErrorReport.Should().BeNull();
            _provider.UsageReport.UsedDuration.Should().Be(TimeSpan.FromMilliseconds(1062.06));
            _provider.UsageReport.BilledDuration.Should().Be(TimeSpan.FromMilliseconds(1000));
            _provider.UsageReport.MaxDuration.Should().Be(TimeSpan.FromSeconds(10));
            _provider.UsageReport.UsedDurationPercent.Should().BeApproximately(0.1062F, 0.00001F);
            _provider.UsageReport.MaxMemory.Should().Be(128);
            _provider.UsageReport.UsedMemory.Should().Be(128);
            _provider.UsageReport.UsedMemoryPercent.Should().BeApproximately(1F, 0.0001F);
        }

        [Fact]
        public void ExecutionReport() {
            _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 5169911c-b198-496a-b235-ab77e8a93e97\tDuration: 0.58 ms\tBilled Duration: 100 ms Memory Size: 128 MB\tMax Memory Used: 20 MB\t", "1539238963679").Wait();
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

        private void CommonErrorReportAsserts() {
            Assert.NotNull(_provider.ErrorReport);
            Assert.Null(_provider.UsageReport);
            _provider.ErrorReport.ModuleName.Should().Be("ModuleName");
            _provider.ErrorReport.ModuleVersion.Should().Be("ModuleVersion");
            _provider.ErrorReport.DeploymentTier.Should().Be("DeploymentTier");
            _provider.ErrorReport.ModuleId.Should().Be("ModuleId");
            _provider.ErrorReport.FunctionName.Should().Be("FunctionName");
            _provider.ErrorReport.Platform.Should().Be("Platform");
            _provider.ErrorReport.Framework.Should().Be("Framework");
            _provider.ErrorReport.Language.Should().Be("Language");
            _provider.ErrorReport.GitSha.Should().Be("GitSha");
            _provider.ErrorReport.GitBranch.Should().Be("GitBranch");
        }
    }
}
