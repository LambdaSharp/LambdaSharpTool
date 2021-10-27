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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.App.Bus.Protocol {

    /// <summary>
    /// The <see cref="AcknowledgeStatusCode"/> enum describes to possible acknowledge status codes.
    /// </summary>
    public enum AcknowledgeStatusCode {
        Ok = 200,
        BadRequest = 400,
        NotFound = 404,
        InternalError = 500,
        NotImplemented = 501
    }

    /// <summary>
    /// The <see cref="BusAck"/> class is used to respond to a
    /// LambdaSharp App Bus action.
    /// </summary>
    public sealed class BusAck : BusAction {

        //--- Class Methods ---

        /// <summary>
        /// The <see cref="Ok(string)"/> method creates a successful acknowledgement to this action.
        /// </summary>
        /// <param name="requestId"></param>
        public static BusAck Ok(string requestId) => new BusAck {
            RequestId = requestId,
            StatusCode = AcknowledgeStatusCode.Ok
        };

        /// <summary>
        /// The <see cref="Ok(string)"/> method creates a successful acknowledgement to this action.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="contentType"></param>
        /// <param name="body"></param>
        public static BusAck Ok(string requestId, string contentType, string body) => new BusAck {
            RequestId = requestId,
            StatusCode = AcknowledgeStatusCode.Ok,
            ContentType = contentType,
            Body = body
        };

        /// <summary>
        /// The <see cref="AcknowledgeBadRequest(string)"/> method creates a bad-request acknowledgement to this action.
        /// </summary>
        /// <param name="message"></param>
        public static BusAck BadRequest(string message, string requestId) => new BusAck {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId)),
            StatusCode = AcknowledgeStatusCode.BadRequest,
            ContentType = BusErrorPayload.MIME_TYPE,
            Body = JsonSerializer.Serialize(new BusErrorPayload {
                Message = message ?? throw new ArgumentNullException(nameof(message))
            })
        };

        /// <summary>
        /// The <see cref="AcknowledgeNotFound(string)"/> method creates a not-found acknowledgement to this action.
        /// </summary>
        /// <param name="message"></param>
        public static BusAck NotFound(string message, string requestId) => new BusAck {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId)),
            StatusCode = AcknowledgeStatusCode.NotFound,
            ContentType = BusErrorPayload.MIME_TYPE,
            Body = JsonSerializer.Serialize(new BusErrorPayload {
                Message = message ?? throw new ArgumentNullException(nameof(message))
            })
        };

        /// <summary>
        /// The <see cref="AcknowledgeInternalError(string)"/> method creates a not-found acknowledgement to this action.
        /// </summary>
        /// <param name="message"></param>
        public static BusAck InternalError(string message, string requestId) => new BusAck {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId)),
            StatusCode = AcknowledgeStatusCode.InternalError,
            ContentType = BusErrorPayload.MIME_TYPE,
            Body = JsonSerializer.Serialize(new BusErrorPayload {
                Message = message ?? throw new ArgumentNullException(nameof(message))
            })
        };

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="BusAck"/>.
        /// </summary>
        public BusAck() => Action = "Ack";

        //--- Properties ---

        /// <summary>
        /// The <see cref="StatusCode"/> property holds the status code of an acknowledgment.
        /// </summary>
        public AcknowledgeStatusCode? StatusCode { get; set; }
    }
}