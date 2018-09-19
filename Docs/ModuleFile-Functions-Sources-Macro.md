![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - CloudFormation Macro Source

See [CloudFormation Macro sample](../Samples/MacroSample/) for an example of how to define a CloudFormation Macro source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Macro: String
```

## Properties

<dl>
<dt><code>Macro</code></dt>
<dd>
The <code>Macro</code> attribute specifies the CloudFormation Macro name by which this Lambda function can be invoked. The macro name is automatically prefixed with the λ# deployment tier name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>
