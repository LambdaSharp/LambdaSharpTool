![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp API Gateway Source with Method Invocation

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

An API Gateway instance is automatically created for the module when a function has an `Api` attribute in its `Sources` section. The `Api` attribute value is composed of two parts: the HTTP method and the request path. In addition, the `Invoke` attribute specifies the method to invoke in the Lambda function for this API entry-point. During compilation, the LambdaSharp CLI creates all required resources for each function and validates that the specified methods exist.

```yaml
Module: Sample.ApiGatewayInvoke
Description: A sample module integrating with API Gateway
Items:

  - Function: MyFunction
    Description: This function is invoked by API Gateway
    Memory: 128
    Timeout: 30
    Sources:

      - Api: GET:/items
        Invoke: GetItems

      - Api: POST:/items
        Invoke: AddItem

      - Api: GET:/items/{id}
        Invoke: GetItem

      - Api: DELETE:/items/{id}
        Invoke: DeleteItem
```

## Function Code

```csharp
public class Function : ALambdaApiGatewayFunction {

    //--- Fields ---
    private List<Item> _items = new List<Item>();

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public AddItemResponse AddItem(AddItemRequest request) { ...  }

    public GetItemsResponse GetItems(string contains = null, int offset = 0, int limit = 10) { ... }

    public GetItemResponse GetItem(string id) { ... }

    public DeleteItemResponse DeleteItem(string id) { ... }
}
```
