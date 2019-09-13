![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Alexa Skill Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by an [Alexa Skill](https://developer.amazon.com/alexa-skills-kit) requires two steps. First, an Alexa Skill must be created with an [Amazon Developer account](https://developer.amazon.com/). Second, the function must have the `Alexa` attribute in its `Sources` section.

Optionally, the `Alexa` attribute can specify an Alexa Skill ID to restrict invocation to a specific Alexa Skill.

```yaml
Module: Sample.Alexa
Description: A sample module using an Alexa skill
Items:

  - Parameter: AlexaSkillID
    Description: Alexa Skill ID
    Default: "*"

  - Function: MyFunction
    Description: This function is invoked by an Alexa Skill
    Memory: 128
    Timeout: 30
    Sources:
      - Alexa: !Ref AlexaSkillID
```

## Function Code

Alexa Skill requests can be parsed using the [Alexa.NET](https://github.com/timheuer/alexa-skills-dotnet) library by Tim Heuer and by deriving the function from the `ALambdaFunction<T>` base class.

```csharp
public class Function : ALambdaFunction<SkillRequest, SkillResponse> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<SkillResponse> ProcessMessageAsync(SkillRequest skill) {
        switch(skill.Request) {
        case LaunchRequest launch:
            LogInfo("Launch");
            break;
        case IntentRequest intent:
            LogInfo("Intent");
            LogInfo($"Intent.Name = {intent.Intent.Name}");
            break;
        case SessionEndedRequest ended:
            LogInfo("Session ended");
            return ResponseBuilder.Empty();
        }
        return ResponseBuilder.Tell(new PlainTextOutputSpeech {
            Text = "Hi!"
        });
    }
}
```

## Reference

The LambdaSharp CLI automatically creates the required permissions to allow the Alexa skill to invoke the Lambda function. The `EventSourceToken` attribute is omitted if the `Alexa` attribute is set to `"*"` in the module definition.

Thw following YAML shows the permission granted to the Alexa service.

```yaml
FunctionAlexaPermission:
  Type: AWS::Lambda::Permission
  Properties:
    Action: lambda:InvokeFunction
    EventSourceToken: !Ref AlexaSkillID
    FunctionName: !GetAtt Function.Arn
    Principal: alexa-appkit.amazon.com
```
