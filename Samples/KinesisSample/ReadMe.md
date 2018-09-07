![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Kinesis Stream Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

Creating a function that is invoked by a Kinesis stream requires two steps. First, the Kinesis stream must either be created or referenced in the `Parameters` section. Second, the function must reference the parameter name in its `Sources` section using the `Kinesis` attribute.

Optionally, the `Kinesis` attribute can specify the maximum number of messages to read from Kinesis using `BatchSize`.

```yaml
Name: KinesisSample

Description: A sample module using Kinesis streams

Parameters:

  - Name: Stream
    Description: Description for Kinesis stream
    Resource:
      Type: AWS::Kinesis::Stream
      Properties:
        ShardCount: 1

Functions:

  - Name: MyFunction
    Description: This function is invoked by a Kinesis stream
    Memory: 128
    Timeout: 15
    Sources:
      - Kinesis: Stream
        BatchSize: 15
```

## Function Code

SQS events can be parsed into a `SQSEvent` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.SQSEvents` nuget package.

```csharp
public class Function : ALambdaFunction<KinesisEvent, string> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<string> ProcessMessageAsync(KinesisEvent evt, ILambdaContext context) {
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
