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

    public sealed class LambdaSharpEventBusClient {

        //--- Types ---
        private class Subscription : ISubscription {

            //--- Fields ---
            private LambdaSharpEventBusClient _client;
            private bool _disposed;

            //--- Constructors ---
            public Subscription(LambdaSharpEventBusClient client)
                => _client = client ?? throw new ArgumentNullException(nameof(client));

            //--- Properties ---
            public string Name { get; set; }
            public string Pattern { get; set; }
            public Func<string, string, Task> Callback { get; set; }

            //--- Methods ---
            public async ValueTask DisposeAsync() {
                if(!_disposed) {
                    _disposed = true;
                    await _client.UnsubscribeAsync(Name);
                }
            }
        }

        //--- Class Methods ---
        private static bool TryParseJsonDocument(byte[] bytes, out JsonDocument document) {
            var utf8Reader = new Utf8JsonReader(bytes);
            return JsonDocument.TryParseValue(ref utf8Reader, out document);
        }

        //--- Fields ---
        private readonly LambdaSharpAppConfig _config;
        private readonly ILogger _logger;
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>();
        private readonly CancellationTokenSource _disposalTokenSource = new CancellationTokenSource();
        private readonly MemoryStream _messageAccumulator = new MemoryStream();
        private readonly Dictionary<string, TaskCompletionSource<AcknowledgeAction>> _pendingRequests = new Dictionary<string, TaskCompletionSource<AcknowledgeAction>>();

        //--- Constructors ---
        public LambdaSharpEventBusClient(LambdaSharpAppConfig config, ILogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //--- Methods ---
        public async Task<ISubscription> SubscribeAsync<T>(string name, EventPattern eventPattern, Func<T, Task> callback) {
            if(name is null) {
                throw new ArgumentNullException(nameof(name));
            }
            if(eventPattern is null) {
                throw new ArgumentNullException(nameof(eventPattern));
            }
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }

            // TODO: open connection; handle missing 'EventBusUrl'

            // TODO: should we just store the subscription if the connection is closed?

            // register subscription
            var subscription = new Subscription(this) {
                Name = name,
                Pattern = JsonSerializer.Serialize(eventPattern)
            };
            _subscriptions[name] = subscription;

            // send 'Subscribe' request and wait for acknowledge response
            try {
                await SendMessageAndWaitForAcknowledgeAsync(new SubscribeAction {
                    Rule = name,
                    Pattern = subscription.Pattern
                });
            } catch {

                // subscription failed; remove it
                _subscriptions.Remove(name);
                throw;
            }
            return subscription;
        }

        private async Task UnsubscribeAsync(string ruleName) {
            if(_subscriptions.TryGetValue(ruleName, out var subscription)) {
                _subscriptions.Remove(ruleName);

                // only send 'Unsubscribe' request if connection is open
                if(_webSocket.State == WebSocketState.Open) {
                    await SendMessageAsync(new UnsubscribeAction {
                        Rule = ruleName
                    });
                }
            }
        }

        private async Task ReconnectWebSocketAsync() {
            _logger.LogDebug("Connecting to: {0}", _config.EventBusUrl);

            // connect/reconnect to websocket
            await _webSocket.ConnectAsync(new Uri(_config.EventBusUrl), _disposalTokenSource.Token);
            _logger.LogDebug("Connected!");

            // announce app
            await SendMessageAndWaitForAcknowledgeAsync(new HelloAction());

            // resubmit any subscritpion that may have been existing before
            foreach(var subscription in _subscriptions.Values) {
                await SendMessageAsync(new SubscribeAction {
                    Rule = subscription.Name,
                    Pattern = subscription.Pattern
                });
            }
        }

        private async Task SendMessageAndWaitForAcknowledgeAsync<T>(T message) where T : AnAction {

            // register completion action
            var taskCompletionSource = new TaskCompletionSource<AcknowledgeAction>();
            _pendingRequests.Add(message.RequestId, taskCompletionSource);

            // send message to event bus
            await SendMessageAsync(message);

            // wait for response or timeout
            if(await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(10)), taskCompletionSource.Task) != taskCompletionSource.Task) {
                _pendingRequests.Remove(message.RequestId);
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

        private async Task SendMessageAsync<T>(T message) where T : AnAction {
            var json = JsonSerializer.Serialize(message);
            _logger.LogDebug("Sending: {0}", json);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, _disposalTokenSource.Token);
        }

        private async Task ReceiveLoop() {
            var buffer = new ArraySegment<byte>(new byte[32 * 1024]);
            while(!_disposalTokenSource.IsCancellationRequested) {
                var received = await _webSocket.ReceiveAsync(buffer, _disposalTokenSource.Token);
                switch(received.MessageType) {
                case WebSocketMessageType.Close:
                    if(!_disposalTokenSource.IsCancellationRequested) {

                        // TODO: dismiss all pending connections
                        _pendingRequests.Clear();

                        // re-open connection while the app is still running
                        await ReconnectWebSocketAsync();
                    }
                    break;
                case WebSocketMessageType.Binary:

                    // unsupported message type; ignore it
                    break;
                case WebSocketMessageType.Text:

                    // text message payload may require more than one frame to be received fully
                    _messageAccumulator.Write(buffer.Array, 0, received.Count);

                    // check if all bytes of the message have been received
                    if(received.EndOfMessage) {

                        // convert accumulated messages into JSON string
                        var bytes = _messageAccumulator.ToArray();
                        var message = Encoding.UTF8.GetString(bytes);
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
                            case "KeepAlive":
                                await Process(JsonSerializer.Deserialize<KeepAliveAction>(message));
                                break;
                            case "Event":
                                await Process(JsonSerializer.Deserialize<EventAction>(message));
                                break;
                            case "Ack":
                                await Process(JsonSerializer.Deserialize<AcknowledgeAction>(message));
                                break;
                            default:
                                _logger.LogDebug($"Unknown message type: {action}");
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        private async Task Process(EventAction action) {
            _logger.LogDebug("Received event matching rules: {0}", string.Join(", ", action.Rules));
            foreach(var rule in action.Rules) {
                if(_subscriptions.TryGetValue(rule, out var subscription)) {
                    try {
                        await subscription.Callback(rule, action.Event);
                    } catch(Exception e) {
                        _logger.LogError(e, $"Error in handler for rule: {0}", rule);
                    }
                }
            }
        }

        private async Task Process(AcknowledgeAction action) {
            if(!_pendingRequests.TryGetValue(action.RequestId, out var pending)) {
                _logger.LogInformation("Received stale acknowledgement");
                return;
            }
            _pendingRequests.Remove(action.RequestId);
            pending.SetResult(action);
        }

        private async Task Process(KeepAliveAction action)
            => _logger.LogDebug("Received Keep-Alive message");
    }
}
