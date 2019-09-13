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
        Invoke: Logic::GetItems

      - Api: POST:/items
        Invoke: Logic::AddItem

      - Api: GET:/items/{id}
        Invoke: Logic::GetItem

      - Api: DELETE:/items/{id}
        Invoke: Logic::DeleteItem

      - Api: POST:/event
        Invoke: Shared::Shared.Logic::CaptureEvent
```

## Application Code

The invoked methods can be kept in a different class in another assembly to separate the application logic from the Lambda framework.

```csharp
public class Logic {

    //--- Methods ---
    public AddItemResponse AddItem(AddItemRequest request) { ... }

    public GetItemsResponse GetItems([FromUri] FilterOptions options) { ... }

    public GetItemResponse GetItem(string id) { ... }

    public DeleteItemResponse DeleteItem(string id) { ... }
}
```

Methods that return `void` or `Task` are automatically invoked asynchronously by API Gateway, reducing response time.

```csharp
public class Logic {

    //--- Methods ---
    public async Task CaptureEvent(CaptureEventRequest request) { ... }
}
```