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

namespace LambdaSharp.App.EventBus.Exceptions {

    /// <summary>
    /// All LambdaSharp App EventBus exceptions are derived from the <see cref="AnEventBusException"/> abstract base class.
    /// </summary>
    public abstract class AnEventBusException : Exception {

        //--- Constructors ---

        /// <summary>
        /// Initializes a <see cref="AnEventBusException"/> instance with the specified exception message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        protected AnEventBusException(string message) : base(message) { }

        /// <summary>
        /// Initializes a <see cref="AnEventBusException"/> instance with the specified exception message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception instance.</param>
        protected AnEventBusException(string message, Exception innerException) : base(message, innerException) { }
    }
}
