![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp SQS Queue Failure Handling Sample

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is triggered by an SQS event and only re-tries failed messages that are marked as retriable, sends non-retriable messages
directly to the dead-letter-queue, and prevents failure for an entire batch of messages is not possible with lambda out of the box.
In order to have a more efficient way to process the sqs messages with lambda use the `ALambdaQueueFunction`.

> **NOTE**: Beware the Lambda function timeout must be less than the SQS message visibility timeout, otherwise the deployment will fail.

```yaml
Module: Sample.SqsHandler
Description: Module description
Items:

  - Resource: SqsQueue
    Scope: all
    Type: AWS::SQS::Queue
    Allow: Receive,Send
    Properties:
      VisibilityTimeout: 60
      RedrivePolicy:
        # This is a sample DLQ to demonstrate how failed messages are
        # processed differently depending on the exception type
        # Use the LambdaSharp DLQ in your code !Ref Module::DeadLetterQueue
        deadLetterTargetArn: !GetAtt SampleDLQ.Arn
        maxReceiveCount: 3

  - Resource: SampleDLQ
    Type: AWS::SQS::Queue

  - Function: SqsProducer
    Description: This function produces numbers that are sent as messages to the SQS queue
    Memory: 256
    Timeout: 30

  - Function: SqsConsumer
    Description: Consumes messages form SQS queue
    Memory: 256
    Timeout: 10
    Sources:

      - Sqs: SqsQueue
        BatchSize: 10
```

## Function Code
This sample code contains two functions:

1. Producer
1. Consumer

### Producer
The producer generates numbers from 0 to N, where N is a number that you pass to the function. These numbers are then sent to an SQS queue.

#### Producer code

```csharp
public class Function : ALambdaFunction<int, string> {

    //--- Fields ---
    private string _sqsQueueUrl;
    private IAmazonSQS _sqsClient;

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {
        _sqsQueueUrl = config.ReadSqsQueueUrl("SqsQueue");
        _sqsClient = new AmazonSQSClient();
    }

    public override async Task<string> ProcessMessageAsync(int request) {
        LogInfo($"generating {request:N0} messages");
        await Task.WhenAll(Enumerable.Range(0, request).Select(i => _sqsClient.SendMessageAsync(_sqsQueueUrl, i.ToString())));
        return "OK";
    }
}
```

### Consumer
The consumer gets messages from the SQS queue and "process" them. For the sake of this example all numbers modulo 10 will generate a re-triable exception,
and all numbers modulo 5 will generate an general exception (modulo 10 is checked first to avoid overlaps) all other "processed".

The consumer extends from the LambdaSharp `ALambdaSqsFunction` class

#### Consumer Code

```csharp
public class Function : ALambdaQueueFunction<int> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task ProcessMessageAsync(int message) {
        LogInfo($"received: {message}");
        if(message % 10 == 0) {
            LogWarn("Retriable Error");
            throw new LambdaRetriableException("Retriable Error!");
        }
        if(message % 5 == 0) {
            LogWarn("Non Retriable Error");
            throw new Exception("Non Retriable Error");
        }
    }
}
```

Despite how many messages are in the batch, LambdaSharp will give you ony a single message at a time.

LambdaSharp will send non-retriable errors directly to the LambdaSharp DQL. All re-triable errors will be retried up to 3 times and then sent to the `SampleDQL`.

## Results

If you run the producer code, you will see that after a couple minutes all numbers multiples of 10 are in the LambdaShapr DQL and all numbers terminated in 5 will end up in the LambdaSharp DQL.

## Reference

Up to 10 messages can be retrieved at a time from an SQS queue. Depending on the throughput needs, AWS Lambda will instantiate more function invocations to process all messages in the queue. Note that the Lambda function `Timeout` attribute and SQS queue `VisibilityTimeout` property are related. The CloudFormation stack deployment fails when the Lambda timeout is greater than the queue visibility timeout.