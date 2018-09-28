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
        private Reporter _reporter;
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
        protected string DeploymentTier { get; private set; }
        private string _requestId;

        //--- Abstract Methods ---
        public abstract Task InitializeAsync(LambdaConfig config);
        public abstract Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context);

        //--- Methods ---
        public async Task<object> FunctionHandlerAsync(Stream stream, ILambdaContext context) {
            try {
                _requestId = context.AwsRequestId;

                // function startup
                Stopwatch.Restart();
                ++Invocations;
                var now = UtcNow;
                LogInfo($"function age: {now - Started:c}");
                LogInfo($"function invocation counter: {Invocations:N0}");

                // check if function needs to be initialized
                if(!_initialized) {
                    try {
                        LogInfo("start function initialization");
                        await InitializeAsync(_envSource, context);
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
                    LogErrorAsWarning(e, "failed during message stream processing");
                    throw;
                } catch(Exception e) {
                    LogError(e, "failed during message stream processing");
                    throw;
                }
                return result;
            } finally {
                LogInfo("invocation completed");
            }
        }

        protected virtual async Task InitializeAsync(ILambdaConfigSource envSource, ILambdaContext context) {

            // read configuration from environment variables
            DeploymentTier = envSource.Read("TIER");
            ModuleName = envSource.Read("MODULE");
            _deadLetterQueueUrl = envSource.Read("DEADLETTERQUEUE");
            var framework = envSource.Read("LAMBDARUNTIME");
            LogInfo($"TIER = {DeploymentTier}");
            LogInfo($"MODULE = {ModuleName}");
            LogInfo($"DEADLETTERQUEUE = {_deadLetterQueueUrl ?? "NONE"}");

            // read optional git-sha file
            var gitsha = File.Exists("gitsha.txt") ? File.ReadAllText("gitsha.txt") : null;
            LogInfo($"GITSHA = {gitsha ?? "NONE"}");

            // convert environment variables to lambda parameters
            _appConfig = new LambdaConfig(new LambdaDictionarySource(await ReadParametersFromEnvironmentVariables()));

            // initialize error/warning reporter
            _reporter = new Reporter(
                ModuleName,
                DeploymentTier,
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
                } else if(key.StartsWith("SEC_", StringComparison.Ordinal)) {

                    // secret with optional encryption context pairs
                    var parts = value.Split('|');
                    Dictionary<string, string> encryptionContext = null;
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
                    var plaintextStream = (await _kmsClient.DecryptAsync(new DecryptRequest {
                        CiphertextBlob = new MemoryStream(Convert.FromBase64String(value)),
                        EncryptionContext = encryptionContext
                    })).Plaintext;
                    parameters.Add(EnvToVarKey(key), Encoding.UTF8.GetString(plaintextStream.ToArray()));
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

        protected void LogError(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.ERROR, exception, format, args);

        protected void LogErrorAsInfo(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.INFO, exception, format, args);

        protected void LogErrorAsWarning(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.WARNING, exception, format, args);

        protected void LogFatal(Exception exception, string format, params object[] args)
            => Log(LambdaLogLevel.FATAL, exception, format, args);

        private void Log(LambdaLogLevel level, string message, string extra)
            => LambdaLogger.Log($"*** {level.ToString().ToUpperInvariant()}: {message} [{Stopwatch.Elapsed:c}]\n{extra}");

        private void Log(LambdaLogLevel level, Exception exception, string format, params object[] args) {
            string message = Reporter.FormatMessage(format, args);
            Log(level, $"{message}", exception?.ToString());
            if(level >= LambdaLogLevel.WARNING) {
                if(_reporter != null) {
                    try {
                        var report = _reporter.CreateReport(_requestId, level.ToString(), exception, format, args);
                        LambdaLogger.Log(SerializeJson(report) + "\n");
                    } catch(Exception e) {
                        LambdaLogger.Log($"EXCEPTION: {e}");
                        LambdaLogger.Log($"EXCEPTION: {exception}");
                    }
                } else {
                    LambdaLogger.Log($"EXCEPTION: {exception}");
                }
            }
        }
        #endregion
    }
}
