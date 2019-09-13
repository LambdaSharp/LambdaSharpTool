![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Kinesis Stream Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by a Kinesis stream requires two steps. First, the Kinesis stream must either be created or referenced in the `Items` section. Second, the function must reference the parameter name in its `Sources` section using the `Kinesis` attribute.

Optionally, the `Kinesis` attribute can specify the maximum number of messages to read from Kinesis using `BatchSize`.

```yaml
Module: Sample.Kinesis
Description: A sample module using Kinesis streams
Items:

  - Resource: Stream
    Description: Description for Kinesis stream
    Type: AWS::Kinesis::Stream
    Allow: Subscribe
    Properties:
      ShardCount: 1

  - Function: MyFunction
    Description: This function is invoked by a Kinesis stream
    Memory: 128
    Timeout: 15
    Sources:
      - Kinesis: Stream
        BatchSize: 15
```

## Function Code

Kinesis stream events can be parsed into a `KinesisEvent` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.KinesisEvents` nuget package.

```csharp
public class Function : ALambdaFunction<KinesisEvent, string> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<string> ProcessMessageAsync(KinesisEvent evt) {
        LogInfo($"# Kinesis Records = {evt.Records.Count}");
        for(var i = 0; i < evt.Records.Count; ++i) {
            var record = evt.Records[i];
            LogInfo($"Record #{i}");
            LogInfo($"AwsRegion = {record.AwsRegion}");
            LogInfo($"EventId = {record.EventId}");
            LogInfo($"EventName = {record.EventName}");
            LogInfo($"EventSource = {record.EventSource}");
            LogInfo($"EventSourceARN = {record.EventSourceARN}");
            LogInfo($"EventVersion = {record.EventVersion}");
            LogInfo($"InvokeIdentityArn = {record.InvokeIdentityArn}");
            LogInfo($"ApproximateArrivalTimestamp = {record.Kinesis.ApproximateArrivalTimestamp}");
            LogInfo($"Data (length) = {record.Kinesis.Data.Length}");
            LogInfo($"KinesisSchemaVersion = {record.Kinesis.KinesisSchemaVersion}");
            LogInfo($"PartitionKey = {record.Kinesis.PartitionKey}");
            LogInfo($"SequenceNumber = {record.Kinesis.SequenceNumber}");
        }
        return "Ok";
    }
}
```

## Reference

Up to 100 messages can be retrieved at a time from a Kinesis stream.
