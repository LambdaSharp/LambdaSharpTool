![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp SQS Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

Creating a function that is invoked by an SQS queue requires two steps. First, the SQS topic must either be created or referenced in the `Parameters` section. Second, the function must reference the parameter name in its `Sources` section using the `Sqs` attribute.

Optionally, the `Sqs` attribute can specify the maximum number of messages to read from SQS.

Beware the Lambda function timeout must be less than the SQS message visibility timeout, otherwise the deployment will fail.

```yaml
Name: SqsSample

Description: A sample module using SQS queues

Parameters:

  - Name: MyFirstQueue
    Description: A sample SQS queue
    Resource:
      Type: AWS::SQS::Queue
      Allow: Receive

  - Name: MySecondQueue
    Description: A sample SQS queue
    Resource:
      Type: AWS::SQS::Queue
      Allow: Receive

Functions:

  - Name: MyFunction
    Description: This function is invoked by a SQS queue
    Memory: 128
    Timeout: 15
    Sources:
      - Sqs: MyFirstQueue
      - Sqs: MySecondQueue
        BatchSize: 1
```

## Function Code

SQS events can be parsed into a `SQSEvent` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.SQSEvents` nuget package.

```csharp
public class Function : ALambdaFunction<SQSEvent> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<object> ProcessMessageAsync(SQSEvent evt, ILambdaContext context) {
        LogInfo($"# SQS Records = {evt.Records.Count}");
        for(var i = 0; i < evt.Records.Count; ++i) {
            var record = evt.Records[i];
            LogInfo($"Body = {record.Body}");
            LogInfo($"EventSource = {record.EventSource}");
            LogInfo($"EventSourceArn = {record.EventSourceArn}");
            LogInfo($"Md5OfBody = {record.Md5OfBody}");
            LogInfo($"Md5OfMessageAttributes = {record.Md5OfMessageAttributes}");
            LogInfo($"MessageId = {record.MessageId}");
            LogInfo($"ReceiptHandle = {record.ReceiptHandle}");
            foreach(var attribute in record.Attributes) {
                LogInfo($"Attributes.{attribute.Key} = {attribute.Value}");
            }
            foreach(var attribute in record.MessageAttributes) {
                LogInfo($"MessageAttributes.{attribute.Key} = {attribute.Value}");
            }
        }
        return "Ok";
    }
}
```

## Reference

Up to 10 messages can be retrieved at a time from an SQS queue. Depending on the throughput needs, AWS Lambda will instantiate more function invocations to process all messages in the queue. Note that the Lambda function `Timeout` attribute and SQS queue `VisibilityTimeout` property are related. The CloudFormation stack deployment fails when the Lambda timeout is greater than the queue visibility timeout.