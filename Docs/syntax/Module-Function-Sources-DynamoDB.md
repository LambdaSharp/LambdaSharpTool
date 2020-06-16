---
title: DynamoDB Stream Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon DynamoDB stream event source
keywords: amazon, dynamodb, stream, event source, declaration, lambda, syntax, yaml, cloudformation
---
# DynamoDB Stream Source

See [DynamoDB Stream sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/DynamoDBSample/) for an example of how to use the DynamoDB Stream source.

## Syntax

```yaml
DynamoDB: String|Expression
BatchSize: Int|Expression
StartingPosition: String|Expression
```

## Properties

<dl>

<dt><code>BatchSize</code></dt>
<dd>

The <code>BatchSize</code> attribute specifies the maximum number of messages to receive from the DynamoDB stream. The value must be in the range from 1 to 100.

<i>Required</i>: No

<i>Type</i>: Int or Expression
</dd>

<dt><code>DynamoDB</code></dt>
<dd>

The <code>DynamoDB</code> attribute specifies the name of a resource parameter of type <code>AWS::DynamoDB::Table</code> that the Lambda function receives messages from.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

<dt><code>StartingPosition</code></dt>
<dd>

The <code>StartingPosition</code> attribute specifies the position in the DynamoDB stream where the Lambda function should start reading. For more information, see <a href="https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_streams_GetShardIterator.html">GetShardIterator</a> in the <i>Amazon DynamoDB API Reference Guide</i>.

<i>Required</i>: No

<i>Type</i>: String or Expression

<i>Valid Values</i>: <code>TRIM_HORIZON</code> | <code>LATEST</code>
</dd>

</dl>
