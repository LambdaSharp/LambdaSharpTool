# API Gateway .NET


> TODO
> * how to wire an api endpoint/route to a method
> * how request bodies are mapped
> * asynchronous methods
> * when to use `APIGatewayProxyRequest` type
> * when to use `APIGatewayProxyResponse` type


## Overview

API Gateway can be configured to automatically deny requests with missing parameters or incorrect payloads before they reach the Lambda function. This capability avoids unnecessary Lambda compute time and comes at no additional cost. Furthermore, the complexity of coordinating the API Gateway endpoint validation with the implementation is fully automated by the λ# tool.

## Reading Parameters from the Request URI

Method parameters can be read from the request URI by REST API endpoints and the WebSocket `$connect` route. Value types, such as `int` and `string`, are resolved by default from the request URI. First, the compiler attempts to find a matching path parameter in the REST API endpoint definition (e.g. `GET:/items/{id}`). If no matching path parameter is found, the compiler expects the parameter to be supplied by the request query string (.e.g `https://example.org/items?id=123`). Since WebSocket routes have no path parameters, all method parameters are expected to be supplied as query string parameters.

REST API path parameters are always required and must occur in the method definition. However, query string parameters can be optional if their type is nullable or a default value is specified. The API Gateway instance will be configured to match the requirements of the method and the REST API endpoint definitions. For WebSockets, only the `$connect` route can use query string parameters during the initial connection attempt. Unfortunately, WebSocket routes cannot be configured to enforce the presence of query string parameters. Therefore, all query string parameters must be defined as optional.

In the following example, the REST API endpoint declaration specifies a single `name` path parameter. Since this parameter is required for the request, it must also appear in the attached invocation method.

```yaml
Sources:
  - Api: GET:/artists/{artist}
    Invoke: GetItems
```

In addition to the required `artist` parameter, the method specifies three optional query string parameters: `filter`, `offset` and `limit`. These additional parameters are optional, because they specify a default value. Alternatively, the `offset` and `limit` parameters could have been declared using a nullable type, such as `int?`.

```csharp
MyResponse GetItems(
    string artist,
    string filter = null,
    int offset = 0,
    int limit = 10
) { ... }
```

Common query string parameters can be captured as a class and easily reused across methods. For example, the following class defines the same query string parameters as in the previous example. The determination if a parameter is required is slightly different, because the λ# tool cannot determine if a property/field initializer is specified. Therefore, it is necessary to use the `JsonProperty` attribute with the `Required` property to specify if a query parameter is required or not.

```csharp
public class FilterOptions {

    //--- Properties ---
    [JsonProperty(PropertyName = "contains", Required = Required.DisallowNull)]
    public string Contains { get; set; }

    [JsonProperty(PropertyName = "offset", Required = Required.DisallowNull)]
    public int Offset { get; set; } = 0;

    [JsonProperty(PropertyName = "limit", Required = Required.DisallowNull)]
    public int Limit { get; set; } = 10;
}
```

The following table summarizes the meaning of the `Required` enumeration, which is not always intuitive.

|Value          |Required |Nullable |
|---------------|---------|---------|
|`Default`      |No       |Yes      |
|`Always`       |Yes      |No       |
|`AllowNull`    |Yes      |Yes      |
|`DisallowNull` |No       |No       |

Finally, the method declaration must use the `FromUri` attribute to indicate the `FilterOptions` parameter should be read from the request URI instead of the request body.

```csharp
public GetItemsResponse GetItems([FromUri] FilterOptions options) { ... }
```


## Reading a Parameter from the Request Body

Only one method parameter can be resolved from the request body. The parameter must have a reference type, unless the `FromBody` attribute is used to force deserialization from the request body.

The following method has two parameters: the `artist` parameter is a value type that is resolved from the request URI and the `album` parameter is a reference type that is resolved from the request body.

```csharp
AddAlbumResponse AddAlbum(
    string artist,
    AddAlbumRequest album
) { ... }
```

The validation of the request body is controlled by the definition of the `AddAlbumRequest` type. The following type definition makes the `Title` property mandatory while keeping `YearPublished` optional. The constraints of the type fields and properties are controlled using the [JsonProperty](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonPropertyAttribute.htm) and [JsonRequired](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonRequiredAttribute.htm) attributes.

```csharp
class AddAlbumRequest {

    //--- Properties ---
    [JsonRequired]
    public string Title { get; set; }

    public int? YearPublished { get; set; }
}
```

The λ# tool uses [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) to create a JSON schema from the type definition. The JSON schema is then attached either to the REST API endpoint or WebSocket route, as appropriate. Note that the request body must always be a JSON value.

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "AddAlbumRequest",
  "type": "object",
  "required": [
    "Title"
  ],
  "properties": {
    "Title": {
      "type": "string"
    },
    "YearPublished": {
      "type": [
        "integer",
        "null"
      ],
      "format": "int32"
    }
  }
}
```



## Response Body

## Asynchronous Invocation






## Parameter Types

### Using `APIGatewayProxyRequest`

### Using No Parameters

### Using a Complex Type

### Using a Simple Type




## Return Type

### Using `APIGatewayProxyResponse`

### Using `void`

### Using a Complex Type

### Using a Simple Type