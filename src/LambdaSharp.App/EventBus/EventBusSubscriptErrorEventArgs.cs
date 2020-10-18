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

// TODO: review all LogDebug statements

namespace LambdaSharp.App.EventBus {

    public class EventBusSubscriptErrorEventArgs : EventArgs {

        //--- Constructors ---
        public EventBusSubscriptErrorEventArgs(IEventBusSubscription subscription, Exception exception) {
            Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        //--- Properties ---
        public IEventBusSubscription Subscription { get; }
        public Exception Exception { get; }
    }
}
