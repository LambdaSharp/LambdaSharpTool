![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - DynamoDB Stream Source

See [DynamoDB Stream sample](../Samples/DynamoDBSample/) for an example of how to use the DynamoDB Stream source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
DynamoDB: String
BatchSize: Int
StartingPosition: String
```

## Properties

<dl>
<dt><code>DynamoDB</code></dt>
<dd>
The <code>DynamoDB</code> attribute specifies the name of a resource parameter of type <code>AWS::DynamoDB::Table</code> that the Lambda function receives messages from.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>BatchSize</code></dt>
<dd>
The <code>BatchSize</code> attribute specifies the maximum number of messages to receive from the DynamoDB stream. The value must be in the range from 1 to 100.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>StartingPosition</code></dt>
<dd>
The <code>StartingPosition</code> attribute specifies the position in the DynamoDB stream where the Lambda function should start reading. For more information, see <a href="https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_streams_GetShardIterator.html">GetShardIterator</a> in the <i>Amazon DynamoDB API Reference Guide</i>.

<i>Required</i>: No

<i>Type</i>: String

<i>Valid Values</i>: <code>TRIM_HORIZON</code> | <code>LATEST</code>
</dd>
</dl>
