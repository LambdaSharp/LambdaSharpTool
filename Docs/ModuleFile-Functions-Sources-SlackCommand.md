![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - Slack Command Source

For Slack commands, the λ# tool deploys an asynchronous API Gateway endpoint that avoids timeout errors due to slow Lambda functions. See [Slack Command sample](../Samples/SlackCommandSample/) for an example of how to use the Slack Command source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
SlackCommand: String
```

## Properties

<dl>
<dt><code>SlackCommand</code></dt>
<dd>
The <code>SlackCommand</code> attribute specifies the resource path that is mapped to the Lambda function. The notation is <span style="white-space: nowrap"><code>/resource/subresource</code></span>. Similarly to the API Gateway source, the API Gateway instance, the API Gateway resources, and the API Gateway methods are automatically created for the module when a Slack Command source is used.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>

## Examples

```yaml
Sources:
  - SlackCommand: /slack
```
