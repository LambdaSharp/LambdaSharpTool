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
using System.Collections.Generic;

namespace LambdaSharp.App.OmniBus.Protocol {

    /// <summary>
    /// The abstract <see cref="OmniBusAction"/> class is the used by all LambdaSharp App OmniBus actions.
    /// </summary>
    public class OmniBusAction {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Action"/> property holds the name of the action.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The <see cref="RequestId"/> property holds a unique identifier
        /// used to reference the action in an acknowledgment.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The <see cref="Headers"/> property holds optional headers for the action.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The <see cref="ContentType"/> property holds the MIME type for the payload.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The <see cref="Body"/> property holds the payload serialized to a string.
        /// </summary>
        public string Body { get; set; }

        //--- Methods ---

        /// <summary>
        /// The <see cref="AcknowledgeOk()"/> method creates a successful acknowledgement to this action.
        /// </summary>
        public OmniBusAck AcknowledgeOk() => OmniBusAck.Ok(RequestId);

        /// <summary>
        /// The <see cref="AcknowledgeOk()"/> method creates a successful acknowledgement to this action.
        /// </summary>
        public OmniBusAck AcknowledgeOk(string contentType, string body) => OmniBusAck.Ok(RequestId, contentType, body);

        /// <summary>
        /// The <see cref="AcknowledgeBadRequest(string)"/> method creates a bad-request acknowledgement to this action.
        /// </summary>
        /// <param name="message"></param>
        public OmniBusAck AcknowledgeBadRequest(string message) => OmniBusAck.BadRequest(message, RequestId);

        /// <summary>
        /// The <see cref="AcknowledgeNotFound(string)"/> method creates a not-found acknowledgement to this action.
        /// </summary>
        /// <param name="message"></param>
        public OmniBusAck AcknowledgeNotFound(string message) => OmniBusAck.NotFound(message, RequestId);

        /// <summary>
        /// The <see cref="AcknowledgeInternalError(string)"/> method creates a not-found acknowledgement to this action.
        /// </summary>
        /// <param name="message"></param>
        public OmniBusAck AcknowledgeInternalError(string message) => OmniBusAck.InternalError(message, RequestId);
    }

    public class OmniBusAction<TPayload> : OmniBusAction { }
}