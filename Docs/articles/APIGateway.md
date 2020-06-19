---
title: API Gateway for .NET - LambdaSharp
description: Documentation on configuring API Gateway with LambdaSharp
keywords: overview, api, gateway, aws, amazon, configuration, validation, security
---
# API Gateway for .NET

## Overview

API Gateway enables Lambda functions to act as REST API endpoints. API Gateway provides a publicly accessible end-point that can be used by other services to interact with Lambda functions. In addition, API Gateway can be configured to automatically deny requests with missing parameters or incorrect payloads before they reach the Lambda function. This capability avoids unnecessary Lambda compute time and comes at no additional cost. Furthermore, the complexity of coordinating the API Gateway endpoint validation with the implementation is fully automated by the LambdaSharp CLI.

## Reading the Request URI

Method parameters can be read from the request URI by REST API endpoints and the WebSocket `$connect` route. Value types, such as `int` and `string`, are resolved by default from the request URI. First, the compiler attempts to find a matching path parameter in the REST API endpoint definition (e.g. `GET:/items/{id}`). If no matching path parameter is found, the compiler expects the parameter to be supplied by the request query string (.e.g `https://example.org/items?id=123`). Since WebSocket routes have no path parameters, all method parameters are expected to be supplied as query string parameters.

REST API path parameters are always required and must occur in the method definition. However, query string parameters can be optional if their type is nullable or a default value is specified. The API Gateway instance will be configured to match the requirements of the method and the REST API endpoint definitions. For WebSockets, only the `$connect` route can use query string parameters during the initial connection attempt. Unfortunately, WebSocket routes cannot be configured to enforce the presence of query string parameters. Therefore, all query string parameters must be defined as optional.

In the following example, the REST API endpoint declaration specifies a single `name` path parameter. Since this parameter is required for the request, it must also appear in the attached invocation method. The name of the attached method must either be `GetItems` or `GetItemsAsync`, following the C# naming convention for asynchronous methods.

```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 30
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

Common query string parameters can be captured as a class and easily reused across methods. For example, the following class defines the same query string parameters as in the previous example. The determination if a parameter is required is slightly different, because the LambdaSharp CLI cannot determine if a property/field initializer is specified. Therefore, it is necessary to use the `JsonProperty` attribute with the `Required` property to specify if a query parameter is required or not.

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


## Reading the Request Body

Only one method parameter can be resolved from the request body. The parameter must have a reference type, unless the `FromBody` attribute is used to force deserialization from the request body.

The following REST API endpoint method has two parameters: the `artist` parameter is a value type that is resolved from the request URI and the `album` parameter is a reference type that is resolved from the request body.

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

The LambdaSharp CLI uses [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) to derive a JSON schema from the type definition. The JSON schema is then attached either to the REST API endpoint or WebSocket route to enforce it on the request body, which is always a JSON value.

The `AddAlbumRequest` type produces the following JSON schema.

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

The validation of a WebSocket route is almost identical. For example, the following declaration attaches the `JoinRoom` method to the `join` route, which expects a `JoinRoomRequest` payload. The JSON schema derived from the `JoinRoomRequest` type is used to validate the payload before invoking the method.

```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 30
  Sources:
    - WebSocket: join
      Invoke: JoinRoom
```

```csharp
JoinRoomResponse JoinRoom(JoinRoomRequest request) { ... }
```


## Reading the Proxy Request

The [`APIGatewayProxyRequest`](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.APIGatewayEvents/APIGatewayProxyRequest.cs) type represents the entire request payload, including the request body, path parameters, query string parameters, headers, and more. A method can read the proxy request in addition to the request URI and request body. Methods attached to the `$connect`, `$disconnect`, and `$default` WebSocket routes must use the `APIGatewayProxyRequest` type since they don't have a predefined request body.

The `APIGatewayProxyRequest` provides the most flexibility for accessing the parts of a request. However, it also provides no JSON schema to enforce. Thus, the REST API endpoint and WebSocket routes perform no validation and allow any payload to go through.

For example, in the following method definition, no JSON schema information can be inferred for the request body.

```csharp
AddAlbumResponse AddAlbum(APIGatewayProxyRequest request) { ... }
```


## Returning a Response

The method return type is used to determine the JSON schema of the response. If the return type uses the generic `Task<T>` type, then the response schema is based on the generic type parameter `T`. For example, the following method has response schema based on the `AddAlbumResponse` type.

```csharp
Task<AddAlbumResponse> AddAlbum(
    string artist,
    AddAlbumRequest album
) { ... }
```

Methods with a response schema always return HTTP status code 200 (OK) when successful or HTTP status code 500 (Internal Server Error) if an exception occurs. In the latter case, the details of the exception are captured in the Lambda CloudWatch logs and not returned to the client.

A custom status code can be returned either by using the [`APIGatewayProxyResponse`](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.APIGatewayEvents/APIGatewayProxyResponse.cs) return type or by calling one of the `Abort()` methods from [`ALambdaApiGatewayFunction`](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction):
* [`Abort(APIGatewayProxyResponse response)`](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction.Abort(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse)): response with the HTTP status code, headers, and response body set in the `APIGatewayProxyResponse` instance.
* [`AbortBadRequest(string message)`](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction.AbortBadRequest(System.String)): responds with HTTP status code 400 (Bad Request) and the provided message.
* [`AbortForbidden(string message)`](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction.AbortForbidden(System.String)): responds with HTTP status code 403 (Forbidden) and the provided message.
* [`AbortNotFound(string message)`](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction.AbortNotFound(System.String)): responds with HTTP status code 404 (Not Found) and the provided message.

### WebSocket Response

When a WebSocket route returns a response, that response is only sent to the client connection from which the request came. To broadcast a message to all connections, use [`IAmazonApiGatewayManagementApi.PostToConnectionAsync(PostToConnectionRequest)`](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/ApiGatewayManagementApi/MIApiGatewayManagementApiPostToConnectionPostToConnectionRequest.html) instead.

### Asynchronous Invocation by API Gateway

A method can also use `void` or `Task` as return type, in which case the REST API endpoint or WebSocket route is configured to not wait for the Lambda function invocation to complete. Instead, the API Gateway response is always an empty JSON object (i.e. `{}`) with HTTP status code 202 (Accepted) for REST API endpoints and nothing for WebSocket routes. The benefit of using an asynchronous API Gateway invocation is that it never is impacted by a Lambda function cold start. In case the request fails to process, the entire request is captured into the dead-letter queue (DLQ) of the Lambda function, so that it can be retried in the future.

A common use case for an asynchronous invocations are client-side events where a response is not expected or WebSocket routes that broadcast a response to multiple active connections.

```csharp
Task CaptureEvent(APIGatewayProxyRequest request) { ... }
```
