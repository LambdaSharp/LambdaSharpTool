![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp SNS Topic Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by an SNS topics requires two steps. First, the SNS topic must either be created or referenced in the `Items` section. Second, the function must reference the parameter name in its `Sources` section using the `Topic` attribute.

Lambda functions require the `sns:Subscribe` permission on the SNS topic. Either request it explicitly or use a [resource permission shorthand](../src/LambdaSharp.Tool/Resources/IAM-Mappings.yml) instead.

```yaml
Module: Sample.Sns
Description: A sample module using an SNS topic
Items:

  - Resource: MyTopic
    Description: An SNS topic used to invoke the function.
    Type: AWS::SNS::Topic
    Allow: Subscribe

  - Function: MyFunction
    Description: This function is invoked by an SNS topic
    Memory: 128
    Timeout: 30
    Sources:
      - Topic: MyTopic
        Filters:
          source:
            - shopping-cart
```

## Function Code

An SNS topic invocation can be easily handled by the `ALambdaTopicFunction<T>` base class. In addition to deserializing the SNS message, the base class also deserializes the contained message body into an instance of the provided type.

```csharp
public class MyMessage {

    //--- Properties ---
    public string Text { get; set; }
}

public class Function : ALambdaTopicFunction<MyMessage> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task ProcessMessageAsync(MyMessage message) {
        LogInfo($"Message.Text = {message.Text}");
        LogInfo($"CurrentRecord.Message = {CurrentRecord.Message}");
        LogInfo($"CurrentRecord.MessageAttributes = {CurrentRecord.MessageAttributes}");
        foreach(var attribute in CurrentRecord.MessageAttributes) {
            LogInfo($"CurrentRecord.MessageAttributes.{attribute.Key} = {attribute.Value}");
        }
        LogInfo($"CurrentRecord.MessageId = {CurrentRecord.MessageId}");
        LogInfo($"CurrentRecord.Signature = {CurrentRecord.Signature}");
        LogInfo($"CurrentRecord.SignatureVersion = {CurrentRecord.SignatureVersion}");
        LogInfo($"CurrentRecord.SigningCertUrl = {CurrentRecord.SigningCertUrl}");
        LogInfo($"CurrentRecord.Subject = {CurrentRecord.Subject}");
        LogInfo($"CurrentRecord.Timestamp = {CurrentRecord.Timestamp}");
        LogInfo($"CurrentRecord.TopicArn = {CurrentRecord.TopicArn}");
        LogInfo($"CurrentRecord.Type = {CurrentRecord.Type}");
        LogInfo($"CurrentRecord.UnsubscribeUrl = {CurrentRecord.UnsubscribeUrl}");
    }
}
```

## Reference

The LambdaSharp CLI automatically creates the required permissions to allow the subscribed SNS topic to invoke the Lambda function.

Thw following YAML shows the permission granted to the AWS SNS service.

```yaml
FunctionTopicSnsPermission:
  Type: AWS::Lambda::Permission
  Properties:
    Action: lambda:InvokeFunction
    FunctionName: !GetAtt Function.Arn
    Principal: sns.amazonaws.com
    SourceArn: !Ref Topic
```
