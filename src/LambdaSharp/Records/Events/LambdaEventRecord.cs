/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Globalization;

namespace LambdaSharp.Records.Events {

    /// <summary>
    /// The <see cref="LambdaEventRecord"/> class defines a structured Lambda log entry
    /// for events.
    /// </summary>
    public sealed class LambdaEventRecord : ALambdaRecord {

        //--- Constructors ---

        /// <summary>
        /// Create a new <see cref="LambdaEventRecord"/> instance.
        /// </summary>
        public LambdaEventRecord() {
            Source = "LambdaEvent";
            Version = "2020-04-12";
        }

        //--- Properties ---

        /// <summary>
        /// The <see cref="Time"/> property describes when the event occurred.
        /// </summary>
        /// <value>The time stamp of the event, per RFC3339.</value>
        public string? Time { get; set; }

        /// <summary>
        /// The <see cref="App"/> property describes the source application of the event.
        /// </summary>
        /// <value></value>
        public string? App { get; set; }

        /// <summary>
        /// The <see cref="Type"/> property describes what fields to expect in the event detail.
        /// </summary>
        /// <value>Then event name.</value>
        public string? Type { get; set; }

        /// <summary>
        /// The <see cref="Details"/> property describes a custom object with additional details for the event.
        /// </summary>
        /// <value>The event details, encoded as a JSON string.</value>
        public string? Details { get; set; }

        /// <summary>
        /// The <see cref="Resources"/> property describes what resources the event is associated with.
        ///
        /// </summary>
        /// <value>The list of associated resources.</value>
        public List<string>? Resources { get; set; }
    }
}
