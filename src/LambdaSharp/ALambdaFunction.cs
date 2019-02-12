/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Amazon.SQS;
using LambdaSharp.ConfigSource;
using LambdaSharp.Reports;

namespace LambdaSharp {

    public abstract class ALambdaFunction : ILambdaLogger {

        //--- Types ---
        private class GitInfo {

            //--- Fields ---
            public string Branch { get; set; }
            public string SHA { get; set; }
        }

        //--- Class Fields ---
        protected static JsonSerializer JsonSerializer = new JsonSerializer();
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private static int Invocations;

        //--- Methods ---
        protected static T DeserializeJson<T>(Stream stream) =>  JsonSerializer.Deserialize<T>(stream);

        protected static T DeserializeJson<T>(string json) {
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) {
                return DeserializeJson<T>(stream);
            }
        }

        protected static string SerializeJson(object value) {
            using(var stream = new MemoryStream()) {
                JsonSerializer.Serialize(value, stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static void ParseModuleString(string moduleInfo, out string moduleOwner, out string moduleName, out string moduleVersion) {
            moduleOwner = null;
            moduleName = null;
            moduleVersion = null;
            if(moduleInfo == null) {
                return;
            }

            // extract module version
            var colon = moduleInfo.IndexOf(':');
            if(colon >= 0) {
                moduleVersion = moduleInfo.Substring(colon + 1);
            } else {
                colon = moduleInfo.Length;
            }

            // extract module owner and module name
            var dot = moduleInfo.IndexOf('.');
            if(dot >= 0) {
                moduleOwner = moduleInfo.Substring(0, dot);
                moduleName = moduleInfo.Substring(dot + 1, colon - dot - 1);
            }
        }

        //--- Fields ---
        private readonly Func<DateTime> _now;
        private readonly DateTime _started;
        private readonly IAmazonKeyManagementService _kmsClient;
        private readonly IAmazonSQS _sqsClient;
        private readonly ILambdaConfigSource _envSource;
        private string _deadLetterQueueUrl;
        private bool _initialized;
        private LambdaConfig _appConfig;

        //--- Constructors ---
        protected ALambdaFunction() : this(LambdaFunctionConfiguration.Instance) { }

        protected ALambdaFunction(LambdaFunctionConfiguration configuration) {

            // NOTE (2019-02-12, bjorg): set environment variable to unwrap aggregate exceptions automatically
            // see: https://www.reddit.com/r/aws/comments/98witj/we_are_the_aws_net_team_ask_the_experts/e98xinf/
            Environment.SetEnvironmentVariable("UNWRAP_AGGREGATE_EXCEPTIONS", "1");

            // initialize function fields from configuration
            _now = configuration.UtcNow ?? (() => DateTime.UtcNow);
            _kmsClient = configuration.KmsClient ?? throw new ArgumentNullException(nameof(configuration.KmsClient));
            _sqsClient = configuration.SqsClient ?? throw new ArgumentNullException(nameof(configuration.SqsClient));
            _envSource = configuration.EnvironmentSource ?? throw new ArgumentNullException(nameof(configuration.EnvironmentSource));
            _started = UtcNow;
        }

        //--- Properties ---
        protected DateTime UtcNow => _now();
        protected DateTime Started => _started;
        protected string ModuleOwner { get; private set; }
        protected string ModuleName { get; private set; }
        protected string ModuleId { get; private set; }
        protected string ModuleVersion { get; private set; }
        protected string FunctionId { get; private set; }
        protected string FunctionName { get; private set; }
        protected string DefaultSecretKey { get; private set; }
        protected string RequestId { get; private set; }
        protected ErrorReporter ErrorReporter { get; private set; }
        protected ILambdaLogger Logger => (ILambdaLogger)this;

        //--- Abstract Methods ---
        public abstract Task InitializeAsync(LambdaConfig config);
        public abstract Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context);

        //--- Methods ---
        public async Task<object> FunctionHandlerAsync(Stream stream, ILambdaContext context) {
            try {
                RequestId = context.AwsRequestId;

                // function startup
                Stopwatch.Restart();
                ++Invocations;
                var now = UtcNow;
                LogInfo($"function age: {now - Started:c}");
                LogInfo($"function invocation counter: {Invocations:N0}");

                // check if function needs to be initialized
                if(!_initialized) {
                    try {
                        LogInfo("initialize function configuration");
                        await InitializeAsync(_envSource, context);
                        LogInfo("start function initialization");
                        await InitializeAsync(_appConfig);
                        LogInfo("end function initialization");
                        _initialized = true;
                    } catch(Exception e) {
                        LogFatal(e, "failed during function initialization");
                        await InitializeFailedAsync(stream, context);
                        throw;
                    }
                }

                // process message stream
                object result;
                try {
                    result = await ProcessMessageStreamAsync(stream, context);
                } catch(ALambdaRetriableException e) {
                    LogErrorAsWarning(e);
                    throw;
                } catch(Exception e) {
                    LogError(e);
                    throw;
                }
                return result;
            } finally {
                LogInfo("invocation completed");
            }
        }

        protected virtual async Task InitializeAsync(ILambdaConfigSource envSource, ILambdaContext context) {

            // register X-RAY for AWS SDK clients (function tracing must be enabled in CloudFormation)
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            // read configuration from environment variables
            ModuleId = envSource.Read("MODULE_ID");
            var moduleInfo = envSource.Read("MODULE_INFO");
            ParseModuleString(moduleInfo, out var moduleOwner, out var moduleName, out var moduleVersion);
            ModuleOwner = moduleOwner;
            ModuleName = moduleName;
            ModuleVersion = moduleVersion;
            var deadLetterQueueArn = envSource.Read("DEADLETTERQUEUE");
            if(deadLetterQueueArn != null) {
                _deadLetterQueueUrl = AwsConverters.ConvertQueueArnToUrl(deadLetterQueueArn);
            }
            DefaultSecretKey = envSource.Read("DEFAULTSECRETKEY");
            FunctionId = envSource.Read("AWS_LAMBDA_FUNCTION_NAME");
            FunctionName = envSource.Read("LAMBDA_NAME");
            var framework = envSource.Read("LAMBDA_RUNTIME");
            LogInfo($"MODULE_ID = {ModuleId}");
            LogInfo($"MODULE_INFO = {moduleInfo}");
            LogInfo($"FUNCTION_NAME = {FunctionName}");
            LogInfo($"FUNCTION_ID = {FunctionId}");
            LogInfo($"DEADLETTERQUEUE = {_deadLetterQueueUrl ?? "NONE"}");
            LogInfo($"DEFAULTSECRETKEY = {DefaultSecretKey ?? "NONE"}");

            // read optional git-info file
            string gitSha = null;
            string gitBranch = null;
            if(File.Exists("git-info.json")) {
                var git = DeserializeJson<GitInfo>(File.ReadAllText("git-info.json"));
                gitSha = git.SHA;
                gitBranch = git.Branch;
                LogInfo($"GIT-SHA = {gitSha ?? "NONE"}");
                LogInfo($"GIT-BRANCH = {gitBranch ?? "NONE"}");
            }

            // convert environment variables to lambda parameters
            _appConfig = new LambdaConfig(new LambdaDictionarySource(await ReadParametersFromEnvironmentVariables()));

            // initialize error/warning reporter
            ErrorReporter = new ErrorReporter(
                ModuleId,
                $"{ModuleOwner}.{ModuleName}:{ModuleVersion}",
                FunctionId,
                FunctionName,
                framework,
                gitSha,
                gitBranch
            );
        }

        public virtual async Task InitializeFailedAsync(Stream stream, ILambdaContext context) { }

        protected virtual async Task RecordFailedMessageAsync(LambdaLogLevel level, string body, Exception exception) {
            if(!string.IsNullOrEmpty(_deadLetterQueueUrl)) {
                await _sqsClient.SendMessageAsync(_deadLetterQueueUrl, body);
            } else {
                LogWarn("dead letter queue not configured");
                throw new LambdaFunctionException("dead letter queue not configured", exception);
            }
        }

        protected async Task<string> DecryptSecretAsync(string secret, Dictionary<string, string> encryptionContext = null) {
            var plaintextStream = (await _kmsClient.DecryptAsync(new DecryptRequest {
                CiphertextBlob = new MemoryStream(Convert.FromBase64String(secret)),
                EncryptionContext = encryptionContext
            })).Plaintext;
            return Encoding.UTF8.GetString(plaintextStream.ToArray());
        }

        protected async Task<string> EncryptSecretAsync(string text, string encryptionKeyId = null, Dictionary<string, string> encryptionContext = null) {
            var response = await _kmsClient.EncryptAsync(new EncryptRequest {
                KeyId = encryptionKeyId ?? DefaultSecretKey,
                Plaintext = new MemoryStream(Encoding.UTF8.GetBytes(text)),
                EncryptionContext = encryptionContext
            });
            return Convert.ToBase64String(response.CiphertextBlob.ToArray());
        }

        private async Task<IDictionary<string, string>> ReadParametersFromEnvironmentVariables() {
            var parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach(DictionaryEntry envVar in Environment.GetEnvironmentVariables()) {
                var key = envVar.Key as string;
                var value = envVar.Value as string;
                if((key == null) || (value == null)) {
                    continue;
                }
                if(key.StartsWith("STR_", StringComparison.Ordinal)) {

                    // plain string value
                    parameters.Add(EnvToVarKey(key), value);
                } else if(key.StartsWith("SEC_", StringComparison.Ordinal) && (value.Length > 0)) {

                    // check for optional encrypt contexts
                    Dictionary<string, string> encryptionContext = null;
                    var parts = value.Split('|');
                    if(parts.Length > 1) {
                        encryptionContext = new Dictionary<string, string>();
                        for(var i = 1; i < parts.Length; ++i) {
                            var pair = parts[i].Split('=', 2);
                            if(pair.Length != 2) {
                                continue;
                            }
                            encryptionContext.Add(pair[0], pair[1]);
                        }
                    }

                    // decrypt secret value
                    parameters.Add(EnvToVarKey(key), await DecryptSecretAsync(parts[0], encryptionContext));
                }
            }
            return parameters;

            // local functions
            string EnvToVarKey(string key) => "/" + key.Substring(4).Replace('_', '/');
        }

        #region --- Logging ---
        protected virtual void RecordErrorReport(ErrorReport report) => LambdaLogger.Log(SerializeJson(report) + "\n");
        protected virtual void RecordException(Exception exception) => LambdaLogger.Log($"EXCEPTION: {exception}");

        protected void LogInfo(string format, params object[] args)
            => Logger.LogInfo(format, args);

        protected void LogWarn(string format, params object[] args)
            => Logger.LogWarn(format, args);

        protected void LogError(Exception exception)
            => Logger.LogError(exception);

        protected void LogError(Exception exception, string format, params object[] args)
            => Logger.LogError(exception, format, args);

        protected void LogErrorAsInfo(Exception exception)
            => Logger.LogErrorAsInfo(exception);

        protected void LogErrorAsInfo(Exception exception, string format, params object[] args)
            => Logger.LogErrorAsInfo(exception, format, args);

        protected void LogErrorAsWarning(Exception exception)
            => Logger.LogErrorAsWarning(exception);

        protected void LogErrorAsWarning(Exception exception, string format, params object[] args)
            => Logger.LogErrorAsWarning(exception, format, args);

        protected void LogFatal(Exception exception, string format, params object[] args)
            => Logger.LogFatal(exception, format, args);
        #endregion

        #region --- ILambdaLogger Members ---
        void ILambdaLogger.Log(LambdaLogLevel level, Exception exception, string format, params object[] args) {
            string message = ErrorReporter.FormatMessage(format, args) ?? exception?.Message;
            if((level >= LambdaLogLevel.WARNING) && (exception != null)) {

                // NOTE (0218-12-18, bjorg): `ErrorReporter` is null until the function has initialized
                if(ErrorReporter != null) {
                    try {
                        var report = ErrorReporter.CreateReport(RequestId, level.ToString(), exception, format, args);
                        RecordErrorReport(report);
                    } catch(Exception e) {
                        RecordException(e);
                        RecordException(exception);
                    }
                } else {
                    RecordException(exception);
                }
            } else {
                LambdaLogger.Log($"*** {level.ToString().ToUpperInvariant()}: {message} [{Stopwatch.Elapsed:c}]\n{exception?.ToString()}");
            }
        }
        #endregion
    }
}
