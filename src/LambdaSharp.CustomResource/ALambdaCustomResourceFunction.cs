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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using LambdaSharp.CustomResource.Internal;
using LambdaSharp.Exceptions;
using LambdaSharp.Logging;
using LambdaSharp.Serialization;

namespace LambdaSharp.CustomResource {

    /// <summary>
    /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}"/> is the abstract base class for
    /// handling custom resources in <a href="https://aws.amazon.com/cloudformation/">AWS CloudFormation</a>. This class takes
    /// care of handling the communication protocol with the AWS CloudFormation service. Depending on the requested operation, the base class invokes either
    /// <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessCreateResourceAsync(Request{TRequestProperties},CancellationToken)"/>,
    /// <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessUpdateResourceAsync(Request{TRequestProperties},CancellationToken)"/>,
    /// or <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessDeleteResourceAsync(Request{TRequestProperties},CancellationToken)"/>.
    /// In case of failure, the AWS CloudFormation service is automatically notified to avoid prolonged timeout errors during a
    /// CloudFormation stack operation.
    /// </summary>
    /// <typeparam name="TProperties">The request properties for the custom resource.</typeparam>
    /// <typeparam name="TAttributes">The response attributes for the custom resource.</typeparam>
    public abstract class ALambdaCustomResourceFunction<TProperties, TAttributes> : ALambdaFunction
        where TProperties : class
        where TAttributes : class
    {

        //--- Constants ---
        private const int MAX_SEND_ATTEMPTS = 3;

        //--- Types ---
        private class CustomResourceAbortException : ALambdaException {

            //--- Constructors ---
            public CustomResourceAbortException(string format, params object[] args) : base(format, args) { }
        }

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaCustomResourceFunction{TProperties,TAttributes}"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        protected ALambdaCustomResourceFunction(ILambdaJsonSerializer serializer) : this(serializer, provider: null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaCustomResourceFunction{TProperties,TAttributes}"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="serializer">Custom implementation of <see cref="ILambdaJsonSerializer"/>.</param>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaCustomResourceFunction(ILambdaJsonSerializer serializer, ILambdaFunctionDependencyProvider? provider) : base(provider) {
            LambdaSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        //--- Properties ---

        /// <summary>
        /// An instance of <see cref="ILambdaJsonSerializer"/> used for serializing/deserializing JSON data.
        /// </summary>
        /// <value>The <see cref="ILambdaJsonSerializer"/> instance.</value>
        protected ILambdaJsonSerializer LambdaSerializer { get; }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessCreateResourceAsync(Request{TRequestProperties},CancellationToken)"/> method is invoked
        /// when AWS CloudFormation attempts to create a custom resource.
        /// </summary>
        /// <param name="request">The CloudFormation request instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Response<TAttributes>> ProcessCreateResourceAsync(Request<TProperties> request, CancellationToken cancellationToken);

        /// <summary>
        /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessUpdateResourceAsync(Request{TRequestProperties},CancellationToken)"/> method is invoked
        /// when AWS CloudFormation attempts to update a custom resource.
        /// </summary>
        /// <param name="request">The CloudFormation request instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Response<TAttributes>> ProcessUpdateResourceAsync(Request<TProperties> request, CancellationToken cancellationToken);

        /// <summary>
        /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessDeleteResourceAsync(Request{TRequestProperties},CancellationToken)"/> method is invoked
        /// when AWS CloudFormation attempts to delete a custom resource.
        /// </summary>
        /// <param name="request">The CloudFormation request instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Response<TAttributes>> ProcessDeleteResourceAsync(Request<TProperties> request, CancellationToken cancellationToken);

        //--- Methods ---

        /// <summary>
        /// The <see cref="ProcessMessageStreamAsync(Stream)"/> method handles the communication protocol with the AWS CloudFormation service.
        /// </summary>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        /// <param name="stream">The stream with the request payload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public override sealed async Task<Stream> ProcessMessageStreamAsync(Stream stream) {
            var rawRequest = DeserializeStream(stream);

            // process message
            CloudFormationResourceResponse<TAttributes> rawResponse;
            LogInfo("processing request");
            try {
                LogInfo($"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation received");
                var request = new Request<TProperties> {
                    RequestType = rawRequest.RequestType,
                    ResourceType = rawRequest.ResourceType,
                    StackId = rawRequest.StackId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId,
                    ResourceProperties = rawRequest.ResourceProperties,
                    OldResourceProperties = rawRequest.OldResourceProperties
                };

                // handle slack request
                Task<Response<TAttributes>> responseTask;
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                switch(request.RequestType) {
                case RequestType.Create:
                    responseTask = ProcessCreateResourceAsync(request, cancellationTokenSource.Token);
                    break;
                case RequestType.Update:
                    responseTask = ProcessUpdateResourceAsync(request, cancellationTokenSource.Token);
                    break;
                case RequestType.Delete:
                    responseTask = ProcessDeleteResourceAsync(request, cancellationTokenSource.Token);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.RequestType), request.RequestType, "unexpected request value");
                }

                // check if the custom resource logic completes before function times out
                var timeout = CurrentContext.RemainingTime - TimeSpan.FromSeconds(1.0);
                if(await Task.WhenAny(Task.Delay(timeout), responseTask) != responseTask) {

                    // cancel operation and wait 100ms before exiting
                    cancellationTokenSource.Cancel();
                    await Task.Delay(500);
                    throw new TimeoutException($"custom resource operation timed out");
                }

                // await completed task to trigger any contained exceptions
                await responseTask;
                LogInfo($"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation was successful");
                rawResponse = new CloudFormationResourceResponse<TAttributes> {
                    Status = CloudFormationResourceResponseStatus.SUCCESS,
                    Reason = "",
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = responseTask.Result.PhysicalResourceId ?? rawRequest.PhysicalResourceId,
                    NoEcho = responseTask.Result.NoEcho,
                    Data = responseTask.Result.Attributes
                };
            } catch(AggregateException aggregateException) when(
                (aggregateException.InnerExceptions.Count() == 1)
                && (aggregateException.InnerExceptions[0] is CustomResourceAbortException e)
            ) {
                LogInfo($"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation ABORTED [{{0}}]", e.Message);
                rawResponse = new CloudFormationResourceResponse<TAttributes> {
                    Status = CloudFormationResourceResponseStatus.FAILED,
                    Reason = e.Message,
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId ?? "no-physical-id"
                };
            } catch(CustomResourceAbortException e) {
                LogInfo($"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation ABORTED [{{0}}]", e.Message);
                rawResponse = new CloudFormationResourceResponse<TAttributes> {
                    Status = CloudFormationResourceResponseStatus.FAILED,
                    Reason = e.Message,
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId ?? "no-physical-id"
                };
            } catch(Exception e) {
                LogError(e, $"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation FAILED [{{0}}]", e.Message);
                rawResponse = new CloudFormationResourceResponse<TAttributes> {
                    Status = CloudFormationResourceResponseStatus.FAILED,
                    Reason = "internal error",
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId ?? "no-physical-id"
                };
            }

            // write response
            await WriteResponse(rawRequest, rawResponse);
            return rawResponse.Status.ToString().ToStream();
        }

        /// <summary>
        /// The <see cref="Abort(string)"/> stops the request processing with an error message.
        /// </summary>
        /// <remarks>
        /// This method never returns as the abort exception is thrown immediately. The <see cref="Exception"/> instance is shown as returned
        /// to make it easier to tell the compiler the control flow.
        /// </remarks>
        /// <param name="message">The response message.</param>
        /// <returns>Nothing. See remarks.</returns>
        /// <example>
        /// <code>
        /// throw Abort("value for property X is invalid");
        /// </code>
        /// </example>
        protected Exception Abort(string message) => throw new CustomResourceAbortException(message);

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
        protected override async Task HandleFailedInitializationAsync(Stream stream) {

            // NOTE (2018-12-12, bjorg): function initialization failed; attempt to notify
            //  the CloudFormation service of the failure.
            try {
                var rawRequest = DeserializeStream(stream);
                var rawResponse = new CloudFormationResourceResponse<TAttributes> {
                    Status = CloudFormationResourceResponseStatus.FAILED,
                    Reason = "custom resource initialization failed",
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId ?? "no-physical-id"
                };
                await WriteResponse(rawRequest, rawResponse);
            } catch { }
        }

        private CloudFormationResourceRequest<TProperties> DeserializeStream(Stream stream) {

            // read stream into memory
            LogInfo("reading message stream");
            string body;
            using(var reader = new StreamReader(stream)) {
                body = reader.ReadToEnd();
            }

            // check if custom resource request is wrapped in an SNS envelope
            LogInfo("deserializing request");
            bool hasRecords;
            using(var json = JsonDocument.Parse(body)) {
                hasRecords = json.RootElement.TryGetProperty("Records", out _);
            }

            // determine if the custom resource request is wrapped in an SNS message
            // or if it is a direct invocation by the CloudFormation service
            if(hasRecords) {

                // deserialize SNS event
                LogInfo("deserializing SNS event");
                var snsEvent = LambdaSerializerSettings.LambdaSharpSerializer.Deserialize<SNSEvent>(body);

                // extract message from SNS event
                LogInfo("deserializing message");
                var messageBody = snsEvent.Records.First().Sns.Message;

                // deserialize message into a cloudformation request
                return LambdaSerializer.Deserialize<CloudFormationResourceRequest<TProperties>>(messageBody);
            } else {

                // deserialize generic JSON into a cloudformation request
                return LambdaSerializer.Deserialize<CloudFormationResourceRequest<TProperties>>(body);
            }
        }

        private async Task WriteResponse(
            CloudFormationResourceRequest<TProperties> rawRequest,
            CloudFormationResourceResponse<TAttributes> rawResponse
        ) {
            Exception? exception = null;
            var backoff = TimeSpan.FromMilliseconds(100);

            // write response to pre-signed S3 URL
            for(var i = 0; i < MAX_SEND_ATTEMPTS; ++i) {
                try {
                    if(rawRequest.ResponseURL == null) {
                        throw new InvalidOperationException("ResponseURL is missing");
                    }
                    var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                        RequestUri = new Uri(rawRequest.ResponseURL),
                        Method = HttpMethod.Put,
                        Content = new ByteArrayContent(Encoding.UTF8.GetBytes(LambdaSerializer.Serialize(rawResponse)))
                    });
                    if(httpResponse.StatusCode != HttpStatusCode.OK) {
                        throw new LambdaCustomResourceException(
                            "PUT operation to pre-signed S3 URL failed with status code: {0} [{1} {2}] = {3}",
                            httpResponse.StatusCode,
                            rawRequest.RequestType,
                            rawRequest.ResourceType ?? "<MISSING>",
                            await httpResponse.Content.ReadAsStringAsync()
                        );
                    }
                    return;
                } catch(InvalidOperationException e) {
                    exception = e;
                    break;
                } catch(Exception e) {
                    exception = e;
                    LogErrorAsWarning(e, "writing response to pre-signed S3 URL failed");
                    await Task.Delay(backoff);
                    backoff = TimeSpan.FromSeconds(backoff.TotalSeconds * 2);
                }
            }
            if(exception == null) {
                exception = new ShouldNeverHappenException($"ALambdaCustomResourceFunction.WriteResponse failed w/o an explicit exception");
            }

            // max attempts have been reached; fail permanently and record the failed request for playback
            LogError(exception);
            await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.CloudFormation, LambdaSerializer.Serialize(rawRequest), exception);
        }
    }
}