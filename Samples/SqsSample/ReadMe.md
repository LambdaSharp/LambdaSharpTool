![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp SQS Queue Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by an SQS queue requires two steps. First, the SQS topic must either be created or referenced in the `Items` section. Second, the function must reference the parameter name in its `Sources` section using the `Sqs` attribute.

Optionally, the `Sqs` attribute can specify the maximum number of messages to read from SQS.

> **NOTE**: Beware the Lambda function timeout must be less than the SQS message visibility timeout, otherwise the deployment will fail.

```yaml
Module: Sample.Sqs
Description: A sample module using SQS queues
Items:

  - Resource: MyFirstQueue
    Description: A sample SQS queue
    Type: AWS::SQS::Queue
    Allow: Receive

  - Resource: MySecondQueue
    Description: A sample SQS queue
    Type: AWS::SQS::Queue
    Allow: Receive

  - Function: MyFunction
    Description: This function is invoked by an SQS queue
    Memory: 128
    Timeout: 15
    Sources:
      - Sqs: MyFirstQueue
      - Sqs: MySecondQueue
        BatchSize: 1
```

## Function Code

SQS events are parsed into individual, deserialized messages by the `ALambdaQueueFunction<T>` base class.

```csharp
public class Function : ALambdaQueueFunction<MyMessage> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task ProcessMessageAsync(MyMessage message) {
        LogInfo($"Message.Text = {message.Text}");
        foreach(var attribute in CurrentRecord.Attributes) {
            LogInfo($"CurrentRecord.Attributes.{attribute.Key} = {attribute.Value}");
        }
        LogInfo($"CurrentRecord.Body = {CurrentRecord.Body}");
        LogInfo($"CurrentRecord.EventSource = {CurrentRecord.EventSource}");
        LogInfo($"CurrentRecord.EventSourceArn = {CurrentRecord.EventSourceArn}");
        LogInfo($"CurrentRecord.Md5OfBody = {CurrentRecord.Md5OfBody}");
        LogInfo($"CurrentRecord.Md5OfMessageAttributes = {CurrentRecord.Md5OfMessageAttributes}");
        foreach(var attribute in CurrentRecord.MessageAttributes) {
            LogInfo($"CurrentRecord.MessageAttributes.{attribute.Key} = {attribute.Value}");
        }
        LogInfo($"CurrentRecord.MessageId = {CurrentRecord.MessageId}");
        LogInfo($"CurrentRecord.ReceiptHandle = {CurrentRecord.ReceiptHandle}");
    }
}
}
```

## Reference

Up to 10 messages can be retrieved at a time from an SQS queue. Depending on the throughput needs, AWS Lambda will instantiate more function invocations to process all messages in the queue. Note that the Lambda function `Timeout` attribute and SQS queue `VisibilityTimeout` property are related. The CloudFormation stack deployment fails when the Lambda timeout is greater than the queue visibility timeout.

The `ALambdaQueueFunction<T>` base class handles partial success by deleting them from the queue upon success. This ensures that only failed messages are retried.
