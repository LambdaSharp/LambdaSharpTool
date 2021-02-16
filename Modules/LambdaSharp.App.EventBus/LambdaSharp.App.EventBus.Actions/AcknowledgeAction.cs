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

namespace LambdaSharp.App.EventBus.Actions {

    /// <summary>
    /// The <see cref="AcknowledgeAction"/> class is used to respond to a
    /// LambdaSharp App EventBus action.
    /// </summary>
    public sealed class AcknowledgeAction : ARuleAction {

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="AcknowledgeAction"/>.
        /// </summary>
        public AcknowledgeAction() => Action = "Ack";

        //--- Properties ---

        /// <summary>
        /// The <see cref="Status"/> property describes the outcome of the action. It's value
        /// is either <code>"Ok"</code> or <code>"Error"</code>.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The <see cref="Message"/> property optionally includes a message describing why the status
        /// was not successful.
        /// </summary>
        public string Message { get; set; }
    }
}