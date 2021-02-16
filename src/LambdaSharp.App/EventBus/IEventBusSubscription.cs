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
    /// The <see cref="EventBusSubscriptionStatus"/> enum describes the different states of a LambdaSharp App EventBus subscription.
    /// </summary>
    public enum EventBusSubscriptionStatus {

        /// <summary>
        /// The subscription is disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// The subscription is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// The subscription is disposed.
        /// </summary>
        Disposed,

        /// <summary>
        /// The subscription has an error.
        /// </summary>
        Error
    }

    /// <summary>
    /// The <see cref="IEventBusSubscription"/> interface defines the accessible properties of a LambdaSharp App EventBus subscription.
    /// </summary>
    public interface IEventBusSubscription : IAsyncDisposable {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Name"/> property holds the name of the LambdaSharp App EventBus subscription.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The <see cref="Pattern"/> property holds the event pattern of the LambdaSharp App EventBus subscription.
        /// </summary>
        string Pattern { get; }

        /// <summary>
        /// The <see cref="Status"/> property holds the status of the LambdaSharp App EventBus subscription.
        /// </summary>
        EventBusSubscriptionStatus Status { get; }
    }
}
