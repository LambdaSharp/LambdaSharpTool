![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp SNS Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

Creating a function that is invoked by an SNS topics requires two steps. First, the SNS topic must either be created or referenced in the `Parameters` section. Second, the function must reference the parameter name in its `Sources` section using the `Topic` attribute.

Lambda functions require the `sns:Subscribe` permission on the SNS topic. Either request it explicitly or use a [resource permission shorthand](../src/MindTouch.LambdaSharp.Tool/Resources/IAM-Mappings.yml) instead.

```yaml
Name: SnsSample

Description: A sample module using an SNS topic

Parameters:

  - Name: MyTopic
    Description: An SNS topic used to invoke the function.
    Resource:
      Type: AWS::SNS::Topic
      Allow: Subscribe

Functions:

  - Name: MyFunction
    Description: This function is invoked by an SNS topic
    Memory: 128
    Timeout: 30
    Sources:
      - Topic: MyTopic
```

## Function Code

An SNS topic invocation can be easily handled by the `ALambdaEventFunction<T>` base class. In addition to deserializing the SNS message, the base class also deserializes the contained message body into an instance of the provided type.

```csharp
public class MyMessage {

    //--- Properties ---
    public string Text { get; set; }
}

public class Function : ALambdaEventFunction<MyMessage> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config) 
        => Task.CompletedTask;

    public override Task ProcessMessageAsync(MyMessage message, ILambdaContext context) {
        LogInfo(message.Text);
        return Task.CompletedTask;
    }
}
```

## Reference

The λ# tool automatically creates the required permissions to allow the subscribed SNS topic to invoke the Lambda function.

Thw following YAML shows the permission granted to the AWS SNS service.

```yaml
FunctionTopicSnsPermission:
  Type: AWS::Lambda::Permission
  Properties:
    Action: lambda:InvokeFunction
    FunctionName:
      Fn::GetAtt: !GetAtt Function.Arn
    Principal: sns.amazonaws.com
    SourceArn: !Ref Topic
```
