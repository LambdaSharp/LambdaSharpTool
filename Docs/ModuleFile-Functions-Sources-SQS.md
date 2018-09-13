![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - SQS Source

See [SQS Queue sample](../Samples/SqsSample/) for an example of how to use the SQS Queue source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Sqs: String
BatchSize: Int
```

## Properties

<dl>
<dt><code>Sqs</code></dt>
<dd>
The <code>Sqs</code> attribute specifies the name of a resource parameter of type <code>AWS::SQS::Queue</code> that the Lambda function fetches messages from.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>BatchSize</code></dt>
<dd>
The <code>BatchSize</code> attribute specifies the maximum number of messages to fetch from the SQS queue. The value must be in the range from 1 to 10.

<i>Required</i>: No

<i>Type</i>: Int
</dd>
</dl>
