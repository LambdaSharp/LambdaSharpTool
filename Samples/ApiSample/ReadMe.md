![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp API Gateway Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

An API Gateway instance is automatically created for the module when a function has an `Api` attribute in its `Sources` section. The `Api` attribute value is composed of two parts: the HTTP method and the request path. The LambdaSharp CLI creates all required resources for each function using the `AWS_PROXY` integration.

```yaml
Module: Sample.ApiGateway
Description: A sample module integrating with API Gateway
Items:

  - Function: MyFunction
    Description: This function is invoked by API Gateway
    Memory: 128
    Timeout: 30
    Sources:
      - Api: GET:/items
      - Api: POST:/items
      - Api: GET:/items/{id}
      - Api: DELETE:/items/{id}
```

## Function Code

An API Gateway request can be handled using the `ALambdaApiGatewayFunction` base class.

```csharp
public class Function : ALambdaApiGatewayFunction {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<APIGatewayProxyResponse> ProcessProxyRequestAsync(APIGatewayProxyRequest request) {
        LogInfo($"Body = {request.Body}");
        LogDictionary("Headers", request.Headers);
        LogInfo($"HttpMethod = {request.HttpMethod}");
        LogInfo($"IsBase64Encoded = {request.IsBase64Encoded}");
        LogInfo($"Path = {request.Path}");
        LogDictionary("PathParameters", request.PathParameters);
        LogDictionary("QueryStringParameters", request.QueryStringParameters);
        LogInfo($"RequestContext.ResourcePath = {request.RequestContext.ResourcePath}");
        LogInfo($"RequestContext.Stage = {request.RequestContext.Stage}");
        LogInfo($"Resource = {request.Resource}");
        LogDictionary("StageVariables", request.StageVariables);
        return new APIGatewayProxyResponse {
            Body = "Ok",
            Headers = new Dictionary<string, string> {
                ["Content-Type"] = "text/plain"
            },
            StatusCode = 200
        };

        // local function
        void LogDictionary(string prefix, IDictionary<string, string> keyValues) {
            if(keyValues != null) {
                foreach(var keyValue in keyValues) {
                    LogInfo($"{prefix}.{keyValue.Key} = {keyValue.Value}");
                }
            }
        }
    }
}
```
