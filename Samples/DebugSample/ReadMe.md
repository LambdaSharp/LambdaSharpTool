![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Debug Logging

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Function Code

The `ALambdaFunction` base class defines the `LogDebug(string format, params object[] arguments)` method, which only logs when `DebugLoggingEnabled` is `true`. For more complex logging, it is recommend to first check `DebugLoggingEnabled` before constructing an expensive log statement that will be discarded.

```csharp
public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) { }

    public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

        // LogDebug() is only emitted to the logs when the DEBUG_LOGGING_ENABLED environment variable is set
        LogDebug("this will only show if DEBUG_LOGGING_ENABLED environment variable is set");

        // to avoid unnecessary overhead, check if debug logging is enabled before constructing debug output
        if(DebugLoggingEnabled) {
            LogDebug("more complex statements should be guarded using the DebugLoggingEnabled property");
        }
        return new FunctionResponse();
    }
}
```
