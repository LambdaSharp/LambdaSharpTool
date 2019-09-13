![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp DynamoDB Stream Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by a DynamoDB stream requires two steps. First, the DynamoDB table must either be created or referenced in the `Items` section. Second, the function must reference the parameter name in its `Sources` section using the `DynamoDB` attribute.

Optionally, the `DynamoDB` attribute can specify the maximum number of messages to read from the DynamoDB stream using `BatchSize`.

```yaml
Module: Sample.DynamoDB
Description: A sample module using Kinesis streams
Items:

  - Resource: Table
    Scope: all
    Description: Description for DynamoDB table
    Type: AWS::DynamoDB::Table
    Allow: Subscribe
    Properties:
      BillingMode: PAY_PER_REQUEST
      AttributeDefinitions:
        - AttributeName: MessageId
          AttributeType: S
      KeySchema:
        - AttributeName: MessageId
          KeyType: HASH
      StreamSpecification:
        StreamViewType: KEYS_ONLY

  - Function: MyFunction
    Description: This function is invoked by a DynamoDB stream
    Memory: 128
    Timeout: 15
    Sources:
      - DynamoDB: Table
        BatchSize: 15
```

## Function Code

DynamoDB stream events can be parsed into a `DynamoDBEvent` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.DynamoDBEvents` nuget package.

```csharp
public class Function : ALambdaFunction<DynamoDBEvent, string> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<string> ProcessMessageAsync(DynamoDBEvent evt) {
        LogInfo($"# Kinesis Records = {evt.Records.Count}");
        for(var i = 0; i < evt.Records.Count; ++i) {
            var record = evt.Records[i];
            LogInfo($"Record #{i}");
            LogInfo($"AwsRegion = {record.AwsRegion}");
            LogInfo($"DynamoDB.ApproximateCreationDateTime = {record.Dynamodb.ApproximateCreationDateTime}");
            LogInfo($"DynamoDB.Keys.Count = {record.Dynamodb.Keys.Count}");
            LogInfo($"DynamoDB.SequenceNumber = {record.Dynamodb.SequenceNumber}");
            LogInfo($"DynamoDB.UserIdentity.PrincipalId = {record.UserIdentity?.PrincipalId}");
            LogInfo($"EventID = {record.EventID}");
            LogInfo($"EventName = {record.EventName}");
            LogInfo($"EventSource = {record.EventSource}");
            LogInfo($"EventSourceArn = {record.EventSourceArn}");
            LogInfo($"EventVersion = {record.EventVersion}");
        }
        return "Ok";
    }
}
```

## Reference

Up to 100 messages can be retrieved at a time from a DynamoDB stream.
