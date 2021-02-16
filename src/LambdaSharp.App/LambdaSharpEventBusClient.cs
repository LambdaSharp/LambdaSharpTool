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
using LambdaSharp.App.EventBus;
using LambdaSharp.App.EventBus.Actions;
using LambdaSharp.App.EventBus.Exceptions;
using LambdaSharp.App.EventBus.Internal;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App {

    /// <summary>
    /// The <see cref="LambdaSharpEventBusClient"/> class is used to subscribing to events on the LambdaSharp App EventBus.
    /// </summary>
    public sealed class LambdaSharpEventBusClient : IAsyncDisposable {

        //--- Types ---
        private class ConnectionHeader {

            //--- Properties ---
            public string Host { get; set; }
            public string ApiKey { get; set; }
            public string Id { get; set; }
        }

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

        /// <summary>
        /// Initializes a new <see cref="LambdaSharpEventBusClient"/> instance.
        /// </summary>
        /// <param name="config">A <see cref="LambdaSharpAppConfig"/> instance.</param>
        /// <param name="logger">A <see cref="Microsoft.Extensions.Logging.ILogger"/> instance.</param>
        public LambdaSharpEventBusClient(LambdaSharpAppConfig config, ILogger<LambdaSharpEventBusClient> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _timer = new Timer(OnTimer, state: null, dueTime: Frequency, period: Frequency);

            // issue a warning if the EventBus client is being initialized, but there is no URL for the websocket
            if(_config.EventBusUrl == null) {
                _logger.LogWarning("EventBus URL missing. Ensure app is subscribed to at least one event source.");
            }
        }

        //--- Events ---

        /// <summary>
        /// The <see cref="StateChanged"/> event is raised when the connection state of the LambdaSharp App EventBus changes.
        /// </summary>
        public event EventHandler<EventBusStateChangedEventArgs> StateChanged;

        /// <summary>
        /// The <see cref="SubscriptionError"/> event is raised when the LambdaSharp App EventBus receives a subscription error message.
        /// </summary>
        public event EventHandler<EventBusSubscriptErrorEventArgs> SubscriptionError;

        //--- Properties ---
        private bool IsConnectionOpen => _webSocket.State == WebSocketState.Open;

        //--- Methods ---

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Func{CloudWatchEvent{T},Task})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Func<CloudWatchEvent<T>, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(IEventBusSubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(@event);
            }
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Func{T,Task})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Func<T, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(IEventBusSubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(@event.Detail);
            }
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Action{CloudWatchEvent{T}})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Action<CloudWatchEvent<T>> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                (_, @event) => callback(@event)
            );
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Action{T})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Action<T> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                (_, @event) => callback(@event.Detail)
            );
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Func{IEventBusSubscription,CloudWatchEvent{T},Task})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Func<IEventBusSubscription, CloudWatchEvent<T>, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(IEventBusSubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(subscription, @event);
            }
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Func{IEventBusSubscription,T,Task})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Func<IEventBusSubscription, T, Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                Callback
            );

            // local functions
            void Callback(IEventBusSubscription subscription, CloudWatchEvent<T> @event) {
                _ = callback(subscription, @event.Detail);
            }
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Action{IEventBusSubscription,CloudWatchEvent{T}})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Action<IEventBusSubscription, CloudWatchEvent<T>> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                callback
            );
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,Action{IEventBusSubscription,T})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="source">The name of the event source.</param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string source, Action<IEventBusSubscription, T> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return SubscribeTo<T>(
                typeof(T).FullName,
                FromEventPatternFrom(source, typeof(T)),
                (subscription, @event) => callback(subscription, @event.Detail)
            );
        }

        /// <summary>
        /// The <see cref="SubscribeTo{T}(string,LambdaSharp.App.EventBus.EventBusPattern,Action{IEventBusSubscription,CloudWatchEvent{T}})"/> method creates a subscription for the specified source and type.
        /// </summary>
        /// <param name="name">The name of the LambdaSharp App EventBus subscription.</param>
        /// <param name="eventPattern"></param>
        /// <param name="callback">The callback to invoke when a matching event is received.</param>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <returns>The subscription instance.</returns>
        public IEventBusSubscription SubscribeTo<T>(string name, EventBusPattern eventPattern, Action<IEventBusSubscription, CloudWatchEvent<T>> callback) {
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

                        // deserialize received event
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
                throw new AcknowledgeEventBusException(acknowledgeAction.Message);
            default:
                throw new UnexpectedEventBusException($"Unrecognized status message: {acknowledgeAction.Status}");
            }
        }

        internal async Task EnableSubscriptionAsync(EventBusSubscription subscription) {
            _logger.LogDebug("Enabling subscription: {0}", subscription.Name);
            if(IsConnectionOpen) {
                try {
                    await SendMessageAndWaitForAcknowledgeAsync(new SubscribeAction {
                        Rule = subscription.Name,
                        Pattern = subscription.Pattern
                    });
                } catch(InvalidOperationException) {

                    // websocket is not connected; ignore error
                } catch(Exception e) {
                    _logger.LogDebug("Activating subscription FAILED: {0}\n{1}", subscription.Name, e);
                    subscription.Status = EventBusSubscriptionStatus.Error;

                    // subscription failed
                    SubscriptionError?.Invoke(this, new EventBusSubscriptErrorEventArgs(subscription, e));
                }
            }
        }

        internal async Task DisableSubscriptionAsync(EventBusSubscription subscription) {
            _logger.LogDebug("Disabling subscription: {0}", subscription.Name);
            if(_subscriptions.Remove(subscription.Name) && IsConnectionOpen) {
                try {
                    await SendMessageAsync(new UnsubscribeAction {
                        Rule = subscription.Name
                    });
                } catch(InvalidOperationException) {

                    // websocket is not connected; ignore error
                } catch(Exception e) {

                    // nothing to do
                    _logger.LogDebug("Deactivating subscription FAILED: {0}\n{1}", subscription.Name, e);
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
            if(
                (_webSocket.State != WebSocketState.Closed)
                && (_webSocket.State != WebSocketState.None)
            ) {

                // websocket is in a transitional state; let the timer try to connect later
                _logger.LogDebug("Cannot open WebSocket in current state: {0}", _webSocket.State);
                return;
            }
            var header = new ConnectionHeader {
                Host = new Uri(_config.EventBusUrl).Host,
                ApiKey = _config.GetEventBusApiKey(),
                Id = _config.AppInstanceId
            };
            var headerBase64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
            var eventBusUri = new Uri($"{_config.EventBusUrl}?header={headerBase64Json}");
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
                StateChanged?.Invoke(this, new EventBusStateChangedEventArgs(open: true));
            } catch(InvalidOperationException) {

                // this exception occurs when the send operation fails because the socket is closed; nothing to do
            } catch(TimeoutException) {

                // this exception occurs when the ack confirmation takes too long
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reset", CancellationToken.None);
            }
        }

        private async Task ReceiveMessageLoopAsync() {
            var buffer = new ArraySegment<byte>(new byte[32 * 1024]);
            while(!_disposalTokenSource.IsCancellationRequested) {
                try {
                    _logger.LogDebug("Waiting for websocket data");
                    var received = await _webSocket.ReceiveAsync(buffer, _disposalTokenSource.Token);
                    _logger.LogDebug("Received: {0}", received.MessageType);
                    switch(received.MessageType) {
                    case WebSocketMessageType.Close:
                        _logger.LogDebug("Connection closed");

                        // dismiss all pending connections
                        _pendingRequests.Clear();
                        StateChanged?.Invoke(this, new EventBusStateChangedEventArgs(open: false));

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
                                    _logger.LogDebug($"Received unknown message type: {action ?? "<null>"}");
                                    break;
                                }
                            }
                        } else {
                            _logger.LogDebug("Buffering incomplete message ({0:N0} bytes received, {1:N0} total bytes accumulated)", received.Count, _messageAccumulator.Position);
                        }
                        break;
                    }
                } catch(Exception e) {
                    _logger.LogError(e, "Receiving data on websocket FAILED");
                }
            }

            // local functions
            async Task ProcessEventAsync(EventAction action) {
                if(action.Rules.Count == 1) {
                    _logger.LogDebug("Received event matching rules: ['{0}']", action.Rules[0]);
                } else {
                    _logger.LogDebug("Received event matching rules: [{0}]", string.Join(", ", action.Rules.Select(rule => $"'{rule}'")));
                }
                foreach(var rule in action.Rules) {
                    if(_subscriptions.TryGetValue(rule, out var subscription)) {
                        _logger.LogDebug("Dispatching event for: {0}", rule);
                        subscription.Dispatch(action);
                    }
                }
            }

            async Task ProcessAcknowledgeAsync(AcknowledgeAction action) {
                if(action.RequestId != null) {
                    if(!_pendingRequests.TryGetValue(action.RequestId, out var pending)) {
                        _logger.LogDebug("Received stale acknowledgement");
                        return;
                    }
                    _logger.LogDebug("Received acknowledgement for: {0}", action.RequestId);
                    _pendingRequests.Remove(action.RequestId);
                    pending.SetResult(action);
                } else {
                    _logger.LogDebug("Received acknowledgement without RequestId: Status='{0}', Message='{1}'", action.Status ?? "<null>", action.Message ?? "<null>");
                }
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

        private EventBusPattern FromEventPatternFrom(string source, Type type)
            => new EventBusPattern {
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
