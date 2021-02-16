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

using System.Collections.Generic;

namespace LambdaSharp.App.EventBus.Actions {

    /// <summary>
    /// The <see cref="EventAction"/> class describes a LambdaSharp App EventBus event.
    /// </summary>
    public sealed class EventAction : AnAction {

        //--- Constructors ---

        /// <summary>
        /// Create new instance of <see cref="EventAction"/>.
        /// </summary>
        public EventAction() => Action = "Event";

        //--- Properties ---

        /// <summary>
        /// The <see cref="Rules"/> property holds the names of all matching subscription rules.
        /// </summary>
        public List<string> Rules { get; set; }

        /// <summary>
        /// The <see cref="Source"/> property holds the name of the event source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The <see cref="Type"/> property holds the name of the event type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The <see cref="Event"/> property holds the JSON serialization of the CloudWatch event data structure.
        /// </summary>
        public string Event { get; set; }
    }
}