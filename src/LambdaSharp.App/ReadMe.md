# LambdaSharp.App

This package contains interfaces and classes used for building Blazor WebAssembly applications that connects with AWS services for logging and events.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaComponent

The `ALambdaComponent` base classes includes logging methods to make it easy to report information to CloudWatch Logs and optionally to an error aggregator, such as Rollbar. Each application instance creates a new CloudWatch Log stream. All logging is directed to the same stream until the application is reloaded.

* `LogDebug()`: Log a debugging message. This message will only appear in the log when debug logging is enabled and will not be forwarded to an error aggregator.
* `LogInfo()`: Log an informational message. This message will only appear in the log and not be forwarded to an error aggregator.
* `LogWarn()`: Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogError()`: Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogErrorAsInfo()`: Log an exception as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
* `LogErrorAsWarning()`: Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogFatal()`: Log an exception with a custom message as a fatal error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogMetric()`: Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.

## LambdaSharpEventBusClient

The `LambdaSharpEventBusClient` class provides a client to subscribe to a pre-configured subset of EventBridge events.

The following code subscribe a handler to EventBridge events that have `Sample.BlazorEventsSample::MyBlazorApp` as their source.
```csharp
protected override async Task OnInitializedAsync() {
    base.OnInitialized();

    // create subscription
    EventBus.SubscribeTo<TodoItem>("Sample.BlazorEventsSample::MyBlazorApp", TodoItemEvent);
}

private void TodoItemEvent(TodoItem todoEvent) {

    // TO-DO: add logic to handle EventBridge event

    // update user interface
    StateHasChanged();
}
```

## License

> Copyright (c) 2018-2022 LambdaSharp (λ#)
>
> Licensed under the Apache License, Version 2.0 (the "License");
> you may not use this file except in compliance with the License.
> You may obtain a copy of the License at
>
> http://www.apache.org/licenses/LICENSE-2.0
>
> Unless required by applicable law or agreed to in writing, software
> distributed under the License is distributed on an "AS IS" BASIS,
> WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
> See the License for the specific language governing permissions and
> limitations under the License.
