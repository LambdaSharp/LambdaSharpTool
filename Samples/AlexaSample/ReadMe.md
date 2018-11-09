![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Alexa Skill Function

Before you begin, make sure to [setup your λ# CLI](../../Runtime/).

## Module Definition

Creating a function that is invoked by an [Alexa Skill](https://developer.amazon.com/alexa-skills-kit) requires two steps. First, an Alexa Skill must be created with an [Amazon Developer account](https://developer.amazon.com/). Second, the function must have the `Alexa` attribute in its `Sources` section.

Optionally, the `Alexa` attribute can specify an Alexa Skill ID to restrict invocation to a specific Alexa Skill.

```yaml
Module: AlexaSample
Description: A sample module using an Alexa skill

Inputs:

  - Parameter: AlexaSkillID
    Description: Alexa Skill ID
    Default: "*"

Functions:

  - Function: MyFunction
    Description: This function is invoked by an Alexa Skill
    Memory: 128
    Timeout: 30
    Sources:
      - Alexa: !Ref AlexaSkillID
```

## Function Code

Alexa Skill requests can be parsed using the excellent [Alexa.NET](https://github.com/timheuer/alexa-skills-dotnet) library by Tim Heuer and by deriving the function from the `ALambdaFunction<T>` base class.

```csharp
public class Function : ALambdaFunction<SkillRequest, SkillResponse> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<SkillResponse> ProcessMessageAsync(SkillRequest skill, ILambdaContext context) {
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
