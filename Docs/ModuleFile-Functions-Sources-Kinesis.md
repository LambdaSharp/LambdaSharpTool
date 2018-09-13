![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - Kinesis Stream Source

See [Kinesis Stream sample](../Samples/KinesisSample/) for an example of how to use the Kinesis Stream source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Kinesis: String
BatchSize: Int
StartingPosition: String
```

## Properties

<dl>
<dt><code>Kinesis</code></dt>
<dd>
The <code>Kinesis</code> attribute specifies the name of a resource parameter of type <code>AWS::Kinesis::Stream</code> that the Lambda function receives messages from.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>BatchSize</code></dt>
<dd>
The <code>BatchSize</code> attribute specifies the maximum number of messages to receive from the Kinesis stream. The value must be in the range from 1 to 100.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>StartingPosition</code></dt>
<dd>
The <code>StartingPosition</code> attribute specifies the position in the Kinesis stream where the Lambda function should start reading. For more information, see <a href="https://docs.aws.amazon.com/kinesis/latest/APIReference/API_GetShardIterator.html#Kinesis-GetShardIterator-request-ShardIteratorType">GetShardIterator</a> in the <i>Amazon Kinesis API Reference Guide</i>.

<i>Required</i>: No

<i>Type</i>: String

<i>Valid Values</i>: <code>TRIM_HORIZON</code> | <code>LATEST</code>
</dd>
</dl>
