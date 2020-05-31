---
title: Kinesis Stream Event Source Declaration - Function
description: LambdaSharp YAML syntax for AWS Kinesis stream event source
keywords: kinesis, event source, declaration, lambda, syntax, yaml, cloudformation
---
# Kinesis Stream Source

See [Kinesis Stream sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/KinesisSample/) for an example of how to use the Kinesis Stream source.

## Syntax

```yaml
Kinesis: String|Expression
BatchSize: Int|Expression
StartingPosition: String|Expression
```

## Properties

<dl>

<dt><code>BatchSize</code></dt>
<dd>

The <code>BatchSize</code> attribute specifies the maximum number of messages to receive from the Kinesis stream. The value must be in the range from 1 to 100.

<i>Required</i>: No

<i>Type</i>: Int or Expression
</dd>

<dt><code>Kinesis</code></dt>
<dd>

The <code>Kinesis</code> attribute specifies the name of a resource parameter of type <code>AWS::Kinesis::Stream</code> that the Lambda function receives messages from.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

<dt><code>StartingPosition</code></dt>
<dd>

The <code>StartingPosition</code> attribute specifies the position in the Kinesis stream where the Lambda function should start reading. For more information, see <a href="https://docs.aws.amazon.com/kinesis/latest/APIReference/API_GetShardIterator.html#Kinesis-GetShardIterator-request-ShardIteratorType">GetShardIterator</a> in the <i>Amazon Kinesis API Reference Guide</i>.

<i>Required</i>: No

<i>Type</i>: String or Expression

<i>Valid Values</i>: <code>TRIM_HORIZON</code> | <code>LATEST</code>
</dd>

</dl>
