# LambdaSharp

This package contains interfaces and classes used for building Lambda functions on AWS.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaFunction

The `ALambdaFunction` base class provides functionality to initialize and process requests/responses using streams.

```csharp
public class Function : ALambdaFunction {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override sealed async Task<Stream> ProcessMessageStreamAsync(Stream stream) {
        var responseStream = new MemoryStream();
        responseStream.Write(Encoding.UTF8.GetBytes("Hello World!"));
        responseStream.Position = 0;
        return responseStream;
    }
}
```

## ALambdaFunction<TRequest, TResponse>

The `ALambdaFunction<TRequest, TResponse>` base class adds functionality to deserialize/serialize the requests/responses with the JSON serializer specified in the constructor.

```csharp
public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

        // TO-DO: add business logic

        return new FunctionResponse();
    }
}
```

## JSON Serializers

This packages contains two serializers: `LambdaSystemTextJsonSerializer` and `LambdaSourceGeneratorJsonSerializer`.

`LambdaSystemTextJsonSerializer` is the recommended JSON serializer. `LambdaSourceGeneratorJsonSerializer` should be considered experimental as JSON source generators have [many limitations](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation-modes?pivots=dotnet-6-0#serialization-optimization-mode).

### LambdaSystemTextJsonSerializer

The construtor takes an optional callback to customize the JSON serialization options.

```csharp
new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer(options => {
    options.Converters.Add(new MyJsonConverter());
});
```

This following options are used by default:
1. `DefaultIgnoreCondition`: `JsonIgnoreCondition.WhenWritingNull`
1. `IncludeFields`: `true`
1. `NumberHandling`: `JsonNumberHandling.AllowReadingFromString`
1. `Encoder`: `System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping`


### LambdaSourceGeneratorJsonSerializer

This class uses JSON serialization source generators, which avoids using reflection during runtime by creating the serialization/deserialization logic during compile time instead. Review [current limitations](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation-modes?pivots=dotnet-6-0#serialization-optimization-mode) of this approach and provide sufficient test coverage to ensure expected behavior.


First declare a partial class to hold the serialization/deserialization logic for the specified types.
```csharp
[JsonSerializable(typeof(FunctionRequest))]
[JsonSerializable(typeof(FunctionResponse))]
public partial class FunctionJsonSerializerContext : JsonSerializerContext { }
```

Then pass in the default instance to the constructor.
```csharp
new LambdaSharp.Serialization.LambdaSourceGeneratorJsonSerializer(FunctionJsonSerializerContext.Default)
```

### LambdaSharp.Serialization.NewtonsoftJson (Additional Package)

The [LambdaSharp.Serialization.NewtonsoftJson](https://www.nuget.org/packages/LambdaSharp.Serialization.NewtonsoftJson/) package is provided as a separate package. It is only recommended for legacy projects that require compatibility with JSON.NET annotations.

The construtor takes an optional callback to customize the JSON.NET serialization settings.

```csharp
new LambdaSharp.Serialization.NewtonsoftJson(settings => {
    settings.NullValueHandling = NullValueHandling.Include;
})
```

## Logging

The `ALambdaFunction` base classes includes logging methods to make it easy to report information to CloudWatch Logs and optionally to an error aggregator, such as Rollbar.

* `LogDebug()`: Log a debugging message. This message will only appear in the log when debug logging is enabled and will not be forwarded to an error aggregator.
* `LogInfo()`: Log an informational message. This message will only appear in the log and not be forwarded to an error aggregator.
* `LogWarn()`: Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogError()`: Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogErrorAsInfo()`: Log an exception as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
* `LogErrorAsWarning()`: Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogFatal()`: Log an exception with a custom message as a fatal error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogMetric()`: Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.

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
