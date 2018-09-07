![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp DynamoDB Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

Creating a function that is invoked by a DynamoDB stream requires two steps. First, the DynamoDB table must either be created or referenced in the `Parameters` section. Second, the function must reference the parameter name in its `Sources` section using the `DynamoDB` attribute.

Optionally, the `DynamoDB` attribute can specify the maximum number of messages to read from the DynamoDB stream using `BatchSize`.

```yaml
Name: DynamoDBSample

Description: A sample module using Kinesis streams

Parameters:


  - Name: Table
    Description: Description for DynamoDB table
    Resource:
      Type: AWS::DynamoDB::Table
      Allow: Subscribe
      Properties:
        AttributeDefinitions:
          - AttributeName: MessageId
            AttributeType: S
        KeySchema:
          - AttributeName: MessageId
            KeyType: HASH
        ProvisionedThroughput:
          ReadCapacityUnits: 1
          WriteCapacityUnits: 1

Functions:

  - Name: MyFunction
    Description: This function is invoked by a DynamoDB stream
    Memory: 128
    Timeout: 15
    Sources:
      - DynamoDB: Table
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

Up to 100 messages can be retrieved at a time from a DynamoDB stream.
