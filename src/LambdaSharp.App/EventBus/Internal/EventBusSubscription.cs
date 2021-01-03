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
using System.Threading.Tasks;
using LambdaSharp.App.EventBus.Actions;

namespace LambdaSharp.App.EventBus.Internal {

    internal class EventBusSubscription : IEventBusSubscription {

        //--- Types ---

        //--- Fields ---
        private LambdaSharpEventBusClient _client;

        //--- Constructors ---
        public EventBusSubscription(string name, string pattern, Action<IEventBusSubscription, string> callback, LambdaSharpEventBusClient client) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        //--- Properties ---
        public string Name { get; }
        public string Pattern { get; }
        public Action<IEventBusSubscription, string> Callback { get; }
        public bool IsEnabled => Status == EventBusSubscriptionStatus.Enabled;
        public EventBusSubscriptionStatus Status { get; set; }

        //--- Methods ---
        public async Task EnableSubscriptionAsync() {
            switch(Status) {
            case EventBusSubscriptionStatus.Enabled:

                // nothing to do
                break;
            case EventBusSubscriptionStatus.Disabled:
                await _client.OpenConnectionAsync();

                // update state and notify event bus
                Status = EventBusSubscriptionStatus.Enabled;
                await _client.EnableSubscriptionAsync(this);
                break;
            case EventBusSubscriptionStatus.Disposed:
                throw new ObjectDisposedException(Name);
            case EventBusSubscriptionStatus.Error:
                throw new InvalidOperationException("EventBus subscription is in Error state");
            default:
                throw new InvalidOperationException($"Unexpected status: {Status}");
            }
        }

        public async Task DisableSubscriptionAsync() {
            switch(Status) {
            case EventBusSubscriptionStatus.Enabled:

                // update state and notify event bus
                Status = EventBusSubscriptionStatus.Disabled;
                await _client.DisableSubscriptionAsync(this);
                break;
            case EventBusSubscriptionStatus.Disabled:
            case EventBusSubscriptionStatus.Disposed:
            case EventBusSubscriptionStatus.Error:

                // nothing to do
                return;
            default:
                throw new InvalidOperationException($"Unexpected status: {Status}");
            }
        }

        public void Dispatch(EventAction action) {
            if(IsEnabled) {
                Callback?.Invoke(this, action.Event);
            }
        }

        public async ValueTask DisposeAsync() {
            await DisableSubscriptionAsync();
            Status = EventBusSubscriptionStatus.Disposed;
        }
    }
}
