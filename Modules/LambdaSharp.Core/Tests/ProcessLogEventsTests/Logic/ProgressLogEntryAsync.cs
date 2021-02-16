/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LambdaSharp.Core.LoggingStreamAnalyzerFunction;
using LambdaSharp.Core.Registrations;
using LambdaSharp.Logging.ErrorReports.Models;
using LambdaSharp.Logging.Events.Models;
using LambdaSharp.Logging.Metrics.Models;
using Xunit;
using Xunit.Abstractions;

namespace LambdaSharp.Core.ProcessLogEventsFunction.Tests {

    public class ProgressLogEntryAsync {

        //--- Types ---
        private class MockDependencyProvider : ILogicDependencyProvider {

            //--- Fields ---
            public LambdaErrorReport ErrorReport;
            public LambdaUsageRecord UsageReport;
            private ITestOutputHelper _output;

            //--- Constructors ---
            public MockDependencyProvider(ITestOutputHelper output) {
                _output = output;
            }

            //--- Methods ---
            public Task SendErrorReportAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaErrorReport report) {
                ErrorReport.Should().BeNull();
                ErrorReport = report;
                return Task.CompletedTask;
            }

            public Task SendUsageReportAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaUsageRecord report) {
                UsageReport.Should().BeNull();
                UsageReport = report;
                return Task.CompletedTask;
            }

            public Task SendEventAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaEventRecord record) {
                return Task.CompletedTask;
            }

            public Task SendMetricsAsync(OwnerMetaData owner, DateTimeOffset timestamp, LambdaMetricsRecord record) {
                return Task.CompletedTask;
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
                ModuleInfo = "Test.Module:1.0@origin",
                Module = "Test.Module",
                ModuleId = "ModuleId",
                FunctionId = "ModuleName-FunctionName-NT5EUXTNTXXD",
                FunctionName = "FunctionName",
                FunctionLogGroupName = "/aws/lambda/MyTestFunction",
                FunctionPlatform = "Platform",
                FunctionFramework = "Framework",
                FunctionLanguage = "Language",
                FunctionMaxDuration = TimeSpan.FromMilliseconds(10_000),
                FunctionMaxMemory = 128
            };
        }

        //--- Methods ---
        [Fact]
        public void StartRequestLogEntry() {
            _logic.ProgressLogEntryAsync(_owner, "START RequestId: 0b4609c1-b2f5-4a3f-8fe9-b8bb5e32b589 Version: $LATEST\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            NoErrorsReportedAssert();
        }

        [Fact]
        public void EndRequestLogEntry() {
            _logic.ProgressLogEntryAsync(_owner, "END RequestId: 0b4609c1-b2f5-4a3f-8fe9-b8bb5e32b589\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            NoErrorsReportedAssert();
        }

        [Fact]
        public void MetricsLogEntry() {
            _logic.ProgressLogEntryAsync(_owner, "{\"_aws\":{\"Timestamp\":1592272360267,\"CloudWatchMetrics\":[{\"Namespace\":\"Module:Demo.TwitterNotifier\",\"Dimensions\":[[\"Stack\"],[\"Stack\",\"Function\"]],\"Metrics\":[{\"Name\":\"MessageSuccess.Count\",\"Unit\":\"Count\"},{\"Name\":\"MessageSuccess.Latency\",\"Unit\":\"Milliseconds\"},{\"Name\":\"MessageSuccess.Lifespan\",\"Unit\":\"Seconds\"}]}]},\"Type\":\"LambdaMetrics\",\"Version\":\"2020-04-16\",\"MessageSuccess.Count\":1.0,\"MessageSuccess.Latency\":3539.9942,\"MessageSuccess.Lifespan\":6.6436802,\"Stack\":\"SteveBv7-Demo-TwitterNotifier\",\"Function\":\"NotifyFunction\",\"GitSha\":\"629552b9cd388811b96e7427d16f80ecec735f8d\",\"GitBranch\":\"WIP-v0.7\"}\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            NoErrorsReportedAssert();
        }

        #region --- Application Error ---
        [Fact]
        public void LambdaSharpJsonLogEntry() {
            _logic.ProgressLogEntryAsync(_owner, "{\"Type\":\"LambdaError\",\"Version\":\"2018-09-27\",\"ModuleInfo\":\"Test.Module:1.0@origin\",\"Module\":\"Test.Module\",\"ModuleVersion\":\"ModuleVersion\",\"ModuleId\":\"ModuleId\",\"FunctionId\":\"ModuleName-FunctionName-NT5EUXTNTXXD\",\"FunctionName\":\"FunctionName\",\"Platform\":\"Platform\",\"Framework\":\"Framework\",\"Language\":\"Language\",\"GitSha\":\"GitSha\",\"GitBranch\":\"GitBranch\",\"RequestId\":\"RequestId\",\"Level\":\"Level\",\"Fingerprint\":\"Fingerprint\",\"Timestamp\":1539238963879,\"Message\":\"failed during message stream processing\"}", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("failed during message stream processing");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963879L);
            _provider.ErrorReport.RequestId.Should().Be("RequestId");
        }

        [Fact]
        public void LambdaSharpLegacyJsonLogEntry() {
            _logic.ProgressLogEntryAsync(_owner, "{\"Source\":\"LambdaError\",\"Version\":\"2018-09-27\",\"Module\":\"Test.Module:1.0@origin\",\"ModuleName\":\"ModuleName\",\"ModuleVersion\":\"ModuleVersion\",\"ModuleId\":\"ModuleId\",\"FunctionId\":\"ModuleName-FunctionName-NT5EUXTNTXXD\",\"FunctionName\":\"FunctionName\",\"Platform\":\"Platform\",\"Framework\":\"Framework\",\"Language\":\"Language\",\"GitSha\":\"GitSha\",\"GitBranch\":\"GitBranch\",\"RequestId\":\"RequestId\",\"Level\":\"Level\",\"Fingerprint\":\"Fingerprint\",\"Timestamp\":1539238963879,\"Message\":\"failed during message stream processing\"}", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("failed during message stream processing");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963879L);
            _provider.ErrorReport.RequestId.Should().Be("RequestId");
        }

        [Fact]
        public void LambdaReturnException() {
            _logic.ProgressLogEntryAsync(_owner, "this exception was thrown on request: Exception\n   at BadModule.FailError.Function.ProcessMessageAsync(FunctionRequest request) in C:\\LambdaSharp\\LambdaSharpTool\\Tests\\BadModule\\FailError\\Function.cs:line 36\n   at LambdaSharp.ALambdaFunction`2.ProcessMessageStreamAsync(Stream stream)\n   at LambdaSharp.ALambdaFunction.FunctionHandlerAsync(Stream stream, ILambdaContext context) in C:\\LambdaSharp\\LambdaSharpTool\\src\\LambdaSharp\\ALambdaFunction.cs:line 398\n   at LambdaSharp.ALambdaFunction.FunctionHandlerAsync(Stream stream, ILambdaContext context) in C:\\LambdaSharp\\LambdaSharpTool\\src\\LambdaSharp\\ALambdaFunction.cs:line 487\n   at lambda_method(Closure , Stream , Stream , LambdaContextInternal )\n\n\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();

            // NOTE (2020-05-12, bjorg): this message is shown when an exception bubbles out of the Lambda function; since
            //  all exceptions are already logged as LambdaError records, this log entry is not needed.
            NoErrorsReportedAssert();
        }
        #endregion

        #region --- Out-of-Memory ---
        [Fact]
        public void OutOfMemory1() {
            _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6\tDuration: 1062.06 ms\tBilled Duration: 1000 ms \tMemory Size: 128 MB\tMax Memory Used: 128 MB", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            _provider.UsageReport.Should().NotBeNull();
            _provider.UsageReport.UsedDuration.Should().Be((float)TimeSpan.FromMilliseconds(1062.06).TotalSeconds);
            _provider.UsageReport.BilledDuration.Should().Be((float)TimeSpan.FromMilliseconds(1000).TotalSeconds);
            _provider.UsageReport.MaxDuration.Should().Be((float)TimeSpan.FromSeconds(10).TotalSeconds);
            _provider.UsageReport.UsedDurationPercent.Should().BeApproximately(0.1062F, 0.00001F);
            _provider.UsageReport.MaxMemory.Should().Be(128);
            _provider.UsageReport.UsedMemory.Should().Be(128);
            _provider.UsageReport.UsedMemoryPercent.Should().BeApproximately(1F, 0.0001F);
            _provider.UsageReport.InitDuration.Should().BeNull();
            CommonErrorReportAsserts(usageReportCheck: false);
            _provider.ErrorReport.Message.Should().Be("Lambda nearing execution limits (Memory 100.00%, Duration: 10.62%)");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("813a64e4-cd22-11e8-acad-d7f8fa4137e6");
        }

        [Fact]
        public void OutOfMemory2() {
            _logic.ProgressLogEntryAsync(_owner, "Exception of type 'System.OutOfMemoryException' was thrown.: OutOfMemoryException\n   at BadModule.FailOutOfMemory.Function.ProcessMessageAsync(FunctionRequest request) in C:\\LambdaSharp\\LambdaSharpTool\\Tests\\BadModule\\FailOutOfMemory\\Function.cs:line 36\n   at LambdaSharp.ALambdaFunction`2.ProcessMessageStreamAsync(Stream stream)\n   at LambdaSharp.ALambdaFunction.FunctionHandlerAsync(Stream stream, ILambdaContext context) in C:\\LambdaSharp\\LambdaSharpTool\\src\\LambdaSharp\\ALambdaFunction.cs:line 398\n   at LambdaSharp.ALambdaFunction.FunctionHandlerAsync(Stream stream, ILambdaContext context) in C:\\LambdaSharp\\LambdaSharpTool\\src\\LambdaSharp\\ALambdaFunction.cs:line 487\n   at lambda_method(Closure , Stream , Stream , LambdaContextInternal )\n\n\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();

            // NOTE (2020-05-12, bjorg): this message is shown when an exception bubbles out of the Lambda function; since
            //  all exceptions are already logged as LambdaError records, this log entry is not needed.
            NoErrorsReportedAssert();
        }
        #endregion

        #region --- Timeout ---
        [Fact]
        public void Timeout() {
            _logic.ProgressLogEntryAsync(_owner, "2018-10-11T07:00:40.906Z 546933ad-cd23-11e8-bb5d-7f3682cfa000 Task timed out after 15.02 seconds", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Lambda timed out after 15.02 seconds");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("546933ad-cd23-11e8-bb5d-7f3682cfa000");
        }
        #endregion

        #region --- Bad Entry Point ---
        [Fact]
        public void BadEntryPoint1() {
            _logic.ProgressLogEntryAsync(_owner, "12 May 2020 21:19:02,997 [WARN] (invoke@invoke.c:297 errno: Address family not supported by protocol) run_dotnet(dotnet_path, &args) failed\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();

            // NOTE (2020-05-12, bjorg): this message is shown when an exception occurs in the constructor or the function
            //  entry point could not be found; both errors are reported already by other log entries.
            NoErrorsReportedAssert();
        }

        [Fact]
        public void BadEntryPoint2() {
            _logic.ProgressLogEntryAsync(_owner, "Unknown application error occurred\n\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();

            // NOTE (2020-05-12, bjorg): this message is shown when an exception occurs in the constructor or the function
            //  entry point could not be found; both errors are reported already by other log entries.
            NoErrorsReportedAssert();
        }

        [Fact]
        public void BadEntryPoint3() {
            _logic.ProgressLogEntryAsync(_owner, "Unable to load type 'LambdaSharp.Core.ProcessLogEventsFunction.Function' from assembly 'ProcessLogEvents'.: LambdaException", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Unable to load type 'LambdaSharp.Core.ProcessLogEventsFunction.Function' from assembly 'ProcessLogEvents'.");
            _provider.ErrorReport.Traces.Should().NotBeNull();
            _provider.ErrorReport.Traces.Count().Should().Be(1);
            _provider.ErrorReport.Traces.ElementAt(0).Exception.Type.Should().Be("LambdaException");
            _provider.ErrorReport.Traces.ElementAt(0).Exception.Message.Should().Be("Unable to load type 'LambdaSharp.Core.ProcessLogEventsFunction.Function' from assembly 'ProcessLogEvents'.");
            _provider.ErrorReport.Traces.ElementAt(0).Frames.Should().BeNull();
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().BeNull();
        }
        #endregion

        #region --- Constructor Exception ---
        [Fact]
        public void ConstructorException1() {
            _logic.ProgressLogEntryAsync(_owner, "An exception was thrown when the constructor for type 'BadModule.FailConstructor.Function' was invoked. Check inner exception for more details.: LambdaException\n\n\n   at System.RuntimeTypeHandle.CreateInstance(RuntimeType type, Boolean publicOnly, Boolean wrapExceptions, Boolean& canBeCached, RuntimeMethodHandleInternal& ctor, Boolean& hasNoDefaultCtor)\n   at System.RuntimeType.CreateInstanceDefaultCtorSlow(Boolean publicOnly, Boolean wrapExceptions, Boolean fillCache)\n   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean skipCheckThis, Boolean fillCache, Boolean wrapExceptions)\n   at System.Activator.CreateInstance(Type type, Boolean nonPublic, Boolean wrapExceptions)\nthis exception was thrown in the constructor: Exception\n   at BadModule.FailConstructor.Function..ctor() in C:\\LambdaSharp\\LambdaSharpTool\\Tests\\BadModule\\FailConstructor\\Function.cs:line 33", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("An exception was thrown when the constructor for type 'BadModule.FailConstructor.Function' was invoked. Check inner exception for more details.");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().BeNull();

            // check exception traces
            _provider.ErrorReport.Traces.Should().NotBeNull();
            _provider.ErrorReport.Traces.Count().Should().Be(2);
            _provider.ErrorReport.Traces.ElementAt(0).Exception.Type.Should().Be("LambdaException");
            _provider.ErrorReport.Traces.ElementAt(0).Exception.Message.Should().Be("An exception was thrown when the constructor for type 'BadModule.FailConstructor.Function' was invoked. Check inner exception for more details.");
            _provider.ErrorReport.Traces.ElementAt(0).Frames.Should().NotBeNull();
            _provider.ErrorReport.Traces.ElementAt(0).Frames.Count().Should().Be(4);

            // check stack frames for first stack trace
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(0).MethodName.Should().Be("System.RuntimeTypeHandle.CreateInstance(RuntimeType type, Boolean publicOnly, Boolean wrapExceptions, Boolean& canBeCached, RuntimeMethodHandleInternal& ctor, Boolean& hasNoDefaultCtor)");
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(0).FileName.Should().BeNull();
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(0).LineNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(0).ColumnNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(1).MethodName.Should().Be("System.RuntimeType.CreateInstanceDefaultCtorSlow(Boolean publicOnly, Boolean wrapExceptions, Boolean fillCache)");
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(1).FileName.Should().BeNull();
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(1).LineNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(1).ColumnNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(2).MethodName.Should().Be("System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean skipCheckThis, Boolean fillCache, Boolean wrapExceptions)");
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(2).FileName.Should().BeNull();
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(2).LineNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(2).ColumnNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(3).MethodName.Should().Be("System.Activator.CreateInstance(Type type, Boolean nonPublic, Boolean wrapExceptions)");
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(3).FileName.Should().BeNull();
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(3).LineNumber.Should().Be(null);
            _provider.ErrorReport.Traces.ElementAt(0).Frames.ElementAt(3).ColumnNumber.Should().Be(null);

            // check stack frames for second stack trace
            _provider.ErrorReport.Traces.ElementAt(1).Exception.Type.Should().Be("Exception");
            _provider.ErrorReport.Traces.ElementAt(1).Exception.Message.Should().Be("this exception was thrown in the constructor");
            _provider.ErrorReport.Traces.ElementAt(1).Frames.Should().NotBeNull();
            _provider.ErrorReport.Traces.ElementAt(1).Frames.Count().Should().Be(1);
            _provider.ErrorReport.Traces.ElementAt(1).Frames.ElementAt(0).MethodName.Should().Be("BadModule.FailConstructor.Function..ctor()");
            _provider.ErrorReport.Traces.ElementAt(1).Frames.ElementAt(0).FileName.Should().Be(@"C:\LambdaSharp\LambdaSharpTool\Tests\BadModule\FailConstructor\Function.cs");
            _provider.ErrorReport.Traces.ElementAt(1).Frames.ElementAt(0).LineNumber.Should().Be(33);
            _provider.ErrorReport.Traces.ElementAt(1).Frames.ElementAt(0).ColumnNumber.Should().Be(null);
        }

        [Fact]
        public void ConstructorException2() {
            _logic.ProgressLogEntryAsync(_owner, "Unknown application error occurred\n\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();

            // NOTE (2020-05-12, bjorg): this message is shown when an exception occurs in the constructor or the function
            //  entry point could not be found; both errors are reported already by other log entries.
            NoErrorsReportedAssert();
        }
        #endregion

        #region --- Usage Report ---
        [Fact]
        public void UsageReport1() {
            _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 5169911c-b198-496a-b235-ab77e8a93e97\tDuration: 0.58 ms\tBilled Duration: 100 ms Memory Size: 128 MB\tMax Memory Used: 20 MB", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            _provider.UsageReport.Should().NotBeNull();
            _provider.ErrorReport.Should().BeNull();
            _provider.UsageReport.UsedDuration.Should().Be((float)TimeSpan.FromMilliseconds(0.58).TotalSeconds);
            _provider.UsageReport.BilledDuration.Should().Be((float)TimeSpan.FromMilliseconds(100).TotalSeconds);
            _provider.UsageReport.MaxDuration.Should().Be((float)TimeSpan.FromSeconds(10).TotalSeconds);
            _provider.UsageReport.UsedDurationPercent.Should().BeApproximately(0.000058F, 0.000001F);
            _provider.UsageReport.MaxMemory.Should().Be(128);
            _provider.UsageReport.UsedMemory.Should().Be(20);
            _provider.UsageReport.UsedMemoryPercent.Should().BeApproximately(0.15625F, 0.0001F);
        }

        [Fact]
        public void UsageReport2() {
            _logic.ProgressLogEntryAsync(_owner, "REPORT RequestId: 5169911c-b198-496a-b235-ab77e8a93e97\tDuration: 0.58 ms\tBilled Duration: 100 ms Memory Size: 128 MB\tMax Memory Used: 20 MB\tInit Duration: 419.31 ms", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            _provider.UsageReport.Should().NotBeNull();
            _provider.ErrorReport.Should().BeNull();
            _provider.UsageReport.UsedDuration.Should().Be((float)TimeSpan.FromMilliseconds(0.58).TotalSeconds);
            _provider.UsageReport.BilledDuration.Should().Be((float)TimeSpan.FromMilliseconds(100).TotalSeconds);
            _provider.UsageReport.MaxDuration.Should().Be((float)TimeSpan.FromSeconds(10).TotalSeconds);
            _provider.UsageReport.UsedDurationPercent.Should().BeApproximately(0.000058F, 0.000001F);
            _provider.UsageReport.MaxMemory.Should().Be(128);
            _provider.UsageReport.UsedMemory.Should().Be(20);
            _provider.UsageReport.UsedMemoryPercent.Should().BeApproximately(0.15625F, 0.0001F);
            _provider.UsageReport.InitDuration.Should().Be((float)TimeSpan.FromMilliseconds(419.31).TotalSeconds);
        }
        #endregion

        #region --- Misc ---
        [Fact]
        public void ProcessExitedBeforeCompletion() {
            _logic.ProgressLogEntryAsync(_owner, "RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6 Process exited before completing request", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Lambda exited before completing request");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("813a64e4-cd22-11e8-acad-d7f8fa4137e6");
        }

        [Fact]
        public void ExitWithoutReason() {
            _logic.ProgressLogEntryAsync(_owner, "RequestId: 813a64e4-cd22-11e8-acad-d7f8fa4137e6 Error: Runtime exited without providing a reason\nRuntime.ExitError", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("Runtime exited without providing a reason [Runtime.ExitError]");
            _provider.ErrorReport.Level.Should().Be("FATAL");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
            _provider.ErrorReport.RequestId.Should().Be("813a64e4-cd22-11e8-acad-d7f8fa4137e6");
        }

        [Fact]
        public void ResponseTooLong() {
            _logic.ProgressLogEntryAsync(_owner, "The Lambda function returned a response that is too long to serialize. The response size limit for a Lambda function is 6MB.: ArgumentException\n    at AWSLambda.Internal.Bootstrap.GenericSerializers.StreamSerializer.Serialize(Stream customerData, Stream outStream) in /opt/workspace/LambdaBYOLDotNetCore31/src/Bootstrap/Serializers/StreamSerializer.cs:line 48\n    at lambda_method(Closure , Stream , Stream , LambdaContextInternal )\n\n    at System.IO.UnmanagedMemoryStream.WriteCore(ReadOnlySpan`1 buffer)\n    at System.IO.UnmanagedMemoryStream.Write(Byte[] buffer, Int32 offset, Int32 count)\n    at System.IO.MemoryStream.CopyTo(Stream destination, Int32 bufferSize)\n", DateTimeOffset.FromUnixTimeMilliseconds(1539238963679L)).GetAwaiter().GetResult();
            CommonErrorReportAsserts();
            _provider.ErrorReport.Message.Should().Be("The Lambda function returned a response that is too long to serialize. The response size limit for a Lambda function is 6MB.");
            _provider.ErrorReport.Level.Should().Be("FATAL");
            _provider.ErrorReport.Timestamp.Should().Be(1539238963679);
        }
        #endregion

        private void CommonErrorReportAsserts(bool usageReportCheck = true) {
            _provider.ErrorReport.Should().NotBeNull();
            _provider.ErrorReport.ModuleInfo.Should().Be("Test.Module:1.0@origin");
            _provider.ErrorReport.Module.Should().Be("Test.Module");
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

        private void NoErrorsReportedAssert() {
            _provider.UsageReport.Should().BeNull();
            _provider.ErrorReport.Should().BeNull();
        }
    }
}
