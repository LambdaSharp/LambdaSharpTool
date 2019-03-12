![λ#](../../Docs/LambdaSharpLogo.png)

# LambdaSharp API Gateway Source with Dispatch

Before you begin, make sure to [setup your λ# CLI](../../Docs/ReadMe.md).

## Module Definition

An API Gateway instance is automatically created for the module when a function has an `Api` attribute in its `Sources` section. The `Api` attribute value is composed of two parts: the HTTP method and the request path. In addition, the `Method` attribute specifies the method to invoke in the Lambda function for this resource. The λ# CLI creates all required resources and methods using for each function using `AWS_PROXY` as integration.

```yaml
Module: LambdaSharp.Sample.ApiGatewayDispatch
Description: A sample module integrating with API Gateway
Items:

  - Function: MyFunction
    Description: This function is invoked by API Gateway
    Memory: 128
    Timeout: 30
    Sources:

      - Api: GET:/items
        Method: GetItems

      - Api: POST:/items
        Method: AddItem

      - Api: GET:/items/{id}
        Method: GetItem

      - Api: DELETE:/items/{id}
        Method: DeleteItem
```

## Function Code

API Gateway requests with direct dispatching can be handled using the `ALambdaRestApiFunction` base class.

```csharp
public class Function : ALambdaRestApiFunction {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public AddItemResponse AddItem(AddItemRequest request) { ... }

    public GetItemsResponse GetItems() { ... }

    public GetItemResponse GetItem(string id) { ... }

    public DeleteItemResponse DeleteItem(string id) { ... }
}
```
