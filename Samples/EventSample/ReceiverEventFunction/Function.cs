/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace Sample.Event.ReceiverEventFunction;

using LambdaSharp;
using LambdaSharp.EventBridge;

public class EventDetails {

    //--- Properties ---
    public string? Message { get; set; }
}

public sealed class Function : ALambdaEventFunction<EventDetails> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) { }

    public override async Task ProcessEventAsync(EventDetails details) {
        var request = CurrentEvent;
        LogInfo($"Version = {request.Version}");
        LogInfo($"Account = {request.Account}");
        LogInfo($"Region = {request.Region}");
        LogInfo($"Detail = {LambdaSerializer.Serialize(details)}");
        LogInfo($"DetailType = {request.DetailType}");
        LogInfo($"Source = {request.Source}");
        LogInfo($"Time = {request.Time}");
        LogInfo($"Id = {request.Id}");
        LogInfo($"Resources = [{string.Join(",", request.Resources ?? Enumerable.Empty<string>())}]");
        LogInfo($"Latency = {DateTime.UtcNow - request.Time}");
    }
}
