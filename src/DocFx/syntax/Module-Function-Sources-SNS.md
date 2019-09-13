---
title: SNS Topic Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon SNS Topic event source
keywords: amazon, sns, topic, event source, declaration, lambda, syntax, yaml, cloudformation
---
# SNS Topic Source

See [SNS Topic sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/SnsSample/) for an example of how to use the SNS Topic source.

## Syntax

```yaml
Topic: String|Expression
Filters: JSON
```

## Properties

<dl>

<dt><code>Filters</code></dt>
<dd>

The <code>Filters</code> section specifies the filter conditions when subscribing to an SNS topic. See <a href="https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html">Amazon SNS Filtering</a> for more details.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Topic</code></dt>
<dd>

The <code>Topic</code> attribute specifies the name of a resource parameter of type <code>AWS::SNS::Topic</code> that the Lambda function subscribes to.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

</dl>

## Examples

### Receive all SNS notifications

```yaml
Sources:
  - Topic: SnsTopic
```

### Receive only the SNS notification that meet the filter condition

```yaml
Sources:
  - Topic: SnsTopic
    Filters:
      source:
        - shopping-cart
```