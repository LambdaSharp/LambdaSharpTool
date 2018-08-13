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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using MindTouch.Rollbar.Data;
using MindTouch.Rollbar.Builders;
using Newtonsoft.Json;

namespace MindTouch.Rollbar {

    [ExcludeFromCodeCoverage]
    public class RollbarClient : IRollbarClient {

        //--- Constants ---
        private const int MAX_SIZE = 512000;

        //--- Class Methods ---
        public static IRollbarClient Create(RollbarConfiguration configuration) {
            var frame = new FrameCollectionBuilder();
            var exception = new ExceptionInfoBuilder();
            var trace = new TraceBuilder(exception, frame);
            var traceChain = new TraceChainBuilder(trace);
            var body = new BodyBuilder(trace, traceChain);
            var title = new TitleBuilder();
            var data = new DataBuilder(configuration, body, title);
            return new RollbarClient(configuration, new PayloadBuilder(configuration, data));
        }

        public static string FormatMessage(string format, object[] args) {
            if(format == null) {
                return null;
            }
            if(args.Length == 0) {
                return format;
            }
            try {
                return string.Format(format, args);
            } catch {
                return format + "(" + string.Join(", ", args.Select(arg => {
                    try {
                        return arg.ToString();
                    } catch {
                        return "<ERROR>";
                    }
                })) + ")";
            }
        }

        //--- Fields ---
        private readonly RollbarConfiguration _configuration;
        private readonly Encoding _encoding;
        private readonly IWebProxy _proxy;
        private readonly IPayloadBuilder _payloadBuilder;

        //--- Constructors ---
        public RollbarClient(RollbarConfiguration configuration, IPayloadBuilder payloadBuilder) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _payloadBuilder = payloadBuilder ?? throw new ArgumentNullException(nameof(payloadBuilder));
            _encoding = Encoding.GetEncoding(_configuration.Encoding);
            if(!string.IsNullOrWhiteSpace(_configuration.Proxy)) {
                _proxy = new WebProxy(_configuration.Proxy);
            }
        }

        //--- Methods ---
        public Task<Result> SendAsync(string level, Exception exception, string format = null, params object[] args)
            => HttpPost(CreatePayload(MAX_SIZE, level, exception, format, args));

        public string CreatePayload(int maxSize, string level, Exception exception, string format = null, params object[] args) {
            var message = FormatMessage(format, args) ?? exception?.Message;
            if(message == null) {
                throw new ArgumentException("both exception and format are null");
            }
            Payload payload;
            if(exception != null) {
                payload = _payloadBuilder.CreateFromException(exception, message, level);
            } else {
                payload = _payloadBuilder.CreateFromMessage(message, level);
            }
            if(exception is ARollbarException rollbarException) {
                payload = _payloadBuilder.CreateWithFingerprintInput(payload, rollbarException.FingerprintValue);
            } else {
                payload = _payloadBuilder.CreateWithFingerprintInput(payload, format);
            }
            return SerializePayload(payload, maxSize);
        }

        private Task<Result> HttpPost(string payload) {
            return Task.Run(() => {

                // convert the json payload to bytes for transmission
                var payloadBytes = _encoding.GetBytes(payload);
                var request = (HttpWebRequest)WebRequest.Create(_configuration.Endpoint);
                request.ContentType = "application/json";
                request.Method = "POST";
                request.ContentLength = payloadBytes.Length;
                if(_proxy != null) {
                    request.Proxy = _proxy;
                }

                // we need to wrap GetRequestStream() in a try block
                // if the endpoint is unreachable, that exception gets thrown here
                try {
                    using(var stream = request.GetRequestStream()) {
                        stream.Write(payloadBytes, 0, payloadBytes.Length);
                    }
                } catch(Exception ex) {
                    return new Result(0, ex.Message);
                }

                // attempt to parse the response. wrap GetResponse() in a try block
                // since WebRequest throws exceptions for HTTP error status codes
                WebResponse response;
                try {
                    response = request.GetResponse();
                } catch(WebException ex) {
                    if(ex.Response == null) {
                        var failMsg = string.Format(
                            "Request failed. Status: {0}. Message: {1}",
                            ex.Status,
                            ex.Message
                        );
                        return new Result(0, failMsg);
                    }
                    return OnRequestCompleted(ex.Response);
                } catch(Exception ex) {
                    return new Result(0, ex.Message);
                }
                return OnRequestCompleted(response);
            });
        }

        private Result OnRequestCompleted(WebResponse response) {
            var responseCode = ((HttpWebResponse)response).StatusCode;
            string responseText;
            using(var stream = response.GetResponseStream()) {
                if(stream == null) {
                    responseText = string.Empty;
                } else {
                    using(var reader = new StreamReader(stream)) {
                        responseText = reader.ReadToEnd();
                    }
                }
            }
            return new Result(responseCode, responseText);
        }

        private string SerializePayload(Payload payload, int maxSize) {
            var payloadString = JsonConvert.SerializeObject(payload, _configuration.JsonSettings);
            var fullPayloadSize = _encoding.GetByteCount(payloadString);

            // NOTE: Rollbar Payload size cannot exceed 512KB
            if(fullPayloadSize <= maxSize) {
                return payloadString;
            }

            // truncate Request body because it could be larger than 512KB
            if((payload.RollbarData.Request != null) && !string.IsNullOrWhiteSpace(payload.RollbarData.Request.Body)) {
                var bodySize = _encoding.GetByteCount(payload.RollbarData.Request.Body);
                var sizeAvailable = maxSize - (fullPayloadSize - bodySize);
                if(sizeAvailable > 0) {
                    var bodyBytes = _encoding.GetBytes(payload.RollbarData.Request.Body).Take(sizeAvailable).ToArray();
                    var body = _encoding.GetString(bodyBytes);
                    var request = new Request(
                        payload.RollbarData.Request.Url,
                        payload.RollbarData.Request.Method,
                        payload.RollbarData.Request.Headers,
                        payload.RollbarData.Request.QueryString,
                        body,
                        payload.RollbarData.Request.UserIpAddress);
                    var context = new Context(
                        payload.RollbarData.ApplicationContext,
                        request,
                        payload.RollbarData.Person,
                        payload.RollbarData.Custom);
                    var data = new RollbarData(payload.RollbarData, context);
                    return JsonConvert.SerializeObject(new Payload(payload.AccessToken, data), _configuration.JsonSettings);
                }
            }
            return payloadString;
        }
    }
}
