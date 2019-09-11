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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using LambdaSharp.CustomResource.Internal;
using LambdaSharp.Exceptions;
using LambdaSharp.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.CustomResource {

    /// <summary>
    /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}"/> is the abstract base class for
    /// handling custom resources in <a href="https://aws.amazon.com/cloudformation/">AWS CloudFormation</a>. This class takes
    /// care of handling the communication protocol with the AWS CloudFormation service. Depending on the requested operation, the base class invokes either
    /// <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessCreateResourceAsync(Request{TRequestProperties})"/>,
    /// <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessUpdateResourceAsync(Request{TRequestProperties})"/>,
    /// or <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessDeleteResourceAsync(Request{TRequestProperties})"/>.
    /// In case of failure, the AWS CloudFormation service is automatically notified to avoid prolonged timeout errors during a
    /// CloudFormation stack operation.
    /// </summary>
    /// <typeparam name="TProperties">The request properties for the custom resource.</typeparam>
    /// <typeparam name="TAttributes">The response attributes for the custom resource.</typeparam>
    public abstract class ALambdaCustomResourceFunction<TProperties, TAttributes> : ALambdaFunction {

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
        protected ALambdaCustomResourceFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaCustomResourceFunction{TProperties,TAttributes}"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaCustomResourceFunction(ILambdaFunctionDependencyProvider provider) : base(provider) { }


        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessCreateResourceAsync(Request{TRequestProperties})"/> method is invoked
        /// when AWS CloudFormation attempts to create a custom resource.
        /// </summary>
        /// <param name="request">The CloudFormation request instance.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Response<TAttributes>> ProcessCreateResourceAsync(Request<TProperties> request);

        /// <summary>
        /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessUpdateResourceAsync(Request{TRequestProperties})"/> method is invoked
        /// when AWS CloudFormation attempts to update a custom resource.
        /// </summary>
        /// <param name="request">The CloudFormation request instance.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Response<TAttributes>> ProcessUpdateResourceAsync(Request<TProperties> request);

        /// <summary>
        /// The <see cref="ALambdaCustomResourceFunction{TRequestProperties,TResponseProperties}.ProcessDeleteResourceAsync(Request{TRequestProperties})"/> method is invoked
        /// when AWS CloudFormation attempts to delete a custom resource.
        /// </summary>
        /// <param name="request">The CloudFormation request instance.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task<Response<TAttributes>> ProcessDeleteResourceAsync(Request<TProperties> request);

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
                switch(request.RequestType) {
                case RequestType.Create:
                    responseTask = ProcessCreateResourceAsync(request);
                    break;
                case RequestType.Update:
                    responseTask = ProcessUpdateResourceAsync(request);
                    break;
                case RequestType.Delete:
                    responseTask = ProcessDeleteResourceAsync(request);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.RequestType), request.RequestType, "unexpected request value");
                }

                // check if the custom resource logic completes before function times out
                var timeout = CurrentContext.RemainingTime - TimeSpan.FromSeconds(0.5);
                if(await Task.WhenAny(Task.Delay(timeout), responseTask) != responseTask) {
                    throw new TimeoutException($"custom resource operation timed out");
                }
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

        /// <inheritdoc/>
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

            // deserialize stream into a generic JSON object
            LogInfo("deserializing request");
            var json = JsonConvert.DeserializeObject<JObject>(body);

            // determine if the custom resource request is wrapped in an SNS message
            // or if it is a direct invocation by the CloudFormation service
            if(json.TryGetValue("Records", out _)) {

                // deserialize SNS event
                LogInfo("deserializing SNS event");
                var snsEvent = json.ToObject<SNSEvent>();

                // extract message from SNS event
                LogInfo("deserializing message");
                var messageBody = snsEvent.Records.First().Sns.Message;

                // deserialize message into a cloudformation request
                return DeserializeJson<CloudFormationResourceRequest<TProperties>>(messageBody);
            } else {

                // deserialize generic JSON into a cloudformation request
                return json.ToObject<CloudFormationResourceRequest<TProperties>>();
            }
        }

        private async Task WriteResponse(
            CloudFormationResourceRequest<TProperties> rawRequest,
            CloudFormationResourceResponse<TAttributes> rawResponse
        ) {
            Exception exception = null;

            // write response to pre-signed S3 URL
            for(var i = 0; i < MAX_SEND_ATTEMPTS; ++i) {
                try {
                    var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                        RequestUri = new Uri(rawRequest.ResponseURL),
                        Method = HttpMethod.Put,
                        Content = new ByteArrayContent(Encoding.UTF8.GetBytes(SerializeJson(rawResponse)))
                    });
                    if(httpResponse.StatusCode != HttpStatusCode.OK) {
                        throw new LambdaCustomResourceException(
                            "PUT operation to pre-signed S3 URL failed with status code: {0} [{1} {2}] = {3}",
                            httpResponse.StatusCode,
                            rawRequest.RequestType,
                            rawRequest.ResourceType,
                            await httpResponse.Content.ReadAsStringAsync()
                        );
                    }
                    return;
                } catch(Exception e) {
                    exception = e;
                    LogErrorAsWarning(e, "writing response to pre-signed S3 URL failed");
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }

            // max attempts have been reached; fail permanently and record the failed request for playback
            LogError(exception);
            await RecordFailedMessageAsync(LambdaLogLevel.ERROR, FailedMessageOrigin.CloudFormation, SerializeJson(rawRequest), exception);
        }
    }
}