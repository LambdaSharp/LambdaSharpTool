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

using LambdaSharp.App.Bus.Protocol;

namespace LambdaSharp.App.Bus.Events.Payloads {

    /// <summary>
    /// The <see cref="UnsubscribePayload"/> class is used to unsubscribe from
    /// a previous subscription on the LambdaSharp App Bus.
    /// </summary>
    public sealed class UnsubscribePayload {

        //--- Constants ---
        public const string ACTION = "Events/Unsubscribe";
        public const string MIME_TYPE = "application/vnd.lambdasharp.bus.unsubscribe2106+json";

        //--- Properties ---

        /// <summary>
        /// The <see cref="Rule"/> property holds the subscription rule name.
        /// </summary>
        public string Rule { get; set; }
    }
}