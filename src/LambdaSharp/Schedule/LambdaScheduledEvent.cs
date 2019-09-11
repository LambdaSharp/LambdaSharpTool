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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LambdaSharp.Schedule {

    /// <summary>
    /// This class represents the event sent by the <c>Schedule</c> source on a λ# module function.
    /// It is similar to the event sent by <a href="https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/CloudWatchEventsandEventPatterns.html">CloudWatch Events Rule</a>
    /// with the addition of the <see cref="LambdaScheduleEvent.Name"/> property.
    /// </summary>
    public class LambdaScheduleEvent {

        //--- Properties ---

        /// <summary>
        /// A unique value is generated for every event. This can be helpful in tracing events as
        /// they move through rules to targets, and are processed.
        /// </summary>
        /// <value>Unique event identifier</value>
        public string Id { get; set; }

        /// <summary>
        /// The event timestamp, which can be specified by the service originating the event.
        /// If the event spans a time interval, the service might choose to report the start
        /// time, so this value can be noticeably before the time the event is actually received.
        /// </summary>
        /// <value>Event timestamp</value>
        public DateTime Time { get; set; }

        /// <summary>
        /// The name corresponds to the optional name defined in the <c>Schedule</c> source using the <c>Name</c> attribute.
        /// </summary>
        /// <value>Event name</value>
        public string Name { get; set; }
    }
}
