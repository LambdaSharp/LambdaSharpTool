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
using LambdaSharp.App.Config;
using LambdaSharp.App.EventBus.Actions;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App.EventBus {

    public interface ISubscription : IAsyncDisposable { }

    public class SubscriptErrorEventArgs : EventArgs {

        //--- Constructors ---
        public SubscriptErrorEventArgs(ISubscription subscription, Exception exception) {
            Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        //--- Properties ---
        public ISubscription Subscription { get; }
        public Exception Exception { get; }
    }

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

    public sealed class LambdaSharpEventBusClient : IDisposable {

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

        //--- Constructors ---

        // TODO: add AppClient to streamline sending events via EventBus
        public LambdaSharpEventBusClient(LambdaSharpAppConfig config, ILogger<LambdaSharpEventBusClient> logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timer = new Timer(OnTimer, state: null, dueTime: Frequency, period: Frequency);
        }

        //--- Events ---
        public event EventHandler ConnectionOpened;
        public event EventHandler ConnectionClosed;
        public event EventHandler<SubscriptErrorEventArgs> SubscriptionError;

        //--- Properties ---
        public bool IsConnectionOpen => _webSocket.State == WebSocketState.Open;
        internal ILogger<LambdaSharpEventBusClient> Logger { get; }

        //--- Methods ---
        public Task<ISubscription> SubscribeAsync<T>(string source, Action<string, T> callback)
            => SubscribeAsync<T>(
                typeof(T).FullName,
                new EventPattern {
                    Source = new[] {
                        source ?? throw new ArgumentNullException(nameof(source))
                    },
                    DetailType = new[] {
                        typeof(T).FullName
                    },
                    Resources = new[] {
                        $"lambdasharp:tier:{_config.DeploymentTier}"
                    }
                },
                callback
            );

        public async Task<ISubscription> SubscribeAsync<T>(string name, EventPattern eventPattern, Action<string, T> callback) {
            if(name is null) {
                throw new ArgumentNullException(nameof(name));
            }
            if(eventPattern is null) {
                throw new ArgumentNullException(nameof(eventPattern));
            }
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            Logger.LogInformation("I'm here!");
            Logger.LogDebug("I'm here!");

            // register subscription
            var subscription = new EventBusSubscription(
                name,
                JsonSerializer.Serialize(eventPattern),
                (name, json) => {
                    try {
                        callback?.Invoke(name, JsonSerializer.Deserialize<T>(json));
                    } catch(Exception e) {
                        Logger.LogError(e, "Callback for rule '{0}' failed", name);
                    }
                },
                this
            );
            _subscriptions[name] = subscription;

            // ensure the connection is open
            if(await OpenConnectionAsync()) {

                // kick off subscription, but don't wait for response
                _ = subscription.EnableSubscriptionAsync();
            }
            return subscription;
        }

        public void Dispose() {
            _disposalTokenSource.Cancel();
            _ = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
        }

        internal void Remove(EventBusSubscription subscription) => _subscriptions.Remove(subscription.Name);

        internal async Task SendMessageAsync<T>(T message) where T : AnAction {
            var json = JsonSerializer.Serialize(message);
            Logger.LogDebug("Sending: {0}", json);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, _disposalTokenSource.Token);
        }

        internal async Task SendMessageAndWaitForAcknowledgeAsync<T>(T message) where T : AnAction {

            // register completion action
            var taskCompletionSource = new TaskCompletionSource<AcknowledgeAction>();
            _pendingRequests.Add(message.RequestId, taskCompletionSource);

            // send message to event bus
            await SendMessageAsync(message);

            // wait for response or timeout
            var outcome = await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(10)), taskCompletionSource.Task);
            _pendingRequests.Remove(message.RequestId);
            if(outcome != taskCompletionSource.Task) {
                throw new TimeoutException();
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

        internal void OnSubscriptionError(EventBusSubscription subscription, Exception exception)
            => SubscriptionError?.Invoke(this, new SubscriptErrorEventArgs(subscription, exception));

        private async Task<bool> OpenConnectionAsync() {
            if(_config.EventBusUrl == null) {

                // nothing to do
                Logger.LogDebug("Cannot open WebSocket without event bus URL");
                return false;
            }
            if(_webSocket.State == WebSocketState.Open) {

                // nothing to do
                return true;
            }
            if(_webSocket.State != WebSocketState.Closed) {

                // websocket is in a transitional state; let the timer try to connect later
                Logger.LogDebug("Cannot open WebSocket in current state: {0}", _webSocket.State);
                return false;
            }
            Logger.LogDebug("Connecting to: {0}", _config.EventBusUrl);

            // connect/reconnect to websocket
            try {
                await _webSocket.ConnectAsync(new Uri(_config.EventBusUrl), _disposalTokenSource.Token);
            } catch(WebSocketException e) {
                Logger.LogDebug("Unable to connect WebSocket: {0}", e);
                return false;
            }
            Logger.LogDebug("Connected!");
            _ = ReceiveMessageLoopAsync();

            // register app with event bus
            try {
                await SendMessageAndWaitForAcknowledgeAsync(new HelloAction());
                ConnectionOpened?.Invoke(this, new EventArgs());
                return true;
            } catch(InvalidOperationException) {

                // this exception occurs when the send operation fails
                return false;
            } catch(TimeoutException) {

                // this exception occurs when the ack confirmation takes too long
                _ = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reset", CancellationToken.None);
                return false;
            }
        }

        private async Task ReceiveMessageLoopAsync() {
            var buffer = new ArraySegment<byte>(new byte[32 * 1024]);
            while(!_disposalTokenSource.IsCancellationRequested) {
                var received = await _webSocket.ReceiveAsync(buffer, _disposalTokenSource.Token);
                switch(received.MessageType) {
                case WebSocketMessageType.Close:

                    // dismiss all pending connections
                    _pendingRequests.Clear();
                    ConnectionClosed?.Invoke(this, new EventArgs());

                    // NOTE (2020-10-15, bjorg): timer will trigger a reconnection attempt
                    return;
                case WebSocketMessageType.Binary:

                    // unsupported message type; ignore it
                    Logger.LogDebug("Binary payload ignored");
                    break;
                case WebSocketMessageType.Text:

                    // text message payload may require more than one frame to be received fully
                    _messageAccumulator.Write(buffer.Array, 0, received.Count);

                    // check if all bytes of the message have been received
                    if(received.EndOfMessage) {

                        // convert accumulated messages into JSON string
                        var bytes = _messageAccumulator.ToArray();
                        var message = Encoding.UTF8.GetString(bytes);

                        // reset message accumulator
                        _messageAccumulator.Position = 0;
                        _messageAccumulator.SetLength(0);

                        // deserialize into a generic JSON document
                        if(!TryParseJsonDocument(bytes, out var response)) {
                            Logger.LogDebug($"Unabled to parse message as JSON document: {message}");
                        } else if(!response.RootElement.TryGetProperty("Action", out var actionProperty)) {
                            Logger.LogDebug($"Missing 'Action' property in message: {message}");
                        } else if(actionProperty.ValueKind != JsonValueKind.String) {
                            Logger.LogDebug($"Wrong type for 'Action' property in message: {message}");
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
                                Logger.LogDebug($"Unknown message type: {action}");
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            // local functions
            async Task ProcessEventAsync(EventAction action) {
                Logger.LogDebug("Received event matching rules: {0}", string.Join(", ", action.Rules));
                foreach(var rule in action.Rules) {
                    if(_subscriptions.TryGetValue(rule, out var subscription)) {
                        subscription.Dispatch(action);
                    }
                }
            }

            async Task ProcessAcknowledgeAsync(AcknowledgeAction action) {
                if(!_pendingRequests.TryGetValue(action.RequestId, out var pending)) {
                    Logger.LogInformation("Received stale acknowledgement");
                    return;
                }
                _pendingRequests.Remove(action.RequestId);
                pending.SetResult(action);
            }

            async Task ProcessKeepAliveAsync(KeepAliveAction action)
                => Logger.LogDebug("Received Keep-Alive message");
        }

        private void OnTimer(object _) {

            // attempt to re-open connection when there are enabled subscriptions
            if(
                _subscriptions.Values.Any(subscription => subscription.IsEnabled)
                && !_disposalTokenSource.IsCancellationRequested
            ) {
                _ = OpenConnectionAsync();
            }
        }
    }
}
