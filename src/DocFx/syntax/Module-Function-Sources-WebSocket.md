---
title: WebSocket Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon WebSocket (API Gateway V2) event source
keywords: amazon, api gateway, websocket, event source, declaration, lambda, syntax, yaml, cloudformation
---
# WebSocket Source

The LambdaSharp CLI uses the [API Gateway Lambda Proxy Integration](https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-lambda-proxy-integrations.html#api-gateway-create-api-as-simple-proxy) to invoke Lambda functions from API Gateway. See [Web Socket sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/WebSocketSample/) for an example of how to use the WebSocket source.

## Syntax

```yaml
WebSocket: String
OperationName: String
ApiKeyRequired: Boolean
Invoke: String
```

## Properties
<dl>

<dt><code>Api</code></dt>
<dd>

The <code>WebSocket</code> attribute specifies the WebSocket route that is mapped to the Lambda function. The WebSocket instance and the WebSocket resources are automatically created for the module when an WebSocket source is used.

<b>NOTE</b>: The WebSocket resource can be referenced by its name `Module::WebSocket`. Similarly, `Module::WebSocket::Stage` and `Module::WebSocket::Deployment` reference the WebSocket stage and deployment, respectively.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>ApiKeyRequired</code></dt>
<dd>

The <code>ApiKeyRequired</code> attribute indicates whether the route requires clients to submit a valid API key.

<b>NOTE</b>: this setting only applies to the <code>$connect</code> route.

<i>Required</i>: No

<i>Type</i>: Boolean
</dd>

<dt><code>Invoke</code></dt>
<dd>

The <code>Invoke</code> attribute holds the name of a C# method to invoke in the Lambda function implementation. The Lambda function implementation must derive from the <code>ALambdaApiGatewayFunction</code> class.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>OperationName</code></dt>
<dd>

The <code>OperationName</code> attribute holds a friendly operation name for the route.

<b>NOTE</b>: this attribute inherits the value of <code>Invoke</code> when not set.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>

## Examples

A LambdaFunction can respond to multiple WebSocket routes at once.

```yaml
Sources:

  - WebSocket: $connect
    ApiKeyRequired: true

  - WebSocket: $disconnect

  - WebSocket: addItem
    Invoke: AddItem

  - WebSocket: deleteItem
    Invoke: RemoveItem
```
