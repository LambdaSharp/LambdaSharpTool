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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using LambdaSharp.CustomResource.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.CustomResource {

    public abstract class ALambdaCustomResourceFunction<TRequestProperties, TResponseProperties> : ALambdaFunction {

        //--- Class Fields ---
        public static HttpClient HttpClient = new HttpClient();

        //--- Abstract Methods ---
        protected abstract Task<Response<TResponseProperties>> HandleCreateResourceAsync(Request<TRequestProperties> request);
        protected abstract Task<Response<TResponseProperties>> HandleUpdateResourceAsync(Request<TRequestProperties> request);
        protected abstract Task<Response<TResponseProperties>> HandleDeleteResourceAsync(Request<TRequestProperties> request);

        //--- Methods ---
        public override async Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context) {

            // read stream into memory
            LogInfo("reading message stream");
            string body;
            try {
                using(var reader = new StreamReader(stream)) {
                    body = reader.ReadToEnd();
                }
            } catch(Exception e) {
                LogError(e);
                throw;
            }

            // deserialize stream into a generic JSON object
            LogInfo("deserializing request");
            JObject json;
            try {
                json = JsonConvert.DeserializeObject<JObject>(body);
            } catch(Exception e) {
                LogError(e);
                await RecordFailedMessageAsync(LambdaLogLevel.ERROR, body, e);
                return $"ERROR: {e.Message}";
            }

            // determine if the custom resource request is wrapped in an SNS message
            CloudFormationResourceRequest<TRequestProperties> rawRequest;
            if(json.TryGetValue("Records", out _)) {

                // deserialize SNS event
                LogInfo("deserializing SNS event");
                SNSEvent snsEvent;
                try {
                    snsEvent = json.ToObject<SNSEvent>();
                } catch(Exception e) {
                    LogError(e);
                    await RecordFailedMessageAsync(LambdaLogLevel.ERROR, body, e);
                    return $"ERROR: {e.Message}";
                }

                // extract message from SNS event
                LogInfo("deserializing message");
                string messageBody;
                try {
                    messageBody = snsEvent.Records.First().Sns.Message;
                } catch(Exception e) {
                    LogError(e);
                    await RecordFailedMessageAsync(LambdaLogLevel.ERROR, body, e);
                    return $"ERROR: {e.Message}";
                }

                // deserialize message into a cloudformation request
                try {
                    rawRequest = DeserializeJson<CloudFormationResourceRequest<TRequestProperties>>(messageBody);
                } catch(Exception e) {
                    LogError(e);
                    await RecordFailedMessageAsync(LambdaLogLevel.ERROR, body, e);
                    return $"ERROR: {e.Message}";
                }
            } else {

                // deserialize generic JSON into a cloudformation request
                try {
                    rawRequest = json.ToObject<CloudFormationResourceRequest<TRequestProperties>>();
                } catch(Exception e) {
                    LogError(e);
                    await RecordFailedMessageAsync(LambdaLogLevel.ERROR, body, e);
                    return $"ERROR: {e.Message}";
                }
            }

            // process message
            LogInfo("processing request");
            CloudFormationResourceResponse<TResponseProperties> rawResponse;
            try {
                LogInfo(JsonConvert.SerializeObject(rawRequest, Formatting.Indented));
                LogInfo($"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation received");
                var request = new Request<TRequestProperties> {
                    RequestType = rawRequest.RequestType,
                    ResourceType = rawRequest.ResourceType,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId,
                    ResourceProperties = rawRequest.ResourceProperties,
                    OldResourceProperties = rawRequest.OldResourceProperties
                };

                // handle slack request
                Response<TResponseProperties> response;
                switch(request.RequestType) {
                case RequestType.Create:
                    response = await HandleCreateResourceAsync(request);
                    break;
                case RequestType.Update:
                    response = await HandleUpdateResourceAsync(request);
                    break;
                case RequestType.Delete:
                    response = await HandleDeleteResourceAsync(request);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.RequestType), request.RequestType, "unexpected request value");
                }
                rawResponse = new CloudFormationResourceResponse<TResponseProperties> {
                    Status = CloudFormationResourceResponseStatus.SUCCESS,
                    Reason = "",
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = response.PhysicalResourceId ?? rawRequest.PhysicalResourceId,
                    NoEcho = response.NoEcho,
                    Data = response.Properties
                };
                LogInfo($"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation was successful");
            } catch(Exception e) {
                LogError(e, $"{rawRequest.ResourceType}: {rawRequest.RequestType.ToString().ToUpperInvariant()} operation FAILED [{{0}}]", e.Message);
                rawResponse = new CloudFormationResourceResponse<TResponseProperties> {
                    Status = CloudFormationResourceResponseStatus.FAILED,
                    Reason = "internal error",
                    StackId = rawRequest.StackId,
                    RequestId = rawRequest.RequestId,
                    LogicalResourceId = rawRequest.LogicalResourceId,
                    PhysicalResourceId = rawRequest.PhysicalResourceId ?? "no-physical-id"
                };
            }
            await WriteResponse(rawRequest, rawResponse);
            return rawResponse.Status;
        }

        public override async Task InitializeFailedAsync(Stream stream, ILambdaContext context) {

            // NOTE (2018-12-12, bjorg): attempt to respond with a failure message when the function
            //  initialization has failed; otherwise, the requesting cloudformation stack will be stuck
            //  until it times out (which can take up to 30 minutes).
            try {
                var rawRequest = DeserializeJson<CloudFormationResourceRequest<TRequestProperties>>(stream);
                var rawResponse = new CloudFormationResourceResponse<TResponseProperties> {
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

        private async Task WriteResponse(
            CloudFormationResourceRequest<TRequestProperties> rawRequest,
            CloudFormationResourceResponse<TResponseProperties> rawResponse
        ) {

            // write response to pre-signed S3 URL
            try {
                var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                    RequestUri = new Uri(rawRequest.ResponseURL),
                    Method = HttpMethod.Put,
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rawResponse)))
                });
                if(httpResponse.StatusCode != HttpStatusCode.OK) {
                    throw new CustomResourceException(
                        "PUT operation to pre-signed S3 URL failed with status code: {0} [{1} {2}] = {3}",
                        httpResponse.StatusCode,
                        rawRequest.RequestType,
                        rawRequest.ResourceType,
                        await httpResponse.Content.ReadAsStringAsync()
                    );
                }
            } catch(Exception e) {
                LogError(e, "writing response to pre-signed S3 URL failed");

                // TODO (2018-06-14, bjorg): how should we handle this? we may want the handler to re-attempt the
                //  resource creation since the request was SNS based
                throw;
            }

        }
    }
}