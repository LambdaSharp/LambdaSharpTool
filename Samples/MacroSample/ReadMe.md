![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CloudFormation Macro Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

Creating a function that is invoked by a CloudFormation macro is straightforward. Simple define a function that lists the CloudFormation Macros it expects to handle in its `Sources` section using the `Macro` attribute. Note that a single Lambda function can handle multiple CloudFormation macros.

```yaml
Name: MacroSample

Description: A sample module defining CloudFormation macros

Functions:

  - Name: MyFunction
    Description: This function is invoked by a CloudFormation macros
    Memory: 128
    Timeout: 30
    Sources:
      - Macro: StringToUpper
      - Macro: StringToLower
```

## Function Code

An SNS topic invocation can be easily handled by the `ALambdaEventFunction<T>` base class. In addition to deserializing the SNS message, the base class also deserializes the contained message body into an instance of the provided type.

```csharp
public class Function : ALambdaFunction<MacroRequest, MacroResponse> {

    //--- Class Methods ---
    private static string SerializeToJson(object value) {
        using(var stream = new MemoryStream()) {
            new JsonSerializer().Serialize(value, stream);
            stream.Position = 0;
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<MacroResponse> ProcessMessageAsync(MacroRequest request, ILambdaContext context) {
        LogInfo($"AwsRegion = {request.region}");
        LogInfo($"AccountID = {request.accountId}");
        LogInfo($"Fragment = {SerializeToJson(request.fragment)}");
        LogInfo($"TransformID = {request.transformId}");
        LogInfo($"Params = {SerializeToJson(request.@params)}");
        LogInfo($"RequestID = {request.requestId}");
        LogInfo($"TemplateParameterValues = {SerializeToJson(request.templateParameterValues)}");

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
