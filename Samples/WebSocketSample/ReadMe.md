![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp WebSocket Source with Method Invocation

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

An WebSocket end-point is automatically created for the module when a function has a `WebSocket` attribute in its `Sources` section. The `WebSocket` attribute hold the route key, which corresponds by default to the `action` field of the received message. In addition, the `Invoke` attribute specifies the method to invoke in the Lambda function for this WebSocket route. During compilation, the LambdaSharp CLI creates all required resources for each function and validates that the specified methods exist.

```yaml
Module: Sample.WebSockets
Items:

  - Function: ConnectionFunction
    Memory: 256
    Timeout: 30
    Sources:

      - WebSocket: $connect
        Invoke: OpenConnectionAsync

      - WebSocket: $disconnect
        Invoke: CloseConnectionAsync

  - Function: MessageFunction
    Memory: 256
    Timeout: 30
    Sources:

      - WebSocket: send
        Invoke: SendMessageAsync

      - WebSocket: $default
        Invoke: UnrecognizedRequest
```

## Function Code

API Gateway requests with method invocations are handled by the `ALambdaApiGatewayFunction` base class.

### ConnectionFunction

```csharp
public class Function : ALambdaApiGatewayFunction {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) { ... }

    public async Task OpenConnectionAsync(APIGatewayProxyRequest request) { ... }

    public async Task CloseConnectionAsync(APIGatewayProxyRequest request) { ... }
}
```

### MessageFunction

```csharp
public class Function : ALambdaApiGatewayFunction {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) { ... }

    public async Task SendMessageAsync(Message request) { ... }

    public APIGatewayProxyResponse UnrecognizedRequest(APIGatewayProxyRequest request) { ... }
}
```


## WebSocket Message

The following site allows interactions with the WebSockets end-point using the WebSocket URL.

https://www.websocket.org/echo.html

The WebSocket payload is a JSON document with the following format:
```json
{
    "action": "send",
    "from": "<username>",
    "text": "<message>"
}
```
