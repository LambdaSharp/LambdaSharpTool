![λ#](LambdaSharpLogo.png)

# LambdaSharp Module Function - API Gateway Source

The λ# CLI uses the <a href="https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-lambda-proxy-integrations.html#api-gateway-create-api-as-simple-proxy">API Gateway Lambda Proxy Integration</a> to invoke Lambda functions from API Gateway. See [API Gateway sample](../Samples/ApiSample/) for an example of how to use the API Gateway source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Api: String
OperationName: String
ApiKeyRequired: Boolean
```

## Properties
<dl>

<dt><code>Api</code></dt>
<dd>
The <code>Api</code> attribute specifies the HTTP method and resource path that is mapped to the Lambda function. The notation is <span style="white-space: nowrap">>code>METHOD /resource/subresource/{param}</code></span>. The API Gateway instance, the API Gateway resources, and the API Gateway methods are automatically created for the module when an API Gateway source is used.

<b>NOTE</b>: The API Gateway resource can be referenced by its logical ID `ModuleRestApi`. Similarly, `ModuleRestApiStage` references the API Gateway stage resource.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>ApiKeyRequired</code></dt>
<dd>
The <code>ApiKeyRequired</code> attribute indicates whether the method requires clients to submit a valid API key.

<i>Required</i>: No

<i>Type</i>: Boolean
</dd>

<dt><code>OperationName</code></dt>
<dd>
The <code>OperationName</code> attribute holds a friendly operation name for the method.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>

## Examples

A LambdaFunction can respond to multiple API Gateway endpoints at once.

```yaml
Sources:
  - Api: GET /items/{id}
  - Api: POST /items
    OperationName: AddItem
    ApiKeyRequired: true
  - Api: DELETE /items/{id}
    ApiKeyRequired: true
    OperationName: RemoveItem
```
