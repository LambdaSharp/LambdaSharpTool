---
title: SQS Queue Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon SQS Queue event source
keywords: amazon, sqs, queue, event source, declaration, lambda, syntax, yaml, cloudformation
---
# SQS Source

See [SQS Queue sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/SqsSample/) for an example of how to use the SQS Queue source.

## Syntax

```yaml
Sqs: String|Expression
BatchSize: Int|Expression
```

## Properties

<dl>

<dt><code>BatchSize</code></dt>
<dd>

The <code>BatchSize</code> attribute specifies the maximum number of messages to fetch from the SQS queue. The value must be in the range from 1 to 10.

<i>Required</i>: No

<i>Type</i>: Int or Expression
</dd>

<dt><code>Sqs</code></dt>
<dd>

The <code>Sqs</code> attribute specifies the name of a resource parameter of type <code>AWS::SQS::Queue</code> that the Lambda function fetches messages from.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

</dl>
