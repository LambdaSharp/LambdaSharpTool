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

namespace LambdaSharp.App.EventBus {

    /// <summary>
    /// The <see cref="EventBusStateChangedEventArgs"/> class defines the event arguments when
    /// the LambdaSharp App EventBus changes its connection state.
    /// </summary>
    public class EventBusStateChangedEventArgs : EventArgs {

        internal EventBusStateChangedEventArgs(bool open) => IsOpen = open;

        //--- Properties ---

        /// <summary>
        /// The <see cref="IsOpen"/> property indicates if the LambdaSharp App EventBus connection is open.
        /// </summary>
        public bool IsOpen { get; }

        /// <summary>
        /// The <see cref="IsClosed"/> property indicates if the LambdaSharp App EventBus connection is closed.
        /// </summary>
        public bool IsClosed => !IsOpen;
    }
}
