![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp API Gateway Function

Before you begin, make sure to [setup your λ# environment](../../Bootstrap/).

## Module File

An API Gateway instance is automatically created for the module when a function has an `Api` attribute in its `Sources` section. The `Api` attribute value is composed of two parts: the HTTP method and the request path. THe λ# tool creates all required resources and methods using for each function using `AWS_PROXY` as integration.

```yaml
Name: ApiSample

Description: A sample module integrating with API Gateway

Functions:
  - Name: MyFunction
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

    public override async Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context) {
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
