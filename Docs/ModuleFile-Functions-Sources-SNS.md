![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - SNS Topic Source

See [SNS Topic sample](../Samples/SnsSample/) for an example of how to use the SNS Topic source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Topic: String
```

## Properties

<dl>
<dt><code>Topic</code></dt>
<dd>
The <code>Topic</code> attribute specifies the name of a resource parameter of type <code>AWS::SNS::Topic</code> that the Lambda function subscribes to.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>

## Examples

```yaml
Sources:
  - Topic: SnsTopic
```
