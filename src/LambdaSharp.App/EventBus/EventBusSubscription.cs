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
using System.Threading.Tasks;
using LambdaSharp.App.EventBus.Actions;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App.EventBus {

    internal class EventBusSubscription : ISubscription {

        // TODO: need to know when a subscription becomes active, because subscriber may have to request missed data
        //  * event  StateChanged

        //--- Types ---
        private enum Status {
            Disabled,
            Enabled,
            Disposed,
            Error
        }

        //--- Fields ---
        private LambdaSharpEventBusClient _client;
        private Status _status = Status.Disabled;

        //--- Constructors ---
        public EventBusSubscription(string name, string pattern, Action<string, string> callback, LambdaSharpEventBusClient client) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // automatically re-subscribe when connection is opened
            _client.ConnectionOpened += Resubscribe;
        }

        //--- Properties ---
        public string Name { get; }
        public string Pattern { get; }
        public Action<string, string> Callback { get; }
        public bool IsEnabled => _status == Status.Enabled;

        //--- Methods ---
        public async Task EnableSubscriptionAsync() {
            if(_status == Status.Disposed) {
                throw new ObjectDisposedException(Name);
            }
            _client.Logger.LogDebug("Enabling subscription {0}", Name);

            // send 'Subscribe' request and wait for acknowledge response
            _status = Status.Enabled;
            if(_client.IsConnectionOpen) {
                try {
                    await _client.SendMessageAndWaitForAcknowledgeAsync(new SubscribeAction {
                        Rule = Name,
                        Pattern = Pattern
                    });
                } catch(InvalidOperationException) {

                    // websocket is not connected; ignore error
                } catch(Exception e) {
                    _client.Logger.LogDebug("Error activating subscription for: {0}", Name);
                    _status = Status.Error;

                    // subscription failed
                    _client.OnSubscriptionError(this, e);
                }
            }
        }

        public async Task DisableSubscriptionAsync() {
            if(_status == Status.Disposed) {
                throw new ObjectDisposedException(Name);
            }
            if(_status == Status.Error) {
                throw new InvalidOperationException();
            }

            // send 'Unsubscribe' request without waiting for acknowledge response
            _status = Status.Disabled;
            _client.Remove(this);
            if(_client.IsConnectionOpen) {
                try {
                    await _client.SendMessageAsync(new UnsubscribeAction {
                        Rule = Name
                    });
                } catch(Exception) {

                    // nothing to do
                }
            }
        }

        public void Dispatch(EventAction action) {
            _client.Logger.LogDebug("Dispatching event for: {0}", Name);
            if(_status == Status.Enabled) {
                Callback?.Invoke(Name, action.Event);
            }
        }

        public async ValueTask DisposeAsync() {
            if(_status == Status.Disposed) {

                // nothing to do
                return;
            }

            // check if event bus needs to be notified
            if(
                (_status == Status.Enabled)
                && _client.IsConnectionOpen
            ) {
                await DisableSubscriptionAsync();
            }
            _status = Status.Disposed;
        }

        private async void Resubscribe(object sender, EventArgs args) {
            if(_status == Status.Enabled) {
                await EnableSubscriptionAsync();
            }
        }
    }
}
