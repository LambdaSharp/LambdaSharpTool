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
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using LambdaSharp.App.Config;
using LambdaSharp.App.EventBus.Actions;
using Microsoft.Extensions.Logging;

// TODO: review all LogDebug statements

namespace LambdaSharp.App.EventBus {

    public interface ISubscription : IAsyncDisposable { }

    public class EventBusSubscriptErrorEventArgs : EventArgs {

        //--- Constructors ---
        public EventBusSubscriptErrorEventArgs(ISubscription subscription, Exception exception) {
            Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        //--- Properties ---
        public ISubscription Subscription { get; }
        public Exception Exception { get; }
    }

    public enum EventBusState {
        Closed,
        Open
    }

    public class EventBusStateChangedEventArgs : EventArgs {

        public EventBusStateChangedEventArgs(EventBusState state) => State = state;

        //--- Properties ---
        public EventBusState State { get; }
        public bool IsOpen => State == EventBusState.Open;

    }

    public sealed class LambdaSharpEventBusClient : IAsyncDisposable {

        //--- Class Fields ---
        private static readonly TimeSpan Frequency = TimeSpan.FromSeconds(10);

        //--- Class Methods ---
        private static bool TryParseJsonDocument(byte[] bytes, out JsonDocument document) {
            var utf8Reader = new Utf8JsonReader(bytes);
            return JsonDocument.TryParseValue(ref utf8Reader, out document);
        }

        //--- Fields ---
        private readonly LambdaSharpAppConfig _config;
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly Dictionary<string, EventBusSubscription> _subscriptions = new Dictionary<string, EventBusSubscription>();
        private readonly CancellationTokenSource _disposalTokenSource = new CancellationTokenSource();
        private readonly MemoryStream _messageAccumulator = new MemoryStream();
        private readonly Dictionary<string, TaskCompletionSource<AcknowledgeAction>> _pendingRequests = new Dictionary<string, TaskCompletionSource<AcknowledgeAction>>();
        private readonly Timer _timer;
        private ILogger<LambdaSharpEventBusClient> _logger;

        //--- Constructors ---

        // TODO: add AppClient to streamline sending events via EventBus
        public LambdaSharpEventBusClient(LambdaSharpAppConfig config, ILogger<LambdaSharpEventBusClient> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _timer = new Timer(OnTimer, state: null, dueTime: Frequency, period: Frequency);

            // issue a warning if the EventBus client is being initialized, but there is no URL for the websocket
            if(_config.EventBusUrl == null) {
                _logger.LogWarning("EventBus URL missing. Check app is subscribed to at least one event source.");
            }
        }

        //--- Events ---
        public event EventHandler<EventBusStateChangedEventArgs> StateChanged;
        public event EventHandler<EventBusSubscriptErrorEventArgs> SubscriptionError;

        //--- Properties ---
        public bool IsConnectionOpen => _webSocket.State == WebSocketState.Open;

        //--- Methods ---
        public ISubscription Subscribe<T>(string source, Func<CloudWatchEvent<T>, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(ISubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(@event);
            }
        }

        public ISubscription Subscribe<T>(string source, Func<T, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(ISubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(@event.Detail);
            }
        }

        public ISubscription Subscribe<T>(string source, Action<CloudWatchEvent<T>> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                (_, @event) => callback(@event)
            );
        }

        public ISubscription Subscribe<T>(string source, Action<T> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                (_, @event) => callback(@event.Detail)
            );
        }

        public ISubscription Subscribe<T>(string source, Func<ISubscription, CloudWatchEvent<T>, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(ISubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(subscription, @event);
            }
        }

        public ISubscription Subscribe<T>(string source, Func<ISubscription, T, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(ISubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(subscription, @event.Detail);
            }
        }

        public ISubscription Subscribe<T>(string source, Action<ISubscription, CloudWatchEvent<T>> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                callback
            );
        }

        public ISubscription Subscribe<T>(string source, Action<ISubscription, T> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return Subscribe<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                (subscription, @event) => callback(subscription, @event.Detail)
            );
        }

        public ISubscription Subscribe<T>(string name, EventPattern eventPattern, Action<ISubscription, CloudWatchEvent<T>> callback) {
            if(name is null) {
                throw new ArgumentNullException(nameof(name));
            }
            if(eventPattern is null) {
                throw new ArgumentNullException(nameof(eventPattern));
            }
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }

            // register subscription
            var subscription = new EventBusSubscription(
                name,
                JsonSerializer.Serialize(eventPattern),
                (callbackSubscription, cloudWatchEventJson) => {
                    try {
                        callback?.Invoke(callbackSubscription, JsonSerializer.Deserialize<CloudWatchEvent<T>>(cloudWatchEventJson));
                    } catch(Exception e) {
                        _logger.LogError(e, "Callback for rule '{0}' failed", name);
                    }
                },
                this
            );
            _subscriptions[name] = subscription;

            // kick off subscription, but don't wait for response
            _ = subscription.EnableSubscriptionAsync();
            return subscription;
        }

        internal async Task SendMessageAsync<T>(T message) where T : AnAction {
            if(_disposalTokenSource.IsCancellationRequested) {
                throw new OperationCanceledException();
            }
            var json = JsonSerializer.Serialize(message);
            _logger.LogDebug("Sending: {0}", json);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, _disposalTokenSource.Token);
        }

        internal async Task SendMessageAndWaitForAcknowledgeAsync<T>(T message) where T : AnAction {
            if(_disposalTokenSource.IsCancellationRequested) {
                throw new OperationCanceledException();
            }

            // register completion action
            var taskCompletionSource = new TaskCompletionSource<AcknowledgeAction>();
            _pendingRequests.Add(message.RequestId, taskCompletionSource);

            // send message to event bus
            await SendMessageAsync(message);

            // wait for response or timeout
            var outcome = await Task.WhenAny(
                Task.Delay(TimeSpan.FromSeconds(10)),
                taskCompletionSource.Task
            );
            _pendingRequests.Remove(message.RequestId);
            if(outcome != taskCompletionSource.Task) {
                if(_disposalTokenSource.IsCancellationRequested) {
                    throw new OperationCanceledException();
                } else {
                    throw new TimeoutException();
                }
            }

            // verify 'Ack' response
            var acknowledgeAction = taskCompletionSource.Task.Result;
            switch(acknowledgeAction.Status) {
            case "Ok":

                // all good; nothing to do
                return;
            case "Error":

                // TODO: better exception
                throw new Exception(acknowledgeAction.Message);
            default:

                // TODO: better exception
                throw new Exception($"unrecognized status message: {acknowledgeAction.Status}");
            }
        }

        internal async Task EnableSubscriptionAsync(EventBusSubscription subscription) {
            _logger.LogDebug("Enabling subscription {0}", subscription.Name);
            if(IsConnectionOpen) {
                try {
                    await SendMessageAndWaitForAcknowledgeAsync(new SubscribeAction {
                        Rule = subscription.Name,
                        Pattern = subscription.Pattern
                    });
                } catch(InvalidOperationException) {

                    // websocket is not connected; ignore error
                } catch(Exception e) {
                    _logger.LogDebug("Error activating subscription for: {0}\n{1}", subscription.Name, e);
                    subscription.Status = EventBusSubscriptionStatus.Error;

                    // subscription failed
                    SubscriptionError?.Invoke(this, new EventBusSubscriptErrorEventArgs(subscription, e));
                }
            }
        }

        internal async Task DisableSubscriptionAsync(EventBusSubscription subscription) {
            _logger.LogDebug("Disabling subscription {0}", subscription.Name);
            if(_subscriptions.Remove(subscription.Name) && IsConnectionOpen) {
                try {
                    await SendMessageAsync(new UnsubscribeAction {
                        Rule = subscription.Name
                    });
                } catch(InvalidOperationException) {

                    // websocket is not connected; ignore error
                } catch(Exception e) {

                    // nothing to do
                    _logger.LogDebug("Error deactivating subscription for: {0}\n{1}", subscription.Name, e);
                }
            }
        }

        internal async Task OpenConnectionAsync() {
            if(_config.EventBusUrl == null) {

                // nothing to do
                return;
            }
            if(_webSocket.State == WebSocketState.Open) {

                // nothing to do
                return;
            }
            _logger.LogDebug("WebSocketState: {0}", _webSocket.State);
            if(
                (_webSocket.State != WebSocketState.Closed)
                && (_webSocket.State != WebSocketState.None)
            ) {

                // websocket is in a transitional state; let the timer try to connect later
                _logger.LogDebug("Cannot open WebSocket in current state: {0}", _webSocket.State);
                return;
            }
            var eventBusUri = new Uri($"{_config.EventBusUrl}?app={_config.AppInstanceId}");
            _logger.LogDebug("Connecting to: {0}", eventBusUri);

            // connect/reconnect to websocket
            try {
                await _webSocket.ConnectAsync(eventBusUri, _disposalTokenSource.Token);
            } catch(Exception e) {
                _logger.LogDebug("Unable to connect WebSocket: {0}", e);
                return;
            }
            _logger.LogDebug("Connected!");
            _ = ReceiveMessageLoopAsync();

            // register app with event bus
            if(_disposalTokenSource.IsCancellationRequested) {

                // nothing to do
                return;
            }
            try {
                await SendMessageAndWaitForAcknowledgeAsync(new HelloAction());

                // re-subscribe all enabled subscriptions
                foreach(var subscription in _subscriptions.Values.Where(subscription => subscription.IsEnabled)) {
                    _ = EnableSubscriptionAsync(subscription);
                }

                // notify event handler on connection state change
                StateChanged?.Invoke(this, new EventBusStateChangedEventArgs(EventBusState.Open));
            } catch(InvalidOperationException e) {

                // this exception occurs when the send operation fails
                _logger.LogDebug("Error sending message: {0}", e);
            } catch(TimeoutException) {

                // this exception occurs when the ack confirmation takes too long
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reset", CancellationToken.None);
            }
        }

        private async Task ReceiveMessageLoopAsync() {
            var buffer = new ArraySegment<byte>(new byte[32 * 1024]);
            while(!_disposalTokenSource.IsCancellationRequested) {
                try {
                    _logger.LogDebug("Waiting on connection data");
                    var received = await _webSocket.ReceiveAsync(buffer, _disposalTokenSource.Token);
                    _logger.LogDebug("Received: {0}", received.MessageType);
                    switch(received.MessageType) {
                    case WebSocketMessageType.Close:
                        _logger.LogDebug("Connection closed");

                        // dismiss all pending connections
                        _pendingRequests.Clear();
                        StateChanged?.Invoke(this, new EventBusStateChangedEventArgs(EventBusState.Closed));

                        // NOTE (2020-10-15, bjorg): timer will trigger a reconnection attempt
                        return;
                    case WebSocketMessageType.Binary:

                        // unsupported message type; ignore it
                        _logger.LogDebug("Binary payload ignored");
                        break;
                    case WebSocketMessageType.Text:

                        // text message payload may require more than one frame to be received fully
                        _messageAccumulator.Write(buffer.Array, 0, received.Count);

                        // check if all bytes of the message have been received
                        if(received.EndOfMessage) {

                            // convert accumulated messages into JSON string
                            var bytes = _messageAccumulator.ToArray();
                            var message = Encoding.UTF8.GetString(bytes);
                            _logger.LogDebug("Received message: {0}", message);

                            // reset message accumulator
                            _messageAccumulator.Position = 0;
                            _messageAccumulator.SetLength(0);

                            // deserialize into a generic JSON document
                            if(!TryParseJsonDocument(bytes, out var response)) {
                                _logger.LogDebug($"Unabled to parse message as JSON document: {message}");
                            } else if(!response.RootElement.TryGetProperty("Action", out var actionProperty)) {
                                _logger.LogDebug($"Missing 'Action' property in message: {message}");
                            } else if(actionProperty.ValueKind != JsonValueKind.String) {
                                _logger.LogDebug($"Wrong type for 'Action' property in message: {message}");
                            } else {
                                var action = actionProperty.GetString();
                                switch(action) {
                                case "Ack":
                                    await ProcessAcknowledgeAsync(JsonSerializer.Deserialize<AcknowledgeAction>(message));
                                    break;
                                case "Event":
                                    await ProcessEventAsync(JsonSerializer.Deserialize<EventAction>(message));
                                    break;
                                case "KeepAlive":
                                    await ProcessKeepAliveAsync(JsonSerializer.Deserialize<KeepAliveAction>(message));
                                    break;
                                default:
                                    _logger.LogDebug($"Unknown message type: {action}");
                                    break;
                                }
                            }
                        }
                        break;
                    }
                } catch(Exception e) {
                    _logger.LogError(e, "Error receiving data on websocket");
                }
            }

            // local functions
            async Task ProcessEventAsync(EventAction action) {
                _logger.LogDebug("Received event matching rules: {0}", string.Join(", ", action.Rules));
                foreach(var rule in action.Rules) {
                    if(_subscriptions.TryGetValue(rule, out var subscription)) {
                        _logger.LogDebug("Dispatching event for: {0}", rule);
                        subscription.Dispatch(action);
                    }
                }
            }

            async Task ProcessAcknowledgeAsync(AcknowledgeAction action) {
                if(!_pendingRequests.TryGetValue(action.RequestId, out var pending)) {
                    _logger.LogDebug("Received stale acknowledgement");
                    return;
                }
                _logger.LogDebug("Received acknowledgement for: {0}", action.RequestId);
                _pendingRequests.Remove(action.RequestId);
                pending.SetResult(action);
            }

            async Task ProcessKeepAliveAsync(KeepAliveAction action)
                => _logger.LogDebug("Received Keep-Alive message");
        }

        private void OnTimer(object _) {

            // attempt to re-open connection when there are enabled subscriptions
            if(
                !_disposalTokenSource.IsCancellationRequested
                && _subscriptions.Values.Any(subscription => subscription.IsEnabled)
            ) {
                _ = OpenConnectionAsync();
            }
        }

        private EventPattern FromEventPatternFrom(string source, Type type)
            => new EventPattern {
                Source = new[] {
                    source ?? throw new ArgumentNullException(nameof(source))
                },
                DetailType = new[] {
                    type.FullName
                },
                Resources = new[] {
                    $"lambdasharp:tier:{_config.DeploymentTier}"
                }
            };

        //--- IAsyncDisposable Members ---
        async ValueTask IAsyncDisposable.DisposeAsync() {
            _logger.LogDebug("Disposing EventBus client");
            if(_disposalTokenSource.IsCancellationRequested) {

                // already disposed; nothing to do
                return;
            }

            // stop timer and wait for any lingering timer operations to finish
            await _timer.DisposeAsync();

            // disconnect socket
            _disposalTokenSource.Cancel();
            _ = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
        }
    }
}
