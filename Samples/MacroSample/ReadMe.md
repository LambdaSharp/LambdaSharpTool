![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp CloudFormation Macro Definition

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by a CloudFormation macro is straightforward. First, define the function in the `Items` section that will be invoked by the macro. Second, create the macro definition using the `Macro` attribute and specify the function as its handler. Note that a single Lambda function can handle multiple CloudFormation macros.

**NOTE:** Support for CloudFormation Macros in LambdaSharp is still experimental.

```yaml
Module: Sample.Macro
Description: A sample module defining CloudFormation macros
Items:

  - Macro: StringToUpper
    Handler: MyFunction

  - Macro: StringToLower
    Handler: MyFunction

  - Function: MyFunction
    Description: This function is invoked by a CloudFormation macros
    Memory: 128
    Timeout: 30
```

## Function Code

The macro invocation can be handled by the `ALambdaFunction<T>` base class.

```csharp
public class Function : ALambdaFunction<MacroRequest, MacroResponse> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<MacroResponse> ProcessMessageAsync(MacroRequest request) {
        LogInfo($"AwsRegion = {request.region}");
        LogInfo($"AccountID = {request.accountId}");
        LogInfo($"Fragment = {SerializeJson(request.fragment)}");
        LogInfo($"TransformID = {request.transformId}");
        LogInfo($"Params = {SerializeJson(request.@params)}");
        LogInfo($"RequestID = {request.requestId}");
        LogInfo($"TemplateParameterValues = {SerializeJson(request.templateParameterValues)}");

        // macro for string operations
        try {
            if(!request.@params.TryGetValue("Value", out object value)) {
                throw new ArgumentException("missing parameter: 'Value");
            }
            if(!(value is string text)) {
                throw new ArgumentException("parameter 'Value' must be a string");
            }
            string result;
            switch(request.transformId) {
            case "StringToUpper":
                result = text.ToUpper();
                break;
            case "StringToLower":
                result = text.ToLower();
                break;
            default:
                throw new NotSupportedException($"requested operation is not supported: '{request.transformId}'");
            }

            // return successful response
            return new MacroResponse {
                requestId = request.requestId,
                status = "SUCCESS",
                fragment = result
            };
        } catch(Exception e) {

            // an error occurred
            return new MacroResponse {
                requestId = request.requestId,
                status = $"ERROR: {e.Message}"
            };
        }
    }
}
```
