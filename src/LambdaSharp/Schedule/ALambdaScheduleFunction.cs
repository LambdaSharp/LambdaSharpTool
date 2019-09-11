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

using System.IO;
using System.Threading.Tasks;

namespace LambdaSharp.Schedule {

    /// <summary>
    /// The <see cref="ALambdaScheduleFunction"/> is the abstract base class for handling scheduled events.
    /// </summary>
    public abstract class ALambdaScheduleFunction : ALambdaFunction {

        //--- Constructors ---

        /// <summary>
        /// Initializes a new <see cref="ALambdaScheduleFunction"/> instance using the default
        /// implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        protected ALambdaScheduleFunction() : this(null) { }

        /// <summary>
        /// Initializes a new <see cref="ALambdaScheduleFunction"/> instance using a
        /// custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.
        /// </summary>
        /// <param name="provider">Custom implementation of <see cref="ILambdaFunctionDependencyProvider"/>.</param>
        protected ALambdaScheduleFunction(ILambdaFunctionDependencyProvider provider) : base(provider) { }

        //--- Abstract Methods ---

        /// <summary>
        /// The <see cref="ProcessEventAsync(LambdaScheduleEvent)"/> method is invoked when a scheduled event occurs.
        /// </summary>
        /// <param name="schedule">The <see cref="LambdaScheduleEvent"/> instance.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public abstract Task ProcessEventAsync(LambdaScheduleEvent schedule);

        //--- Methods ---

        /// <summary>
        /// The <see cref="ProcessMessageStreamAsync(Stream)"/> method is overridden to
        /// provide specific behavior for this base class.
        /// </summary>
        /// <remarks>
        /// This method cannot be overridden.
        /// </remarks>
        /// <param name="stream">The stream with the request payload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public override sealed async Task<Stream> ProcessMessageStreamAsync(Stream stream) {
            var schedule = DeserializeJson<LambdaScheduleEvent>(stream);
            LogInfo($"received schedule event '{schedule.Name ?? schedule.Id}'");
            await ProcessEventAsync(schedule);
            return "Ok".ToStream();
        }
    }
}
