![λ#](../../Docs/images/LambdaSharpLogo.png)

# LambdaSharp CloudWatch Event Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by a CloudWatch Event is straightforward. Simply add an `EventBus` source and define an event `Pattern` to listen to.

Learn more about [event patterns](https://docs.aws.amazon.com/eventbridge/latest/userguide/filtering-examples-structure.html) on the official AWS documentation site.

```yaml
Module: Sample.Event
Description: Sample module to demonstrate sending and receiving events
Items:

  - Function: ReceiverFunction
    Description: Lambda function for receiving CloudWatch events
    Memory: 256
    Timeout: 30
    Sources:
      - EventBus: default
        Pattern:
          source:
            - MySample
          detail-type:
            - MyEvent
          resources:
            - !Sub "lambdasharp:tier:${Deployment::Tier}"
```

## Function Code

CloudWatch Event messages can parsed into a `CloudWatchEvent<T>` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.CloudWatchEvents` nuget package.

```csharp
public sealed class Function : ALambdaFunction<CloudWatchEvent<EventDetails>, FunctionResponse> {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) { }

    public override async Task<FunctionResponse> ProcessMessageAsync(CloudWatchEvent<EventDetails> request) {
        LogInfo($"Version = {request.Version}");
        LogInfo($"Account = {request.Account}");
        LogInfo($"Region = {request.Region}");
        LogInfo($"Detail = {LambdaSerializer.Serialize(request.Detail)}");
        LogInfo($"DetailType = {request.DetailType}");
        LogInfo($"Source = {request.Source}");
        LogInfo($"Time = {request.Time}");
        LogInfo($"Id = {request.Id}");
        LogInfo($"Resources = [{string.Join(",", request.Resources ?? Enumerable.Empty<string>())}]");
        return new FunctionResponse();
    }
}
```
