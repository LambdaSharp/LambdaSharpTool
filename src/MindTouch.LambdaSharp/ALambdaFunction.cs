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
using MindTouch.LambdaSharp.ConfigSource;
using MindTouch.LambdaSharp.Reports;

namespace MindTouch.LambdaSharp {

    public abstract class ALambdaFunction {

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
            _now = configuration.UtcNow ?? (() => DateTime.UtcNow);
            _kmsClient = configuration.KmsClient ?? throw new ArgumentNullException(nameof(configuration.KmsClient));
            _sqsClient = configuration.SqsClient ?? throw new ArgumentNullException(nameof(configuration.SqsClient));
            _envSource = configuration.EnvironmentSource ?? throw new ArgumentNullException(nameof(configuration.EnvironmentSource));
            _started = UtcNow;
        }

        //--- Properties ---
        protected DateTime UtcNow => _now();
        protected DateTime Started => _started;
        protected string ModuleName { get; private set; }
        protected string ModuleId { get; private set; }
        protected string ModuleVersion { get; private set; }
        protected string FunctionId { get; private set; }
        protected string FunctionName { get; private set; }
        protected string DefaultSecretKey { get; private set; }
        protected string RequestId { get; private set; }
        protected ErrorReporter ErrorReporter { get; private set; }

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

            // read configuration from environment variables
            ModuleName = envSource.Read("MODULE_NAME");
            ModuleId = envSource.Read("MODULE_ID");
            ModuleVersion = envSource.Read("MODULE_VERSION");
            _deadLetterQueueUrl = AwsConverters.ConvertQueueArnToUrl(envSource.Read("DEADLETTERQUEUE"));
            DefaultSecretKey = envSource.Read("DEFAULTSECRETKEY");
            FunctionId = AwsConverters.ConvertFunctionArnToName(context.InvokedFunctionArn);
            FunctionName = envSource.Read("LAMBDA_NAME");
            var framework = envSource.Read("LAMBDA_RUNTIME");
            LogInfo($"MODULE_NAME = {ModuleName}");
            LogInfo($"MODULE_VERSION = {ModuleVersion}");
            LogInfo($"MODULE_ID = {ModuleId}");
            LogInfo($"FUNCTION_NAME = {FunctionName}");
            LogInfo($"FUNCTION_ID = {FunctionId}");
            LogInfo($"DEADLETTERQUEUE = {_deadLetterQueueUrl ?? "NONE"}");
            LogInfo($"DEFAULTSECRETKEY = {DefaultSecretKey ?? "NONE"}");

            // read optional git-sha file
            var gitsha = File.Exists("gitsha.txt") ? File.ReadAllText("gitsha.txt") : null;
            LogInfo($"GITSHA = {gitsha ?? "NONE"}");

            // convert environment variables to lambda parameters
            _appConfig = new LambdaConfig(new LambdaDictionarySource(await ReadParametersFromEnvironmentVariables()));

            // initialize error/warning reporter
            ErrorReporter = new ErrorReporter(
                ModuleId,
                ModuleName,
                ModuleVersion,
                FunctionId,
                FunctionName,
                framework,
                gitsha,
                gitBranch: null
            );
        }

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
                            encryptionContext.Add(
                                Uri.UnescapeDataString(pair[0]),
                                Uri.UnescapeDataString(pair[1])
                            );
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

        #region *** Logging ***
        protected void LogInfo(string format, params object[] args)
            => Log(LambdaLogLevel.INFO, exception: null, format: format, args: args);

        protected void LogWarn(string format, params object[] args)
            => Log(LambdaLogLevel.WARNING, exception: null, format: format, args: args);

        protected void LogError(Exception exception)
            => Log(LambdaLogLevel.ERROR, exception, exception.Message, new object[0]);

        protected void LogError(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.ERROR, exception, format, args);

        protected void LogErrorAsInfo(Exception exception)
            => Log(LambdaLogLevel.INFO, exception, exception.Message, new object[0]);

        protected void LogErrorAsInfo(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.INFO, exception, format, args);

        protected void LogErrorAsWarning(Exception exception)
            => Log(LambdaLogLevel.WARNING, exception, exception.Message, new object[0]);

        protected void LogErrorAsWarning(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.WARNING, exception, format, args);

        protected void LogFatal(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.FATAL, exception, format, args);

        private void Log(LambdaLogLevel level, string message, string extra)
            => LambdaLogger.Log($"*** {level.ToString().ToUpperInvariant()}: {message} [{Stopwatch.Elapsed:c}]\n{extra}");

        private void Log(LambdaLogLevel level, Exception exception, string format, params object[] args) {
            string message = ErrorReporter.FormatMessage(format, args) ?? exception?.Message;
            if((level >= LambdaLogLevel.WARNING) && (exception != null)) {
                if(ErrorReporter != null) {
                    try {
                        var report = ErrorReporter.CreateReport(RequestId, level.ToString(), exception, format, args);
                        LambdaLogger.Log(SerializeJson(report) + "\n");
                    } catch(Exception e) {
                        LambdaLogger.Log($"EXCEPTION: {e}");
                        LambdaLogger.Log($"EXCEPTION: {exception}");
                    }
                } else {
                    LambdaLogger.Log($"EXCEPTION: {exception}");
                }
            } else {
                Log(level, $"{message}", exception?.ToString());
            }
        }
        #endregion
    }
}
