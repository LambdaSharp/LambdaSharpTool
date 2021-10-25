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

namespace LambdaSharp.App.OmniBus.Events.Actions {

    /// <summary>
    /// The <see cref="SubscribePayload"/> class is used to subscribe to an
    /// event pattern on the LambdaSharp App OmniBus.
    /// </summary>
    public sealed class SubscribePayload {

        //--- Constants ---
        public const string ACTION = "Events/Subscribe";
        public const string MIME_TYPE = "application/vnd.lambdasharp.omnibus.subscribe2106+json";

        //--- Properties ---

        /// <summary>
        /// The <see cref="Rule"/> property holds the subscription rule name.
        /// </summary>
        public string Rule { get; set; }

        /// <summary>
        /// The <see cref="Pattern"/> property holds the JSON-serialized event pattern to match.
        /// </summary>
        public string Pattern { get; set; }
    }
}