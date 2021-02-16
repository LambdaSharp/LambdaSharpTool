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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.XRay.Recorder.Handlers.System.Net;
using LambdaSharp.ConfigSource;
using LambdaSharp.Exceptions;
using LambdaSharp.Logging;
using LambdaSharp.Logging.ErrorReports;
using LambdaSharp.Logging.ErrorReports.Models;
using LambdaSharp.Logging.Events;
using LambdaSharp.Logging.Events.Models;
using LambdaSharp.Logging.Metrics;
using LambdaSharp.Records.ErrorReports;
using LambdaSharp.Serialization;

namespace LambdaSharp {

    /// <summary>
    /// <see cref="ALambdaFunction"/> is the abstract base class for all AWS Lambda functions. This class takes care of initializing the function from
    /// environment variables, before invoking <see cref="ALambdaFunction.ProcessMessageStreamAsync(Stream)"/>.
    /// </summary>
    public abstract class ALambdaFunction : ILambdaSharpLogger {

        //--- Types ---
        private class GitInfo {

            //--- Fields ---
            public string? Branch { get; set; }
            public string? SHA { get; set; }
        }

        /// <summary>
        /// The <see cref="FunctionInfo"/> struct exposes the function initialization settings.
        /// </summary>
        protected struct FunctionInfo : ILambdaSharpInfo {

            //--- Fields ---
            private ALambdaFunction _function;

            //--- Constructors ---
            internal FunctionInfo(ALambdaFunction function) => _function = function;

            //--- Properties ---

            /// <summary>
            /// The timestamp when the function started running. This property can be used to determine how long this function has been running.
            /// </summary>
            public DateTime Started => _function._started;

            /// <summary>
            /// The module full name, version, and origin.
            /// </summary>
            public string? ModuleInfo => _function._moduleInfo;

            /// <summary>
            /// The namespace of the module.
            /// </summary>
            public string? ModuleNamespace => _function._moduleNamespace;

            /// <summary>
            /// The name of the module.
            /// </summary>
            public string? ModuleName => _function._moduleName;

            /// <summary>
            /// The full-name of the module (ModuleNamespace.ModuleName)
            /// </summary>
            public string? ModuleFullName => (_function._moduleName != null) ? $"{ModuleNamespace}.{ModuleName}" : null;

            /// <summary>
            /// The CloudFormation stack name of the module deployment.
            /// </summary>
            public string? ModuleId => _function._moduleId;

            /// <summary>
            /// The version of the module.
            /// </summary>
            public string? ModuleVersion => _function._moduleVersion;

            /// <summary>
            /// The origin of the module.
            /// </summary>
            public string? ModuleOrigin => _function._moduleOrigin;

            /// <summary>
            /// The ID of the AWS Lambda function. This value corresponds to the Physical ID of the AWS Lambda function in the CloudFormation template.
            /// </summary>
            public string? FunctionId => _function._functionId;

            /// <summary>
            /// The name of the AWS Lambda function. This value corresponds to the Logical ID of the AWS Lambda function in the CloudFormation template.
            /// </summary>
            public string? FunctionName => _function._functionName;

            /// <summary>
            /// The framework used by the AWS Lambda function.
            /// </summary>
            public string? FunctionFramework => _function._functionFramework;

            /// <summary>
            /// The URL of the dead-letter queue for the AWS Lambda function. This value can be <c>null</c> if the module has no dead-letter queue.
            /// </summary>
            public string? DeadLetterQueueUrl => _function._deadLetterQueueUrl;

            /// <summary>
            /// The S3 bucket name where the module artifacts are located.
            /// </summary>
            public string? DeploymentBucketName => _function._deploymentBucketName;

            /// <summary>
            /// The deployment tier name.
            /// </summary>
            public string? DeploymentTier => _function._deploymentTier;

            /// <summary>
            /// The Git SHA from which the function was built from.
            /// </summary>
            public string? GitSha => _function._gitSha;

            /// <summary>
            /// The Git branch from which the function was built form.
            /// </summary>
            public string? GitBranch => _function._gitBranch;

            //--- ILambdaSharpInfo Members ---
            string? ILambdaSharpInfo.AppName => null;
            string? ILambdaSharpInfo.AppId => null;
            string? ILambdaSharpInfo.AppInstanceId => null;
        }

        /// <summary>
        /// The <see cref="FailedMessageOrigin"/> describes the origin of the failed message. The origin determines how
        /// the failed message will be retried.
        /// </summary>
        public enum FailedMessageOrigin {

            /// <summary>
            /// The message originated from the API Gateway service.
            /// </summary>
            ApiGateway,

            /// <summary>
            /// The message originated from the CloudFormation service.
            /// </summary>
            CloudFormation,

            /// <summary>
            /// The message originated from the Simple Notification Service service.
            /// </summary>
            SNS,

            /// <summary>
            /// The message originated from the Simple Queue Service service.
            /// </summary>
            SQS,

            /// <summary>
            /// The message originated from the CloudWatch service.
            /// </summary>
            CloudWatch
        }

        private class TerminateLambdaException : Exception { }

        //--- Class Fields ---
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private static int Invocations;
        private static readonly Regex ModuleKeyPattern = new Regex(@"^(?<Namespace>\w+)\.(?<Name>[\w\.]+)(:(?<Version>\*|[\w\.\-]+))?(@(?<Origin>[\w\-%]+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---

        private static void ParseModuleInfoString(string? moduleInfo, out string? moduleNamespace, out string? moduleName, out string? moduleVersion, out string? moduleOrigin) {
            moduleNamespace = null;
            moduleName = null;
            moduleVersion = null;
            moduleOrigin = null;
            if(moduleInfo != null) {

                // try parsing module reference
                var match = ModuleKeyPattern.Match(moduleInfo);
                if(match.Success) {
                    moduleNamespace = GetMatchValue("Namespace");
                    moduleName = GetMatchValue("Name");
                    moduleOrigin = GetMatchValue("Origin");
                    moduleVersion = GetMatchValue("Version");
                }

                // local function
                string? GetMatchValue(string groupName) {
                    var group = match.Groups[groupName];
                    return group.Success ? group.Value : null;
                }
            }
        }

        //--- Fields ---
        private DateTime _started;
        private string? _deploymentTier;
        private string? _deploymentBucketName;
        private string? _deadLetterQueueUrl;
        private string? _moduleInfo;
        private string? _moduleNamespace;
        private string? _moduleName;
        private string? _moduleId;
        private string? _moduleVersion;
        private string? _moduleOrigin;
        private string? _functionId;
        private string? _functionName;
        private string? _functionFramework;
        private string? _gitSha;
        private string? _gitBranch;
        private bool _initialized;
        private LambdaConfig? _config;
        private readonly Dictionary<Exception, LambdaLogLevel> _reportedExceptions = new Dictionary<Exception, LambdaLogLevel>();
        private List<Task> _pendingTasks = new List<Task>();
        private object _pendingTasksSyncRoot = new object();
        private ILambdaContext? _currentContext;
        private HttpClient? _httpClient;

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaFunction"/> instance using the default implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        protected ALambdaFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaFunction"/> instance using a custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <c>null</c>.
        /// </exception>
        protected ALambdaFunction(ILambdaFunctionDependencyProvider? provider) {
            Provider = provider ?? new LambdaFunctionDependencyProvider();
            ErrorReportGenerator = new LogErrorReportGenerator(Provider);

            // initialize function fields from configuration
            _started = UtcNow;
        }

        //--- Properties ---

        /// <summary>
        /// The <see cref="ILambdaFunctionDependencyProvider"/> instance used by the Lambda function to
        /// satisfy its required dependencies.
        /// </summary>
        /// <value>The <see cref="ILambdaFunctionDependencyProvider"/> instance.</value>
        protected ILambdaFunctionDependencyProvider Provider { get; }

        /// <summary>
        /// Retrieves the current date-time in UTC timezone.
        /// </summary>
        /// <value>Current date-time in UTC timezone.</value>
        protected DateTime UtcNow => Provider.UtcNow;

        /// <summary>
        /// Retrieve the Lambda function initialization settings.
        /// </summary>
        /// <value>The <see cref="FunctionInfo"/> value.</value>
        protected FunctionInfo Info => new FunctionInfo(this);

        /// <summary>
        /// Retrieve the <see cref="ErrorReportGenerator"/> instance used to generate error reports.
        /// </summary>
        /// <value>The <see cref="ErrorReportGenerator"/> instance.</value>
        protected ILambdaErrorReportGenerator ErrorReportGenerator { get; private set; }

        /// <summary>
        /// Retrieve the <see cref="ILambdaSharpLogger"/> instance.
        /// </summary>
        /// <value>The <see cref="ILambdaSharpLogger"/> instance.</value>
        protected ILambdaSharpLogger Logger => this;

        /// <summary>
        /// Retrieve the current <see cref="ILambdaContext"/> for the request.
        /// </summary>
        /// <remarks>
        /// This property is only set during the invocation of <see cref="ProcessMessageStreamAsync(Stream)"/>. Otherwise, it returns <c>null</c>.
        /// </remarks>
        /// <value>The <see cref="ILambdaContext"/> instance.</value>
        protected ILambdaContext CurrentContext => _currentContext ?? throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="HttpClient"/> property holds a <c>HttpClient</c> instance that is initialized with X-Ray support.
        /// </summary>
        /// <value>The <see cref="HttpClient"/> instance.</value>
        protected HttpClient HttpClient {
            get => _httpClient ?? throw new InvalidOperationException();
            set => _httpClient = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// The <see cref="DebugLoggingEnabled"/> property indicates if the requests received and responses emitted
        /// by this Lambda function should be shown in the CloudWatch logs. This can be useful to determine check for
        /// issues caused by inconsistencies in serialization or deserialization.
        /// </summary>
        /// <value>Boolean indicating if requests and responses are logged</value>
        protected virtual bool DebugLoggingEnabled => Provider.DebugLoggingEnabled;

        private LambdaConfig Config {
            get => _config ?? throw new InvalidOperationException();
            set => _config = value ?? throw new ArgumentNullException();
        }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="InitializeAsync(LambdaConfig)"/> method is invoke on first request. It is responsible for initializing the Lambda function
        /// using the provided <see cref="LambdaConfig"/> instance.
        /// </summary>
        /// <param name="config">The <see cref="LambdaConfig"/> instance to use.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task InitializeAsync(LambdaConfig config);

        /// <summary>
        /// The <see cref="ProcessMessageStreamAsync(Stream)"/> method is invoked for every received request. It is
        /// responsible for deserializing the stream and processing the received message. The return stream is
        /// sent as response.
        /// </summary>
        /// <param name="stream">The stream with the request payload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Stream> ProcessMessageStreamAsync(Stream stream);

        //--- Methods ---

        /// <summary>
        /// The <see cref="FunctionHandlerAsync(Stream, ILambdaContext)"/> method is the entry point for the Lambda function.
        /// It is responsible for initializing the Lambda function on first invocation, then invoking <see cref="ProcessMessageStreamAsync(Stream)"/>
        /// and handling any failures that occur.
        /// </summary>
        /// <param name="stream">The request stream.</param>
        /// <param name="context">
        /// The <see cref="ILambdaContext"/> instance associated with this request. The instance can be retrieved using the
        /// <see cref="CurrentContext"/> property.
        /// </param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaSystemTextJsonSerializer))]
        public async Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext context) {
            _currentContext = context;
            Exception? foregroundException = null;
            try {

                // function startup
                Stopwatch.Restart();
                ++Invocations;
                var now = UtcNow;
                LogInfo($"function age: {now - _started:c}");
                LogInfo($"function invocation counter: {Invocations:N0}");

                // check if function needs to be initialized
                if(!_initialized) {
                    try {
                        LogInfo("start function initialization");
                        await InitializePrologueAsync(Provider.ConfigSource);
                        LogInfo("initialize function configuration");
                        await InitializeAsync(Config);
                        LogInfo("end function initialization");
                        await InitializeEpilogueAsync();
                        LogInfo("initialization complete");
                        _initialized = true;
                    } catch(Exception e) {
                        LogFatal(e, "failed during function initialization");
                        await HandleFailedInitializationAsync(stream);
                        throw;
                    }
                }

                // check if the request stream should be logged for debugging purposes
                if(DebugLoggingEnabled) {

                    // convert request stream to memory stream, so we can read it twice
                    if(!(stream is MemoryStream memoryStream)) {
                        memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                    }

                    // log the request data verbatim
                    LogDebug($"request stream: {Encoding.UTF8.GetString(memoryStream.ToArray())}");

                    // replace request stream
                    stream = memoryStream;
                }

                // process message stream
                Stream result;
                try {
                    result = await ProcessMessageStreamAsync(stream);
                } catch(AggregateException e) {
                    foreach(var innerException in e.Flatten().InnerExceptions) {
                        if(innerException is LambdaRetriableException) {
                            LogErrorAsWarning(innerException);
                        } else {
                            LogError(innerException);
                        }
                    }
                    throw;
                } catch(LambdaRetriableException e) {
                    LogErrorAsWarning(e);
                    throw;
                } catch(Exception e) {
                    LogError(e);
                    throw;
                }

                // check if response stream should be logged for debugging purposes
                if(DebugLoggingEnabled) {

                    // convert response stream to memory stream, so we can read it twice
                    if(!(result is MemoryStream memoryStream)) {
                        memoryStream = new MemoryStream();
                        result.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                    }

                    // log the response data verbatim
                    LogDebug($"response stream: {Encoding.UTF8.GetString(memoryStream.ToArray())}");

                    // replace response stream
                    result = memoryStream;
                }
                return result;
            } catch(Exception e) {
                foregroundException = e;
                throw;
            } finally {
                List<Exception>? exceptions = null;

                // wait for pending background tasks before returning from lambda invocation
                while(true) {

                    // check if any pending tasks exist
                    List<Task> pendingTasks;
                    lock(_pendingTasksSyncRoot) {
                        if(_pendingTasks.Count == 0) {
                            break;
                        }

                        // replace pending tasks with fresh list
                        pendingTasks = _pendingTasks;
                        _pendingTasks = new List<Task>();
                    }

                    // wait for copied tasks to finish and report exceptions as appropriate
                    try {
                        await Task.WhenAll(pendingTasks);
                    } catch(AggregateException e) {
                        foreach(var innerException in e.Flatten().InnerExceptions) {

                            // capture background exception
                            if(exceptions == null) {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(innerException);

                            // report background exception
                            if(innerException is LambdaRetriableException) {
                                LogErrorAsWarning(innerException);
                            } else {
                                LogError(innerException);
                            }
                        }
                    }
                }

                // clear function state
                LogInfo($"invocation completed (reported errors: {_reportedExceptions.Count:N0})");
                _reportedExceptions.Clear();
                _currentContext = null;

                // NOTE (2019-06-20, bjorg): we can let the normal control flow exit the finally statement when no background exceptions occur;
                //  if a foreground exception has occurred, it will the thrown automatically at the end of the finally statement;
                //  however, if background exceptions occurred, we need to combine them with the foreground exception, so that all exceptions
                //  (forground and background) are reported on.

                // check if foreground exception needs to be added to background exceptions
                if(exceptions != null) {

                    // check if we need to add a foreground exception
                    if(foregroundException != null) {
                        exceptions.Add(foregroundException);
                    }

                    // check if an aggregate exception needs to be created
                    switch(exceptions.Count) {
                    case 1:

                        // rethrow the lonely exception
                        ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
                        break;
                    default:

                        // multiple exceptions occurred, rethrow them as an aggregate exception
                        throw new AggregateException(exceptions);
                    }
                }
            }
        }

        /// <summary>
        /// The <see cref="InitializePrologueAsync(ILambdaConfigSource)"/> method is invoked to prepare the Lambda function
        /// for initialization. This is the first of three methods that are invoked to initialize the Lambda function.
        /// </summary>
        /// <param name="envSource">The <see cref="ILambdaConfigSource"/> instance from which to read the configuration settings.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task InitializePrologueAsync(ILambdaConfigSource envSource) {

            // register X-RAY for AWS SDK clients (function tracing must be enabled in CloudFormation)
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();
            HttpClient = new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler()));

            // read Lambda runtime environment variables
            _functionId = envSource.Read("AWS_LAMBDA_FUNCTION_NAME");
            _functionName = envSource.Read("LAMBDA_NAME");
            _functionFramework = envSource.Read("LAMBDA_RUNTIME");

            // read LambdaSharp provided environment variables
            _moduleId = envSource.Read("MODULE_ID");
            _moduleInfo = envSource.Read("MODULE_INFO");
            ParseModuleInfoString(_moduleInfo, out _moduleNamespace, out _moduleName, out _moduleVersion, out _moduleOrigin);
            _deploymentTier = envSource.Read("DEPLOYMENT_TIER");
            _deploymentBucketName = envSource.Read("DEPLOYMENTBUCKETNAME");
            var deadLetterQueueArn = envSource.Read("DEADLETTERQUEUE");
            if(deadLetterQueueArn != null) {
                _deadLetterQueueUrl = AwsConverters.ConvertQueueArnToUrl(deadLetterQueueArn);
            }

            // log function start-up information
            var info = new Dictionary<string, string?> {
                ["MODULE_ID"] = _moduleId,
                ["MODULE_INFO"] = _moduleInfo,
                ["FUNCTION_NAME"] = _functionName,
                ["FUNCTION_ID"] = _functionId,
                ["DEPLOYMENT_TIER"] = _deploymentTier,
                ["DEPLOYMENTBUCKETNAME"] = _deploymentBucketName,
                ["DEADLETTERQUEUE"] = _deadLetterQueueUrl,
            };

            // read optional git-info file
            if(File.Exists("git-info.json")) {
                var git = LambdaSerializerSettings.LambdaSharpSerializer.Deserialize<GitInfo>(File.ReadAllText("git-info.json"));
                _gitSha = git.SHA;
                _gitBranch = git.Branch;
                info["GIT-SHA"] = _gitSha;
                info["GIT-BRANCH"] = _gitBranch;
            }
            if(info.Any()) {
                LogInfo("function startup information\n{0}", LambdaSerializerSettings.LambdaSharpSerializer.Serialize(info));
            }

            // initialize error/warning reporter
            ErrorReportGenerator = new LambdaErrorReportGenerator(
                _moduleId ?? "<MISSING>",
                _moduleInfo ?? "<MISSING>",
                platform: $"AWS Lambda [{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}] ({System.Environment.OSVersion})",
                _functionId ?? "<MISSING>",
                _functionName ?? "<MISSING>",
                appId: null,
                appName: null,
                _functionFramework ?? "<MISSING>",
                _gitSha,
                _gitBranch
            );

            // convert environment variables to lambda parameters
            Config = new LambdaConfig(new LambdaDictionarySource(await ReadParametersFromEnvironmentVariables()));
        }

        /// <summary>
        /// The <see cref="InitializeEpilogueAsync()"/> method is invoked to complet the initialization of the
        /// Lambda function. This is the last of three methods that are invoked to initialize the Lambda function.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task InitializeEpilogueAsync() { }

        /// <summary>
        /// The <see cref="HandleFailedInitializationAsync(Stream)"/> method is only invoked when an error occurs during the
        /// Lambda function initialization. This method can be overridden to provide custom behavior for how to handle such
        /// failures more gracefully.
        /// </summary>
        /// <remarks>
        /// Regardless of what this method does. Once completed, the Lambda function exits by rethrowing the original exception
        /// that occurred during initialization.
        /// </remarks>
        /// <param name="stream">The stream with the request payload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task HandleFailedInitializationAsync(Stream stream) { }

        /// <summary>
        /// The <see cref="RecordFailedMessageAsync(LambdaLogLevel, FailedMessageOrigin, string, Exception)"/> method is invoked when a permanent
        /// failure is detected during processing and the message should be sent to the dead-letter queue if possible. If no
        /// dead-letter queue is configured, the original exception is rethrown instead.
        /// </summary>
        /// <param name="level">The severity level of the failure. This should either be <see cref="LambdaLogLevel.ERROR"/> or <see cref="LambdaLogLevel.FATAL"/>.</param>
        /// <param name="origin">The origin of the failed message.</param>
        /// <param name="message">The failed message.</param>
        /// <param name="exception">The exception that was triggered by the failed message.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task RecordFailedMessageAsync(LambdaLogLevel level, FailedMessageOrigin origin, string message, Exception exception) {

            // check if a dead-letter queue is configured
            if(!string.IsNullOrEmpty(_deadLetterQueueUrl)) {
                await Provider.SendMessageToQueueAsync(_deadLetterQueueUrl, message, new[] {
                    new KeyValuePair<string, string>("LambdaSharp.FailedMessageOrigin", origin.ToString()),
                    new KeyValuePair<string, string>("LambdaSharp.FailedFunctionArn", _currentContext?.InvokedFunctionArn ?? "")
                });
            } else {

                // let the original exception propagate since there is no dead-letter queue
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw new ShouldNeverHappenException();
            }
        }

        /// <summary>
        /// The <see cref="DecryptSecretAsync(string, Dictionary{string, string},CancellationToken)"/> method decrypts a Base64-encoded string with an optional encryption context. The Lambda function
        /// requires permission to use the <c>kms:Decrypt</c> operation on the KMS key used to
        /// encrypt the original message.
        /// </summary>
        /// <param name="secret">Base64-encoded string of the encrypted value.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected async Task<string> DecryptSecretAsync(string secret, Dictionary<string, string>? encryptionContext = null, CancellationToken cancellationToken = default) {
            return Encoding.UTF8.GetString(await Provider.DecryptSecretAsync(
                Convert.FromBase64String(secret),
                encryptionContext,
                cancellationToken
            ));
        }

        /// <summary>
        /// The <see cref="EncryptSecretAsync(string, string, Dictionary{string, string},CancellationToken)"/> encrypts a sequence of bytes using the specified KMS key. The Lambda function requires
        /// permission to use the <c>kms:Encrypt</c> opeartion on the specified KMS key.
        /// </summary>
        /// <param name="text">The plaintext string to encrypt.</param>
        /// <param name="encryptionKeyId">The KMS key ID used encrypt the plaintext bytes.</param>
        /// <param name="encryptionContext">An optional encryption context. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected async Task<string> EncryptSecretAsync(string text, string encryptionKeyId, Dictionary<string, string>? encryptionContext = null, CancellationToken cancellationToken = default) {
            return Convert.ToBase64String(await Provider.EncryptSecretAsync(
                Encoding.UTF8.GetBytes(text),
                encryptionKeyId ?? throw new ArgumentNullException(encryptionKeyId),
                encryptionContext,
                cancellationToken
            ));
        }

        /// <summary>
        /// The <see cref="AddPendingTask(Task)"/> method adds the specified task to the list of pending tasks. The Lambda function waits until all
        /// pendings tasks have completed before responding to the active invocation.
        /// </summary>
        /// <param name="task">A task to wait for before responding to the active invocation.</param>
        protected void AddPendingTask(Task task) {
            lock(_pendingTasksSyncRoot) {
                _pendingTasks.Add(task);
            }
        }

        /// <summary>
        /// The <see cref="RunTask(Action, CancellationToken)"/> method queues the specified work for background execution. The Lambda function waits until all
        /// queued background work has completed before completing the active invocation.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">An optional cancellation token that can be used to cancel the work.</param>
        protected void RunTask(Action action, CancellationToken cancellationToken = default) => AddPendingTask(Task.Run(action, cancellationToken));

        /// <summary>
        /// The <see cref="RunTask(Func{Task}, CancellationToken)"/> method queues the specified work for background execution. The Lambda function waits until all
        /// queued background work has completed before completing the active invocation.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">An optional cancellation token that can be used to cancel the work.</param>
        protected void RunTask(Func<Task> function, CancellationToken cancellationToken = default)  => AddPendingTask(Task.Run(function, cancellationToken));

        /// <summary>
        /// The <see cref="ForceLambdaColdStart(System.String)"/> method causes the Lambda runtime to re-initialize as if a cold start had occurred. This methos is useful
        /// when the global environment is corrupted and only a restart can fix it.
        /// </summary>
        protected void ForceLambdaColdStart(string reason) {
            LogFatal(new ApplicationException(reason), "restart Lambda runtime");

            // NOTE (2020-10-13, bjorg): the following line will cause an uncatchable exception that force the Lambda runtime to start over
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Type).GetType()).ToString();
        }

        /// <summary>
        /// The <see cref="TerminateLambdaInstance(string)"/> method forces the Lambda instance to terminate and perform a cold start on next invocation.
        /// This method should only be used when the processing environment has become corrupted beyond repair.
        /// </summary>
        /// <param name="reason">Optional message shown as reason for terminating the Lambda instance.</param>
        protected void TerminateLambdaInstance(string? reason = null) {
            var message = (reason != null)
                ? $"Lambda instance was intentionally terminated (reason: {reason})"
                : $"Lambda instance was intentionally terminated (no reason provided)";
            LogFatal(exception: new TerminateLambdaException(), message);
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Type).GetType()).ToString();
        }

        private async Task<IDictionary<string, string>> ReadParametersFromEnvironmentVariables() {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach(DictionaryEntry? envVar in Environment.GetEnvironmentVariables()) {
                if(envVar == null) {
                    continue;
                }
                var key = envVar.Value.Key as string;
                var value = envVar.Value.Value as string;
                if((key == null) || (value == null)) {
                    continue;
                }
                if(key.StartsWith("STR_", StringComparison.Ordinal)) {

                    // plain string value
                    parameters.Add(EnvToVarKey(key), value);
                } else if(key.StartsWith("SEC_", StringComparison.Ordinal) && (value.Length > 0)) {

                    // check for optional encrypt contexts
                    Dictionary<string, string>? encryptionContext = null;
                    var parts = value.Split('|');
                    if(parts.Length > 1) {
                        encryptionContext = new Dictionary<string, string>();
                        for(var i = 1; i < parts.Length; ++i) {
                            var pair = parts[i].Split(new[] { '=' }, 2);
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

        /// <summary>
        /// The <see cref="RecordErrorReport(LambdaErrorReport)"/> method is invoked record errors for later reporting.
        /// </summary>
        /// <param name="report">The <see cref="LambdaErrorReport"/> to record.</param>
        protected virtual void RecordErrorReport(LambdaErrorReport report) => Provider.Log(LambdaSerializerSettings.LambdaSharpSerializer.Serialize(report) + "\n");

        /// <summary>
        /// The <see cref="RecordException(Exception)"/> method is only invoked when Lambda function <see cref="ErrorReportGenerator"/> instance
        /// has not yet been initialized of if an exception occurred while invoking <see cref="RecordErrorReport(LambdaErrorReport)"/>.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        protected virtual void RecordException(Exception exception) => Provider.Log($"EXCEPTION: {exception}\n");

        /// <summary>
        /// Log a debugging message. This message will only appear in the log when debug logging is enabled and will not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        protected void LogDebug(string format, params object[] arguments) => Logger.LogDebug(format, arguments);

        /// <summary>
        /// Log an informational message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        protected void LogInfo(string format, params object[] arguments) => Logger.LogInfo(format, arguments);

        /// <summary>
        /// Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="format">The message format string. If not arguments are supplied, the message format string will be printed as a plain string.</param>
        /// <param name="arguments">Optional arguments for the message string.</param>
        protected void LogWarn(string format, params object[] arguments) => Logger.LogWarn(format, arguments);

        /// <summary>
        /// Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        protected void LogError(Exception exception) => Logger.LogError(exception);

        /// <summary>
        /// Log an exception with a custom message as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogError(Exception exception, string format, params object[] arguments) => Logger.LogError(exception, format, arguments);

        /// <summary>
        /// Log an exception as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/> or <see cref="LogErrorAsWarning(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        protected void LogErrorAsInfo(Exception exception) => Logger.LogErrorAsInfo(exception);

        /// <summary>
        /// Log an exception with a custom message as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/> or <see cref="LogErrorAsWarning(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogErrorAsInfo(Exception exception, string format, params object[] arguments) => Logger.LogErrorAsInfo(exception, format, arguments);

        /// <summary>
        /// Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        protected void LogErrorAsWarning(Exception exception) => Logger.LogErrorAsWarning(exception);

        /// <summary>
        /// Log an exception with a custom message as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <remarks>
        /// Only use this method when the exception has no operational impact.
        /// Otherwise, either use <see cref="LogError(Exception)"/>.
        /// </remarks>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogErrorAsWarning(Exception exception, string format, params object[] arguments) => Logger.LogErrorAsWarning(exception, format, arguments);

        /// <summary>
        /// Log an exception with a custom message as a fatal error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
        /// </summary>
        /// <param name="exception">The exception to log. The exception is logged with its message, stacktrace, and any nested exceptions.</param>
        /// <param name="format">Optional message to use instead of <c>Exception.Message</c>. This parameter can be <c>null</c>.</param>
        /// <param name="arguments">Optional arguments for the <c>format</c> parameter.</param>
        protected void LogFatal(Exception exception, string format, params object[] arguments) => Logger.LogFatal(exception, format, arguments);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        protected void LogMetric(
            string name,
            double value,
            LambdaMetricUnit unit
        ) => Logger.LogMetric(name, value, unit);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="unit">Metric unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        protected void LogMetric(
            string name,
            double value,
            LambdaMetricUnit unit,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) => Logger.LogMetric(name, value, unit, dimensionNames, dimensionValues);

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        protected void LogMetric(IEnumerable<LambdaMetric> metrics) => LogMetric(metrics, Array.Empty<string>(), new Dictionary<string, string>());

        /// <summary>
        /// Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.
        /// </summary>
        /// <param name="metrics">Enumeration of metrics, including their name, value, and unit.</param>
        /// <param name="dimensionNames">Metric dimensions as comma-separated list (e.g. [ "A", "A,B" ]).</param>
        /// <param name="dimensionValues">Dictionary of dimesion name-value pairs.</param>
        protected virtual void LogMetric(
            IEnumerable<LambdaMetric> metrics,
            IEnumerable<string> dimensionNames,
            Dictionary<string, string> dimensionValues
        ) => Logger.LogMetric(metrics, dimensionNames, dimensionValues);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge. The 'detail-type' property is set to the full type name of the detail value.
        /// </summary>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        protected void LogEvent<T>(T detail, IEnumerable<string>? resources = null)
            => Logger.LogEvent($"{Info.ModuleFullName}::{Info.FunctionName}", detail, resources);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge. The 'detail-type' property is set to the full type name of the detail value.
        /// </summary>
        /// <param name="source">Name of the event source.</param>
        /// <param name="detail">Data-structure to serialize as a JSON string. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        protected void LogEvent<T>(string source, T detail, IEnumerable<string>? resources = null)
            => Logger.LogEvent(source, detail, resources);

        /// <summary>
        /// Send a CloudWatch event with optional event details and resources it applies to. This event is forwarded to the configured EventBridge.
        /// </summary>
        /// <param name="source">Name of the event source.</param>
        /// <param name="detailType">Free-form string used to decide what fields to expect in the event detail.</param>
        /// <param name="detail">Data-structure to serialize as a JSON string using <see cref="System.Text.Json.JsonSerializer"/>. If value is already a <code>string</code>, it is sent as-is. There is no other schema imposed. The data-structure may contain fields and nested subobjects.</param>
        /// <param name="resources">Optional AWS or custom resources, identified by unique identifier (e.g. ARN), which the event primarily concerns. Any number, including zero, may be present.</param>
        protected void LogEvent<T>(string source, string detailType, T detail, IEnumerable<string>? resources = null)
            => Logger.LogEvent(source, detailType, detail, resources);
        #endregion

        #region --- ILambdaLogLevelLogger Members ---
        bool ILambdaSharpLogger.DebugLoggingEnabled => DebugLoggingEnabled;
        ILambdaSharpInfo ILambdaSharpLogger.Info => Info;

        void ILambdaSharpLogger.Log(LambdaLogLevel level, Exception exception, string format, params object[] arguments) {
            if(level >= LambdaLogLevel.NONE) {
                return;
            }
            string message = LambdaErrorReportGenerator.FormatMessage(format, arguments) ?? exception?.Message ?? "Missing Exception Message";
            if((level >= LambdaLogLevel.WARNING) && (exception != null)) {

                // avoid reporting the same error multiple times as it works its way up the stack
                if(_reportedExceptions.TryGetValue(exception, out var previousLogLevel) && (previousLogLevel >= level)) {
                    return;
                }
                _reportedExceptions[exception] = level;

                // abort messages are printed, but not reported since they are not logic errors
                if(exception is LambdaAbortException) {
                    Provider.Log($"*** ABORT: {message} [{Stopwatch.Elapsed:c}]\n{PrintException()}");
                    return;
                }
                try {
                    var report = ErrorReportGenerator.CreateReport(_currentContext?.AwsRequestId ?? "<NONE>", level.ToString(), exception, format, arguments);
                    if(report != null) {
                        RecordErrorReport(report);
                    }
                } catch(Exception e) {
                    RecordException(e);
                    RecordException(exception);
                }
            } else if(message != null) {
                Provider.Log($"*** {level.ToString().ToUpperInvariant()}: {message} [{Stopwatch.Elapsed:c}]\n{PrintException()}");
            }

            // record metrics on warnings, errors, and fatal errors being logged
            switch(level) {
            case LambdaLogLevel.WARNING:
                LogMetric("LogWarning.Count", 1, LambdaMetricUnit.Count);
                break;
            case LambdaLogLevel.ERROR:
                LogMetric("LogError.Count", 1, LambdaMetricUnit.Count);
                break;
            case LambdaLogLevel.FATAL:
                LogMetric("LogFatal.Count", 1, LambdaMetricUnit.Count);
                break;
            default:

                // nothing to do
                break;
            }

            // local functions
            string PrintException() => (exception != null) ? exception.ToString() + "\n" : "";
        }

        void ILambdaSharpLogger.LogRecord(ALambdaLogRecord record) {
            Provider.Log(LambdaSerializerSettings.LambdaSharpSerializer.Serialize<object>(record ?? throw new ArgumentNullException(nameof(record))) + "\n");

            // emit events
            if(record is LambdaEventRecord eventRecord) {
                AddPendingTask(Provider.SendEventAsync(
                    DateTimeOffset.UtcNow,
                    eventRecord.EventBus,
                    eventRecord.Source,
                    eventRecord.DetailType,
                    eventRecord.Detail,
                    eventRecord.Resources
                ));
            }
        }
        #endregion
    }
}
