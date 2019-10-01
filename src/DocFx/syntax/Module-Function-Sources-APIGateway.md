---
title: API Gateway Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon API Gateway event source
keywords: amazon, api gateway, event source, declaration, lambda, syntax, yaml, cloudformation
---
# API Gateway Source

The LambdaSharp CLI uses the [API Gateway Lambda Proxy Integration](https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-lambda-proxy-integrations.html#api-gateway-create-api-as-simple-proxy) to invoke Lambda functions from API Gateway. See [API Gateway sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/ApiSample/) for an example of how to use the API Gateway source.

## Syntax

```yaml
Api: String
ApiKeyRequired: Boolean
AuthorizationType: String
AuthorizationScopes:
  - String
AuthorizerId: String
OperationName: String
Invoke: String
```

## Properties
<dl>

<dt><code>Api</code></dt>
<dd>

The <code>Api</code> attribute specifies the HTTP method and resource path that is mapped to the Lambda function. The notation is <span style="white-space: nowrap"><code>HTTP-METHOD:/resource/{param}</code></span>. The API Gateway instance, the API Gateway resources, and the API Gateway methods are automatically created for the module when an API Gateway source is used.

<b>NOTE</b>: The API Gateway resource can be referenced by its name `Module::RestApi`. Similarly, `Module::RestApi::Stage` and `Module::RestApi::Deployment` reference the API Gateway stage and deployment, respectively.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>ApiKeyRequired</code></dt>
<dd>

The <code>ApiKeyRequired</code> attribute indicates whether the method requires clients to submit a valid API key.

<i>Required</i>: No

<i>Type</i>: Boolean
</dd>

<dt><code>AuthorizationType</code></dt>
<dd>

The <code>AuthorizationType</code> attribute indicates the authorization type for the method. Valid values are <code>NONE</code> for open access, <code>AWS_IAM</code> for using AWS IAM permissions, <code>CUSTOM</code> for using a custom authorizer, or <code>COGNITO_USER_POOLS</code> for using a Cognito user pool.

<i>Required</i>: No (Default: <code>NONE</code>)

<i>Type</i>: String
</dd>

<dt><code>AuthorizationScopes</code></dt>
<dd>

The <code>AuthorizationScopes</code> attribute holds a list of authorization scopes configured on the method. The scopes are used with a <code>COGNITO_USER_POOLS</code> authorizer to authorize the method invocation. The authorization works by matching the method scopes against the scopes parsed from the access token in the incoming request. The method invocation is authorized if any method scopes match a claimed scope in the access token. Otherwise, the invocation is not authorized. When the method scope is configured, the client must provide an access token instead of an identity token for authorization purposes.

<i>Required</i>: No

<i>Type</i>: List of String
</dd>

<dt><code>AuthorizerId</code></dt>
<dd>

The <code>AuthorizerId</code> attribute holds the identifier of the <code>Authorizer</code> resource associated with this method.

<i>Required</i>: Conditional (Required when <code>AuthorizerType</code> is <code>CUSTOM</code> or <code>COGNITO_USER_POOLS</code>)

<i>Type</i>: String
</dd>

<dt><code>Invoke</code></dt>
<dd>

The <code>Invoke</code> attribute holds the name of a C# method to invoke in the Lambda function implementation. The Lambda function implementation must derive from the <code>ALambdaApiGatewayFunction</code> class.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>OperationName</code></dt>
<dd>

The <code>OperationName</code> attribute holds a friendly operation name for the method.

<b>NOTE</b>: this attribute inherits the value of <code>Invoke</code> when not set.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>

## Examples

A LambdaFunction can respond to multiple API Gateway endpoints at once.

```yaml
Sources:

  - Api: GET:/items/{id}

  - Api: POST:/items
    OperationName: AddItem
    ApiKeyRequired: true

  - Api: DELETE:/items/{id}
    ApiKeyRequired: true
    OperationName: RemoveItem
```
